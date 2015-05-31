using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GolfGPS.Scripts
{
    public class GolfTerrain : MonoBehaviour
    {
        private Terrain _ter;
        private TerrainData _td;

        public void Awake()
        {
            _ter = GetComponent<Terrain>();
            _td = _ter.terrainData;
            _td.size = new Vector3(100, 1, 400);
        }
        
    }
}
