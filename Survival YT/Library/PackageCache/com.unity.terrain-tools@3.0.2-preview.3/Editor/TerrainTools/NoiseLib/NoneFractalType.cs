using System.Collections.Generic;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// A FractalType implementation for a fractal that does nothing. This will give you raw
    /// noise values from the "first" fractal (from Fractal Brownian Motion, for instance) when used
    /// </summary>
    public class NoneFractalType : FractalType<NoneFractalType>
    {
        public override FractalTypeDescriptor GetDescription() => new FractalTypeDescriptor()
        {
            name = "None",
            templatePath = "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Templates/FractalNone.noisehlsltemplate",
            supportedDimensions = NoiseDimensionFlags._1D | NoiseDimensionFlags._2D | NoiseDimensionFlags._3D,
            inputStructDefinition = null,
            additionalIncludePaths = new List<string>()
            {
                "Packages/com.unity.terrain-tools/Shaders/NoiseLib/NoiseCommon.hlsl"
            }
        };
    }
}