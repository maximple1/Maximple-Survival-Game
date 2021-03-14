using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEngine.Experimental.TerrainAPI
{
    public class NoiseComponent : MonoBehaviour
    {
        public Material mat;
        public NoiseSettings noiseSettings;

        void Update()
        {
            if(mat != null)
            {
                noiseSettings.SetupMaterial( mat );
            }
        }
    }
}