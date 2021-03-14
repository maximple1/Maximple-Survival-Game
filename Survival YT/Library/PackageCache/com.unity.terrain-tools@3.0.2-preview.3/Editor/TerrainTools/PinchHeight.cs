using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class PinchHeightTool : TerrainPaintTool<PinchHeightTool>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Pinch Tool", typeof(TerrainToolShortcutContext))]                     // tells shortcut manager what to call the shortcut and what to pass as args
        static void SelectShortcut(ShortcutArguments args) {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;              // gets interface to modify state of TerrainTools
            context.SelectPaintTool<PinchHeightTool>();                                                  // set active tool
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Pinch Tool");
        }
#endif

        private bool m_ShowControls = true;

        [SerializeField]
        IBrushUIGroup m_commonUI;
        private IBrushUIGroup commonUI
        {
            get
            {
                if( m_commonUI == null )
                {
                    LoadSettings();
                    m_commonUI = new DefaultBrushUIGroup( "PinchTool" );
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }

        [SerializeField]
        float m_PinchAmount = 5.0f;

        [SerializeField]
        bool m_AffectMaterials = true;
        [SerializeField]
        bool m_AffectHeight = true;

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("Hidden/TerrainTools/PinchHeight"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Transform/Pinch";
        }

        public override string GetDesc()
        {
            return "Click to Pinch the terrain height. Click plus control to bulge.";
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

            Texture brushTexture = editContext.brushTexture;
            
            using(IBrushRenderPreviewUnderCursor brushRender = new BrushRenderPreviewUIGroupUnderCursor(commonUI, "PinchHeight", brushTexture))
            {
                if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                {
                    Material material = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();
                    PaintContext ctx = brushRender.AcquireHeightmap(false, brushXform.GetBrushXYBounds(), 1);
                
                    brushRender.RenderBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, brushXform, material, 0);

                    // draw result preview
                    {
                        float finalPinchAmount = m_PinchAmount * 0.005f; //scale to a reasonable value and negate so default mode is clockwise
                        if (Event.current.shift) {
                            finalPinchAmount *= -1.0f;
                        }

                        ApplyBrushInternal(brushRender, ctx, commonUI.brushStrength, finalPinchAmount, brushTexture, brushXform);

                        // restore old render target
                        RenderTexture.active = ctx.oldRenderTexture;

                        material.SetTexture("_HeightmapOrig", ctx.sourceRenderTexture);
                        brushRender.RenderBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture, brushXform, material, 1);
                    }
                }
            }
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();

            commonUI.OnInspectorGUI(terrain, editContext);

            m_ShowControls = TerrainToolGUIHelper.DrawHeaderFoldoutForBrush(Styles.controls, m_ShowControls, Reset);

            if(m_ShowControls)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PrefixLabel(Styles.targets);
                        m_AffectMaterials = GUILayout.Toggle(m_AffectMaterials, Styles.materials, GUI.skin.button);
                        m_AffectHeight = GUILayout.Toggle(m_AffectHeight, Styles.heightmap, GUI.skin.button);
                    }
                    EditorGUILayout.EndHorizontal();

                    m_PinchAmount = EditorGUILayout.Slider(Styles.pinchAmount, m_PinchAmount, -100.0f, 100.0f);
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

        private void Reset()
        {
            m_PinchAmount = 5.0f;
            m_AffectMaterials = true;
            m_AffectHeight = true;
        }

        public void ApplyBrushInternal(IPaintContextRender renderer, PaintContext paintContext, float brushStrength, float pinchAmount, Texture brushTexture, BrushTransform brushXform) 
        {
            Material mat = GetPaintMaterial();

            pinchAmount = Event.current.control ? -pinchAmount : pinchAmount; //TODO - use shortcut system once it supports binding modifiers

            Vector4 brushParams = new Vector4(brushStrength, 0.0f, pinchAmount, Mathf.Deg2Rad * commonUI.brushRotation);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            
            renderer.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
            renderer.RenderBrush(paintContext, mat, 0);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            commonUI.OnPaint(terrain, editContext);

            if(!commonUI.allowPaint) { return true; }

            using(IBrushRenderUnderCursor brushRender = new BrushRenderUIGroupUnderCursor(commonUI, "PinchHeight", editContext.brushTexture))
            {
                if(brushRender.CalculateBrushTransform(out BrushTransform brushXform))
                {
                    float finalPinchAmount = m_PinchAmount * 0.005f; //scale to a reasonable value and negate so default mode is clockwise
                    if (Event.current.shift) {
                        finalPinchAmount *= -1.0f;
                    }

                    Material mat = GetPaintMaterial();

                    //smudge splat map
                    if (m_AffectMaterials)
                    {
                        for (int i = 0; i < terrain.terrainData.terrainLayers.Length; i++)
                        {
                            TerrainLayer layer = terrain.terrainData.terrainLayers[i];
                            PaintContext paintContext = brushRender.AcquireTexture(true, brushXform.GetBrushXYBounds(), layer);
                            var brushMask = RTUtils.GetTempHandle(paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
                            Utility.SetFilterRT(commonUI, paintContext.sourceRenderTexture, brushMask, mat);

                            paintContext.sourceRenderTexture.filterMode = FilterMode.Bilinear;

                            brushRender.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
                            brushRender.RenderBrush(paintContext, mat, 0);
                            brushRender.Release(paintContext);
                            RTUtils.Release(brushMask);
                        }
                    }

                    if (m_AffectHeight) {
                        PaintContext paintContext = brushRender.AcquireHeightmap(true, brushXform.GetBrushXYBounds(), 1);
                        var brushMask = RTUtils.GetTempHandle(paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
                        Utility.SetFilterRT(commonUI, paintContext.sourceRenderTexture, brushMask, mat);

                        paintContext.sourceRenderTexture.filterMode = FilterMode.Bilinear;

                        ApplyBrushInternal(brushRender, paintContext, commonUI.brushStrength, finalPinchAmount, editContext.brushTexture, brushXform);
                        brushRender.Release(paintContext);
                        RTUtils.Release(brushMask);
                    }
                }
            }
            return false;
        }

        private static class Styles
        {
            public static readonly GUIContent controls = EditorGUIUtility.TrTextContent("Pinch Height Controls");
            public static readonly GUIContent pinchAmount = EditorGUIUtility.TrTextContent("Pinch Amount", "Negative values bulge, positive values pinch");
            public static readonly GUIContent targets = EditorGUIUtility.TrTextContent("Targets", "Determines which textures the pinch operations target");
            public static readonly GUIContent materials = EditorGUIUtility.TrTextContent("Materials");
            public static readonly GUIContent heightmap = EditorGUIUtility.TrTextContent("Heightmap");
        }
        
        private void SaveSetting()
        {
            EditorPrefs.SetFloat("Unity.TerrainTools.Pinch.PinchAmount", m_PinchAmount);
            EditorPrefs.SetBool("Unity.TerrainTools.Pinch.Heightmap", m_AffectHeight);
            EditorPrefs.SetBool("Unity.TerrainTools.Pinch.Materials", m_AffectMaterials);
        }

        private void LoadSettings()
        {
            m_PinchAmount = EditorPrefs.GetFloat("Unity.TerrainTools.Pinch.PinchAmount", 5.0f);
            m_AffectHeight = EditorPrefs.GetBool("Unity.TerrainTools.Pinch.Heightmap", true);
            m_AffectMaterials = EditorPrefs.GetBool("Unity.TerrainTools.Pinch.Materials", true);
        }

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters() => new TerrainToolsAnalytics.IBrushParameter[]{
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.pinchAmount.text, Value = m_PinchAmount},
            new TerrainToolsAnalytics.BrushParameter<bool>{Name = Styles.materials.text, Value = m_AffectHeight},
            new TerrainToolsAnalytics.BrushParameter<bool>{Name = Styles.heightmap.text, Value = m_AffectMaterials},
        
        };
        #endregion
    }
}
