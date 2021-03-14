using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// A FractalType implementation for Fractal Brownian Motion
    /// </summary>
    [System.Serializable]
    public class FbmFractalType : FractalType<FbmFractalType>
    {
        [System.Serializable]
        public struct FbmFractalInput
        {
            public Vector4  warpOffsets;
            public Vector2  octavesMinMax;
            public Vector2  amplitudeMinMax;
            public Vector2  frequencyMinMax;
            public Vector2  lacunarityMinMax;
            public Vector2  persistenceMinMax;
            public Vector2  warpIterationsMinMax;
            public Vector2  warpStrengthMinMax;
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
            }
        }

        [SerializeField]
        private FbmFractalInput m_input;

        public override FractalTypeDescriptor GetDescription() => new FractalTypeDescriptor()
        {
            name = "Fbm",
            templatePath = "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Templates/FractalFbm.noisehlsltemplate",
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
                new HlslInput() { name = "warpOffsets", float4Value = new HlslFloat4(2.5f, 1.4f, 3.2f, 2.7f) }
            },
            additionalIncludePaths = new List<string>()
            {
                "Packages/com.unity.terrain-tools/Shaders/NoiseLib/NoiseCommon.hlsl"
            }
        };

        public override string GetDefaultSerializedString()
        {
            FbmFractalInput fbm = new FbmFractalInput();

            fbm.Reset();

            return ToSerializedString(fbm);
        }

        public override string DoGUI(string serializedString)
        {
            if (string.IsNullOrEmpty(serializedString))
            {
                serializedString = GetDefaultSerializedString();
            }

            // deserialize string
            FbmFractalInput fbm = (FbmFractalInput)FromSerializedString(serializedString);

            // do gui here
            fbm.octaves = EditorGUILayout.Slider(Styles.octaves, fbm.octaves, fbm.octavesMinMax.x, fbm.octavesMinMax.y);
            fbm.amplitude = EditorGUILayout.Slider(Styles.amplitude, fbm.amplitude, fbm.amplitudeMinMax.x, fbm.amplitudeMinMax.y);
            fbm.persistence = EditorGUILayout.Slider(Styles.persistence, fbm.persistence, fbm.persistenceMinMax.x, fbm.persistenceMinMax.y);
            fbm.frequency = EditorGUILayout.Slider(Styles.frequency, fbm.frequency, fbm.frequencyMinMax.x, fbm.frequencyMinMax.y);
            fbm.lacunarity = EditorGUILayout.Slider(Styles.lacunarity, fbm.lacunarity, fbm.lacunarityMinMax.x, fbm.lacunarityMinMax.y);

            bool toggled = fbm.warpEnabled;

            fbm.warpExpanded = TerrainToolGUIHelper.DrawToggleHeaderFoldout(Styles.domainWarpSettings, fbm.warpExpanded, ref toggled);

            if (fbm.warpExpanded)
            {
                EditorGUI.indentLevel++;
                {
                    DomainWarpSettingsGUI(ref fbm);
                }
                EditorGUI.indentLevel--;
            }
            
            fbm.warpEnabled = toggled;

            return ToSerializedString(fbm);
        }

        private void DomainWarpSettingsGUI(ref FbmFractalInput fbm)
        {
            using(new EditorGUI.DisabledScope(!fbm.warpEnabled))
            {
                fbm.warpIterations = EditorGUILayout.Slider(Styles.warpIterations, fbm.warpIterations, fbm.warpIterationsMinMax.x, fbm.warpIterationsMinMax.y);
                fbm.warpStrength = EditorGUILayout.Slider(Styles.warpStrength, fbm.warpStrength, fbm.warpStrengthMinMax.x, fbm.warpStrengthMinMax.y);
                fbm.warpOffsets = EditorGUILayout.Vector4Field(Styles.warpOffsets, fbm.warpOffsets);
            }
        }

        public override void SetupMaterial(Material mat, string serializedString)
        {
            if (string.IsNullOrEmpty(serializedString))
            {
                serializedString = GetDefaultSerializedString();
            }

            FbmFractalInput fbm = (FbmFractalInput)FromSerializedString(serializedString);

            // set noise domain values
            mat.SetFloat("_FbmOctaves", fbm.octaves);
            mat.SetFloat("_FbmAmplitude", fbm.amplitude);
            mat.SetFloat("_FbmFrequency", fbm.frequency);
            mat.SetFloat("_FbmPersistence", fbm.persistence);
            mat.SetFloat("_FbmLacunarity", fbm.lacunarity);

            // warp values
            mat.SetFloat("_FbmWarpIterations", fbm.warpEnabled ? fbm.warpIterations : 0);
            mat.SetFloat("_FbmWarpStrength", fbm.warpStrength);
            mat.SetVector("_FbmWarpOffsets", fbm.warpOffsets);
        }

        public override string ToSerializedString(object target)
        {
            if(target == null)
            {
                return null;
            }

            if(!(target is FbmFractalInput))
            {
                Debug.LogError($"Attempting to serialize an object that is not of type {typeof(FbmFractalInput)}");
                return null;
            }
            
            FbmFractalInput fbm = (FbmFractalInput)target;

            string serializedString = JsonUtility.ToJson(fbm);

            return serializedString;
        }

        public override object FromSerializedString(string serializedString)
        {
            if(string.IsNullOrEmpty(serializedString))
            {
                serializedString = GetDefaultSerializedString();
            }

            // TODO(wyatt): do validation/upgrading here

            FbmFractalInput target = JsonUtility.FromJson<FbmFractalInput>(serializedString);

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
        }
    }
}