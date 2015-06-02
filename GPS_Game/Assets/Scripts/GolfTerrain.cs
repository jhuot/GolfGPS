using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
			_td.size = new Vector3(100f,  118.332f, 400f);
        }

        public void Start()
        {
			_td.SetHeights(0, 118, GetHeightsFromFile());
        }

        public float[,] GetHeightsFromFile()
        {
            string mapBinData = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\course_unity_binary.dat";
            if (File.Exists(mapBinData))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = File.Open(mapBinData, FileMode.Open);
                float[,] heights = (float[,])bf.Deserialize(fs);
                fs.Close();
                return heights;
            }
            return new float[400, 100];
        }

    }
}
