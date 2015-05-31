using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace GolfGPS.Models
{
    public class Course
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public float Slope { get; set; }
        public float Rating { get; set; }

        public List<Hole> Holes { get; set; }
    }
}
