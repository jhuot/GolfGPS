using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoogleMapsApi;
using GoogleMapsApi.Entities.Common;
using GoogleMapsApi.Entities.Elevation.Request;
using GoogleMapsApi.Entities.Elevation.Response;
using Newtonsoft.Json;

namespace HoleMapper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private double R = 6371000;
        public List<SimpleGPS> SimpleGPSs = new List<SimpleGPS>();
        
        private void btnCalculate_Click(object sender, EventArgs e)
        {

            if (CreateGpsSkeleton(out SimpleGPSs))
            {
                SaveFile();
            }
            
        }

        private void SaveFile()
        {
            //Stream str;
            //SaveFileDialog sfd = new SaveFileDialog();

            //sfd.Filter = ".dat files (*.dat)|*.dat|All files (*.*)|*.*";
            //sfd.FilterIndex = 2;
            //sfd.RestoreDirectory = true;

            var datPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (StreamWriter outFile = new StreamWriter(datPath + @"\course.dat"))
            {
                outFile.Write(JsonConvert.SerializeObject(SimpleGPSs));
            }

            //Build Float Array
            var min = SimpleGPSs.Where(s => s.Elevation > 0).Min(s => s.Elevation);
            var max = SimpleGPSs.Where(s => s.Elevation > 0).Max(s => s.Elevation);

            Console.WriteLine("New Range: " + (max - min).ToString());

            int h = int.Parse(txtWidth.Text);
            int w = int.Parse(txtHeight.Text);
            float[,] unityMaps = new float[w+1,h+1];
            SimpleGPSs.All(s =>
            {
                try {
                    var zeroed = s.Elevation - min;
                    var elevationRatio = zeroed / max;
                    unityMaps[s.X, s.Y] = Convert.ToSingle(elevationRatio);
                }catch(Exception ex)
                {
                    unityMaps[s.X, s.Y] = 0;
                }
                return true;
            });

            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = File.Open(datPath + @"\course_unity_binary.dat", FileMode.OpenOrCreate);
            bf.Serialize(fs, unityMaps);
            fs.Close();

            using (StreamWriter outFile = new StreamWriter(datPath + @"\course_unity.dat"))
            {
                outFile.Write(JsonConvert.SerializeObject(unityMaps));
            }

        }


        private bool CreateGpsSkeleton(out List<SimpleGPS> gps)
        {
            gps = new List<SimpleGPS>();
            try {
                int width = int.Parse(txtWidth.Text);
                int height = int.Parse(txtHeight.Text);


                var row = 0;
                var col = 0;

                SimpleGPS lastGPS = null;

                for (col = 0; col < width; ++col)
                {
                    if (col == 0)
                    {
                        lastGPS = new SimpleGPS
                        {
                            Latitude = double.Parse(txtLat.Text),
                            Longitude = double.Parse(txtLong.Text),
                            Elevation = -1,
                            X = col,
                            Y = row,
                            Z = -1
                        };
                        SimpleGPSs.Add(lastGPS);
                    }
                    else
                    {
                        lastGPS = getNext(lastGPS, col, row, 90, 1);
                        SimpleGPSs.Add(lastGPS);
                    }
                    for (row = 1; row < height; ++row)
                    {
                        lastGPS = getNext(lastGPS, col, row, 180, 1);
                        SimpleGPSs.Add(lastGPS);
                    }
                }


                ElevationRequest eReq = new ElevationRequest()
                {
                    Locations = SimpleGPSs.Take(10).Select(s => s.ToLocation()).ToList<Location>()
                };

                var result = GoogleMaps.Elevation.Query(eReq);

                if (result.Status == GoogleMapsApi.Entities.Elevation.Response.Status.OVER_QUERY_LIMIT)
                    Console.WriteLine("Elevation Request Query Limit Exceeded");
                else
                    Populate(result.Results);

                return true;
            }catch(Exception ex)
            {
                Console.WriteLine("Ex:" + ex.Message);
                return false;
            }
        }

        private SimpleGPS getNext(SimpleGPS origin, int col, int row, int brng, int distance)
        {
            
            
            //double sinDR = Math.Sin(1 / R);
            //double cosDR = Math.Cos(1 / R);

            /*
                var φ2 = Math.asin( Math.sin(φ1)*Math.cos(d/R) + Math.cos(φ1)*Math.sin(d/R)*Math.cos(brng) );
                var λ2 = λ1 + Math.atan2(Math.sin(brng)*Math.sin(d/R)*Math.cos(φ1),Math.cos(d/R)-Math.sin(φ1)*Math.sin(φ2));
            */


            //double lat2 = Math.Asin(Math.Sin(origin.Latitude) * Math.Cos(distance / R) + Math.Cos(origin.Latitude) * Math.Sin(distance / R) * Math.Cos(brng));
            //double lng2 = origin.Longitude + Math.Atan2(Math.Sin(brng) * Math.Sin(distance / R) * Math.Cos(origin.Latitude), Math.Cos(distance / R) - Math.Sin(origin.Latitude) * Math.Sin(lat2));

           
            
            double distRatio = distance / R;
            var distRatioSine = Math.Sin(distRatio);
            var distRatioCosine = Math.Cos(distRatio);

            var startLatRad = DegreesToRadians(origin.Latitude);
            var startLonRad = DegreesToRadians(origin.Longitude);

            var startLatCos = Math.Cos(startLatRad);
            var startLatSin = Math.Sin(startLatRad);

            var endLatRads = Math.Asin((startLatSin * distRatioCosine) + (startLatCos * distRatioSine * Math.Cos(brng)));

            var endLonRads = startLonRad
                + Math.Atan2(
                    Math.Sin(brng) * distRatioSine * startLatCos,
                    distRatioCosine - startLatSin * Math.Sin(endLatRads));

            return new SimpleGPS
            {
                Latitude = RadiansToDegrees(endLatRads),
                Longitude = RadiansToDegrees(endLonRads),
                Elevation = -1,
                X = row,
                Y = col,
                Z = -1
            };
        }

        public static double DegreesToRadians(double degrees)
        {
            const double degToRadFactor = Math.PI / 180;
            return degrees * degToRadFactor;
        }

        public static double RadiansToDegrees(double radians)
        {
            const double radToDegFactor = 180 / Math.PI;
            return radians * radToDegFactor;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var datPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try {
                using (StreamReader sr = new StreamReader(datPath + @"\course.dat"))
                {
                    var raw = sr.ReadToEnd();
                    SimpleGPSs = JsonConvert.DeserializeObject<List<SimpleGPS>>(raw);
                }

                Console.WriteLine("Total Points: " + SimpleGPSs.Count());
                Console.WriteLine("Total Points without elevations: " + SimpleGPSs.Where(s => s.Elevation == -1).Count());
                Console.WriteLine("MaxHeight: " + SimpleGPSs.Max(s => s.Elevation));

            }catch(Exception ex)
            {
                Console.WriteLine("The file could not be read.");
                Console.WriteLine(ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (SimpleGPSs.Where(s => s.Elevation == -1).Count() > 0)
            {
                UpdateElevations();
            }
            else
            {
                SaveFile();
            }
        }

        public void UpdateElevations()
        {
            ElevationRequest eReq = new ElevationRequest()
            {
                Locations = SimpleGPSs.Where(s => s.Elevation == -1).Take(50).Select(s => s.ToLocation()).ToList<Location>()
            };

            var result = GoogleMaps.Elevation.Query(eReq);

            if (result.Status == GoogleMapsApi.Entities.Elevation.Response.Status.OVER_QUERY_LIMIT)
            {
                Console.WriteLine("Elevation Request Query Limit Exceeded");
                this.Close();
            }
            else
            {
                Populate(result.Results);
                SaveFile();
                Console.WriteLine(SimpleGPSs.Where(s => s.Elevation == -1).Count() + " OF " + SimpleGPSs.Count() + " have been mapped.");

                if (SimpleGPSs.Where(s => s.Elevation == -1).Count() > 0)
                {
                    UpdateElevations();
                }else
                {
                    this.Close();
                }
            }
        }

        private void Populate(IEnumerable<Result> results)
        {
            results.All(r =>
            {
                SimpleGPSs.Where(s => s.ToLocation().LocationString == r.Location.LocationString).FirstOrDefault().Elevation = r.Elevation;
                return true;
            });


        }
        
    }

    public class SimpleGPS
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }


        public int X { get; set; } 
        public int Y { get; set; }
        public int Z { get; set; }

        public string ToLatLong()
        {
            //{lat: -34, lng: 151}
            return "{lat: " + Latitude + ", lng: " + Longitude + "}";
        }

        public Location ToLocation()
        {
            return new Location(Latitude, Longitude);
        }
    }




}
