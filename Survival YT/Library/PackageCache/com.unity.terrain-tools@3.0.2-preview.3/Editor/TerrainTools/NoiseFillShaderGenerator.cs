using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    internal class NoiseFillShaderGenerator : NoiseShaderGenerator<NoiseFillShaderGenerator>
    {
        private static ShaderGeneratorDescriptor m_desc = new ShaderGeneratorDescriptor()
        {
            name = "NoiseFill",
            shaderCategory = "Hidden/TerrainTools/NoiseFill",
            outputDir = "Packages/com.unity.terrain-tools/Shaders/Generated",
            templatePath = "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Templates/Fill.noisehlsltemplate"
        };

        public override ShaderGeneratorDescriptor GetDescription() => m_desc;
    }
}