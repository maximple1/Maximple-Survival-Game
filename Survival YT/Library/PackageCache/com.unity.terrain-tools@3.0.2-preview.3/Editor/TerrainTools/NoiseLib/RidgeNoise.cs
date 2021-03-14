using System.Collections.Generic;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// A NoiseType implementation for Ridge noise
    /// </summary>
    [System.Serializable]
    public class RidgeNoise : NoiseType<RidgeNoise>
    {
        private static NoiseTypeDescriptor desc = new NoiseTypeDescriptor()
        {
            name = "Ridge",
            outputDir = "Packages/com.unity.terrain-tools/Shaders/NoiseLib",
            sourcePath = "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Implementation/RidgeImpl.hlsl",
            supportedDimensions = NoiseDimensionFlags._1D | NoiseDimensionFlags._2D | NoiseDimensionFlags._3D,
            inputStructDefinition = null
        };

        public override NoiseTypeDescriptor GetDescription() => desc;
    }
}