using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
    //[FilePathAttribute("Library/TerrainTools/Stamp", FilePathAttribute.Location.ProjectFolder)]
    public class StampTool : TerrainPaintTool<StampTool>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Stamp Tool", typeof(TerrainToolShortcutContext))]
        static void SelectShortcut(ShortcutArguments args) {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintTool<StampTool>();
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Stamp Tool");
        }
#endif

        class Styles
        {
            public readonly GUIContent controls = EditorGUIUtility.TrTextContent("Stamp Tool Controls");
            public readonly GUIContent description = EditorGUIUtility.TrTextContent("Left click to stamp the brush onto the terrain.\n\nHold control and mousewheel to adjust height.");
            public readonly GUIContent height = EditorGUIUtility.TrTextContent("Stamp Height", "You can set the Stamp Height manually or you can hold control and mouse wheel on the terrain to adjust it.");
            public readonly GUIContent down = EditorGUIUtility.TrTextContent("Subtract", "Subtract the stamp from the terrain.");
            public readonly GUIContent maxadd = EditorGUIUtility.TrTextContent("Max <--> Add", "Blend between adding the heights, and taking the maximum.");

        }

        private static Styles m_styles;
        private Styles GetStyles()
        {
            if (m_styles == null)
            {
                m_styles = new Styles();
            }
            return m_styles;
        }

        
        [System.Serializable]
        class StampToolSerializedProperties
        {
            public float m_StampHeight;
            public bool stampDown;
            public float m_MaxBlendAdd;

            public void SetDefaults()
            {
                m_StampHeight = 100.0f;
                stampDown = false;
                m_MaxBlendAdd = 0.0f;
            }
        }

        StampToolSerializedProperties stampToolProperties = new StampToolSerializedProperties();

        [SerializeField]
        IBrushUIGroup m_commonUI;
        private IBrushUIGroup commonUI
        {
            get
            {
                if( m_commonUI == null )
                {
                    LoadSettings();
                    m_commonUI = new DefaultBrushUIGroup("StampTool", UpdateAnalyticParameters);
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }

        public override string GetName()
        {
            return "Stamp Terrain";
        }

        public override string GetDesc()
        {
            return "Left click to stamp the brush onto the terrain.\n\nHold control and mousewheel to adjust height.";
        }

        private void ApplyBrushInternal(IPaintContextRender renderer, PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform, Terrain terrain)
        {
            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();


            float height = stampToolProperties.m_StampHeight / (terrain.terrainData.size.y * 2.0f);
            
            if(stampToolProperties.stampDown)
            {
                height = -height;
            }

            Vector4 brushParams = new Vector4(brushStrength, 0.0f, height, stampToolProperties.m_MaxBlendAdd);

            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            var brushMask = RTUtils.GetTempHandle(paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
            Utility.SetFilterRT(commonUI, paintContext.sourceRenderTexture, brushMask, mat);
            renderer.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
            renderer.RenderBrush(paintContext, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.StampHeight);
            RTUtils.Release(brushMask);
        }

        public override void OnEnterToolMode() {
            base.OnEnterToolMode();
            commonUI.OnEnterToolMode();
        }

        public override void OnExitToolMode() {
            base.OnExitToolMode();
            commonUI.OnExitToolMode();
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            commonUI.OnPaint(terrain, editContext);

            // ignore mouse drags
            if(Event.current.type != EventType.MouseDrag && !Event.current.shift)
            {
                Texture brushTexture = editContext.brushTexture;
            
                using(IBrushRenderUnderCursor brushRender = new BrushRenderUIGroupUnderCursor(commonUI, "Stamp", brushTexture))
                {
                    if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                    {
                        PaintContext paintContext = brushRender.AcquireHeightmap(true, brushXform.GetBrushXYBounds());

                        ApplyBrushInternal(brushRender, paintContext, commonUI.brushStrength, brushTexture, brushXform, terrain);

                        
                    }
                }
            }
            return true;
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            commonUI.OnSceneGUI2D(terrain, editContext);

            if(!editContext.hitValidTerrain && !commonUI.isInUse)
            {
                return;
            }

            commonUI.OnSceneGUI(terrain, editContext);

            Event evt = Event.current;
            if (evt.control && (evt.type == EventType.ScrollWheel))
            {
                const float k_mouseWheelToHeightRatio = -0.0004f;
                stampToolProperties.m_StampHeight += Event.current.delta.y * k_mouseWheelToHeightRatio * editContext.raycastHit.distance;
                evt.Use();
                editContext.Repaint();
                SaveSetting();
            }

            // We're only doing painting operations, early out if it's not a repaint
            if (evt.type != EventType.Repaint)
            {
                return;
            }

            if (commonUI.isRaycastHitUnderCursorValid)
            {
                Texture brushTexture = editContext.brushTexture;
                
                using(IBrushRenderPreviewUnderCursor brushRender = new BrushRenderPreviewUIGroupUnderCursor(commonUI, "Stamp", brushTexture))
                {
                    if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                    {
                        PaintContext paintContext = brushRender.AcquireHeightmap(false, brushXform.GetBrushXYBounds(), 1);
                        Material material = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();

                        brushRender.RenderBrushPreview(paintContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, brushXform, material, 0);

                        // draw result preview
                        {
                            ApplyBrushInternal(brushRender, paintContext, commonUI.brushStrength, brushTexture, brushXform, terrain);

                            // restore old render target
                            RenderTexture.active = paintContext.oldRenderTexture;

                            material.SetTexture("_HeightmapOrig", paintContext.sourceRenderTexture);

                            brushRender.RenderBrushPreview(paintContext, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture, brushXform, material, 1);
                        }
                        TerrainPaintUtility.ReleaseContextResources(paintContext);
                    }
                }                
            }
        }

        bool m_ShowControls = true;
        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();
            Styles styles = GetStyles();
            commonUI.OnInspectorGUI(terrain, editContext);

            m_ShowControls = TerrainToolGUIHelper.DrawHeaderFoldoutForBrush(styles.controls, m_ShowControls, stampToolProperties.SetDefaults);

            if(!m_ShowControls)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.BeginVertical("GroupBox");
                {
                    float height = Mathf.Abs(stampToolProperties.m_StampHeight);
                    
                    height = EditorGUILayout.Slider(styles.height, height, 0, terrain.terrainData.size.y);
                    stampToolProperties.stampDown = EditorGUILayout.Toggle(styles.down, stampToolProperties.stampDown);
                    stampToolProperties.m_StampHeight = height;
                    stampToolProperties.m_MaxBlendAdd = EditorGUILayout.Slider(styles.maxadd, stampToolProperties.m_MaxBlendAdd, 0.0f, 1.0f);

                }
                EditorGUILayout.EndVertical();
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                SaveSetting();
                Save(true);
                TerrainToolsAnalytics.OnParameterChange();
            }

            base.OnInspectorGUI(terrain, editContext);
        }

        private void SaveSetting()
        {
            string stampToolData = JsonUtility.ToJson(stampToolProperties);
            EditorPrefs.SetString("Unity.TerrainTools.Stamp", stampToolData);
        }

        private void LoadSettings()
        {
            string stampToolData = EditorPrefs.GetString("Unity.TerrainTools.Stamp");
            stampToolProperties.SetDefaults();
            JsonUtility.FromJsonOverwrite(stampToolData, stampToolProperties);
        }

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters() => new TerrainToolsAnalytics.IBrushParameter[]{
            new TerrainToolsAnalytics.BrushParameter<float>{Name = GetStyles().height.text, Value = stampToolProperties.m_StampHeight},
            new TerrainToolsAnalytics.BrushParameter<bool>{Name = GetStyles().down.text, Value = stampToolProperties.stampDown},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = GetStyles().maxadd.text, Value = stampToolProperties.m_MaxBlendAdd},
            };
        #endregion
    }
}
