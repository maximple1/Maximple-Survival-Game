using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Flags passed to NoiseSettingsGUI.OnGUI. Used to specify which portions of the Noise Settings GUI to draw.
    /// </summary>
    public enum NoiseSettingsGUIFlags
    {
        Preview = (1 << 0),
        Settings = (1 << 1),
        All = ( ~0 ),
    }

    /// <summary>
    /// Class used to draw the GUI for Noise Settings.
    /// </summary>
    public class NoiseSettingsGUI
    {
        /// <summary>
        /// The SerializedObject for the NoiseSettings that this NoiseSettingsGUI instance is currently rendering the GUI for.
        /// </summary>
        public SerializedObject serializedNoise;
        
        // noise settings properties
        // noise settings properties
        // noise settings properties

        // coord space settings properties
        private SerializedProperty transformSettings;
        private SerializedProperty translation;
        private SerializedProperty rotation;
        private SerializedProperty scale;
        private SerializedProperty flipScaleX;
        private SerializedProperty flipScaleY;
        private SerializedProperty flipScaleZ;

        // domain settings properties
        private SerializedProperty domainSettings;
        private SerializedProperty noiseTypeName;
        private SerializedProperty noiseTypeParams;
        private SerializedProperty fractalTypeName;
        private SerializedProperty fractalTypeParams;
        
        // filter stack settings
        // private SerializedProperty filterSettings;
        // private FilterStackView m_filterStackView;
        // private FilterStack m_filterStack;
        // private SerializedObject m_serializedFilterStack;
        // private bool m_showFilterStack;

        /// <summary>
        /// The current NoiseSettings object that is associated with this instance of NoiseSettingsGUI.
        /// </summary>
        public NoiseSettings target = null;

        /// <summary>
        /// Sets up this instance of NoiseSettingsGUI with the specified NoiseSettings object.
        /// GUI will be drawn for this NoiseSettings instance.
        /// </summary>
        /// <param name="noiseSettings"> The NoiseSettings instance for which GUI will be drawn </param>
        public void Init(NoiseSettings noiseSettings)
        {
            Init(new SerializedObject(noiseSettings));
        }

        /// <summary>
        /// Sets up this instance of NoiseSettingsGUI with the specified SerializedObject containing an object reference
        /// to a NoiseSettings instance. GUI will be drawn for this serialized NoiseSettings instance.
        /// </summary>
        /// <param name="serializedNoise"> A SerializedObject instance containing an object reference to a NoiseSettings object </param>
        public void Init(SerializedObject serializedNoise)
        {
            this.serializedNoise = serializedNoise;

            target = this.serializedNoise.targetObject as NoiseSettings;

            // transform settings
            transformSettings = this.serializedNoise.FindProperty("transformSettings");
            translation = transformSettings.FindPropertyRelative("translation");
            rotation = transformSettings.FindPropertyRelative("rotation");
            scale = transformSettings.FindPropertyRelative("scale");
            flipScaleX = transformSettings.FindPropertyRelative("flipScaleX");
            flipScaleY = transformSettings.FindPropertyRelative("flipScaleY");
            flipScaleZ = transformSettings.FindPropertyRelative("flipScaleZ");

            // domain settings
            domainSettings = this.serializedNoise.FindProperty("domainSettings");
            noiseTypeName = domainSettings.FindPropertyRelative("noiseTypeName");
            noiseTypeParams = domainSettings.FindPropertyRelative("noiseTypeParams");
            fractalTypeName = domainSettings.FindPropertyRelative("fractalTypeName");
            fractalTypeParams = domainSettings.FindPropertyRelative("fractalTypeParams");

            // filter settings
            // filterSettings = serializedNoise.FindProperty( "filterSettings" );
            // m_filterStack = filterSettings.FindPropertyRelative( "filterStack" ).objectReferenceValue as FilterStack;
            // m_serializedFilterStack = new SerializedObject( m_filterStack );
            // m_filterStackView = new FilterStackView( new GUIContent( "Filters" ), m_serializedFilterStack, m_serializedFilterStack.targetObject as FilterStack );
        }

        /// <summary>
        /// Renders the GUI for the NoiseSettings instance associated with this NoiseSettingsGUI instance.
        /// </summary>
        /// <param name="flags"> Flags specifying which portions of the GUI to draw </param>
        public void OnGUI(NoiseSettingsGUIFlags flags = NoiseSettingsGUIFlags.All)
        {
            serializedNoise.Update();

            if((flags & NoiseSettingsGUIFlags.Preview) != 0)
            {
                DrawPreviewTexture(256f, true);
            }

            if((flags & NoiseSettingsGUIFlags.Settings) != 0)
            {
                TerrainToolGUIHelper.DrawFoldout(transformSettings, Styles.transformSettings, TransformSettingsGUI);
                TerrainToolGUIHelper.DrawFoldout(domainSettings, Styles.domainSettings, DomainSettingsGUI);
                // TerrainToolGUIHelper.DrawFoldout(filterSettings, Styles.filterSettings, FilterSettingsGUI);
            }
            
            serializedNoise.ApplyModifiedProperties();
        }

        private void TransformSettingsGUI()
        {
            EditorGUILayout.PropertyField(translation);
            EditorGUILayout.PropertyField(rotation);
            EditorGUILayout.PropertyField(scale);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(Styles.flipScale);
                flipScaleX.boolValue = GUILayout.Toggle(flipScaleX.boolValue, Styles.flipScaleX, GUI.skin.button);
                flipScaleY.boolValue = GUILayout.Toggle(flipScaleY.boolValue, Styles.flipScaleY, GUI.skin.button);
                flipScaleZ.boolValue = GUILayout.Toggle(flipScaleZ.boolValue, Styles.flipScaleZ, GUI.skin.button);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DomainSettingsGUI()
        {
            noiseTypeName.stringValue = NoiseLib.NoiseTypePopup(Styles.noiseType, noiseTypeName.stringValue);
            INoiseType noiseType = NoiseLib.GetNoiseTypeInstance(noiseTypeName.stringValue);
            noiseTypeParams.stringValue = noiseType?.DoGUI(noiseTypeParams.stringValue);

            fractalTypeName.stringValue = NoiseLib.FractalTypePopup(Styles.fractalType, fractalTypeName.stringValue);
            IFractalType fractalType = NoiseLib.GetFractalTypeInstance(fractalTypeName.stringValue);
            fractalTypeParams.stringValue = fractalType?.DoGUI(fractalTypeParams.stringValue);
        }

        private void DoMinMaxSliderInt(GUIContent label, SerializedProperty prop, SerializedProperty minMaxProp)
        {
            int min = minMaxProp.vector2IntValue.x;
            int max = minMaxProp.vector2IntValue.y;
            prop.intValue = TerrainToolGUIHelper.MinMaxSliderInt(label, prop.intValue, ref min, ref max);
            minMaxProp.vector2IntValue = new Vector2Int(min, max);
        }

        private void DoMinMaxSlider(GUIContent label, SerializedProperty prop, SerializedProperty minMaxProp)
        {
            float min = minMaxProp.vector2Value.x;
            float max = minMaxProp.vector2Value.y;
            prop.floatValue = TerrainToolGUIHelper.MinMaxSlider(label, prop.floatValue, ref min, ref max);
            minMaxProp.vector2Value = new Vector2(min, max);
        }

        // private void FilterSettingsGUI()
        // {
        //     m_filterStackView.OnGUI();
        // }

        private void HandlePreviewTextureInput(Rect previewRect)
        {
            if(GUIUtility.hotControl != 0)
            {
                return;
            }

            Vector3 t = translation.vector3Value;
            Vector3 r = rotation.vector3Value;
            Vector3 s = scale.vector3Value;

            EventType eventType = Event.current.type;

            bool draggingPreview = Event.current.button == 0 &&
                                   (eventType == EventType.MouseDown ||
                                    eventType == EventType.MouseDrag);

            Vector2 previewDims = new Vector2(previewRect.width, previewRect.height);
            Vector2 abs = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.z));

            if (Event.current.type == EventType.ScrollWheel)
            {
                abs += Vector2.one * .001f;
                
                float scroll = Event.current.delta.y;
                
                s.x += abs.x * scroll * .05f;
                s.z += abs.y * scroll * .05f;
                
                scale.vector3Value = s;

                Event.current.Use();
            }
            else if (draggingPreview)
            {
                // change noise offset panning icon
                Vector2 sign = new Vector2(-Mathf.Sign(s.x), Mathf.Sign(s.z));
                Vector2 delta = Event.current.delta / previewDims * abs * sign;
                Vector3 d3 = new Vector3(delta.x, 0, delta.y);

                d3 = Quaternion.Euler( r ) * d3;

                t += d3;
                
                translation.vector3Value = t;

                Event.current.Use();
            }
        }

        /// <summary>
        /// Renders an interactive Noise Preview along with tooltip icons and an optional Export button that opens a new ExportNoiseWindow.
        /// A background image is also rendered behind the preview that takes up the entire width of the EditorWindow currently being drawn.
        /// </summary>
        /// <param name = "minSize"> Minimum size for the Preview </param>
        /// <param name = "showExportButton"> Whether or not to render the Export button </param>
        public void DrawPreviewTexture(float minSize, bool showExportButton = true)
        {
            // Draw label with tooltip
            GUILayout.Label( Styles.noisePreview );

            float padding = 4f;
            float iconWidth = 40f;
            int size = (int)Mathf.Min(minSize, EditorGUIUtility.currentViewWidth);
            Rect totalRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, size + padding * 2); // extra pixels for highlight border

            Color prev = GUI.color;
            GUI.color = new Color(.15f, .15f, .15f, 1f);
            GUI.DrawTexture(totalRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;

            // draw info icon
            // if(totalRect.Contains(Event.current.mousePosition))
            {
                Rect infoIconRect = new Rect( totalRect.x + padding, totalRect.y + padding, iconWidth, iconWidth );
                GUI.Label( infoIconRect, Styles.infoIcon );
                // GUI.Label( infoIconRect, Styles.noiseTooltip );
            }
            
            // draw export button
            float buttonWidth = GUI.skin.button.CalcSize(Styles.export).x;
            float buttonHeight = EditorGUIUtility.singleLineHeight;
            Rect exportRect = new Rect( totalRect.xMax - buttonWidth - padding, totalRect.yMax - buttonHeight - padding, buttonWidth, buttonHeight );
            if(GUI.Button(exportRect, Styles.export))
            {
                serializedNoise.ApplyModifiedProperties();
                serializedNoise.Update();

                ExportNoiseWindow.ShowWindow( serializedNoise.targetObject as NoiseSettings );
            }

            float safeSpace = Mathf.Max( iconWidth * 2, buttonWidth * 2 ) + padding * 4;
            float minWidth = Mathf.Min( size, totalRect.width - safeSpace );
            Rect previewRect = new Rect(totalRect.x + totalRect.width / 2 - minWidth / 2, totalRect.y + totalRect.height / 2 - minWidth / 2, minWidth, minWidth);

            EditorGUIUtility.AddCursorRect(previewRect, MouseCursor.Pan);

            if (previewRect.Contains(Event.current.mousePosition))
            {
                serializedNoise.Update();

                HandlePreviewTextureInput(previewRect);

                serializedNoise.ApplyModifiedProperties();
            }

            if ( Event.current.type == EventType.Repaint )
            {
                RenderTexture prevActive = RenderTexture.active;

                // create preview RT here and keep until the next Repaint
                var previewRT = RenderTexture.GetTemporary(512, 512, 0, NoiseUtils.previewFormat);

                NoiseSettings noiseSettings = serializedNoise.targetObject as NoiseSettings;
                RenderTexture tempRT = RenderTexture.GetTemporary(512, 512, 0, NoiseUtils.singleChannelFormat);
                
                NoiseUtils.Blit2D(noiseSettings, tempRT);
                NoiseUtils.BlitPreview2D(tempRT, previewRT);
                RenderTexture.active = prevActive;
                GUI.DrawTexture(previewRect, previewRT, ScaleMode.ScaleToFit, false);

                RenderTexture.ReleaseTemporary(tempRT);
                RenderTexture.ReleaseTemporary(previewRT);
            }

            GUI.color = prev;
        }

        public static class Styles
        {
            public static GUIContent noiseSettings = EditorGUIUtility.TrTextContent("Noise Settings:");
            public static GUIContent randomize = EditorGUIUtility.TrTextContent("Randomize");
            public static GUIContent reset = EditorGUIUtility.TrTextContent("Reset");
            public static GUIContent flipScale = EditorGUIUtility.TrTextContent("Flip Scale", "Flips the scale of the Noise Domain Space");
            public static GUIContent flipScaleX = EditorGUIUtility.TrTextContent("X", "Flips the scale of the Noise Domain Space along the X-axis");
            public static GUIContent flipScaleY = EditorGUIUtility.TrTextContent("Y", "Flips the scale of the Noise Domain Space along the Y-axis");
            public static GUIContent flipScaleZ = EditorGUIUtility.TrTextContent("Z", "Flips the scale of the Noise Domain Space along the Z-axis");
            public static GUIContent noiseType = EditorGUIUtility.TrTextContent("Noise Type", "The type of Noise to be used");
            public static GUIContent fractalType = EditorGUIUtility.TrTextContent("Fractal Type", "The type of Fractal to be used when generating Noise");
            public static GUIContent noisePreview;
            public static GUIContent noiseTooltip = EditorGUIUtility.TrTextContent("",
                                "Scroll Mouse Wheel:\nZooms the preview in and out and changes the noise scale\n\n" +
                                "Left-mouse Drag:\nPans the noise field and changes the noise translation\n\n" +
                                "Color Key:\nCyan = negative noise values\nGrayscale = values between 0 and 1\nBlack = values are 0\nRed = Values greater than 1. Used for debugging texture normalization");
            public static GUIContent seed = EditorGUIUtility.TrTextContent("Seed");
            public static GUIContent domainSettings = EditorGUIUtility.TrTextContent("Domain Settings", "Settings governing the Noise Domain. This is specific to each Noise Type and Fractal Type implementation");
            public static GUIContent transformSettings = EditorGUIUtility.TrTextContent("Transform Settings", "Settings governing the transformations applied to positions in the Noise Domain Space");
            // public static GUIContent filterSettings = EditorGUIUtility.TrTextContent("Filter Settings");
            public static GUIContent ridgePower = EditorGUIUtility.TrTextContent("Ridge Power");
            public static GUIContent billowPower = EditorGUIUtility.TrTextContent("Billow Power");
            public static GUIContent voronoiPower = EditorGUIUtility.TrTextContent("Voronoi Power");
            public static GUIContent power = EditorGUIUtility.TrTextContent("Power");
            public static GUIContent export = EditorGUIUtility.TrTextContent("Export", "Open a window providing options for exporting Noise to Textures");
            public static GUIContent infoIcon = new GUIContent("", EditorGUIUtility.FindTexture("console.infoicon"),
                                "Scroll Mouse Wheel:\nZooms the preview in and out and changes the noise scale\n\n" +
                                "Left-mouse Drag:\nPans the noise field and changes the noise translation\n\n" +
                                "Color Key:\nCyan = negative noise values\nGrayscale = values between 0 and 1\nBlack = values are 0\nRed = Values greater than 1. Used for debugging texture normalization");

            static Styles()
            {
                noisePreview = EditorGUIUtility.TrTextContent("Noise Field Preview:");
                                // ,
                                // "Scroll Mouse Wheel:\nZooms the preview in and out and changes the noise scale\n\n" +
                                // "Left-mouse Drag:\nPans the noise field and changes the noise translation\n\n" +
                                // "Color Key:\nCyan = negative noise values\nGrayscale = values between 0 and 1\nBlack = values are 0\nRed = Values greater than 1. Used for debugging texture normalization");
                // noisePreview.image = infoIcon.image;
            }
        }
    }
}