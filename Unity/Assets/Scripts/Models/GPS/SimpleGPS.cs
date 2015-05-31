using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GolfGPS.Models
{
    public class SimpleGPS
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Height { get; set; }

        public static double ToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
    }
}
