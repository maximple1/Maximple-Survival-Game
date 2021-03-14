using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
    internal class SmoothHeightTool : TerrainPaintTool<SmoothHeightTool>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Smooth Tool", typeof(TerrainToolShortcutContext), KeyCode.F3)]
        static void SelectShortcut(ShortcutArguments args) {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintTool<SmoothHeightTool>();
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Smooth Tool");
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
                    m_commonUI = new DefaultBrushUIGroup("SmoothTool", UpdateAnalyticParameters);
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }

        const string toolName = "Smooth Height";

        [SerializeField]
        public float m_direction = 0.0f;     // -1 to 1
        [SerializeField]
        public int m_KernelSize = 1; //blur kernel size

        Material m_Material = null;
        Material GetMaterial() {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("Hidden/TerrainTools/SmoothHeight"));
            return m_Material;
        }

        ComputeShader m_DiffusionCS = null;
        ComputeShader GetDiffusionShader() {
            if(m_DiffusionCS == null) {
                m_DiffusionCS = (ComputeShader)Resources.Load("Diffusion");
            }
            return m_DiffusionCS;
        }

        public override string GetName()
        {
            return toolName;
        }

        public override string GetDesc()
        {
            return Styles.description.text;
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
        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();
            commonUI.OnInspectorGUI(terrain, editContext);

            m_ShowControls = TerrainToolGUIHelper.DrawHeaderFoldoutForBrush(Styles.controls, m_ShowControls, Reset);
            if (m_ShowControls)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                    m_direction = EditorGUILayout.Slider(Styles.direction, m_direction, -1.0f, 1.0f);
                    m_KernelSize = EditorGUILayout.IntSlider(Styles.kernelSize, m_KernelSize, 1, terrain.terrainData.heightmapResolution / 2 - 1);
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveSetting();
                Save(true);
                TerrainToolsAnalytics.OnParameterChange();
            }
        }


        private void Reset()
        {
            m_direction = 0.0f;     // -1 to 1
            m_KernelSize = 1; //blur kernel size
        }

        private void ApplyBrushInternal(Terrain terrain, PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform)
        {
            /*
            ComputeShader cs = GetDiffusionShader();

            int kernel = cs.FindKernel("Diffuse");
            cs.SetFloat("dt", 0.1f);
            cs.SetFloat("diff", 0.01f);
            cs.SetVector("texDim", new Vector4(paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 0.0f, 0.0f));
            cs.SetTexture(kernel, "InputTex", paintContext.sourceRenderTexture);
            cs.SetTexture(kernel, "OutputTex", paintContext.destinationRenderTexture);
            cs.Dispatch(kernel, paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 1);
            */

            RenderTexture prev = RenderTexture.active;
            
            Material mat = GetMaterial();
            var brushMask = RTUtils.GetTempHandle(paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
            Utility.SetFilterRT(commonUI, paintContext.sourceRenderTexture, brushMask, mat);
            Vector4 brushParams = new Vector4(Mathf.Clamp(brushStrength, 0.0f, 1.0f), 0.0f, 0.0f, 0.0f);
            
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Vector4 smoothWeights = new Vector4(
                Mathf.Clamp01(1.0f - Mathf.Abs(m_direction)),   // centered
                Mathf.Clamp01(-m_direction),                    // min
                Mathf.Clamp01(m_direction),                     // max
                0);                                             
            mat.SetInt("_KernelSize", (int)Mathf.Max(1, m_KernelSize)); // kernel size
            mat.SetVector("_SmoothWeights", smoothWeights);
            
            var texelCtx = Utility.CollectTexelValidity(terrain, brushXform.GetBrushXYBounds());
            Utility.SetupMaterialForPaintingWithTexelValidityContext(paintContext, texelCtx, brushXform, mat);

            paintContext.sourceRenderTexture.wrapMode = TextureWrapMode.Clamp;
            paintContext.destinationRenderTexture.wrapMode = TextureWrapMode.Clamp;

            // Two pass blur (first horizontal, then vertical)
            var tmpRT = RTUtils.GetTempHandle(paintContext.destinationRenderTexture.descriptor);
            tmpRT.RT.wrapMode = TextureWrapMode.Clamp;
            mat.SetVector("_BlurDirection", Vector2.right);
            Graphics.Blit(paintContext.sourceRenderTexture, tmpRT, mat);
            mat.SetVector("_BlurDirection", Vector2.up);
            Graphics.Blit(tmpRT, paintContext.destinationRenderTexture, mat);

            RTUtils.Release(tmpRT);
            RTUtils.Release(brushMask);
            texelCtx.Cleanup();
            
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(brushMask);
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

            using(IBrushRenderPreviewUnderCursor brushRender = new BrushRenderPreviewUIGroupUnderCursor(commonUI, "SmoothHeight", editContext.brushTexture))
            {
                if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                {
                    Material material = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();
                    PaintContext ctx = brushRender.AcquireHeightmap(false, brushXform.GetBrushXYBounds(), 1);
                
                    material.SetVector("_JitterOffset", Vector3.zero);
                    brushRender.RenderBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, brushXform, material, 0);

                    // draw result preview
                    {
                        ApplyBrushInternal(terrain, ctx, commonUI.brushStrength, editContext.brushTexture, brushXform);

                        // restore old render target
                        RenderTexture.active = ctx.oldRenderTexture;

                        material.SetTexture("_HeightmapOrig", ctx.sourceRenderTexture);
                        brushRender.RenderBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture, brushXform, material, 1);
                    }
                }
            }
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            commonUI.OnPaint(terrain, editContext);
            
            if(!commonUI.allowPaint) { return true; }

            using(IBrushRenderUnderCursor brushRender = new BrushRenderUIGroupUnderCursor(commonUI, "SmoothHeight", editContext.brushTexture))
            {
                if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                {
                    PaintContext paintContext = brushRender.AcquireHeightmap(true, brushXform.GetBrushXYBounds());
                
                    ApplyBrushInternal(terrain, paintContext, commonUI.brushStrength, editContext.brushTexture, brushXform);
                }
            }
            return true;
        }

        private static class Styles
        {
            public static readonly GUIContent controls = EditorGUIUtility.TrTextContent("Smooth Controls");
            public static readonly GUIContent description = EditorGUIUtility.TrTextContent("Click to smooth the terrain height.");
            public static readonly GUIContent direction = EditorGUIUtility.TrTextContent("Verticality", "Blur only up (1.0), only down (-1.0) or both (0.0)");
            public static readonly GUIContent kernelSize = EditorGUIUtility.TrTextContent("Blur Radius", "Specifies the size of the blurring operation in texture space. This is used to determine the number of texels to include in the blur sample average");
        }

        private void SaveSetting()
        {
            EditorPrefs.SetFloat("Unity.TerrainTools.SmoothHeight.Verticality", m_direction);
        }

        private void LoadSettings()
        {
            m_direction = EditorPrefs.GetFloat("Unity.TerrainTools.SmoothHeight.Verticality", 0.0f);
        }

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters() => new TerrainToolsAnalytics.IBrushParameter[]{
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.direction.text, Value = m_direction},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.kernelSize.text, Value = m_KernelSize},
        };
        #endregion
    }
}
