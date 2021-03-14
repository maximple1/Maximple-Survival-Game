using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class NoiseHeightTool : TerrainPaintTool<NoiseHeightTool>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Noise Height Brush", typeof(TerrainToolShortcutContext))]               // tells shortcut manager what to call the shortcut and what to pass as args
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;          // gets interface to modify state of TerrainTools
            context.SelectPaintTool<NoiseHeightTool>();                                                                        // set active tool
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Noise Height Brush");
        }
#endif

        [System.Serializable]
        public enum CoordinateSpace
        {
            World = 0,
            Brush,
        }

        [System.Serializable]
        private struct NoiseToolSettings
        {
            public Vector2          worldHeightRemap;
            public CoordinateSpace  coordSpace;
            // TODO(wyatt): what happens if they don't have an active noise settings asset?
            public string           noiseAssetGUID;

            public void Reset()
            {
                worldHeightRemap = new Vector2(0, 1);
                coordSpace = CoordinateSpace.World;
                noiseAssetGUID = null;
            }
        }

        private NoiseToolSettings m_toolSettings;
        private RenderTexture m_previewRT;
        
        private float m_simulationTime;
        private bool m_showToolGUI = true;
        private bool m_liveUpdate;
        private bool m_simulate;

        [SerializeField]
        IBrushUIGroup m_commonUI;
        private IBrushUIGroup commonUI
        {
            get
            {
                if( m_commonUI == null )
                {
                    LoadSettings();
                    m_commonUI = new DefaultBrushUIGroup("NoiseHeightTool", UpdateAnalyticParameters);
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }

        private NoiseSettings m_noiseSettingsIfNull;
        private NoiseSettings noiseSettingsIfNull
        {
            get
            {
                if(m_noiseSettingsIfNull == null)
                {
                    m_noiseSettingsIfNull = ScriptableObject.CreateInstance<NoiseSettings>();
                }

                return m_noiseSettingsIfNull;
            }
        }

        private string getNoiseSettingsPath
        {
            get { return Application.persistentDataPath + "/TerrainTools_NoiseHeightTool_NoiseSettings.noisesettings"; }
        }

        private NoiseSettings m_activeNoiseSettingsProfile;
        private NoiseSettings m_noiseSettings;
        private NoiseSettings noiseSettings
        {
            get
            {
                if(m_noiseSettings == null)
                {
                    if( System.IO.File.Exists( getNoiseSettingsPath ) )
                    {
                        UnityEngine.Object[] obs = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget( getNoiseSettingsPath );
                        m_noiseSettings = obs[ 0 ] as NoiseSettings;
                    }
                    else
                    {
                        m_noiseSettings = ScriptableObject.CreateInstance<NoiseSettings>();
                        m_noiseSettings.name = "NoiseHeightTool_NoiseSettings";
                    }
                }

                return m_noiseSettings;
            }
        }

        private NoiseSettingsGUI m_noiseSettingsGUI;
        private NoiseSettingsGUI noiseSettingsGUI
        {
            get
            {
                if(m_noiseSettingsGUI == null)
                {
                    m_noiseSettingsGUI = new NoiseSettingsGUI();
                }

                if( m_noiseSettingsGUI.target == null || m_noiseSettingsGUI.serializedNoise.targetObject == null )
                {
                    m_noiseSettingsGUI.Init( noiseSettings );
                }

                return m_noiseSettingsGUI;
            }
        }
        
        public override void OnEnterToolMode()
        {
            base.OnEnterToolMode();
            commonUI.OnEnterToolMode();
        }

        public override void OnExitToolMode()
        {
            base.OnExitToolMode();
            commonUI.OnExitToolMode();
        }

        // brush pivot noise rotation from chris
        /* 
            private Vector2 m_brushPosWS;
            private float m_lastBrushRotation;
            private float m_brushRotation;

            // definition of noise space (in world space XZ)
            private Vector2 noiseOrigin = new Vector2(0.0f, 0.0f);
            private Vector2 noiseU = new Vector2(1.0f, 0.0f);
            private Vector2 noiseV { get { return new Vector2(-noiseU.y, noiseU.x); } }

            void OnUpdateBrushRotation()
            {
                if (m_brushRotation != m_lastBrushRotation)
                {
                    // user must have rotated!
                    RotateNoiseSpaceAroundPoint(m_brushPosWS, m_brushRotation - m_lastBrushRotation);
                    m_lastBrushRotation = m_brushRotation;
                }
            }

            void RotateNoiseSpaceAroundPoint(Vector2 brushXZinWS, float degrees)
            {
                Quaternion rotQ = Quaternion.AngleAxis( degrees, Vector3.up );
                Matrix4x4 rot = Matrix4x4.Rotate(rotQ);

                Vector4 o = new Vector4(noiseOrigin.x, 0, noiseOrigin.y, 0);
                o = rot * (o - new Vector4(brushXZinWS.x, 0, brushXZinWS.y, 0)  + new Vector4(brushXZinWS.x, 0, brushXZinWS.y, 0));

                Vector4 u = new Vector4(noiseU.x, 0, noiseU.y, 0);
                u = rot * u;

                noiseOrigin = new Vector2(o.x, o.z);
                noiseU = new Vector2(u.x, u.z);

                noiseU = noiseU.normalized;
            }

            Vector2 NoiseSpaceToWorldSpace(Vector2 noiseSpace)
            {
                return noiseOrigin + noiseU * noiseSpace.x + noiseV * noiseSpace.y;
            }

            Vector2 WorldSpaceToNoiseSpace(Vector2 worldSpace)
            {
                float noiseX = Vector3.Dot(worldSpace - noiseOrigin, noiseU);
                float noiseZ = Vector3.Dot(worldSpace - noiseOrigin, noiseV);

                return new Vector2(noiseX, noiseZ);
            }
        */

        private static Material m_paintMaterial;
        private static Material paintMaterial
        {
            get
            {
                if(m_paintMaterial == null)
                {
                    m_paintMaterial = new Material(Shader.Find("Hidden/TerrainTools/NoiseHeightTool"));
                }

                return m_paintMaterial;
            }
        }

        public override void OnDisable()
        {
            EditorApplication.update -= NoiseSimulationCB;
        }
        
        public override string GetName()
        {
            return "Sculpt/Noise";
        }

        public override string GetDesc()
        {
            return "Tool for painting height based on noise";
        }

        //===================================================================================================
        //
        //      NOISE TOOL GUI
        //
        //===================================================================================================

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();
            {
                GUILayout.Space(12);

                // brush GUI
                commonUI.OnInspectorGUI(terrain, editContext);

                m_showToolGUI = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.noiseToolSettings, m_showToolGUI);

                if(m_showToolGUI)
                {
                    GUILayout.Space(12);

                    noiseSettingsGUI.DrawPreviewTexture(256f, true);

                    // GUILayout.Space(12);

                    // // fill controls
                    // EditorGUILayout.BeginHorizontal();
                    // {
                    //     EditorGUILayout.PrefixLabel( Styles.fillOptions );

                    //     if (GUILayout.Button(Styles.fillSelected))
                    //     {
                    //         FillTile(terrain);
                    //     }
                        
                    //     if(GUILayout.Button(Styles.fillGroup))
                    //     {
                    //         FillAllTiles(terrain);
                    //     }
                    // }
                    // EditorGUILayout.EndHorizontal();
                    // // end fill controls

                    GUILayout.Space(12);

                    // brush coordinate space toolbar
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PrefixLabel(Styles.coordSpace);

                        if(GUILayout.Toggle(m_toolSettings.coordSpace == CoordinateSpace.World, Styles.worldSpace, GUI.skin.button))
                        {
                            m_toolSettings.coordSpace = CoordinateSpace.World;
                        }

                        if(GUILayout.Toggle(m_toolSettings.coordSpace == CoordinateSpace.Brush, Styles.brushSpace, GUI.skin.button))
                        {
                            m_toolSettings.coordSpace = CoordinateSpace.Brush;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(12);

                    // DoSimulationControls();
                    
                    DoNoiseSettingsObjectField();
                
                    noiseSettingsGUI.OnGUI(NoiseSettingsGUIFlags.Settings);

                    GUILayout.Space(12);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
                Save(true);
                TerrainToolsAnalytics.OnParameterChange();
            }
        }

        private void DoSimulationControls()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(Styles.simulationLabel, TerrainToolGUIHelper.dontExpandWidth);
                if (GUILayout.Toggle(m_simulate, Styles.simulationButton, GUI.skin.button, TerrainToolGUIHelper.dontExpandWidth))
                {
                    m_simulate = !m_simulate;

                    EditorApplication.update -= NoiseSimulationCB;

                    if (m_simulate)
                    {
                        EditorApplication.update += NoiseSimulationCB;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(Styles.time);
                m_simulationTime = EditorGUILayout.Slider(m_simulationTime, m_simulationTime - 1, m_simulationTime + 1, TerrainToolGUIHelper.dontExpandWidth);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(12);
        }

        private void DoNoiseSettingsObjectField()
        {
            bool profileInUse = m_activeNoiseSettingsProfile != null;
            float buttonWidth = 60;
            float indentOffset = EditorGUI.indentLevel * 15f;
            Rect lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            Rect labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            Rect fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth * (profileInUse ? 3 : 2), lineRect.height);
            Rect resetButtonRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);
            Rect saveButtonRect = new Rect(resetButtonRect.xMax, lineRect.y, buttonWidth, lineRect.height);
            Rect saveAsButtonRect = new Rect(profileInUse ? saveButtonRect.xMax : resetButtonRect.xMax, lineRect.y, buttonWidth, lineRect.height);

            EditorGUI.PrefixLabel(labelRect, Styles.noiseSettingsProfile);

            NoiseSettings settingsProfile = m_activeNoiseSettingsProfile;
            settingsProfile = (NoiseSettings)EditorGUI.ObjectField(fieldRect, settingsProfile, typeof(NoiseSettings), false);

            if(m_activeNoiseSettingsProfile != null)
            {
                if(GUI.Button(resetButtonRect, Styles.revert))
                {
                    Undo.RecordObject(noiseSettings, "Noise Settings - Revert");

                    noiseSettings.Copy(m_activeNoiseSettingsProfile);
                }
            }
            else
            {
                if(GUI.Button(resetButtonRect, Styles.reset))
                {
                    Undo.RecordObject(noiseSettings, "Noise Settings - Reset");

                    noiseSettings.Reset();
                }
            }

            if(profileInUse && GUI.Button(saveButtonRect, Styles.apply))
            {
                Undo.RecordObject( m_activeNoiseSettingsProfile, "NoiseHeightTool - Apply Settings" );
                m_activeNoiseSettingsProfile.CopySerialized(noiseSettings);
            }

            if(GUI.Button(saveAsButtonRect, Styles.saveAs))
            {
                string path = EditorUtility.SaveFilePanel("Save Noise Settings",
                                                          Application.dataPath,
                                                          "New Noise Settings.asset",
                                                          "asset");
                // saving to project's asset folder
                if(path.StartsWith(Application.dataPath))
                {
                    // TODO(wyatt): need to check if this works with different locales/languages. folder might not be
                    //              called "Assets" in non-English Editor builds
                    string assetPath = path.Substring(Application.dataPath.Length - 6);
                    // settingsProfile = NoiseSettings.CreateAsset(assetPath, noiseSettings);
                    settingsProfile = NoiseSettingsFactory.CreateAsset(assetPath);
                    settingsProfile.CopySerialized(noiseSettings);
                }
                // saving asset somewhere else. why? dunno!
                else if(!string.IsNullOrEmpty(path))
                {
                    Debug.LogError("Invalid path specified for creation of new Noise Settings asset. Must be a valid path within the current Unity project's Assets folder/data path.");
                }
            }

            // check if the profile in the object field changed
            bool changed = settingsProfile != m_activeNoiseSettingsProfile;

            if (changed)
            {
                if (settingsProfile == null)
                {
                    noiseSettings.Copy( noiseSettingsIfNull );
                }
                else
                {
                    if (m_activeNoiseSettingsProfile == null)
                    {
                        noiseSettingsIfNull.Copy(noiseSettings);
                    }

                    noiseSettings.Copy(settingsProfile);
                }

                noiseSettingsGUI.Init(noiseSettings);
                m_activeNoiseSettingsProfile = settingsProfile;
            }

            GUILayout.Space(12);
        }

        //===================================================================================================
        //
        //      FILL TILES
        //
        //===================================================================================================

        private void FillAllTiles(Terrain terrain)
        {
            Terrain[] terrains = TerrainFillUtility.GetTerrainsInGroup(terrain);

            foreach (Terrain tile in terrains)
            {
                FillTile(tile);
            }
        }

        private void FillTile(Terrain terrain)
        {
            // NoiseSettings noiseSettings = noiseSettings;
            // Material material = GetNoiseMaterial(NoiseLib.GetFractalFromName(noiseSettings.domainSettings.fractalTypeName).GetType());
            // int pass = NoiseLib.GetNoiseIndex(noiseSettings.domainSettings.noiseTypeName);

            // float previewSize = 1 / 512f;
            // float brushSize = 1;
            // // brushSize = brushSize * previewSize;

            // float brushRotation = 0;

            // float brushStrength = commonUI.brushStrength;
            // Texture brushTexture = Texture2D.whiteTexture;

            // Vector3 brushPosWS = terrain.transform.position + .5f * new Vector3( terrain.terrainData.size.x, 0, terrain.terrainData.size.z );

            // // set brush params
            // Vector4 brushParams         = new Vector4( 0.01f * brushStrength, 0.0f, brushSize, 1 / brushSize );
            // Quaternion rotQ             = Quaternion.AxisAngle(Vector3.up, 0);
            // Matrix4x4 translation       = Matrix4x4.Translate( brushPosWS );
            // Matrix4x4 rotation          = Matrix4x4.Rotate( rotQ );
            // Matrix4x4 scale             = Matrix4x4.Scale( Vector3.one * brushSize );
            // Matrix4x4 b2w               = translation * rotation * scale;

            // // brush mask and settings
            // material.SetTexture( "_BrushTex", brushTexture );
            // material.SetVector( "_BrushParams", brushParams );

            // // use this value to remap noise values
            // material.SetVector( "_WorldHeightRemap", m_toolSettings.worldHeightRemap );
            
            // // assign matrices
            // material.SetMatrix( "_b2w",             b2w );
            // material.SetMatrix( "_b2w_Translation", translation );
            // material.SetMatrix( "_b2w_Rotation",    rotation );
            // material.SetMatrix( "_b2w_Scale",       scale );

            // // assign inverse matrices
            // material.SetMatrix( "_w2b",             b2w.inverse );
            // material.SetMatrix( "_w2b_Translation", translation.inverse );
            // material.SetMatrix( "_w2b_Rotation",    rotation.inverse );
            // material.SetMatrix( "_w2b_Scale",       scale.inverse );

            // noiseSettings.SetupMaterial(material);
            
            // BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, Vector2.one * .5f,
            //                                     Mathf.Min(terrain.terrainData.size.x, terrain.terrainData.size.z), 0);
            // PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

            // TerrainPaintUtility.SetupTerrainToolMaterialProperties(ctx, brushXform, material);

            // Graphics.Blit(ctx.sourceRenderTexture, ctx.destinationRenderTexture, material, pass);

            // TerrainFillUtility.EndFillHeightmap(ctx, "Terrain Fill - Noise");
            // terrain.ApplyDelayedHeightmapModification();
        }

        //===================================================================================================
        //
        //      APPLY BRUSH
        //
        //===================================================================================================

        private void ApplyBrushInternal(Terrain terrain, PaintContext ctx, BrushTransform brushXform, Vector3 brushPosWS,
                                        float brushRotation, float brushStrength, float brushSize, Texture brushTexture)
        {
            var prevRT = RenderTexture.active;

            brushPosWS.y = 0;

            /*
                blit steps
                1. blit noise to intermediate RT, this includes all the noise transformations and filters,
                using the appropriate noise material. do this with NoiseUtils.Blit2D?
                2. use that noise texture and mult it with brushmask to paint height on terrain
            */

            // TODO(wyatt): remove magic number and tie it into NoiseSettingsGUI preview size somehow
            float previewSize = 1 / 512f;

            // get proper noise material from current noise settings
            NoiseSettings noiseSettings = this.noiseSettings;
            Material matNoise = NoiseUtils.GetDefaultBlitMaterial( noiseSettings );

            // setup the noise material with values in noise settings
            noiseSettings.SetupMaterial( matNoise );

            // convert brushRotation to radians
            brushRotation *= Mathf.PI / 180;

            // change pos and scale so they match the noiseSettings preview
            bool isWorldSpace = ( m_toolSettings.coordSpace == CoordinateSpace.World );
            brushSize = isWorldSpace ? brushSize * previewSize : 1;
            brushPosWS = isWorldSpace ? brushPosWS * previewSize : Vector3.zero;

            // // override noise transform
            var rotQ              = Quaternion.AngleAxis( -brushRotation, Vector3.up );
            var translation       = Matrix4x4.Translate( brushPosWS );
            var rotation          = Matrix4x4.Rotate( rotQ );
            var scale             = Matrix4x4.Scale( Vector3.one * brushSize );
            var noiseToWorld      = translation * scale;

            matNoise.SetMatrix( NoiseSettings.ShaderStrings.transform,
                                noiseSettings.trs * noiseToWorld );

            var noisePass = NoiseUtils.kNumBlitPasses * NoiseLib.GetNoiseIndex( noiseSettings.domainSettings.noiseTypeName );

            // render the noise field to a texture
            // TODO(wyatt): Handle the 3D case. Would need to blit to Volume Texture
            var rtDesc = ctx.destinationRenderTexture.descriptor;
            rtDesc.graphicsFormat = NoiseUtils.singleChannelFormat;
            rtDesc.sRGB = false;
            var noiseRT = RTUtils.GetTempHandle( rtDesc );
            RenderTexture.active = noiseRT; // keep this
            Graphics.Blit(noiseRT, matNoise, noisePass);

            // then add the result to the heightmap using the noise height tool shader
            Material matFinal = paintMaterial;
            var brushMask = RTUtils.GetTempHandle(ctx.sourceRenderTexture.width, ctx.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
            Utility.SetFilterRT(commonUI, ctx.sourceRenderTexture, brushMask, matFinal);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties( ctx, brushXform, matFinal );
            // set brush params
            Vector4 brushParams = new Vector4( 0.01f * brushStrength, 0.0f, brushSize, 1 / brushSize );
            matFinal.SetVector( "_BrushParams", brushParams );
            matFinal.SetTexture( "_BrushTex", brushTexture );
            matFinal.SetTexture( "_NoiseTex", noiseRT );
            matFinal.SetVector( "_WorldHeightRemap", m_toolSettings.worldHeightRemap );
            Graphics.Blit( ctx.sourceRenderTexture, ctx.destinationRenderTexture, matFinal, 0 );

            RTUtils.Release( noiseRT );
            RTUtils.Release( brushMask );
            
            RenderTexture.active = prevRT;
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            commonUI.OnSceneGUI2D(terrain, editContext);

            // only do the rest if user mouse hits valid terrain or they are using the
            // brush parameter hotkeys to resize, etc
            if (!editContext.hitValidTerrain && !commonUI.isInUse)
            {
                return;
            }

            // update brush UI group
            commonUI.OnSceneGUI(terrain, editContext);

            // dont render preview if this isnt a repaint. losing performance if we do
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            using( IBrushRenderPreviewUnderCursor brushPreview =
                    new BrushRenderPreviewUIGroupUnderCursor(commonUI, "NoiseHeightTool", editContext.brushTexture) )
            {
                
                float brushSize = commonUI.brushSize;
                float brushRotation = commonUI.brushRotation;
                float brushStrength = Event.current.control ? -commonUI.brushStrength : commonUI.brushStrength;
                Vector3 brushPosWS = commonUI.raycastHitUnderCursor.point;

                BrushTransform brushXform;
                brushPreview.CalculateBrushTransform(out brushXform);

                PaintContext ctx = brushPreview.AcquireHeightmap(false, brushXform.GetBrushXYBounds(), 1);
                
                Material material = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();
                brushPreview.RenderBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, brushXform, material, 0);

                ApplyBrushInternal(terrain, ctx, brushXform, brushPosWS, commonUI.brushRotation,
                                    brushStrength, brushSize, editContext.brushTexture);

                TerrainPaintUtility.SetupTerrainToolMaterialProperties(ctx, brushXform, material);

                // restore old render target
                RenderTexture.active = ctx.oldRenderTexture;

                material.SetTexture("_HeightmapOrig", ctx.sourceRenderTexture);

                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture,
                                                            editContext.brushTexture, brushXform, material, 1);

                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }

        private Vector3 WSPosFromTerrainUV(Terrain terrain, Vector2 uv)
        {
            return terrain.transform.position +
                   terrain.terrainData.size.x * uv.x * Vector3.right +
                   terrain.terrainData.size.z * uv.y * Vector3.forward;
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            commonUI.OnPaint(terrain, editContext);

            if (commonUI.allowPaint)
            {
                Vector2 uv = editContext.uv;

                if(commonUI.ScatterBrushStamp(ref terrain, ref uv))
                {
                    BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, uv, commonUI.brushSize, commonUI.brushRotation);
                    PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());
                    float brushStrength = Event.current.control ? -commonUI.brushStrength : commonUI.brushStrength;
                    
                    Vector3 brushPosWS = WSPosFromTerrainUV(terrain, uv);

                    ApplyBrushInternal(terrain, paintContext, brushXform, brushPosWS, commonUI.brushRotation,
                                        brushStrength, commonUI.brushSize, editContext.brushTexture);

                    TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Noise");   
                }
            }
            editContext.Repaint(RepaintFlags.UI);

            return true;
        }

        private void NoiseSimulationCB()
        {
            SceneView.RepaintAll();
        }

        //===================================================================================================
        //
        //      TOOL SETTINGS I/O
        //
        //===================================================================================================

        const string kToolSettingsName = "Unity.TerrainTools.NoiseHeightTool";

        private void LoadSettings()
        {
            NoiseToolSettings defaultSettings = new NoiseToolSettings();
            defaultSettings.Reset();

            string settingsStr = EditorPrefs.GetString(kToolSettingsName, JsonUtility.ToJson(defaultSettings));
            m_toolSettings = JsonUtility.FromJson<NoiseToolSettings>(settingsStr);
            
            string assetPath = AssetDatabase.GUIDToAssetPath(m_toolSettings.noiseAssetGUID);

            if(!string.IsNullOrEmpty(assetPath))
            {
                m_activeNoiseSettingsProfile = AssetDatabase.LoadAssetAtPath<NoiseSettings>(assetPath);
            }
        }

        private void SaveSettings()
        {
            if(m_activeNoiseSettingsProfile != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(m_activeNoiseSettingsProfile);

                if(!string.IsNullOrEmpty(assetPath))
                {
                    m_toolSettings.noiseAssetGUID = AssetDatabase.AssetPathToGUID(assetPath);
                }
            }
            
            string settingsStr = JsonUtility.ToJson(m_toolSettings);
            EditorPrefs.SetString(kToolSettingsName, settingsStr);

            // save the noise settings
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget( new UnityEngine.Object[] { m_noiseSettings }, getNoiseSettingsPath, true );
        }


        //===================================================================================================
        //
        //      GUI CONTENT AND STYLES
        //
        //===================================================================================================

        static class Styles
        {
            public static GUILayoutOption dontExpandWidth = GUILayout.ExpandWidth(false);
            public static GUIContent simulationLabel = EditorGUIUtility.TrTextContent("Simulation Controls:");
            public static GUIContent simulationButton = EditorGUIUtility.TrTextContent("Simulate");
            public static GUIContent time = EditorGUIUtility.TrTextContent("Time");
            public static GUIContent fillOptions = EditorGUIUtility.TrTextContent("Fill Options");
            public static GUIContent fillSelected = EditorGUIUtility.TrTextContent("Fill Selected Tile");
            public static GUIContent fillGroup = EditorGUIUtility.TrTextContent("Fill Tiles in Group");
            public static GUIContent reset = EditorGUIUtility.TrTextContent("Reset", "Reset the Noise Settings to the default settings");
            public static GUIContent revert = EditorGUIUtility.TrTextContent("Revert", "Revert the Noise Settings to the values of the Asset that is saved on disk");
            public static GUIContent apply = EditorGUIUtility.TrTextContent("Apply", "Apply the current Noise Settings to the Asset that is saved on disk");
            public static GUIContent saveAs = EditorGUIUtility.TrTextContent("Save As", "Open a window allowing you to save the current Noise Settings to a new Asset on disk");
            public static GUIContent liveUpdate = EditorGUIUtility.TrTextContent("Live Update:");
            public static GUIContent noiseSettingsProfile = EditorGUIUtility.TrTextContent("Noise Settings Profile", "The Noise Settings Asset to use when generating Noise for this tool");
            public static GUIContent coordSpace = EditorGUIUtility.TrTextContent("Coordinate Space", "The coordinate space that is used when calculating positions fed into the Noise generation");
            public static GUIContent worldSpace = EditorGUIUtility.TrTextContent("World", "World space positions are used to generate Noise");
            public static GUIContent brushSpace = EditorGUIUtility.TrTextContent("Brush", "Brush space positions based on Brush UVs are used to generate Noise");
            public static GUIContent worldSpaceHeightRange = EditorGUIUtility.TrTextContent("World Height Range");
            public static GUIContent noiseToolSettings = EditorGUIUtility.TrTextContent("Noise Height Tool Settings", "Settings for the Noise Height Tool");
        }

        #region Analytics
        List<TerrainToolsAnalytics.IBrushParameter> m_AnalyticsData = new List<TerrainToolsAnalytics.IBrushParameter>();
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters()
        {
            m_AnalyticsData.Clear();

            SerializedProperty transformSettings = noiseSettingsGUI.serializedNoise.FindProperty("transformSettings");
            SerializedProperty domainSettings = noiseSettingsGUI.serializedNoise.FindProperty("domainSettings");
            SerializedProperty fractalTypeName = domainSettings.FindPropertyRelative("fractalTypeName");
            SerializedProperty fractalTypeParams = domainSettings.FindPropertyRelative("fractalTypeParams");

            //Add Generic, Transform, and Domain type settings to be sent as analytic data
            m_AnalyticsData.AddRange(new TerrainToolsAnalytics.IBrushParameter[]{ 
                //Generic Settings
                new TerrainToolsAnalytics.BrushParameter<string>{Name = Styles.coordSpace.text,
                    Value = m_toolSettings.coordSpace.ToString()},

                //Transform Settings
                new TerrainToolsAnalytics.BrushParameter<Vector3>{Name = "Translation",
                    Value = transformSettings.FindPropertyRelative("translation").vector3Value},
                new TerrainToolsAnalytics.BrushParameter<Vector3>{Name = "Rotation",
                    Value = transformSettings.FindPropertyRelative("rotation").vector3Value},
                new TerrainToolsAnalytics.BrushParameter<Vector3>{Name = "Scale",
                    Value = transformSettings.FindPropertyRelative("scale").vector3Value},
                new TerrainToolsAnalytics.BrushParameter<bool>{Name = NoiseSettingsGUI.Styles.flipScaleX.text,
                    Value = transformSettings.FindPropertyRelative("flipScaleX").boolValue},
                new TerrainToolsAnalytics.BrushParameter<bool>{Name = NoiseSettingsGUI.Styles.flipScaleY.text,
                    Value = transformSettings.FindPropertyRelative("flipScaleY").boolValue},
                new TerrainToolsAnalytics.BrushParameter<bool>{Name = NoiseSettingsGUI.Styles.flipScaleZ.text,
                    Value = transformSettings.FindPropertyRelative("flipScaleZ").boolValue},

                //Domain
                new TerrainToolsAnalytics.BrushParameter<string>{Name = NoiseSettingsGUI.Styles.noiseType.text,
                        Value = domainSettings.FindPropertyRelative("noiseTypeName").stringValue},
                new TerrainToolsAnalytics.BrushParameter<string>{Name = NoiseSettingsGUI.Styles.fractalType.text,
                        Value = domainSettings.FindPropertyRelative("fractalTypeName").stringValue}}
            );

            //Add fractal specific settings to be sent as analytic data
            IFractalType fractalType = NoiseLib.GetFractalTypeInstance(fractalTypeName.stringValue);
            switch(domainSettings.FindPropertyRelative("fractalTypeName").stringValue)
            {
                case "Fbm":
                    FbmFractalType.FbmFractalInput fbmFractalSettings = (FbmFractalType.FbmFractalInput)fractalType.FromSerializedString(fractalTypeParams.stringValue);
                    m_AnalyticsData.AddRange(new TerrainToolsAnalytics.IBrushParameter[]{
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = FbmFractalType.Styles.octaves.text, Value = fbmFractalSettings.octaves},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = FbmFractalType.Styles.amplitude.text, Value = fbmFractalSettings.amplitude},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = FbmFractalType.Styles.persistence.text, Value = fbmFractalSettings.persistence},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = FbmFractalType.Styles.frequency.text, Value = fbmFractalSettings.frequency},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = FbmFractalType.Styles.lacunarity.text, Value = fbmFractalSettings.lacunarity},
                        new TerrainToolsAnalytics.BrushParameter<bool>{Name = FbmFractalType.Styles.domainWarpSettings.text, Value = fbmFractalSettings.warpEnabled},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = FbmFractalType.Styles.warpIterations.text, Value = fbmFractalSettings.warpIterations},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = FbmFractalType.Styles.warpStrength.text, Value = fbmFractalSettings.warpStrength},
                        new TerrainToolsAnalytics.BrushParameter<Vector4>{Name = FbmFractalType.Styles.warpOffsets.text, Value = fbmFractalSettings.warpOffsets},
                     });
                    break;
                case "Strata":
                    StrataFractalType.StrataFractalInput strataFractalSettings = (StrataFractalType.StrataFractalInput)fractalType.FromSerializedString(fractalTypeParams.stringValue);
                    m_AnalyticsData.AddRange(new TerrainToolsAnalytics.IBrushParameter[]{
                        
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = StrataFractalType.Styles.octaves.text, Value = strataFractalSettings.octaves},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = StrataFractalType.Styles.amplitude.text, Value = strataFractalSettings.amplitude},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = StrataFractalType.Styles.persistence.text, Value = strataFractalSettings.persistence},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = StrataFractalType.Styles.frequency.text, Value = strataFractalSettings.frequency},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = StrataFractalType.Styles.lacunarity.text, Value = strataFractalSettings.lacunarity},
                        new TerrainToolsAnalytics.BrushParameter<bool>{Name = StrataFractalType.Styles.domainWarpSettings.text, Value = strataFractalSettings.warpEnabled},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = StrataFractalType.Styles.warpIterations.text, Value = strataFractalSettings.warpIterations},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = StrataFractalType.Styles.warpStrength.text, Value = strataFractalSettings.warpStrength},
                        new TerrainToolsAnalytics.BrushParameter<Vector4>{Name = StrataFractalType.Styles.warpOffsets.text, Value = strataFractalSettings.warpOffsets},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = StrataFractalType.Styles.strataOffset.text, Value = strataFractalSettings.strataOffset},
                        new TerrainToolsAnalytics.BrushParameter<float>{Name = StrataFractalType.Styles.strataScale.text, Value = strataFractalSettings.strataScale},
                     });
                    break;
                case "None":
                default:
                    break;
            }

            return m_AnalyticsData.ToArray();
        }
        #endregion
    }
}