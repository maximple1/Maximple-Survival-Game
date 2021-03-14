using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;
using Erosion;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class HydroErosionTool : TerrainPaintTool<HydroErosionTool>, IValidationTests
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Hydraulic Erosion Brush", typeof(TerrainToolShortcutContext), KeyCode.F4)]               // tells shortcut manager what to call the shortcut and what to pass as args
        static void SelectShortcut(ShortcutArguments args) {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;          // gets interface to modify state of TerrainTools
            context.SelectPaintTool<HydroErosionTool>();                                                                        // set active tool
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Hydraulic Erosion Brush");
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
                    m_commonUI = new DefaultBrushUIGroup("HydroErosion", UpdateAnalyticParameters);
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }


        NoiseSettings m_HardnessNoiseSettings = null;

        #region Resources

        Erosion.HydraulicEroder m_Eroder = null;// = new Erosion.HydraulicEroder();

        Material m_Material = null;
        Material GetPaintMaterial() {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("Hidden/TerrainTools/SimpleHeightBlend"));
            return m_Material;
        }

        #endregion

        #region GUI

        public override void OnEnable() {
            base.OnEnable();
            m_Eroder = new Erosion.HydraulicEroder();
            m_Eroder.OnEnable();
        }

        public override string GetName()
        {
            return "Erosion/Hydraulic";
        }

        public override string GetDesc()
        {
            return "Hydraulic Erosion\nErodes the terrain according to a fluid simulation.";
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

            using(IBrushRenderPreviewUnderCursor brushRender = new BrushRenderPreviewUIGroupUnderCursor(commonUI, "HydroErosion", editContext.brushTexture))
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
            if (m_HardnessNoiseSettings == null) {
                m_HardnessNoiseSettings = ScriptableObject.CreateInstance<NoiseSettings>();
                m_HardnessNoiseSettings.Reset();
            }


            
            Erosion.HydraulicErosionSettings erosionSettings = ((Erosion.HydraulicEroder)m_Eroder).m_ErosionSettings;

            EditorGUI.BeginChangeCheck();

            commonUI.OnInspectorGUI(terrain, editContext);

            m_Eroder.OnInspectorGUI(terrain, editContext);

            commonUI.validationMessage = ValidateAndGenerateUserMessage(terrain);

            if (EditorGUI.EndChangeCheck()) {  
                Save(true);
                TerrainToolsAnalytics.OnParameterChange();
            }
        }
        #endregion

        #region Paint

        public override void OnEnterToolMode() {
            base.OnEnterToolMode();
            commonUI.OnEnterToolMode();
        }

        public override void OnExitToolMode() {
            base.OnExitToolMode();
            commonUI.OnExitToolMode();
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext) {
            commonUI.OnPaint(terrain, editContext);

            if(!commonUI.allowPaint) { return true; }

            using(IBrushRenderUnderCursor brushRender = new BrushRenderUIGroupUnderCursor(commonUI, "HydroErosion", editContext.brushTexture))
            {
                if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                {
                    PaintContext paintContext = brushRender.AcquireHeightmap(true, brushXform.GetBrushXYBounds(), 1);
                    paintContext.sourceRenderTexture.filterMode = FilterMode.Bilinear;

                    m_Eroder.inputTextures["Height"] = paintContext.sourceRenderTexture;

                    Vector2 texelSize = new Vector2(terrain.terrainData.size.x / terrain.terrainData.heightmapResolution,
                                                    terrain.terrainData.size.z / terrain.terrainData.heightmapResolution);
                    m_Eroder.ErodeHeightmap(terrain.terrainData.size, brushXform.GetBrushXYBounds(), texelSize, commonUI.ModifierActive(BrushModifierKey.BRUSH_MOD_INVERT));
                    m_Eroder.ErodeHeightmap(terrain.terrainData.size, brushXform.GetBrushXYBounds(), texelSize, Event.current.control);

                    Material mat = GetPaintMaterial();
                    var brushMask = RTUtils.GetTempHandle(paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
                    Utility.SetFilterRT(commonUI, paintContext.sourceRenderTexture, brushMask, mat);
                    Vector4 brushParams = new Vector4(commonUI.brushStrength, 0.0f, 0.0f, 0.0f);
                    mat.SetTexture("_BrushTex", editContext.brushTexture);
                    mat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Height"]);
                    mat.SetVector("_BrushParams", brushParams);
                    
                    brushRender.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
                    brushRender.RenderBrush(paintContext, mat, 0);
                    RTUtils.Release(brushMask);
                }
            }

            return true;
        }

        #endregion

        #region IValidationTests
        public virtual string ValidateAndGenerateUserMessage(Terrain terrain)
        {
            if (terrain.terrainData.heightmapResolution < 1025)
                return "Erosion tools work best with a heightmap resolution of 1025 or greater.";

            return "";

        }

        #endregion

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters()
        {
            HydraulicErosionSettings settings = m_Eroder.m_ErosionSettings;
            return new TerrainToolsAnalytics.IBrushParameter[]{
            //Advanced Section
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_SimulationScale.text, Value = settings.m_SimScale.value},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_TimeDelta.text, Value = settings.m_HydroTimeDelta.value},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_NumIterations.text, Value = settings.m_HydroIterations.value},
            
            //Thermal Smoothing
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_ThermalDTScalar.text, Value = settings.m_ThermalTimeDelta},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_NumIterations.text, Value = settings.m_ThermalIterations},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_AngleOfRepose.text, Value = settings.m_ThermalReposeAngle},

            //Water Transport
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_PrecipitationRate.text, Value = settings.m_PrecipRate.value},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_EvaporationRate.text, Value = settings.m_EvaporationRate.value},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_FlowRate.text, Value = settings.m_FlowRate.value},

            //Sediment Transport
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_SedimentCap.text, Value = settings.m_SedimentCapacity.value},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_SedimentDeposit.text, Value = settings.m_SedimentDepositRate.value},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_SedimentDissolve.text, Value = settings.m_SedimentDissolveRate.value},

            //Riverbank
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_RiverbankDeposit.text, Value = settings.m_RiverBankDepositRate.value},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_RiverbankDissolve.text, Value = settings.m_RiverBankDissolveRate.value},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_RiverbedDeposit.text, Value = settings.m_RiverBedDepositRate.value},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Erosion.Styles.m_RiverbedDissolve.text, Value = settings.m_RiverBedDissolveRate.value},

            };
        }
        #endregion
    }
}
