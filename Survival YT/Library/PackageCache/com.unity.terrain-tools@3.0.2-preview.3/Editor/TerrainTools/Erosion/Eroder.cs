using UnityEngine;
using System.Collections.Generic;

namespace Erosion {
    public delegate void ResetTool();
    
    public interface ITerrainEroder {
        void OnEnable();

        void ErodeHeightmap(Vector3 terrainDimensions, Rect domainRect, Vector2 texelSize, bool invertEffect = false);

        Dictionary<string, RenderTexture> inputTextures { get; set; }
        Dictionary<string, RenderTexture> outputTextures { get; }

        
    }
}
