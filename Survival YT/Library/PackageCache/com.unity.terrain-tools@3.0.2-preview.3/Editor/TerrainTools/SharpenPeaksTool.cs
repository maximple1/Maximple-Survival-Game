using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class SharpenPeaksTool : TerrainPaintTool<SharpenPeaksTool>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Sharpen Peaks Tool", typeof(TerrainToolShortcutContext))]                     // tells shortcut manager what to call the shortcut and what to pass as args
        static void SelectShortcut(ShortcutArguments args) {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;              // gets interface to modify state of TerrainTools
            context.SelectPaintTool<SharpenPeaksTool>();                                                  // set active tool
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Sharpen Peaks Tool");
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
                    m_commonUI = new DefaultBrushUIGroup( "SharpenPeaksTool", UpdateAnalyticParameters );
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }

        [SerializeField]
        float m_ErosionStrength = 16.0f;

        [SerializeField]
        float m_MixStrength = 0.7f;

        Material m_Material = null;

        Material GetPaintMaterial()
        {
            if (m_Material == null)
				m_Material = new Material(Shader.Find("Hidden/TerrainTools/SharpenPeaks"));
            return m_Material;
        }

        public override string GetName()
        {
			return "Effects/Sharpen Peaks";
        }

        public override string GetDesc()
        {
            return "Sharpens peak features.";
        }

        public override void OnEnterToolMode() {
            base.OnEnterToolMode();
            commonUI.OnEnterToolMode();
        }

        public override void OnExitToolMode() {
            base.OnExitToolMode();
            commonUI.OnExitToolMode();
        }

        bool m_ShowControls = true;
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

            using(IBrushRenderPreviewUnderCursor brushRender = new BrushRenderPreviewUIGroupUnderCursor(commonUI, "SharpenPeak", editContext.brushTexture))
            {
                if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                {
                    PaintContext ctx = brushRender.AcquireHeightmap(false, brushXform.GetBrushXYBounds(), 1);
                
                    brushRender.RenderBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                }
            }
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();
            
            commonUI.OnInspectorGUI(terrain, editContext);

            m_ShowControls = TerrainToolGUIHelper.DrawHeaderFoldoutForBrush(Styles.controls, m_ShowControls, () => { m_MixStrength = 0.7f; });
            if (m_ShowControls) {
                EditorGUILayout.BeginVertical("GroupBox");
                    m_MixStrength = EditorGUILayout.Slider(Styles.featureSharpness, m_MixStrength, 0, 1);
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveSetting();
                Save(true);
                TerrainToolsAnalytics.OnParameterChange();
            }
		}
		
		public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            commonUI.OnPaint(terrain, editContext);
            if(!commonUI.allowPaint) { return true; }

            using(IBrushRenderUnderCursor brushRender = new BrushRenderUIGroupUnderCursor(commonUI, "SharpenPeak", editContext.brushTexture))
            {
                if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                {
                    PaintContext paintContext = brushRender.AcquireHeightmap(true, brushXform.GetBrushXYBounds(), 1);

                    Material mat = GetPaintMaterial();
                    var brushMask = RTUtils.GetTempHandle(paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
                    Utility.SetFilterRT(commonUI, paintContext.sourceRenderTexture, brushMask, mat);

                    // apply brush
                    Vector4 brushParams = new Vector4(
                        commonUI.brushStrength,
                        m_ErosionStrength,
                        m_MixStrength,
                        0.0f);

                    mat.SetTexture("_BrushTex", editContext.brushTexture);
                    mat.SetVector("_BrushParams", brushParams);
                
                    brushRender.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
                    brushRender.RenderBrush(paintContext, mat, 0);
                    RTUtils.Release(brushMask);
                }
            }

            return false;
        }

        private static class Styles
        {
            public static readonly GUIContent controls = EditorGUIUtility.TrTextContent("Sharpen Peaks Tool Controls");
            public static readonly GUIContent featureSharpness = EditorGUIUtility.TrTextContent("Sharpness", "Values close to 1 make peaks sharper, and values closer to 0 flatten areas.");
        }

        private void SaveSetting()
        {
            EditorPrefs.SetFloat("Unity.TerrainTools.SharpenPeaks.FeatureSharpness", m_MixStrength);
        }

        private void LoadSettings()
        {
            m_MixStrength = EditorPrefs.GetFloat("Unity.TerrainTools.SharpenPeaks.FeatureSharpness", 0.7f);
        }

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters() => new TerrainToolsAnalytics.IBrushParameter[]{
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.featureSharpness.text, Value = m_MixStrength},
        };
        #endregion
    }
}
