using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Utility struct for creating all the different shader snippets that are
    /// used when generating the shaders for the various NoiseTypes and FractalTypes
    /// </summary>
    internal struct GeneratedShaderInfo
    {
        public FractalTypeDescriptor fractalDesc;
        public NoiseTypeDescriptor noiseDesc;
        public string generatedIncludePath { get; private set; }
        public string additionalIncludePaths { get; private set; }
        public string noiseIncludeStr { get; private set; }
        public string variantName { get; private set; }
        public string outputDir { get; private set; }
        public string noiseStructName {get; private set;}
        public string noiseStructDef {get; private set;}
        public string fractalStructName {get; private set;}
        public string fractalStructDef {get; private set;}
        public string fractalParamStr {get; private set;}
        public string noiseParamStr {get; private set;}
        public string functionInputStr {get; private set;}
        public string functionParamStr {get; private set;}
        public string getDefaultInputsStr {get; private set;}
        public string getInputsStr {get; private set;}
        public string getNoiseInputStr {get; private set;}
        public string getFractalInputStr {get; private set;}
        public string getDefaultFractalInputStr {get; private set;}
        public string getDefaultNoiseInputStr {get; private set;}
        public string fractalPropertyDefStr {get; private set;}
        public int numFractalInputs {get; private set;}
        public int numNoiseInputs {get; private set;}

        public GeneratedShaderInfo(IFractalType fractalType, INoiseType noiseType)
        {
            this.fractalDesc = fractalType.GetDescription();
            this.noiseDesc = noiseType.GetDescription();

            this.noiseIncludeStr = string.Format("#include \"{0}\"", noiseDesc.sourcePath);
            
            if(!string.IsNullOrEmpty(fractalDesc.name))
            {
                this.variantName = string.Format("{0}{1}", fractalDesc.name, noiseDesc.name);
            }
            else
            {
                this.variantName = noiseDesc.name;
            }

            // set the path of the generated file. this will be used when writing the file
            // to disk and when adding the include in any generated shaders that use this
            // fractal and noise type variant
            this.generatedIncludePath = string.Format("{0}/{1}/{2}.hlsl", noiseDesc.outputDir,
                                                                            fractalDesc.name,
                                                                            noiseDesc.name);
            this.outputDir = string.Format("{0}/{1}", noiseDesc.outputDir, fractalDesc.name);

            fractalStructName = string.Format("{0}FractalInput", fractalDesc.name);
            noiseStructName = string.Format("{0}NoiseInput", noiseDesc.name);
            numFractalInputs = fractalDesc.inputStructDefinition == null ? 0 : fractalDesc.inputStructDefinition.Count;
            numNoiseInputs = noiseDesc.inputStructDefinition == null ? 0 : noiseDesc.inputStructDefinition.Count;
            fractalParamStr = null;
            noiseParamStr = null;
            functionInputStr = "";
            
            // construct include paths string
            additionalIncludePaths = "\n";

            for(int i = 0; i < fractalDesc.additionalIncludePaths.Count; ++i)
            {
                additionalIncludePaths += $"#include \"{ fractalDesc.additionalIncludePaths[ i ] }\"\n";
            }

            additionalIncludePaths += "\n";

            // generate the string for the fractal type structure as it would appear as a parameter
            // in an HLSL function declaration
            if( numFractalInputs > 0)
            {
                fractalParamStr = string.Format("{0} {1}", fractalStructName, "fractalInput");
            }

            // generate the string for the noise type structure as it would appear as a parameter
            // in an HLSL function declaration
            if( numNoiseInputs > 0 )
            {
                noiseParamStr = string.Format("{0} {1}", noiseStructName, "noiseInput");
            }

            // generate the argument string for an HLSL function declaration that would be 
            // using this combination of noise and fractal type structure definitions
            functionParamStr = "";

            if(fractalParamStr != null)
            {
                functionParamStr += fractalParamStr;
                functionInputStr += "fractalInput";
            }
            
            if(fractalParamStr != null && noiseParamStr != null)
            {
                functionParamStr += ", ";
                functionInputStr += ", ";
            }

            if(noiseParamStr != null)
            {
                functionParamStr += noiseParamStr;
                functionInputStr += "noiseInput";
            }

            fractalStructDef = "";

            if(numFractalInputs > 0)
            {
                fractalStructDef = NoiseLib.BuildStructString(fractalStructName, fractalDesc.inputStructDefinition);

                string getDefaultFuncStr = NoiseLib.GetDefaultFunctionString(fractalStructName, fractalDesc.inputStructDefinition);
                fractalStructDef += $"\n\n{ getDefaultFuncStr }\n\n";
            }

            noiseStructDef = "";

            if(numNoiseInputs > 0)
            {
                noiseStructDef = NoiseLib.BuildStructString(noiseStructName, noiseDesc.inputStructDefinition);
            }

            // get input str construction
            getInputsStr = "";
            getFractalInputStr = NoiseLib.GetInputFunctionCallString(fractalStructName);
            getNoiseInputStr = NoiseLib.GetInputFunctionCallString(fractalStructName);
            
            if (numFractalInputs > 0)
            {
                getInputsStr += getFractalInputStr;
            }

            if (numFractalInputs > 0 && numNoiseInputs > 0)
            {
                getInputsStr += ", ";
            }

            if (numNoiseInputs > 0)
            {
                getInputsStr += getNoiseInputStr;
            }
            
            // get default input str construction
            getDefaultInputsStr = "";
            getDefaultFractalInputStr = NoiseLib.GetDefaultInputFunctionCallString(fractalStructName);
            getDefaultNoiseInputStr = NoiseLib.GetDefaultInputFunctionCallString(noiseStructName);

            if(numFractalInputs > 0)
            {
                getDefaultInputsStr += getDefaultFractalInputStr;
            }

            if(numFractalInputs > 0 && numNoiseInputs > 0)
            {
                getDefaultInputsStr += ", ";
            }

            if(numNoiseInputs > 0)
            {
                getDefaultInputsStr += getDefaultNoiseInputStr;
            }

            fractalPropertyDefStr = "";

            if(fractalDesc.inputStructDefinition != null &&
               fractalDesc.inputStructDefinition.Count > 0)
            {
                fractalPropertyDefStr = NoiseLib.GetPropertyDefinitionStr(fractalDesc.name, fractalDesc.inputStructDefinition);
                fractalPropertyDefStr += "\n" + NoiseLib.GetPropertyFunctionStr(fractalStructName, fractalDesc.name, fractalDesc.inputStructDefinition);
            }
        }

        public void ReplaceTags(StringBuilder sb)
        {
            string fractalMacroDef = fractalStructName.ToUpper() + "_DEF";
            string guardedFractalDataDefinitions =
$@"

#ifndef { fractalMacroDef } // [ { fractalMacroDef }
#define { fractalMacroDef }

{fractalStructDef}
{fractalPropertyDefStr}

#endif // ] { fractalMacroDef }

";

            sb.Replace(NoiseLib.Strings.k_tagIncludes, noiseIncludeStr + additionalIncludePaths);       // add the noise include
            sb.Replace(NoiseLib.Strings.k_tagFractalName, fractalDesc.name);                            // add fractal name
            sb.Replace(NoiseLib.Strings.k_tagNoiseName, noiseDesc.name);                                // add noise name
            sb.Replace(NoiseLib.Strings.k_tagVariantName, variantName);                                 // add combined fractal and noise name
            sb.Replace(NoiseLib.Strings.k_tagFractalDataDefinitions, guardedFractalDataDefinitions);
            sb.Replace(NoiseLib.Strings.k_tagFunctionParams, functionParamStr);
            sb.Replace(NoiseLib.Strings.k_tagFunctionInputs, functionInputStr);
            sb.Replace(NoiseLib.Strings.k_tagGetDefaultFractalInput, getDefaultFractalInputStr);
            sb.Replace(NoiseLib.Strings.k_tagGetDefaultNoiseInput, getDefaultNoiseInputStr);
            sb.Replace(NoiseLib.Strings.k_tagGetDefaultInputs, getDefaultInputsStr);
            sb.Replace(NoiseLib.Strings.k_tagGetInputs, getInputsStr);
        }
    }
}