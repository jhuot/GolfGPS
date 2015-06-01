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
                outFile.Write(JsonConvert.SerializeObject(SimpleGPSs.OrderBy(s => s.Y).OrderBy(s => s.X)));
            }

            //Build Float Array
            if (SimpleGPSs.Where(s => s.Elevation > 0).Count() > 0)
            {
                var min = SimpleGPSs.Where(s => s.Elevation > 0).Min(s => Math.Round(s.Elevation, 4));
                var max = SimpleGPSs.Where(s => s.Elevation > 0).Max(s => Math.Round(s.Elevation, 4));

                Console.WriteLine("New Range: " + (max - min).ToString());

                int h = int.Parse(txtHeight.Text);
                int w = int.Parse(txtWidth.Text);
                float[,] unityMaps = new float[w, h];
                float[,] heightMap = new float[w, h];

                SimpleGPSs.All(s =>
                {
                    try
                    {
                        var zeroed = Math.Round(s.Elevation, 4) - min;
                        var elevationRatio = zeroed / (max - min);
                        unityMaps[s.X, s.Y] = Convert.ToSingle(Math.Round(elevationRatio, 4));
                        //Console.WriteLine("({0} - {1})/{2} = {3}", Math.Round(s.Elevation, 4), min, max - min, unityMaps[s.X, s.Y]);
                    }
                    catch (Exception ex)
                    {
                        //unityMaps[s.X, s.Y] = 0;
                        Console.WriteLine(String.Format("Error Setting 2D Array ({0},{1}) = {2}", s.X, s.Y, Math.Round(s.Elevation, 4) - min));
                        //Console.WriteLine(ex.Message);
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

                // Create Image
                if (SimpleGPSs.Where(s => s.Elevation < 0).Count() <= 0)
                {
                    Bitmap bmp = new Bitmap(w, h);
                    var i = 0;
                    Color g = Color.Red;
                    for (var c = 0; c < w; c++)
                    {
                        for (var r = 0; r < h; r++)
                        {
                            i += 1;
                            int rgb = Convert.ToInt32(unityMaps[c, r] * 255);
                            g = Color.FromArgb(255, rgb, rgb, rgb);
                            bmp.SetPixel(c, r, g);
                            //Console.WriteLine("{0},{1} - {2}", c, r, g);
                        }
                    }

                    //bmp.SetPixel(0, 0, Color.Red);
                    bmp.Save(datPath + @"\test.png");
                    Console.WriteLine("Image Generated");
                }
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

               

                double pLat = Math.Round(double.Parse(txtLat.Text), 6);
                double pLng = Math.Round(double.Parse(txtLong.Text), 6);
                int cBrng = 90;
                for (col = 0; col < width; col++)
                {
                    cBrng = 90;
                    for (row = 0; row < height; row++)
                    {
                        if (row == 0 && col == 0)
                        {
                            gps.Add(new SimpleGPS
                            {
                                Latitude = pLat,
                                Longitude = pLng,
                                Elevation = -1,
                                X = col,
                                Y = row,
                                Z = -1
                            });
                        }
                        else {
                            SimpleGPS lastGPS = getNext(pLat, pLng, col, row, cBrng, 1);
                            pLat = lastGPS.Latitude;
                            pLng = lastGPS.Longitude;
                            gps.Add(lastGPS);
                        }
                        cBrng = 0;
                    }
                }

               

                Console.WriteLine("GPS Skeleton Completed");


                //ElevationRequest eReq = new ElevationRequest()
                //{
                //    Locations = SimpleGPSs.Take(10).Select(s => s.ToLocation()).ToList<Location>()
                //};

                //var result = GoogleMaps.Elevation.Query(eReq);

                //if (result.Status == GoogleMapsApi.Entities.Elevation.Response.Status.OVER_QUERY_LIMIT)
                //    Console.WriteLine("Elevation Request Query Limit Exceeded");
                //else
                //    Populate(result.Results);

                return true;
            }catch(Exception ex)
            {
                Console.WriteLine("Ex:" + ex.Message);
                return false;
            }
        }

        private SimpleGPS getNext(double Latitude, double Longitude, int col, int row, int brng, int distance)
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

            var startLatRad = DegreesToRadians(Latitude);
            var startLonRad = DegreesToRadians(Longitude);

            var startLatCos = Math.Cos(startLatRad);
            var startLatSin = Math.Sin(startLatRad);

            var endLatRads = Math.Asin((startLatSin * distRatioCosine) + (startLatCos * distRatioSine * Math.Cos(brng)));

            var endLonRads = startLonRad
                + Math.Atan2(
                    Math.Sin(brng) * distRatioSine * startLatCos,
                    distRatioCosine - startLatSin * Math.Sin(endLatRads));

            return new SimpleGPS
            {
                Latitude = Math.Round(RadiansToDegrees(endLatRads),6),
                Longitude = Math.Round(RadiansToDegrees(endLonRads),6),
                Elevation = -1,
                X = col,
                Y = row,
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

                txtWidth.Text = (SimpleGPSs.Max(s => s.X) + 1).ToString();
                txtHeight.Text = (SimpleGPSs.Max(s => s.Y) + 1).ToString();

                txtLat.Text = SimpleGPSs.First().Latitude.ToString();
                txtLong.Text = SimpleGPSs.First().Longitude.ToString();

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
                SimpleGPSs.Where(s => s.ToLocation().LocationString == r.Location.LocationString).FirstOrDefault().Elevation = Math.Round(r.Elevation,4);
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
