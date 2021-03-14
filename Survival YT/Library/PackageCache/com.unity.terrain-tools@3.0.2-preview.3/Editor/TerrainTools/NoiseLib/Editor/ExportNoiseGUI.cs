using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Class that implements EditorWindow in order to provide options for exporting noise to 
    /// textures that can be saved to disk.
    /// </summary>
    public class ExportNoiseGUI
    {
        public enum ExportMode
        {
            Texture2D = 0,
            Texture3D,
        }

        private NoiseSettings m_noise;
        private ExportMode m_exportMode = ExportMode.Texture2D;
        private Vector2Int dims2D = new Vector2Int(512,512);
        private Vector3Int dims3D = new Vector3Int(64, 64, 64);
        private bool m_normalize;
        private GraphicsFormat m_format = GraphicsFormat.R16_UNorm;

        /// <summary>
        /// Initializes the ExportNoiseWindow instance with the given NoiseSettings instance.
        /// </summary>
        /// <param name = "noise"> The NoiseSettings instance to be used with this ExportNoiseWindow instance </param>
        public ExportNoiseGUI( NoiseSettings noise )
        {
            m_noise = noise;
        }

        /// <summary>
        /// Renders the GUI for the ExportNoiseWindow instance
        /// </summary>
        public void OnGUI()
        {
            GUILayout.Space(16);

            using( new EditorGUI.DisabledScope(true) )
            {
                EditorGUILayout.ObjectField(Styles.noise, m_noise, typeof(NoiseSettings), false);
            }

            m_exportMode = ( ExportMode )EditorGUILayout.EnumPopup( Styles.exportMode, m_exportMode );

            EditorGUILayout.BeginHorizontal();
            {
                if(m_exportMode == ExportMode.Texture2D)
                {
                    EditorGUILayout.PrefixLabel( Styles.dims2D );
                    dims2D = EditorGUILayout.Vector2IntField(GUIContent.none, dims2D);
                }
                else if(m_exportMode == ExportMode.Texture3D)
                {
                    EditorGUILayout.PrefixLabel( Styles.dims3D );
                    dims3D = EditorGUILayout.Vector3IntField(GUIContent.none, dims3D);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel( Styles.normalize );
                m_normalize = GUILayout.Toggle( m_normalize, GUIContent.none );
            }
            EditorGUILayout.EndHorizontal();

            m_format = ( GraphicsFormat )EditorGUILayout.EnumPopup( Styles.format, m_format );

            GUILayout.Space(16);

            if( GUILayout.Button(Styles.export) )
            {
                if(m_exportMode == ExportMode.Texture2D)
                {
                    Export2D();
                }
                else if(m_exportMode == ExportMode.Texture3D)
                {
                    Export3D();
                }
            }
        }

        private void Export2D()
        {
            Texture2D texture = null;

            try
            {
                string path = EditorUtility.SaveFilePanel("Export Noise To Texture2D",
                                                       Application.dataPath,
                                                       "New Noise Texture2D.png",
                                                       "png");

                if(!path.StartsWith(Application.dataPath))
                {
                    Debug.LogError("You must specificy a path in your project's Assets folder to export a Noise Texture");
                }

                if(!string.IsNullOrEmpty(path) )
                {
                    EditorUtility.DisplayProgressBar("Exporting Noise to Texture2D", "Making some noise...", 0.1f);

                    texture = NoiseUtils.BakeToTexture2D( m_noise, dims2D.x, dims2D.y, m_format, TextureCreationFlags.None );

                    byte[] bytes = ImageConversion.EncodeToPNG(texture);

                    System.IO.File.WriteAllBytes(path, bytes);

                    Texture2D.DestroyImmediate(texture);
                    texture = null;

                    string assetPath = path.Remove(0, Application.dataPath.Length - "Assets".Length);

                    AssetDatabase.Refresh();

                    EditorUtility.ClearProgressBar();

                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    EditorGUIUtility.PingObject(texture);
                }
            }
            catch(Exception e)
            {
                Debug.LogError( e );

                if(texture != null)
                {
                    Texture2D.DestroyImmediate( texture );
                }

                Debug.Log("Exception caught");

                EditorUtility.ClearProgressBar();
            }
        }

        private void Export3D()
        {
            Texture3D texture = null;

            try
            {
                string path = EditorUtility.SaveFilePanel("Export Noise To Texture3D",
                                                            Application.dataPath,
                                                            "New Noise Texture3D.asset",
                                                            "asset");

                if(!path.StartsWith(Application.dataPath))
                {
                    Debug.LogError("You must specificy a path in your project's Assets folder to export a Noise Texture");
                }

                if(!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    EditorUtility.DisplayProgressBar("Exporting Noise to Texture3D", "Making some noise...", 0.1f);

                    texture = NoiseUtils.BakeToTexture3D( m_noise, dims3D.x, dims3D.y, dims3D.z, m_format, TextureCreationFlags.None );
                    
                    AssetDatabase.CreateAsset(texture, path.Remove(0, Application.dataPath.Length - "Assets".Length));
                    
                    AssetDatabase.Refresh();

                    EditorUtility.ClearProgressBar();

                    EditorGUIUtility.PingObject(texture);
                }
            }
            catch(Exception e)
            {
                Debug.LogError( e );

                if(texture != null)
                {
                    Texture2D.DestroyImmediate( texture );
                }

                EditorUtility.ClearProgressBar();
            }
        }

        private static class Styles
        {
            public static GUIContent title = EditorGUIUtility.TrTextContent("Export Noise to Texture");
            public static GUIContent noise = EditorGUIUtility.TrTextContent("Noise Settings");
            public static GUIContent normalize = EditorGUIUtility.TrTextContent("Normalize", "Normalize pixel values so they fit between a 0 - 1 range");
            public static GUIContent exportMode = EditorGUIUtility.TrTextContent("Export Mode");
            public static GUIContent export = EditorGUIUtility.TrTextContent("Export");
            public static GUIContent dims2D = EditorGUIUtility.TrTextContent("Dimensions", "Texture Dimensions. X = Width, Y = Height");
            public static GUIContent dims3D = EditorGUIUtility.TrTextContent("Dimensions", "Texture Dimensions. X = Width, Y = Height, Z = Depth");
            public static GUIContent format = EditorGUIUtility.TrTextContent("Texture Format");
        }
    }
}