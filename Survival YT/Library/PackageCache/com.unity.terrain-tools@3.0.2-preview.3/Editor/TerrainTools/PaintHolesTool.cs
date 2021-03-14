using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

#if UNITY_2019_3_OR_NEWER
namespace UnityEditor.Experimental.TerrainAPI
{
    internal class PaintHolesTool : TerrainPaintTool<PaintHolesTool>
    {

        [Shortcut("Terrain/Select Paint Holes Tool", typeof(TerrainToolShortcutContext), KeyCode.F8)]
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintTool<PaintHolesTool>();
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Paint Holes Tool");
        }

        [SerializeField]
        IBrushUIGroup m_commonUI;
        private IBrushUIGroup commonUI
        {
            get
            {
                if (m_commonUI == null)
                {
                    m_commonUI = new DefaultBrushUIGroup("PaintHoles");
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }

        Material m_PaintHoleMat;
        Material GetPaintHolesMaterial()
        {
            m_PaintHoleMat = null;
            if (m_PaintHoleMat == null)
            {
                m_PaintHoleMat = new Material(Shader.Find("Hidden/TerrainEngine/PaintHeightTool"));
            }
            return m_PaintHoleMat;
        }

        public override string GetName()
        {
            return "Paint Holes";
        }

        public override string GetDesc()
        {
            return "Left click to paint a hole.\n\nHold Control and left click to erase it.";
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

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            commonUI.OnInspectorGUI(terrain, editContext);
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            commonUI.OnSceneGUI2D(terrain, editContext);

            if (!editContext.hitValidTerrain && !commonUI.isInUse)
            {
                return;
            }

            // update brush UI group
            commonUI.OnSceneGUI(terrain, editContext);

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            
            if (commonUI.isRaycastHitUnderCursorValid)
            {
                using(IBrushRenderPreviewUnderCursor brushRender = new BrushRenderPreviewUIGroupUnderCursor(commonUI, "PaintHoles", editContext.brushTexture))
                {
                    if (brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                    {
                        PaintContext paintContext = brushRender.AcquireHeightmap(false, brushXform.GetBrushXYBounds(), 1);
                        Material material = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();

                        brushRender.RenderBrushPreview(paintContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, brushXform, material, 0);
                    }
                }
            }
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            commonUI.OnPaint(terrain, editContext);

            if (commonUI.allowPaint)
            {
                Texture brushTexture = editContext.brushTexture;

                using (IBrushRenderUnderCursor brushRender = new BrushRenderUIGroupUnderCursor(commonUI, "PaintHoles", brushTexture))
                {
                    Vector2 halfTexelOffset = new Vector2(0.5f / terrain.terrainData.holesResolution, 0.5f / terrain.terrainData.holesResolution);
                    BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv - halfTexelOffset, commonUI.brushSize, commonUI.brushRotation);
                    PaintContext paintContext = brushRender.AquireHolesTexture(true, brushXform.GetBrushXYBounds());
                    PaintContext paintContextHeight = brushRender.AcquireHeightmap(false, brushXform.GetBrushXYBounds());

                    // filter stack
                    Material mat = GetPaintHolesMaterial();
                    var brushMask = RTUtils.GetTempHandle(paintContextHeight.sourceRenderTexture.width, paintContextHeight.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
                    Utility.SetFilterRT(commonUI, paintContextHeight.sourceRenderTexture, brushMask, mat);

                    // hold control key to erase
                    float brushStrength = Event.current.control ? commonUI.brushStrength : -commonUI.brushStrength;
                    Vector4 brushParams = new Vector4(brushStrength, 0.0f, 0.0f, 0.0f);
                    mat.SetTexture("_BrushTex", editContext.brushTexture);
                    mat.SetVector("_BrushParams", brushParams);

                    brushRender.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
                    brushRender.RenderBrush(paintContext, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.PaintHoles);

                    TerrainPaintUtility.EndPaintHoles(paintContext, "Terrain Paint - Paint Holes");
                    RTUtils.Release(brushMask);
                }			
            }		
            return true;
        }
    }
}
#endif
