using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
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

            using (StreamWriter outFile = new StreamWriter(datPath + @"\course.txt"))
            {
                SimpleGPSs.OrderBy(s => s.Y).OrderBy(s => s.X).All(s =>
                {
                    outFile.WriteLine("{0},{1}", s.Latitude, s.Longitude);
                    return true;
                });

                outFile.WriteLine(" ==== FULL ==== ");

                SimpleGPSs.OrderBy(s => s.X).OrderBy(s => s.Y).All(s =>
                {
                    outFile.WriteLine("{0},{1}  - ({2},{3})", s.Latitude, s.Longitude, s.X, s.Y);
                    return true;
                });

                outFile.WriteLine(" ==== Coords ==== ");

                SimpleGPSs.OrderBy(s => s.X).OrderBy(s => s.Y).All(s =>
                {
                    outFile.WriteLine("({0},{1})", s.X, s.Y);
                    return true;
                });
            }

            //Build Float Array
            if (SimpleGPSs.Where(s => s.Elevation > 0).Count() > 0)
            {
                var min = SimpleGPSs.Where(s => s.Elevation > 0).Min(s => s.Elevation);
                var max = SimpleGPSs.Where(s => s.Elevation > 0).Max(s => s.Elevation);

                Console.WriteLine("New Range: " + (max - min).ToString());

                int h = int.Parse(txtHeight.Text);
                int w = int.Parse(txtWidth.Text);
                float[,] unityMaps = new float[w, h];
                float[,] heightMap = new float[w, h];

                SimpleGPSs.OrderBy(s => s.X).OrderBy(s => s.Y).All(s =>
                {
                    try
                    {
                        var zeroed = s.Elevation - min;
                        var elevationRatio = zeroed / (max - min);
                        //var elevationRatio = s.Elevation / max;
                        unityMaps[s.X, s.Y] = Convert.ToSingle(elevationRatio);
                        //Console.WriteLine("{0},{1}  - ({2},{3})", s.Latitude, s.Longitude, s.X, s.Y);
                        //Console.WriteLine("({0} - {1})/{2} = {3}", Math.Round(s.Elevation, 4), min, max - min, unityMaps[s.X, s.Y]);
                    }
                    catch (Exception ex)
                    {
                        //unityMaps[s.X, s.Y] = 0;
                        Console.WriteLine(String.Format("Error Setting 2D Array ({0},{1}) = {2}", s.X, s.Y, s.Elevation - min));
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


                Bitmap bmp = new Bitmap(w, h);
                

                SimpleGPSs.OrderBy(s => s.X).OrderBy(s => s.Y).All(s =>
                {
                    var ratio = (unityMaps[s.X, s.Y] > 0) ? unityMaps[s.X, s.Y] : 0;
                    int rgb = Convert.ToInt32(ratio * 255);
                    Color g = Color.FromArgb(255, rgb, rgb, rgb);
                    bmp.SetPixel(s.X, s.Y, g);
                    return true;
                });


                //bmp.SetPixel(0, 0, Color.Red);
                bmp.Save(datPath + @"\test.png");
                Console.WriteLine("Image Generated");
                ByteArrayToFile(datPath + @"\course_unity_terrain.raw", ConvertBitmap(bmp));
                Console.WriteLine("Raw Generated");
            }
        }

        public bool ByteArrayToFile(string _FileName, byte[] _ByteArray)
        {
            try
            {
                // Open file for reading
                System.IO.FileStream _FileStream =
                   new System.IO.FileStream(_FileName, System.IO.FileMode.Create,
                                            System.IO.FileAccess.Write);
                // Writes a block of bytes to this stream using data from
                // a byte array.
                _FileStream.Write(_ByteArray, 0, _ByteArray.Length);

                // close file stream
                _FileStream.Close();

                return true;
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}",
                                  _Exception.ToString());
            }

            // error occured, return false
            return false;
        }

        /// <summary>
        /// Convert a bitmap to a byte array
        /// </summary>
        /// <param name="bitmap">image to convert</param>
        /// <returns>image as bytes</returns>
        private byte[] ConvertBitmap(Bitmap bitmap)
        {
            //Code excerpted from Microsoft Robotics Studio v1.5
            BitmapData raw = null;  //used to get attributes of the image
            byte[] rawImage = null; //the image as a byte[]

            try
            {
                //Freeze the image in memory
                raw = bitmap.LockBits(
                    new Rectangle(0, 0, (int)bitmap.Width, (int)bitmap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb
                );

                int size = raw.Height * raw.Stride;
                rawImage = new byte[size];

                //Copy the image into the byte[]
                System.Runtime.InteropServices.Marshal.Copy(raw.Scan0, rawImage, 0, size);
            }
            finally
            {
                if (raw != null)
                {
                    //Unfreeze the memory for the image
                    bitmap.UnlockBits(raw);
                }
            }
            return rawImage;
        }


        private bool CreateGpsSkeleton(out List<SimpleGPS> gps)
        {
            gps = new List<SimpleGPS>();
            try {
                int width = int.Parse(txtWidth.Text);
                int height = int.Parse(txtHeight.Text);


                var row = 0;
                var col = 0;

                //Create Matrix
                for(var c = 0; c < width; c++)
                {
                    for (var r = 0; r < height; r++)
                    {
                        gps.Add(new SimpleGPS
                        {
                            Latitude = 0,
                            Longitude = 0,
                            Elevation = -1,
                            X = c,
                            Y = r
                        });
                    }
                }

               
                double pLat = double.Parse(txtLat.Text);
                double pLng = double.Parse(txtLong.Text);

                //Fill all primary Coloumns
                gps.Where(g => g.Y == 0).OrderBy(g => g.X).All(g =>
                {
                    if(g.X == 0)
                    {
                        g.Latitude = pLat;
                        g.Longitude = pLng;
                    }else
                    {
                        var tmpGPS = getNext(pLat, pLng, g.X, g.Y, 90, 1);
                        g.Latitude = pLat;
                        pLng = g.Longitude = tmpGPS.Longitude;
                    }
                    return true;
                });

                var tmp = new List<SimpleGPS>(gps.ToArray());
                //pLat = double.Parse(txtLat.Text);
                //pLng = double.Parse(txtLong.Text);
                //Fill In Rest

                for (var r = 0; r < width; r++)
                {

                    var origin = tmp.Where(g => g.Y == 0 && g.X == r).FirstOrDefault();
                    pLat = origin.Latitude;
                    pLng = origin.Longitude;
                    //Console.WriteLine("Origin - R:{0} - ({1},{2}) - {3},{4}", r, origin.X, origin.Y, origin.Latitude, origin.Longitude);

                    gps.Where(g => g.X == r && g.Y > 0).All(g =>
                    {
                       // Console.WriteLine("G1 - R:{0} - ({1},{2}) - {3},{4}", r, g.X, g.Y, g.Latitude, g.Longitude);
                        var tmpGPS = getNext(pLat, pLng, g.X, g.Y, 0, 1);
                        pLat = g.Latitude = tmpGPS.Latitude;
                        g.Longitude = pLng;
                        //Console.WriteLine("G2 - R:{0} - ({1},{2}) - {3},{4}", r, g.X, g.Y, g.Latitude, g.Longitude);
                        return true;
                    });
                   
                }


                Console.WriteLine("GPS Skeleton Completed");

                return true;
            }catch(Exception ex)
            {
                Console.WriteLine("Ex:" + ex.Message);
                return false;
            }
        }

        private SimpleGPS getNext(double Latitude, double Longitude, int col, int row, int brng, int distance)
        {
            
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
                Latitude = RadiansToDegrees(endLatRads),
                Longitude = RadiansToDegrees(endLonRads),
                Elevation = -1,
                X = col,
                Y = row
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

        private Thread UpdateElevationThread = null;
        private void button2_Click(object sender, EventArgs e)
        {
            if (SimpleGPSs.Where(s => s.Elevation == -1).Count() > 0)
            {
                this.UpdateElevationThread = new Thread(new ThreadStart(this.UpdateElevations));
                this.UpdateElevationThread.Start();
            }
            else
            {
                SaveFile();
            }
        }

        public void UpdateElevations()
        {
            if (!chkStopUpdate.Checked)
            {
                int TakeAmt = 50;
                ElevationRequest eReq = new ElevationRequest()
                {
                    Locations = SimpleGPSs.Where(s => s.Elevation == -1).OrderBy(s => s.X).OrderBy(s => s.Y).Take(TakeAmt).Select(s => s.ToLocation()).ToList<Location>()
                };

                var result = GoogleMaps.Elevation.Query(eReq);

                if (result.Status == GoogleMapsApi.Entities.Elevation.Response.Status.OVER_QUERY_LIMIT)
                {
                    Console.WriteLine("Elevation Request Query Limit Exceeded");
                    SaveFile();
                }
                else
                {
                    Populate(result.Results);
                   
                    
                    if (SimpleGPSs.Where(s => s.Elevation == -1).Count() > 0)
                    {
                        //Console.WriteLine(SimpleGPSs.Where(s => s.Elevation == -1).Count() + " OF " + SimpleGPSs.Count() + " have been mapped.");
                        UpdateElevations();
                    }
                    else
                    {
                        this.ManageStopFlag("Update Completed...");
                    }
                }
            }else
            {
                this.ManageStopFlag("User Stopped Updating - Begin Save()...");
            }
        }

        private void ManageStopFlag(string msg)
        {
                //this.chkStopUpdate.Checked = false;
                Console.WriteLine(msg);
                SaveFile();
                Console.WriteLine("Stopped Updating - Save Completed...");
            
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

        private double _lat;
        private double _lng;
        private double _ele;


        public double Latitude { get { return _lat; } set { _lat = value; } }// Math.Round(value, 15); } }
        public double Longitude { get { return _lng; } set { _lng = value; } }// Math.Round(value, 15); } }
        public double Elevation { get { return _ele; } set { _ele = value; } }// Math.Round(value, 3); } }


        public int X { get; set; } 
        public int Y { get; set; }

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
