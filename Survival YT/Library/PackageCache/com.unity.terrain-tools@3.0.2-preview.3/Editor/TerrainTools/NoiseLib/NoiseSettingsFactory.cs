using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Class for creating NoiseSettings Assets. If you do not need to save the NoiseSettings
    /// to disk, consider using ScriptableObject.CreateInstance< NoiseSettings >() instead.
    /// </summary>
    public class NoiseSettingsFactory
    {
        /// <summary>
        /// Creates a new NoiseSettings Asset in the root Assets folder. This is the function
        /// accessible via the "Assets/Create/Noise Settings" MenuItem
        /// </summary>
        /// <returns> A reference to the newly created NoiseSettings Asset </returns>d
        [MenuItem("Assets/Create/Noise Settings")]
        public static NoiseSettings CreateAsset()
        {
            return CreateAsset(AssetDatabase.GenerateUniqueAssetPath("Assets/New Noise Settings.asset"));
        }

        /// <summary>
        /// Creates a new NoiseSettings Asset at the specified Asset path
        /// </summary>
        /// <param name="assetPath"> The path in the AssetDatabase where the new NoiseSettings Asset should be saved </param>
        /// <returns> A reference to the newly created NoiseSettings Asset </returns>
        public static NoiseSettings CreateAsset(string assetPath)
        {
            NoiseSettings noiseSettings = ScriptableObject.CreateInstance<NoiseSettings>();

            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CreateAsset(noiseSettings, assetPath);
            AssetDatabase.SaveAssets();
            
            EditorGUIUtility.PingObject(noiseSettings);

            return noiseSettings;
        }
    }
}