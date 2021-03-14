using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Linq;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Class responsible for loading all the NoiseType and FractalType implementations
    /// and generating the associated shaders
    /// </summary>
    [InitializeOnLoad]
    public static partial class NoiseLib
    {
        private static INoiseType[]     s_noiseTypes;
        private static string[]         s_noiseNames;
        private static IFractalType[]   s_fractalTypes;
        private static string[]         s_fractalNames;

        private static BindingFlags s_bindingFlags =    BindingFlags.Public     |
                                                        BindingFlags.NonPublic  |
                                                        BindingFlags.Static     |
                                                        BindingFlags.Instance   |
                                                        BindingFlags.FlattenHierarchy;

        private static Dictionary<Type, INoiseShaderGenerator>      s_generators;
        // generator type => fractal type => shader
        private static Dictionary<Type, Dictionary<Type, Shader>>   s_generatedShaderMap;
        // generator type => fractal type => material
        private static Dictionary<Type, Dictionary<Type, Material>> s_generatedMaterialMap;

        static NoiseLib()
        {
            GenerateHeaderFiles();
            GenerateShaders();
        }

        /*============================================================================================
        
            UI Helpers
        
        ============================================================================================*/
        
        /// <summary>
        /// Renders a Popup using EditorGUILayout.Popup for all loaded NoiseType implementations
        /// </summary>
        /// <param name="label"> Label prefix for the Popup </param>
        /// <param name="selectedName"> The currently selected NoiseType name </param>
        public static string NoiseTypePopup(GUIContent label, string selectedName)
        {
            string[] names = NoiseLib.GetNoiseNames();
            int index = NoiseLib.GetNoiseIndex(selectedName);
            index = index < 0 ? 0 : index;

            int newIndex = EditorGUILayout.Popup(label, index, names);
            string newName = names[newIndex];

            if (newName.CompareTo(selectedName) != 0)
            {
                selectedName = newName;
            }

            return selectedName;
        }

        /// <summary>
        /// Renders a Popup using EditorGUILayout.Popup for all loaded FractalType implementations
        /// </summary>
        /// <param name="label"> Label prefix for the Popup </param>
        /// <param name="selectedName"> The currently selected FractalType name </param>
        public static string FractalTypePopup(GUIContent label, string selectedName)
        {
            string[] names = NoiseLib.GetFractalNames();
            int index = NoiseLib.GetFractalIndex(selectedName);
            index = index < 0 ? 0 : index;

            int newIndex = EditorGUILayout.Popup(label, index, names);
            string newName = names[newIndex];

            if (newName.CompareTo(selectedName) != 0)
            {
                selectedName = newName;
            }

            return selectedName;
        }

        /*==========================================================================================
        
            Get Noise
        
        ==========================================================================================*/

        // TODO(wyatt): this needs to be read-only
        private static INoiseType[] GetAllNoiseTypes()
        {
            return s_noiseTypes;
        }

        // TODO(wyatt): this needs to be read-only
        private static string[] GetNoiseNames()
        {
            return s_noiseNames;
        }

        /// <summary>
        /// Returns the Singleton instance for the specified NoiseType
        /// </summary>
        /// <param name="noiseName"> The name of the NoiseType </param>
        public static INoiseType GetNoiseTypeInstance(string noiseName)
        {
            int index = GetNoiseIndex(noiseName);

            return index == -1 ? null : s_noiseTypes[index];
        }

        /// <summary>
        /// Returns the Singleton instance for the specified NoiseType
        /// </summary>
        /// <param name="t"> The Type for the NoiseType implementation </param>
        public static INoiseType GetNoiseTypeInstance(Type t)
        {
            INoiseType[] instances = s_noiseTypes;

            for(int i = 0; i < instances.Length; ++i)
            {
                INoiseType noiseType = instances[i];

                if(noiseType.GetType() == t)
                {
                    return instances[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the global NoiseType index associated with provided NoiseSettings instance
        /// </summary>
        /// <param name="noise"> The NoiseSettings instance </param>
        public static int GetNoiseIndex(NoiseSettings noise)
        {
            return GetNoiseIndex(noise.domainSettings.noiseTypeName);
        }

        /// <summary>
        /// Returns the global index for the specified NoiseType
        /// </summary>
        /// <param name="noiseName"> The name of the NoiseType </param>
        public static int GetNoiseIndex(string noiseName)
        {
            int index = -1;

            INoiseType[] instances = s_noiseTypes;

            for(int i = 0; i < instances.Length; ++i)
            {
                NoiseTypeDescriptor desc = instances[i].GetDescription();

                if(noiseName.CompareTo(desc.name) == 0)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        /*==========================================================================================
        
            Get Fractals
        
        ==========================================================================================*/

        // TODO(wyatt): this needs to be a read-only collection
        private static IFractalType[] GetAllFractalTypes()
        {
            return s_fractalTypes;
        }

        // TODO(wyatt): this needs to be a read-only collection
        private static string[] GetFractalNames()
        {
            return s_fractalNames;
        }

        /// <summary>
        /// Returns the Singleton instance for the specified FractalType implementation
        /// </summary>
        public static IFractalType GetFractalTypeInstance(string fractalName)
        {
            int index = GetFractalIndex(fractalName);

            return index == -1 ? null : s_fractalTypes[ index ];
        }

        /// <summary>
        /// Returns the Singleton instance for the specified FractalType
        /// </summary>
        /// <param name="t"> The Type for the FractalType implementation </param>
        public static IFractalType GetFractalTypeInstance(Type t)
        {
            IFractalType[] instances = s_fractalTypes;

            for(int i = 0; i < instances.Length; ++i)
            {
                IFractalType fractalType = instances[i];

                if(fractalType.GetType() == t)
                {
                    return instances[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the global FractalType index associated with provided NoiseSettings instance
        /// </summary>
        /// <param name="noise"> The NoiseSettings instance </param>
        public static int GetFractalIndex(NoiseSettings noise)
        {
            return GetFractalIndex(noise.domainSettings.fractalTypeName);
        }

        /// <summary>
        /// Returns the global index for the specified FractalType
        /// </summary>
        /// <param name="fractalName"> The name of the FractalType </param>
        public static int GetFractalIndex(string fractalName)
        {
            if(string.IsNullOrEmpty(fractalName))
            {
                return -1;
            }

            IFractalType[] instances = s_fractalTypes;
            string[] fractalNames = GetFractalNames();
            
            for (int i = 0; i < instances.Length && i < fractalNames.Length; ++i)
            {
                if (fractalName.CompareTo(fractalNames[i]) == 0)
                {
                    return i;
                }
            }

            return -1;
        }
        
        /*=========================================================================

            Gather Types
        
        =========================================================================*/

        private static bool IsSubclassOfGenericType(Type t, Type genericType)
        {
            if(t.IsGenericType && t.GetGenericTypeDefinition() == genericType)
            {
                return false;
            }

            for(t = t.BaseType; t != null; t = t.BaseType)
            {
                if(t.IsGenericType && t.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Type> GetSubclassesOfGenericType(Type[] types, Type genericType)
        {
            return types.Where( t => IsSubclassOfGenericType( t, genericType ) );
        }

        private static void GatherNoiseTypes()
        {
            List<INoiseType> instances = new List<INoiseType>();
            List<string> names = new List< string >();

            List<Type> types = new List< Type >();

            foreach( Assembly asm in AppDomain.CurrentDomain.GetAssemblies() )
            {
                Type[] assemblyTypes = null;

                try
                {
                    assemblyTypes = asm.GetTypes();
                }
                catch( Exception )
                {
                    Debug.Log( "NoiseLib::GatherNoiseTypes: Failed to get types from assembly: " + asm );
                    assemblyTypes = null;
                }

                if( assemblyTypes != null )
                {
                    types.AddRange( GetSubclassesOfGenericType( assemblyTypes, typeof( NoiseType<> ) ) );
                }
            }

            foreach (Type t in types)
            {
                PropertyInfo propertyInfo = t.GetProperty("instance", s_bindingFlags);
                MethodInfo methodInfo = propertyInfo.GetGetMethod();
                INoiseType instance = (INoiseType)methodInfo.Invoke(null, null);
                
                NoiseTypeDescriptor desc = instance.GetDescription();

                if(string.IsNullOrEmpty(desc.name))
                {
                    Debug.LogError("NoiseType name cannot be null or empty! Skipping noise type: " + t);
                    continue;
                }

                instances.Add(instance);
                names.Add(desc.name);
            }

            s_noiseTypes = instances.ToArray();
            s_noiseNames = names.ToArray();
        }

        private static void GatherFractalTypes()
        {
            List<IFractalType> instances = new List<IFractalType>();
            List<string> names = new List<string>();

            List<Type> types = new List< Type >();

            foreach( Assembly asm in AppDomain.CurrentDomain.GetAssemblies() )
            {
                Type[] assemblyTypes = null;

                try
                {
                    assemblyTypes = asm.GetTypes();
                }
                catch( Exception )
                {
                    Debug.Log( "NoiseLib::GatherFractalTypes: Failed to get types from assembly: " + asm );
                    assemblyTypes = null;
                }

                if( assemblyTypes != null )
                {
                    types.AddRange( GetSubclassesOfGenericType( assemblyTypes, typeof( FractalType<> ) ) );
                }
            }

            foreach (Type t in types)
            {
                PropertyInfo propertyInfo = t.GetProperty("instance", s_bindingFlags);
                MethodInfo methodInfo = propertyInfo.GetGetMethod();
                IFractalType instance = (IFractalType)methodInfo.Invoke(null, null);
                FractalTypeDescriptor desc = instance.GetDescription();
                
                if(string.IsNullOrEmpty(desc.name))
                {
                    Debug.LogError("FractalType name cannot be null or empty! Skipping fractal type: " + desc.name);
                    continue;
                }
                
                instances.Add(instance);
                names.Add(desc.name);
            }

            s_fractalTypes = instances.ToArray();
            s_fractalNames = names.ToArray();
        }

        /*==========================================================================================

            Load Source
        
        ==========================================================================================*/

        private static string[] LoadNoiseSource(INoiseType[] noiseTypes)
        {
            // load noise source
            List<string> noiseSource = new List<string>();
            foreach (INoiseType noise in noiseTypes)
            {
                NoiseTypeDescriptor desc = noise.GetDescription();
                string path = desc.sourcePath;
                string source = null;

                if (!File.Exists(path))
                {
                    Debug.LogError("NoiseLib: Noise Include File could not be found! Skipping generation of: " + desc.name);
                }
                else
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        source = sr.ReadToEnd();
                    }
                }

                noiseSource.Add(source);
            }

            return noiseSource.ToArray();
        }

        private static string[] LoadFractalSource(IFractalType[] fractalTypes)
        {
            // load fractal templates
            List<string> fractalSource = new List<string>();
            foreach (IFractalType fractal in fractalTypes)
            {
                FractalTypeDescriptor desc = fractal.GetDescription();
                string path = desc.templatePath;
                string source = null;

                if (!File.Exists(path))
                {
                    Debug.LogError("NoiseLib: Fractal Template File at \"" + path + "\" could not be found! Skipping generation of: " + desc.name);
                }
                else
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        source = sr.ReadToEnd();
                    }
                }

                fractalSource.Add(source);
            }

            return fractalSource.ToArray();
        }

        internal static string GetPropertyDefinitionStr(string prefix, List<HlslInput> inputs)
        {
            string ret = "";

            char[] charArray = prefix.ToCharArray();
            charArray[0] = char.ToUpper(charArray[0]);
            prefix = new string(charArray);

            for(int i = 0; i < inputs.Count; ++i)
            {
                string name = inputs[i].name;
                charArray = name.ToCharArray();
                charArray[0] = char.ToUpper(charArray[0]);
                name = new string(charArray);

                ret += string.Format("{0} {1};\n", inputs[i].GetHlslValueTypeString(), string.Format("_{0}{1}", prefix, name));
            }

            return ret;
        }

        internal static string GetStringFromHlslInputList(List<HlslInput> inputList)
        {
            string ret = "";
            int i = 0;

            for(; i < inputList.Count - 1; ++i)
            {
                ret += string.Format("{0}, ", inputList[i]);
            }

            if(i < inputList.Count)
            {
                ret += inputList[inputList.Count - 1];
            }

            return ret;
        }

        internal static string BuildStructString(string structName, List<HlslInput> inputList)
        {
            string structDefStr = "struct " + structName + "\n{\n";

            for(int i = 0; i < inputList.Count; ++i)
            {
                HlslInput input = inputList[i];

                string valueString = input.GetHlslValueTypeString();

                structDefStr += string.Format("\t{0} {1};\n", valueString, input.name); 
            }

            structDefStr += "};\n\n";

            return structDefStr;
        }

        internal static string GetDefaultInputFunctionCallString(string structName)
        {
            return string.Format("GetDefault{0}()", structName);
        }

        internal static string GetInputFunctionCallString(string structName)
        {
            return string.Format("Get{0}()", structName);
        }

        internal static string GetDefaultFunctionString(string structName, List<HlslInput> inputList)
        {
            string getDefaultFunctionString = string.Format("{0} {1}", structName, GetDefaultInputFunctionCallString(structName)) + "\n{\n";

            getDefaultFunctionString += string.Format("\t{0} ret;\n\n", structName);

            for(int i = 0; i < inputList.Count; ++i)
            {
                HlslInput input = inputList[i];
                string defaultValueString = input.GetDefaultValueString();
                getDefaultFunctionString += string.Format("\tret.{0} = {1};\n", input.name, defaultValueString);
            }

            getDefaultFunctionString += "\n\treturn ret;\n}\n\n";

            return getDefaultFunctionString;
        }

        internal static string GetPropertyFunctionCallString(string structName)
        {
            return string.Format("Get{0}()", structName);
        }

        internal static string GetPropertyFunctionStr(string structName, string propertyPrefix, List<HlslInput> inputs)
        {
            string ret = string.Format("{0} {1}", structName, GetPropertyFunctionCallString(structName)) + "\n{\n";

            char[] charArray = propertyPrefix.ToCharArray();
            charArray[0] = char.ToUpper(charArray[0]);
            propertyPrefix = new string(charArray);

            ret += string.Format("\t{0} ret;\n\n", structName);

            for(int i = 0; i < inputs.Count; ++i)
            {
                string name = inputs[i].name;
                charArray = name.ToCharArray();
                charArray[0] = char.ToUpper(charArray[0]);
                name = new string(charArray);

                ret += string.Format("\tret.{0} = {1};\n", inputs[i].name, string.Format("_{0}{1}", propertyPrefix, name));
            }

            ret += "\n\treturn ret;\n}\n\n";

            return ret;
        }

        /*==========================================================================================

            Generate HLSL
            
        ==========================================================================================*/

        /// <summary>
        /// Forces generation of the NoiseType and FractalType variant HLSL header files
        /// </summary>
        public static void GenerateHeaderFiles()
        {
            System.Globalization.CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            GatherNoiseTypes();
            GatherFractalTypes();

            INoiseType[] noiseTypes = s_noiseTypes;
            IFractalType[] fractalTypes = s_fractalTypes;

            string[] fractalContents = LoadFractalSource(fractalTypes);
            string[] noiseContents = LoadNoiseSource(noiseTypes);

            for(int f = 0; f < fractalTypes.Length; ++f)
            {
                string fractalStr = fractalContents[f];
                
                // dont generate for this fractal type if the source could not be found
                if(fractalStr == null)
                {
                    continue;
                }

                IFractalType fractal = fractalTypes[f];

                for(int n = 0; n < noiseTypes.Length; ++n)
                {
                    string noiseStr = noiseContents[n];

                    // dont generate for this noise type if the source could not be found
                    if(noiseStr == null)
                    {
                        continue;
                    }

                    INoiseType noise = noiseTypes[n];
                    GeneratedShaderInfo info = new GeneratedShaderInfo(fractal, noise);

                    StringBuilder sb = new StringBuilder();

                    sb.Append(Strings.k_warningHeader);                             // add the DO NOT EDIT warning
                    sb.Append(fractalStr);                                          // add the fractal template
                    
                    info.ReplaceTags(sb);
                    
                    string newContents = sb.ToString();

                    // do some code cleanup
                    newContents = Regex.Replace(newContents, Strings.k_regexDupCommas, ", ");
                    newContents = Regex.Replace(newContents, Strings.k_emptyArgsRight, " )");
                    newContents = Regex.Replace(newContents, Strings.k_emptyArgsLeft, "( ");

                    newContents = NormalizeLineEndings(newContents);

                    string outputDir = info.outputDir;

                    // TODO(wyatt): need to verify this is actually a directory and not a file
                    if(!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    string oldContents = null;

                    FileInfo fi = new FileInfo( info.generatedIncludePath );

                    if( File.Exists( info.generatedIncludePath ) )
                    {
                        using( StreamReader sr = new StreamReader( info.generatedIncludePath ) )
                        {
                            oldContents = sr.ReadToEnd();
                            oldContents = NormalizeLineEndings( oldContents );
                        }
                    }

                    if( !fi.IsReadOnly )
                    {
                        if(oldContents == null || newContents.CompareTo( oldContents ) != 0)
                        {
                            try
                            {
                                using( StreamWriter sw = new StreamWriter( info.generatedIncludePath ) )
                                {
                                    sw.Write( newContents );
                                }
                            }
                            catch( Exception )
                            {
                                // restore previous cultureinfo
                                System.Threading.Thread.CurrentThread.CurrentCulture = prevCultureInfo;
                            }
                        }
                    }
                }
            }
            
            // restore previous cultureinfo
            System.Threading.Thread.CurrentThread.CurrentCulture = prevCultureInfo;

            // UnityEditor.AssetDatabase.Refresh();
        }

        /*==========================================================================================

            Generate Tool Shaders
        
        ==========================================================================================*/

        /// <summary>
        /// Returns a Material associated with the provided Type of NoiseShaderGenerator
        /// and Type of FractalType
        /// </summary>
        /// <param name="generatorType"> The Type of a NoiseShaderGenerator </param>
        /// <param name="fractalType"> The Type of a FractalType </param>
        public static Material GetGeneratedMaterial(Type generatorType, Type fractalType)
        {
            LoadShadersAndCreateMaterials();

            if(s_generatedMaterialMap.ContainsKey(generatorType))
            {
                if(s_generatedMaterialMap[generatorType].ContainsKey(fractalType))
                {
                    return s_generatedMaterialMap[generatorType][fractalType];
                }
            }

            return null;
        }

        private static string GetShaderName(ShaderGeneratorDescriptor generatorDesc, FractalTypeDescriptor fractalDesc)
        {
            string shaderStr = string.Format("{0}/{1}{2}", generatorDesc.shaderCategory,
                                                           generatorDesc.name,
                                                           fractalDesc.name);

            return shaderStr;
        }

        private static void GatherGenerators()
        {
            var gatheredAsmTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(
                asm =>
                {
                    Type[] asmTypes = null;
                    List< Type > types = null;

                    try
                    {
                        asmTypes = asm.GetTypes();
                        if( asmTypes != null )
                        {
                            types = new List< Type >( GetSubclassesOfGenericType( asmTypes, typeof( NoiseShaderGenerator<> ) ) );
                        }
                    }
                    catch( Exception )
                    {
                        asmTypes = null;
                        types = null;
                    }

                    return types == null ? new List< Type >() : types;
                }
            );
            // List<Type> generatorTypes = new List<Type>(
            //     AppDomain.CurrentDomain.GetAssemblies().SelectMany(
            //         asm => GetSubclassesOfGenericType( asm.GetTypes(), typeof(NoiseShaderGenerator<>) )
            //     )
            // );
            List<Type> generatorTypes = new List<Type>( gatheredAsmTypes );

            s_generators = new Dictionary<Type, INoiseShaderGenerator>();
            
            foreach(Type t in generatorTypes)
            {
                PropertyInfo propertyInfo = t.GetProperty("instance", s_bindingFlags);
                MethodInfo methodInfo = propertyInfo.GetGetMethod();
                INoiseShaderGenerator generator = (INoiseShaderGenerator)methodInfo.Invoke(null, null);
                s_generators.Add(t, generator);
            }
        }

        private static string NormalizeLineEndings(string str)
        {
            return str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }

        /// <summary>
        /// Forces the generation of any shaders that make use of generated noise header files. Gathers all
        /// the NoiseShaderGenerators and generates shaders based on the ".noisehlsltemplate" file
        /// provided by that particular NoiseShaderGenerator implementation
        /// </summary>
        public static void GenerateShaders()
        {
            System.Globalization.CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            GatherGenerators();

            IFractalType[] fractalTypes = s_fractalTypes;
            INoiseType[] noiseTypes = s_noiseTypes;
            Dictionary<Type, INoiseShaderGenerator> generators = s_generators;

            StringBuilder shaderSB = new StringBuilder();
            StringBuilder passesSB = new StringBuilder();

            foreach(KeyValuePair<Type, INoiseShaderGenerator> pair in generators)
            {
                shaderSB.Clear();
                passesSB.Clear();

                string shaderTemplateStr = null;
                
                INoiseShaderGenerator generator = pair.Value;
                ShaderGeneratorDescriptor generatorDesc = generator.GetDescription();

                if(!File.Exists(generatorDesc.templatePath))
                {
                    Debug.LogError("Could not find specified template file for noise shader generator: " +  generator);
                    continue;
                }

                // load contents of shader template
                using(StreamReader sr = new StreamReader(generatorDesc.templatePath))
                {
                    shaderTemplateStr = sr.ReadToEnd();
                }

                // find the pass template using regex matching
                Match passTemplateMatch = Regex.Match(shaderTemplateStr, Strings.k_regexPassTemplate1);

                if(!passTemplateMatch.Success)
                {
                    Debug.LogError($"Could not find pass template in {generatorDesc.templatePath}. Skipping noise shader generation for this generator type!!");
                    continue;
                }

                string passTemplateStr = passTemplateMatch.Value;

                // generate shaders for each fractal type
                foreach(IFractalType fractal in fractalTypes)
                {
                    FractalTypeDescriptor fractalDesc = fractal.GetDescription();
                    string fullShaderCategory = GetShaderName(generatorDesc, fractalDesc);

                    shaderSB.Append(NoiseLib.Strings.k_warningHeader);
                    shaderSB.Append(shaderTemplateStr);
                    shaderSB.Replace(NoiseLib.Strings.k_tagShaderCategory, $"\"{fullShaderCategory}\"");

                    // add passes for each noise type
                    foreach(INoiseType noise in noiseTypes)
                    {
                        GeneratedShaderInfo info = new GeneratedShaderInfo(fractal, noise);

                        // add to passes string builer
                        passesSB.Append(passTemplateStr);
                        passesSB.AppendLine();
                        passesSB.Replace(NoiseLib.Strings.k_tagIncludes, string.Format("#include \"{0}\"", info.generatedIncludePath));
                       
                        info.ReplaceTags(passesSB);
                    }

                    // replace template with generated passes
                    string newContents = Regex.Replace(shaderSB.ToString(), Strings.k_regexPassTemplate2, passesSB.ToString());

                    newContents = newContents.Replace(NoiseLib.Strings.k_tagFractalName, fractalDesc.name);

                    // load shader contents from disk if it exists
                    string fileName = string.Format("{0}{1}.shader", generatorDesc.name, fractalDesc.name);
                    string filePath = string.Format("{0}/{1}", generatorDesc.outputDir, fileName);

                    if(!Directory.Exists(generatorDesc.outputDir))
                    {
                        Directory.CreateDirectory(generatorDesc.outputDir);
                    }

                    string currentContents = null;

                    FileInfo fi = new FileInfo( filePath );

                    if( File.Exists( filePath ) )
                    {
                        using(StreamReader sr = new StreamReader(filePath))
                        {
                            currentContents = sr.ReadToEnd();
                            currentContents = NormalizeLineEndings(currentContents);
                        }
                    }

                    // do some code cleanup
                    newContents = Regex.Replace(newContents, NoiseLib.Strings.k_regexDupCommas, ", ");
                    newContents = Regex.Replace(newContents, NoiseLib.Strings.k_emptyArgsRight, " )");
                    newContents = Regex.Replace(newContents, NoiseLib.Strings.k_emptyArgsLeft, "( ");

                    newContents = NormalizeLineEndings(newContents);

                    // only write to file if it is not read-only, ie. if it is one of the generated
                    // shader files that we ship with the TerrainTools package
                    if( !fi.IsReadOnly )
                    {
                        if(currentContents == null || currentContents.CompareTo(newContents) != 0)
                        {
                            try
                            {
                                using(StreamWriter sw = new StreamWriter(filePath))
                                {
                                    sw.Write(newContents);
                                }
                            }
                            catch( Exception )
                            {
                                // restore previous cultureinfo
                                System.Threading.Thread.CurrentThread.CurrentCulture = prevCultureInfo;
                            }
                        }
                    }

                    shaderSB.Clear();
                    passesSB.Clear();
                }
            }

            // restore previous cultureinfo
            System.Threading.Thread.CurrentThread.CurrentCulture = prevCultureInfo;

            // UnityEditor.AssetDatabase.Refresh();
        }

        private static void LoadShadersAndCreateMaterials()
        {
            IFractalType[] fractalTypes = s_fractalTypes;

            // CHECK IF MATERIALS HAVE ALREADY BEEN INITIALIZED BUT LOST REFERENCE FOR SOME REASON
            // this happens when running brush tests, for example
            if(s_generatedShaderMap != null && s_generatedMaterialMap != null)
            {
                foreach(KeyValuePair<Type, INoiseShaderGenerator> pair in s_generators)
                {
                    foreach(IFractalType fractal in fractalTypes)
                    {
                        Shader s = s_generatedShaderMap[pair.Key][fractal.GetType()];

                        if(s == null)
                        {
                            string shaderPath = GetShaderName(pair.Value.GetDescription(), fractal.GetDescription());
                            s = Shader.Find(shaderPath);

                            s_generatedShaderMap[pair.Key][fractal.GetType()] = s;
                        }

                        if(s_generatedMaterialMap[pair.Key][fractal.GetType()] == null)
                        {
                            s_generatedMaterialMap[pair.Key][fractal.GetType()] = new Material(s);
                        }
                    }
                }

                return;
            }

            s_generatedShaderMap = new Dictionary<Type, Dictionary<Type, Shader>>();
            s_generatedMaterialMap = new Dictionary<Type, Dictionary<Type, Material>>();

            // load related shaders
            foreach(KeyValuePair<Type, INoiseShaderGenerator> pair in s_generators)
            {
                s_generatedShaderMap.Add(pair.Key, new Dictionary<Type, Shader>());
                s_generatedMaterialMap.Add(pair.Key, new Dictionary<Type, Material>());

                foreach(IFractalType fractal in fractalTypes)
                {
                    string shaderPath = GetShaderName(pair.Value.GetDescription(), fractal.GetDescription());
                    Shader s = Shader.Find(shaderPath);

                    if(s == null)
                    {
                        Debug.LogError($"Could not find shader: {shaderPath}");
                        continue;
                    }

                    s_generatedShaderMap[pair.Key].Add(fractal.GetType(), s);
                    s_generatedMaterialMap[pair.Key].Add(fractal.GetType(), new Material(s));
                }
            }
        }

        internal static class Strings
        {
            public static readonly string k_regexPassTemplate1 = @"(?<=BEGINPASSTEMPLATE)[^\\]*(?=ENDPASSTEMPLATE)";
            public static readonly string k_regexPassTemplate2 = @"BEGINPASSTEMPLATE[^\\]*ENDPASSTEMPLATE";
            public static readonly string k_emptyArgsLeft = @"\(\s*,";
            public static readonly string k_emptyArgsRight = @",\s*\)";
            public static readonly string k_regexDupCommas = @",\s*,";
            public static readonly string k_tagPasses = "${Passes}";
            public static readonly string k_tagIncludes = "${Includes}";
            public static readonly string k_tagNoiseName = "${NoiseName}";
            public static readonly string k_tagFractalName = "${FractalName}";
            public static readonly string k_tagVariantName = "${VariantName}";
            public static readonly string k_tagShaderCategory = "${ShaderCategory}";
            public static readonly string k_tagFunctionParams = "${FunctionParams}";
            public static readonly string k_tagFunctionInputs = "${FunctionInputs}";
            public static readonly string k_tagGetDefaultFractalInput = "${GetDefaultFractalInput}";
            public static readonly string k_tagGetDefaultNoiseInput = "${GetDefaultNoiseInput}";
            public static readonly string k_tagGetDefaultInputs = "${GetDefaultInputs}";
            public static readonly string k_tagGetInputs = "${GetInputs}";
            public static readonly string k_tagFractalDataDefinitions = "${FractalDataDefinitions}";
            public static readonly string k_warningHeader =
                "//////////////////////////////////////////////////////////////////////////\n" +
                "//\n" +
                "//      DO NOT EDIT THIS FILE!! THIS IS AUTOMATICALLY GENERATED!!\n" +
                "//      DO NOT EDIT THIS FILE!! THIS IS AUTOMATICALLY GENERATED!!\n" +
                "//      DO NOT EDIT THIS FILE!! THIS IS AUTOMATICALLY GENERATED!!\n" +
                "//\n" +
                "//////////////////////////////////////////////////////////////////////////\n\n";
        }
    }
}