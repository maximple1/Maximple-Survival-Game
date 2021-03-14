using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class TerraceErosion : TerrainPaintTool<TerraceErosion>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Terrace Tool", typeof(TerrainToolShortcutContext))]
        static void SelectShortcut(ShortcutArguments args) {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintTool<TerraceErosion>();
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Terrace Tool");
        }
#endif

        [SerializeField]
        IBrushUIGroup m_commonUI;
        private IBrushUIGroup commonUI
        {
            get
            {
                if( m_commonUI == null )
                {
                    LoadSettings();
                    m_commonUI = new DefaultBrushUIGroup( "TerraceTool", UpdateAnalyticParameters );
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }

        [System.Serializable]
        class TerraceToolSerializedProperties
        {
            public float m_FeatureSize;
            public float m_BevelAmountInterior;
            public float m_JitterTerraceCount;

            public void SetDefaults()
            {
                m_FeatureSize = 150.0f;
                m_BevelAmountInterior = 0.0f;
                m_JitterTerraceCount = 0.0f;
            }
        }

        TerraceToolSerializedProperties terraceToolProperties = new TerraceToolSerializedProperties();



        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("Hidden/TerrainTools/TerraceErosion"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Sculpt/Terrace";
        }

        public override string GetDesc()
        {
            return "Use to terrace terrain";
        }

        public override void OnEnterToolMode() {
            base.OnEnterToolMode();
            commonUI.OnEnterToolMode();
        }

        public override void OnExitToolMode() {
            base.OnExitToolMode();
            commonUI.OnExitToolMode();
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

            using(IBrushRenderPreviewUnderCursor brushRender = new BrushRenderPreviewUIGroupUnderCursor(commonUI, "TerraceErosion", editContext.brushTexture))
            {
                if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                {
                    PaintContext ctx = brushRender.AcquireHeightmap(false, brushXform.GetBrushXYBounds(), 1);
                
                    brushRender.RenderBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                }
            }
        }

        bool m_ShowControls = true;
        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();

            commonUI.OnInspectorGUI(terrain, editContext);

            m_ShowControls = TerrainToolGUIHelper.DrawHeaderFoldoutForBrush(Styles.controls, m_ShowControls, terraceToolProperties.SetDefaults);
            if (m_ShowControls)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                {
                    terraceToolProperties.m_FeatureSize = EditorGUILayout.Slider(Styles.terraceCount, terraceToolProperties.m_FeatureSize, 2.0f, 1000.0f);
                    terraceToolProperties.m_JitterTerraceCount = EditorGUILayout.Slider(Styles.jitter, terraceToolProperties.m_JitterTerraceCount, 0.0f, 1.0f);
                    terraceToolProperties.m_BevelAmountInterior = EditorGUILayout.Slider(Styles.cornerWeight, terraceToolProperties.m_BevelAmountInterior, 0.0f, 1.0f);
                }
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveSetting();
                Save(true);
                TerrainToolsAnalytics.OnParameterChange();
            }
        }

        private void ApplyBrushInternal(Terrain terrain, IPaintContextRender renderer, PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform)
        {
            Material mat = GetPaintMaterial();
            var brushMask = RTUtils.GetTempHandle(paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
            Utility.SetFilterRT(commonUI, paintContext.sourceRenderTexture, brushMask, mat);
            float delta = terraceToolProperties.m_JitterTerraceCount * 50.0f;
            float jitteredFeatureSize = terraceToolProperties.m_FeatureSize + Random.Range(terraceToolProperties.m_FeatureSize - delta, terraceToolProperties.m_FeatureSize + delta);
            Vector4 brushParams = new Vector4(brushStrength, jitteredFeatureSize, terraceToolProperties.m_BevelAmountInterior, 0.0f);
            
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            
            renderer.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
            renderer.RenderBrush(paintContext, mat, 0);
            RTUtils.Release(brushMask);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            commonUI.OnPaint(terrain, editContext);

            if(commonUI.allowPaint)
            {
                using(IBrushRenderUnderCursor brushRender = new BrushRenderUIGroupUnderCursor(commonUI, "TerraceErosion", editContext.brushTexture))
                {
                    if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                    {
                        PaintContext paintContext = brushRender.AcquireHeightmap(true, brushXform.GetBrushXYBounds());

                        ApplyBrushInternal(terrain, brushRender, paintContext, commonUI.brushStrength, editContext.brushTexture, brushXform);
                    }
                }

                return false;
            }
                
            return true;
        }

        private static class Styles
        {
            public static readonly GUIContent controls = EditorGUIUtility.TrTextContent("Terrace Controls");
            public static readonly GUIContent terraceCount = EditorGUIUtility.TrTextContent("Terrace Count", "Larger value will result in more terraces");
            public static readonly GUIContent jitter = EditorGUIUtility.TrTextContent("Jitter", "Randomize terrace count with each brush stamp");
            public static readonly GUIContent cornerWeight = EditorGUIUtility.TrTextContent("Interior Corner Weight", "The amount of the original height to retain in each interior corner of the terrace steps");
        }

        private void SaveSetting()
        {
            string terraceToolData = JsonUtility.ToJson(terraceToolProperties);
            EditorPrefs.SetString("Unity.TerrainTools.Terrace", terraceToolData);
        }

        private void LoadSettings()
        {
            string terraceToolData = EditorPrefs.GetString("Unity.TerrainTools.Terrace");
            terraceToolProperties.SetDefaults();
            JsonUtility.FromJsonOverwrite(terraceToolData, terraceToolProperties);
        }

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters() => new TerrainToolsAnalytics.IBrushParameter[]{
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.terraceCount.text, Value = terraceToolProperties.m_FeatureSize},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.jitter.text, Value = terraceToolProperties.m_JitterTerraceCount},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.cornerWeight.text, Value = terraceToolProperties.m_BevelAmountInterior},
        };
        #endregion
    }
}
