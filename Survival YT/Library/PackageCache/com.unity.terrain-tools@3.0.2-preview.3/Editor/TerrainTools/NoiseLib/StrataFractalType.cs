using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// A FractalType implementation for Stratified Noise
    /// </summary>
    [System.Serializable]
    public class StrataFractalType : FractalType< StrataFractalType >
    {
        [System.Serializable]
        public struct StrataFractalInput
        {
            public Vector4  warpOffsets;
            public Vector2  octavesMinMax;
            public Vector2  amplitudeMinMax;
            public Vector2  frequencyMinMax;
            public Vector2  lacunarityMinMax;
            public Vector2  persistenceMinMax;
            public Vector2  warpIterationsMinMax;
            public Vector2  warpStrengthMinMax;
            public float    strataOffset;
            public float    strataScale;
            public float    octaves;
            public float    amplitude;
            public float    frequency;
            public float    persistence;
            public float    lacunarity;
            public float    warpIterations;
            public float    warpStrength;
            public bool     warpEnabled;
            public bool     warpExpanded;

            public void Reset()
            {
                octaves = 8;
                amplitude = .5f;
                frequency = 1;
                lacunarity = 2;
                persistence = .5f;

                octavesMinMax = new Vector2(0, 16);
                amplitudeMinMax = new Vector2(0, 1);
                frequencyMinMax = new Vector2(0, 2);
                lacunarityMinMax = new Vector2(0, 4);
                persistenceMinMax = new Vector2(0, 1);

                warpExpanded = true;
                warpEnabled = false;
                warpIterations = 1;
                warpStrength = .5f;
                warpOffsets = new Vector4(2.5f, 1.4f, 3.2f, 2.7f);

                warpIterationsMinMax = new Vector2(0, 8);
                warpStrengthMinMax = new Vector2(-2, 2);

                strataOffset = 0;
                strataScale = 1;
            }
        }

        [SerializeField]
        private StrataFractalInput m_input;

        public override FractalTypeDescriptor GetDescription() => new FractalTypeDescriptor()
        {
            name = "Strata",
            templatePath = "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Templates/FractalStrata.noisehlsltemplate",
            supportedDimensions = NoiseDimensionFlags._1D | NoiseDimensionFlags._2D | NoiseDimensionFlags._3D,
            inputStructDefinition = new List<HlslInput>()
            {
                new HlslInput() { name = "octaves", floatValue = new HlslFloat(8.0f) },
                new HlslInput() { name = "amplitude", floatValue = new HlslFloat(.5f) },
                new HlslInput() { name = "persistence", floatValue = new HlslFloat(.5f) },
                new HlslInput() { name = "frequency", floatValue = new HlslFloat(1) },
                new HlslInput() { name = "lacunarity", floatValue = new HlslFloat(2) },
                new HlslInput() { name = "warpIterations", floatValue = new HlslFloat(0) },
                new HlslInput() { name = "warpStrength", floatValue = new HlslFloat(.5f) },
                new HlslInput() { name = "warpOffsets", float4Value = new HlslFloat4(2.5f, 1.4f, 3.2f, 2.7f) },
                new HlslInput() { name = "strataScale", floatValue = new HlslFloat( 1.0f ) },
                new HlslInput() { name = "strataOffset", floatValue = new HlslFloat( 0.0f ) }
            },
            additionalIncludePaths = new List<string>()
            {
                "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Strata/Value.hlsl",
                // "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Implementation/ValueImpl.hlsl",
                "Packages/com.unity.terrain-tools/Shaders/NoiseLib/NoiseCommon.hlsl"
            }
        };

        public override string GetDefaultSerializedString()
        {
            StrataFractalInput strata = new StrataFractalInput();

            strata.Reset();

            return ToSerializedString( strata );
        }

        public override string DoGUI(string serializedString)
        {
            if ( string.IsNullOrEmpty( serializedString ) )
            {
                serializedString = GetDefaultSerializedString();
            }

            // deserialize string
            StrataFractalInput strata = ( StrataFractalInput )FromSerializedString( serializedString );

            // do gui here
            EditorGUILayout.Space();
            strata.strataOffset = EditorGUILayout.FloatField( Styles.strataOffset, strata.strataOffset );
            strata.strataScale = EditorGUILayout.FloatField( Styles.strataScale, strata.strataScale );
            EditorGUILayout.Space();
            strata.octaves = EditorGUILayout.Slider(Styles.octaves, strata.octaves, strata.octavesMinMax.x, strata.octavesMinMax.y);
            strata.amplitude = EditorGUILayout.Slider(Styles.amplitude, strata.amplitude, strata.amplitudeMinMax.x, strata.amplitudeMinMax.y);
            strata.persistence = EditorGUILayout.Slider(Styles.persistence, strata.persistence, strata.persistenceMinMax.x, strata.persistenceMinMax.y);
            strata.frequency = EditorGUILayout.Slider(Styles.frequency, strata.frequency, strata.frequencyMinMax.x, strata.frequencyMinMax.y);
            strata.lacunarity = EditorGUILayout.Slider(Styles.lacunarity, strata.lacunarity, strata.lacunarityMinMax.x, strata.lacunarityMinMax.y);

            bool toggled = strata.warpEnabled;

            strata.warpExpanded = TerrainToolGUIHelper.DrawToggleHeaderFoldout(Styles.domainWarpSettings, strata.warpExpanded, ref toggled);

            if (strata.warpExpanded)
            {
                EditorGUI.indentLevel++;
                {
                    DomainWarpSettingsGUI(ref strata);
                }
                EditorGUI.indentLevel--;
            }
            
            strata.warpEnabled = toggled;

            return ToSerializedString(strata);
        }

        private void DomainWarpSettingsGUI( ref StrataFractalInput strata )
        {
            using(new EditorGUI.DisabledScope( !strata.warpEnabled ) )
            {
                strata.warpIterations = EditorGUILayout.Slider(Styles.warpIterations, strata.warpIterations, strata.warpIterationsMinMax.x, strata.warpIterationsMinMax.y);
                strata.warpStrength = EditorGUILayout.Slider(Styles.warpStrength, strata.warpStrength, strata.warpStrengthMinMax.x, strata.warpStrengthMinMax.y);
                strata.warpOffsets = EditorGUILayout.Vector4Field(Styles.warpOffsets, strata.warpOffsets);
            }
        }

        public override void SetupMaterial(Material mat, string serializedString)
        {
            if (string.IsNullOrEmpty(serializedString))
            {
                serializedString = GetDefaultSerializedString();
            }

            StrataFractalInput strata = (StrataFractalInput)FromSerializedString(serializedString);

            // set noise domain values
            mat.SetFloat("_StrataOctaves", strata.octaves);
            mat.SetFloat("_StrataAmplitude", strata.amplitude);
            mat.SetFloat("_StrataFrequency", strata.frequency);
            mat.SetFloat("_StrataPersistence", strata.persistence);
            mat.SetFloat("_StrataLacunarity", strata.lacunarity);

            // warp values
            mat.SetFloat("_StrataWarpIterations", strata.warpEnabled ? strata.warpIterations : 0);
            mat.SetFloat("_StrataWarpStrength", strata.warpStrength);
            mat.SetVector("_StrataWarpOffsets", strata.warpOffsets);

            mat.SetFloat("_StrataStrataOffset", strata.strataOffset );
            mat.SetFloat("_StrataStrataScale", strata.strataScale );
        }

        public override string ToSerializedString(object target)
        {
            if(target == null)
            {
                return null;
            }

            if(!(target is StrataFractalInput))
            {
                Debug.LogError($"Attempting to serialize an object that is not of type {typeof(StrataFractalInput)}");
                return null;
            }
            
            StrataFractalInput strata = (StrataFractalInput)target;

            string serializedString = JsonUtility.ToJson(strata);

            return serializedString;
        }

        public override object FromSerializedString(string serializedString)
        {
            if(string.IsNullOrEmpty(serializedString))
            {
                serializedString = GetDefaultSerializedString();
            }

            // TODO(wyatt): do validation/upgrading here

            StrataFractalInput target = JsonUtility.FromJson<StrataFractalInput>(serializedString);

            return target;
        }

        internal static class Styles
        {
            public static GUIContent warpStrength = EditorGUIUtility.TrTextContent("Strength", "The overall strength of the warping effect");
            public static GUIContent warpIterations = EditorGUIUtility.TrTextContent("Iterations", "The number of warping iterations applied to the Noise");
            public static GUIContent warpOffsets = EditorGUIUtility.TrTextContent("Offset", "The offset direction to be used when warping the Noise");
            public static GUIContent domainWarpSettings = EditorGUIUtility.TrTextContent("Warp Settings", "Settings for applying turbulence to the Noise");
            public static GUIContent octaves = EditorGUIUtility.TrTextContent("Octaves", "The number of Octaves of Noise to generate. Each Octave is generally a smaller scale than the previous Octave and a larger scale than the next");
            public static GUIContent amplitude = EditorGUIUtility.TrTextContent("Amplitude", "The unmodified amplitude applied to each Octave of Noise. At each Octave, the amplitude is multiplied by the Persistence");
            public static GUIContent persistence = EditorGUIUtility.TrTextContent("Persistence", "The scaling factor applied to the Noise Amplitude at each Octave");
            public static GUIContent frequency = EditorGUIUtility.TrTextContent("Frequency", "The unmodified frequency of Noise at each Octave. At each Octave, the Frequency is multiplied by the Lacunarity");
            public static GUIContent lacunarity = EditorGUIUtility.TrTextContent("Lacunarity", "The scaling factor applied to the Noise Frequency at each Octave");
            public static GUIContent strataScale = EditorGUIUtility.TrTextContent("StrataScale", "The scaling applied to the strata. Higher values will produce more banding");
            public static GUIContent strataOffset = EditorGUIUtility.TrTextContent("StrataOffset", "The vertical offset applied to the strata");
        }
    }
}