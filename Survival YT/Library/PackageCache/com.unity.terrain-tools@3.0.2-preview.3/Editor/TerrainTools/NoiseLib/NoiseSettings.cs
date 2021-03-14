using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// A data class that can be used to define various types of noise.
    /// </summary>
    [System.Serializable]
    public class NoiseSettings : ScriptableObject
    {
        /// <summary>
        /// Struct containing information about the transform (translation, rotation, scale) to be
        /// applied to the noise. Usage of these values is depending on the actual noise
        /// implementation.
        /// </summary>
        [System.Serializable]
        public struct NoiseTransformSettings
        {
            /// <summary>
            /// The translation used when generating the noise field
            /// </summary>
            [Tooltip("The translational offset to use when generating the noise")]
            public Vector3          translation;

            /// <summary>
            /// The scale used when generating the noise field
            /// </summary>
            [Tooltip("The scale of the generated noise")]
            public Vector3          scale;

            /// <summary>
            /// The rotation used when generating the noise field
            /// </summary>
            [Tooltip("The rotation of the generated noise")]
            public Vector3          rotation;

            /// <summary>
            /// Determines whether the scale should be flipped across the x-axis
            /// </summary>
            [Tooltip("Whether or not the 2D noise field should be flipped along the X-axis")]
            public bool             flipScaleX;

            /// <summary>
            /// Determines whether the scale should be flipped across the y-axis
            /// </summary>
            [Tooltip("Whether or not the 2D noise field should be flipped along the Y-axis")]
            public bool             flipScaleY;

            /// <summary>
            /// Determines whether the scale should be flipped across the z-axis
            /// </summary>
            [Tooltip("Whether or not the 2D noise field should be flipped along the Z-axis")]
            public bool             flipScaleZ;

            /// <summary>
            /// Resets the members of the transform struct to their default states and values
            /// </summary>
            public void Reset()
            {
                translation = Vector3.zero;
                scale = Vector3.one * 5f;
                rotation = Vector3.zero;

                flipScaleX = false;
                flipScaleY = false;
                flipScaleZ = false;
            }
        }

        /// <summary>
        /// Struct containing strings that reference the noise type and fractal type in use
        /// as well as the serialized data associated with the noise and fractal type.
        /// </summary>
        [System.Serializable]
        public struct NoiseDomainSettings
        {
            /// <summary>
            /// String representing the name of the NoiseType that is in use
            /// </summary>
            [Tooltip("The type of noise being generated")]
            public string noiseTypeName;

            /// <summary>
            /// String representing the name of the FractalType that is in use
            /// </summary>
            [Tooltip("The type of fractal used with the generated noise")]
            public string fractalTypeName;

            /// <summary>
            /// String containing serialized data for the active NoiseType
            /// </summary>
            [Tooltip("Settings specific to noise type")]
            public string noiseTypeParams;

            /// <summary>
            /// String containing serialized data for the active FractalType
            /// </summary>
            [Tooltip("Settings specific to noise type")]
            public string fractalTypeParams;

            /// <summary>
            /// Resets the domain settings to the defaults for the built-in Perlin NoiseType and Fbm FractalType
            /// </summary>
            public void Reset()
            {
                noiseTypeName = PerlinNoise.instance.GetDescription().name;
                fractalTypeName = FbmFractalType.instance.GetDescription().name;
                noiseTypeParams = PerlinNoise.instance.GetDefaultSerializedString();
                fractalTypeParams = FbmFractalType.instance.GetDefaultSerializedString();
            }
        }

        // put filter stack in separate class just for the isExpanded functionality
        // of SerializedProperties
        // [System.Serializable]
        // public struct FilterSettings
        // {
        //     [SerializeField]
        //     public FilterStack filterStack;
        // }

        /// <summary>
        /// The transform settings for the defined noise field
        /// </summary>
        [Tooltip("Settings for noise transform and coordinate space")]
        public NoiseTransformSettings       transformSettings;
        /// <summary>
        /// The domain settings for the defined noise field. Contains serialized data
        /// defining the NoiseType and FractalType for this NoiseSettings instance.
        /// </summary>
        [Tooltip("Settings for noise domain")]
        public NoiseDomainSettings          domainSettings;

        public bool useTextureForPositions;
        public Texture positionTexture;

        // /// <summary>
        // /// MISSING
        // /// </summary>
        // public FilterSettings               filterSettings;

        /// <summary>
        /// The noise field's TRS transformation Matrix
        /// </summary>
        /// <returns> A Matrix4x4 for the noise field's TRS matrix </returns>
        public Matrix4x4 trs
        {
            get
            {
                // set noise transform values
                Vector3 scale = transformSettings.scale;
                scale.x = transformSettings.flipScaleX ? -scale.x : scale.x;
                scale.y = transformSettings.flipScaleY ? -scale.y : scale.y;
                scale.z = transformSettings.flipScaleZ ? -scale.z : scale.z;
                // scale.w = transformSettings.flipScaleW ? -scale.w : scale.w;

                Matrix4x4 trs = Matrix4x4.TRS(transformSettings.translation,
                                            Quaternion.Euler(transformSettings.rotation),
                                            scale);

                return trs;
            }
        }

        /// <summary>
        /// Copies the runtime information from a provided NoiseSettings instance.
        /// </summary>
        /// <param name="noiseSettings"> The NoiseSettings instance to copy from </param>
        public void Copy(NoiseSettings noiseSettings)
        {
            transformSettings = noiseSettings.transformSettings;
            domainSettings = noiseSettings.domainSettings;
            
            // // TODO(wyatt): copy Filter Stack
            // Debug.LogError("TODO(wyatt): copy filter stack");
        }

        /// <summary>
        /// Copies the serialized information from a provided NoiseSettings instance
        /// </summary>
        /// <param name="noiseSettings"> The NoiseSettings instance to copy from </param>
        public void CopySerialized(NoiseSettings noiseSettings)
        {
            SerializedObject copy = new SerializedObject(noiseSettings);
            SerializedObject _this = new SerializedObject(this);
            copy.Update();
            _this.Update();

            _this.CopyFromSerializedProperty(copy.FindProperty("transformSettings"));
            _this.CopyFromSerializedProperty(copy.FindProperty("domainSettings"));
            // _this.CopyFromSerializedProperty(copy.FindProperty("m_filterSettings"));

            _this.ApplyModifiedProperties();
        }

        /// <summary>
        /// Resets this NoiseSettings instance to have the default settings
        /// </summary>
        public void Reset()
        {
            transformSettings.Reset();
            domainSettings.Reset();
        }

        /// <summary>
        /// Sets up the provided Material to be used with this NoiseSettings instance. Some assumptions are made
        /// here as far as definitions and variable declarations in the Material's Shader go.
        /// float4 _NoiseTranslation, float4 _NoiseRotation, float4 _NoiseScale, and float4x4 _NoiseTransform are
        /// assumed to be declared. Sets up the Material using the NoiseType and FractalType SetupMaterial
        /// functions.
        /// </summary>
        /// <param name="mat"> The Material to set up for use with this NoiseSettings instance </param>
        public void SetupMaterial(Material mat)
        {
            INoiseType noiseType = NoiseLib.GetNoiseTypeInstance(domainSettings.noiseTypeName);
            IFractalType fractalType = NoiseLib.GetFractalTypeInstance(domainSettings.fractalTypeName);

            // set individual transform info
            mat.SetVector(ShaderStrings.translation, transformSettings.translation);
            mat.SetVector(ShaderStrings.rotation, transformSettings.rotation);
            mat.SetVector(ShaderStrings.scale, transformSettings.scale);

            // set full transform matrix
            mat.SetMatrix(ShaderStrings.transform, trs);

            noiseType?.SetupMaterial(mat, domainSettings.noiseTypeParams);
            fractalType?.SetupMaterial(mat, domainSettings.fractalTypeParams);

            if( useTextureForPositions )
            {
                mat.EnableKeyword( "USE_NOISE_TEXTURE" );
                mat.SetTexture( "_NoiseTex", positionTexture );
            }
            else
            {
                mat.DisableKeyword( "USE_NOISE_TEXTURE" );
            }
        }

        /// <summary>
        /// Class containing the string names of shader properties used by noise shaders
        /// </summary>
        public static class ShaderStrings
        {
            /// <summary>
            /// Property for noise translation. Value = "_NoiseTranslation"
            /// </summary>
            public static readonly string translation = "_NoiseTranslation";
            /// <summary>
            /// Property for noise scale. Value = "_NoiseScale"
            /// </summary>
            public static readonly string scale =       "_NoiseScale";
            /// <summary>
            /// Property for noise rotation. Value = "_NoiseRotation"
            /// </summary>
            public static readonly string rotation =    "_NoiseRotation";
            /// <summary>
            /// Property for the noise transform. Value = "_NoiseTransform"
            /// </summary>
            public static readonly string transform =   "_NoiseTransform";
        }
    }
}