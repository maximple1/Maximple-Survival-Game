using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;

namespace UnityEditor.Experimental.TerrainAPI
{
    //[FilePathAttribute("Library/TerrainTools/PaintTexture", FilePathAttribute.Location.ProjectFolder)]
    public class PaintTextureTool : TerrainPaintTool<PaintTextureTool>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Paint Texture Tool", typeof(TerrainToolShortcutContext), KeyCode.F2)] // tells shortcut manager what to call the shortcut and what to pass as args
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;              // gets interface to modify state of TerrainTools
            context.SelectPaintTool<PaintTextureTool>();                                                // set active tool
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Paint Texture Tool");
        }

        [ClutchShortcut("Terrain/Layer Eyedropper", KeyCode.A, ShortcutModifiers.Shift)]
        static void LayerShortcut(ShortcutArguments args)
        {
            m_EyedropperSelected = args.stage == ShortcutStage.Begin ? true : false;
            if (m_EyedropperSelected && m_PaintToolSelected)
            {
                Cursor.SetCursor(m_CursorTexture, m_CursorOffset, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                TerrainToolsAnalytics.OnShortcutKeyRelease("Layer Eyedropper");
            }
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
                    m_commonUI = new DefaultBrushUIGroup("PaintTexture", UpdateAnalyticParameters);
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }


        const string toolName = "Paint Texture";
        const int kMaxLayerHD = 8;
        const int kMaxNoLimit = 20;

        [SerializeField]
        TerrainPalette m_SelectedLayerPalette;
        ReorderableList m_LayerList;
        List<Layer> m_PaletteLayers = new List<Layer>();
        Texture2D m_layerTexture;
        Vector2 m_ScrollPos;
        TerrainLayer m_PickedLayer;
        [SerializeField]
        string m_LayerName = "NewLayer";
        int m_objPickerWindowID = -1;
        int m_layerPickerWindowID = -1;
        int m_MaxLayerCount;

        [SerializeField]
        float m_TargetStrength = 1.0f;

        [SerializeField]
        Terrain m_SelectedTerrain = null;
        TerrainLayer m_SelectedTerrainLayer = null;
        [SerializeField]
        bool m_ToggleAllElements = false;

#if UNITY_2019_2_OR_NEWER
        Editor m_TemplateMaterialEditor = null;
        Editor m_SelectedTerrainLayerInspector = null;

        [SerializeField]
        bool m_ShowMaterialEditor = false;
        [SerializeField]
        bool m_ShowLayerProperties = false;
        bool m_LayerRepaintFlag = false;
#endif
#if UNITY_2019_1_OR_NEWER
        static bool m_EyedropperSelected = false;
        static Texture2D m_CursorTexture;
        static Vector2 m_CursorOffset = new Vector2(0, 30);
        static bool m_PaintToolSelected = false;
#endif

        [SerializeField]
        bool m_ShowLayerInspector = false;

        #region Resources
        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("PaintTextureAdvance"));
            return m_Material;
        }

        Material m_BlendMat = null;
        Material GetBlendMaterial()
        {
            if (m_BlendMat == null)
            {
                m_BlendMat = new Material(Shader.Find("Hidden/TerrainTools/BlendModes"));
            }
            return m_BlendMat;
        }
        Material m_BrushPreviewMat = null;
        Material GetBrushPreviewMaterial()
        {
            if (m_BrushPreviewMat == null)
            {
                m_BrushPreviewMat = new Material(Shader.Find("Hidden/TerrainEngine/PaintMaterialBrushPreview"));
            }
            return m_BrushPreviewMat;
        }

        #endregion

        public override string GetName()
        {
            return toolName;
        }

        public override string GetDesc()
        {
            return Styles.description.text;
        }

        public override void OnEnterToolMode()
        {
            base.OnEnterToolMode();
            commonUI.OnEnterToolMode();
#if UNITY_2019_1_OR_NEWER
            m_PaintToolSelected = true;
#endif
        }

        public override void OnExitToolMode()
        {
            base.OnExitToolMode();
            commonUI.OnExitToolMode();
#if UNITY_2019_1_OR_NEWER
            m_PaintToolSelected = false;
#endif
        }

        public override void OnEnable()
        {
            base.OnEnable();
            GetAndSetActiveRenderPipelineSettings();
#if UNITY_2019_1_OR_NEWER
            m_CursorTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.unity.terrain-tools/Editor/Resources/Icons/LayersEyedropper.png", typeof(Texture2D));
#endif
        }

        #region Paint
        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
#if UNITY_2019_1_OR_NEWER
            if (m_EyedropperSelected && m_LayerList != null)
            {
                Texture2D[] splatmaps = terrain.terrainData.alphamapTextures;
                int splatOffset = 0;
                foreach(Texture2D splatmap in splatmaps)
                {
                    Color pixel = splatmap.GetPixelBilinear(editContext.uv.x, editContext.uv.y);
                    if (pixel.r > .5f)
                    {
                        m_SelectedTerrainLayer = terrain.terrainData.terrainLayers[0 + splatOffset];
                        m_LayerList.index = 0 + splatOffset;
                        break;
                    }
                    else if (pixel.g > .5f)
                    {
                        m_SelectedTerrainLayer = terrain.terrainData.terrainLayers[1 + splatOffset];
                        m_LayerList.index = 1 + splatOffset;
                        break;
                    }
                    else if (pixel.b > .5f)
                    {
                        m_SelectedTerrainLayer = terrain.terrainData.terrainLayers[2 + splatOffset];
                        m_LayerList.index = 2 + splatOffset;
                        break;
                    }
                    else if (pixel.a > .5f)
                    {
                        m_SelectedTerrainLayer = terrain.terrainData.terrainLayers[3 + splatOffset];
                        m_LayerList.index = 3 + splatOffset;
                        break;
                    }

                    splatOffset += 4;
                }
                
                return true;
            }
#endif
            commonUI.OnPaint(terrain, editContext);

            if (commonUI.allowPaint)
            {
                Texture brushTexture = editContext.brushTexture;

                using (IBrushRenderUnderCursor brushRender = new BrushRenderUIGroupUnderCursor(commonUI, "PaintTextureTool", brushTexture))
                {
                    if (brushRender.CalculateBrushTransform(out BrushTransform brushTransform))
                    {
                        Rect brushRect = brushTransform.GetBrushXYBounds();
                        PaintContext paintContext = brushRender.AcquireTexture(true, brushRect, m_SelectedTerrainLayer);

                        if (paintContext != null)
                        {
                            PaintContext heightContext = brushRender.AcquireHeightmap(false, brushRect);

                            // Evaluate the brush mask filter stack
                            Material mat = GetPaintMaterial();
                            var brushMask = RTUtils.GetTempHandle(heightContext.sourceRenderTexture.width, heightContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
                            Utility.SetFilterRT(commonUI, heightContext.sourceRenderTexture, brushMask, mat);

                            // apply brush
                            float targetAlpha = m_TargetStrength;
                            float s = commonUI.InvertStrength ? -commonUI.brushStrength : commonUI.brushStrength;
                            Vector4 brushParams = new Vector4(s, targetAlpha, 0.0f, 0.0f);
                            mat.SetTexture("_BrushTex", editContext.brushTexture);
                            mat.SetVector("_BrushParams", brushParams);

                            brushRender.SetupTerrainToolMaterialProperties(paintContext, brushTransform, mat);
                            brushRender.RenderBrush(paintContext, mat, 0);
                            RTUtils.Release(brushMask);
                        }
                    }
                }
            }

            return true;
        }
        #endregion

        #region GUI
        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            commonUI.OnSceneGUI2D(terrain, editContext);
#if UNITY_2019_1_OR_NEWER
            // Don't paint if eyedropper is selected
            if (m_EyedropperSelected)
            {
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, Screen.width, Screen.height), MouseCursor.CustomCursor);
                editContext.Repaint();
                return;
            }
#endif

            // We're only doing painting operations, early out if it's not a repaint
            if (!editContext.hitValidTerrain && !commonUI.isInUse)
            {
                return;
            }

            // update brush UI group
            commonUI.OnSceneGUI(terrain, editContext);

            // Don't render preview if this isn't a repaint. losing performance if we do
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Texture brushTexture = editContext.brushTexture;

            using (IBrushRenderPreviewUnderCursor brushRender = new BrushRenderPreviewUIGroupUnderCursor(commonUI, "PaintTextureTool", brushTexture))
            {
                if (brushRender.CalculateBrushTransform(out BrushTransform brushTransform)) //cute.
                {
                    RenderTexture tmpRT = RenderTexture.active;
                    Rect brushBounds = brushTransform.GetBrushXYBounds();
                    PaintContext heightmapContext = brushRender.AcquireHeightmap(false, brushBounds, 1);
                    Material brushMaterial = GetBrushPreviewMaterial();
                    var defaultPreviewMaterial = Utility.GetDefaultPreviewMaterial();

                    if (commonUI.brushMaskFilterStack.filters.Count > 0)
                    {
                        // Evaluate the brush mask filter stack
                        Vector3 brushPos = new Vector3( commonUI.raycastHitUnderCursor.point.x, 0, commonUI.raycastHitUnderCursor.point.z );
                        var brushMask = RTUtils.GetTempHandle(heightmapContext.sourceRenderTexture.width, heightmapContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
                        commonUI.GetBrushMask(heightmapContext.sourceRenderTexture, brushMask);

                        //Composite the brush texture onto the filter stack result
                        var compRT = RTUtils.GetTempHandle(brushMask.Desc);
                        Material blendMat = GetBlendMaterial();
                        blendMat.SetTexture("_BlendTex", editContext.brushTexture);
                        blendMat.SetVector("_BlendParams", new Vector4(0.0f, 0.0f, -(commonUI.brushRotation * Mathf.Deg2Rad), 0.0f));
                        TerrainPaintUtility.SetupTerrainToolMaterialProperties(heightmapContext, brushTransform, blendMat);
                        Graphics.Blit(brushMask, compRT, blendMat, 0);

                        RenderTexture.active = tmpRT;

                        BrushTransform identityBrushTransform = TerrainPaintUtility.CalculateBrushTransform(commonUI.terrainUnderCursor, commonUI.raycastHitUnderCursor.textureCoord, commonUI.brushSize, 0.0f);
                        
                        var texelCtx = Utility.CollectTexelValidity(heightmapContext.originTerrain, identityBrushTransform.GetBrushXYBounds());
                        Utility.SetupMaterialForPaintingWithTexelValidityContext(heightmapContext, texelCtx, identityBrushTransform, brushMaterial);
                        Utility.SetupMaterialForPaintingWithTexelValidityContext(heightmapContext, texelCtx, identityBrushTransform, defaultPreviewMaterial);
                        TerrainPaintUtilityEditor.DrawBrushPreview(heightmapContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, compRT, identityBrushTransform, brushMaterial, 0);
                        RTUtils.Release(compRT);
                        RTUtils.Release(brushMask);
                        texelCtx.Cleanup();
                    }

                    brushRender.RenderBrushPreview(heightmapContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, brushTransform, defaultPreviewMaterial, 0);
                }
            }
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();

            commonUI.OnInspectorGUI(terrain, editContext);

            m_TargetStrength = EditorGUILayout.Slider(Styles.targetStrengthTxt, m_TargetStrength, 0.0f, 1.0f);

#if UNITY_2019_2_OR_NEWER
            // Material GUI
            m_ShowMaterialEditor = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.materialControls, m_ShowMaterialEditor);
            if (m_ShowMaterialEditor)
            {
                Editor.DrawFoldoutInspector(terrain.materialTemplate, ref m_TemplateMaterialEditor);
                EditorGUILayout.Space();
            }
#endif
            // Layers GUI
            UpdateLayerPalette(terrain);
            m_ShowLayerInspector = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.layerControls, m_ShowLayerInspector);
            if (m_ShowLayerInspector)
            {
                LayersGUI();

#if UNITY_2019_2_OR_NEWER
                m_ShowLayerProperties = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.layerProperties, m_ShowLayerProperties);
                if (m_ShowLayerProperties)
                {
                    if(!m_LayerRepaintFlag)
                    {
                        TerrainLayerUtility.ShowTerrainLayerGUI(terrain, m_SelectedTerrainLayer, ref m_SelectedTerrainLayerInspector,
                        (m_TemplateMaterialEditor as MaterialEditor)?.customShaderGUI as ITerrainLayerCustomUI);
                    }
                    else
                    {
                        m_LayerRepaintFlag = false; // flag to skip layer property repaint when layer list modified
                    }
                }
#endif
            }
            m_SelectedTerrain = terrain;

            if (EditorGUI.EndChangeCheck())
            {
                TerrainToolsAnalytics.OnParameterChange();
            }
        }
        #endregion

        private static class Styles
        {
            public static readonly GUIContent description = EditorGUIUtility.TrTextContent("Add material layers to use on the terrain. Left click to paint the selected material layer onto the terrain. \n\nHold Shift and A to pick a material layer from the terrain.");
#if UNITY_2019_2_OR_NEWER
            public static readonly GUIContent materialControls = EditorGUIUtility.TrTextContent("Material");
            public static readonly GUIContent layerProperties = EditorGUIUtility.TrTextContent("Layer Properties");
#endif
            public static readonly GUIContent layerControls = EditorGUIUtility.TrTextContent("Layers");			
            public static readonly GUIContent PalettePreset = EditorGUIUtility.TrTextContent("Layer Palette Profile", "Select an existing layer palette asset or create a new palette asset from the layer list.");
            public static readonly GUIContent NewLayer = EditorGUIUtility.TrTextContent("Create New Layer", "Create a new layer with provided name and add to the layer palette.");
            public static readonly GUIContent CreateLayersBtn = EditorGUIUtility.TrTextContent("Create...", "Create a new layer.");
            public static readonly GUIContent SavePaletteBtn = EditorGUIUtility.TrTextContent("Save", "Save the current layer list into selected palette asset file on disk.");
            public static readonly GUIContent SaveAsPaletteBtn = EditorGUIUtility.TrTextContent("Save As", "Save the current palette asset as a new file on disk.");
            public static readonly GUIContent RefreshPaletteBtn = EditorGUIUtility.TrTextContent("Refresh", "Load selected palette and apply to the layer list.");
            public static readonly GUIContent RemoveLayersBtn = EditorGUIUtility.TrTextContent("Remove Selected Layers", "Removes layers that are selected within the Layer Palette.");
            public static readonly GUIContent targetStrengthTxt = EditorGUIUtility.TrTextContent("Target Strength", "Maximum opacity this brush will paint to.");
            public static readonly string LayersWarning = "The selected terrain doesn't contain any layers. Add layer(s) to paint on the terrain.";
        }

        // layer list view
        const int kElementHeight = 70;
        const int kElementObjectFieldHeight = 16;
        const int kElementPadding = 2;
        const int kElementObjectFieldWidth = 240;
        const int kElementToggleWidth = 20;
        const int kElementImageWidth = 64;
        const int kElementImageHeight = 64;

        void LayersGUI()
        {
            if (m_SelectedTerrain != null && m_SelectedTerrain.terrainData.terrainLayers.Length == 0)
            {
                EditorGUILayout.HelpBox(Styles.LayersWarning, MessageType.Warning);
            }

            // Layer Palette preset
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.PalettePreset, GUILayout.Width(EditorGUIUtility.labelWidth - 4.5f));
            EditorGUI.BeginChangeCheck();
            m_SelectedLayerPalette = (TerrainPalette)EditorGUILayout.ObjectField(m_SelectedLayerPalette, typeof(TerrainPalette), false, GUILayout.MinWidth(130));
            if (EditorGUI.EndChangeCheck() && m_SelectedLayerPalette != null)
            {
                if (EditorUtility.DisplayDialog("Confirm", "Load palette from selected?", "OK", "Cancel"))
                {
                    LoadPalette();
                }
            }
            if (GUILayout.Button(Styles.SavePaletteBtn))
            {
                if (GetPalette())
                {
                    m_SelectedLayerPalette.PaletteLayers.Clear();
                    foreach (var layer in m_PaletteLayers)
                    {
                        m_SelectedLayerPalette.PaletteLayers.Add(layer.AssignedLayer);
                    }
                    AssetDatabase.SaveAssets();
                }
            }
            if (GUILayout.Button(Styles.SaveAsPaletteBtn))
            {
                CreateNewPalette();
            }
            if (GUILayout.Button(Styles.RefreshPaletteBtn))
            {
                if (GetPalette())
                {
                    LoadPalette();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Reorderable list view			
            EditorGUILayout.BeginVertical("Box");
            if (m_LayerList == null)
            {
                m_LayerList = new ReorderableList(m_PaletteLayers, typeof(Layer), true, true, false, false);
                m_LayerList.elementHeight = kElementHeight;
                m_LayerList.drawHeaderCallback = DrawHeader;
                m_LayerList.drawElementCallback = DrawLayerElement;
                m_LayerList.onSelectCallback = OnSelectLayerElement;
                m_LayerList.onReorderCallbackWithDetails = OnReorderLayerElement;
            }
            
            CreateLayersIfNeeded();
            m_LayerList.DoLayoutList();

            // Layer creation
            if (Event.current.commandName == "ObjectSelectorClosed" && 
                    EditorGUIUtility.GetObjectPickerControlID() == m_layerPickerWindowID)
            {
                m_PickedLayer = (TerrainLayer)EditorGUIUtility.GetObjectPickerObject();				
            }

            if (m_PickedLayer != null && Event.current.type == EventType.Repaint)
            {
                TerrainLayer tempLayer = m_PickedLayer;
                m_PickedLayer = null;
                if (!LayerExists(tempLayer))
                {
                    AddLayerElement(tempLayer);
                }								
            }

            if (Event.current.commandName == "ObjectSelectorClosed" &&
                    EditorGUIUtility.GetObjectPickerControlID() == m_objPickerWindowID)
            {
                m_layerTexture = (Texture2D)EditorGUIUtility.GetObjectPickerObject();
            }

            if (m_layerTexture != null && Event.current.type == EventType.Repaint)
            {
                Texture2D tempTexture = m_layerTexture;
                m_layerTexture = null;
                CreateNewLayerWithTexture(tempTexture);				
            }

            // Control buttons
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!CanAddLayerElement());
            if (GUILayout.Button("Add Layer", GUILayout.Height(20)))
            {
                m_layerPickerWindowID = EditorGUIUtility.GetControlID(FocusType.Passive) + 200; // had to bump this to make it unique
                EditorGUIUtility.ShowObjectPicker<TerrainLayer>(null, false, "", m_layerPickerWindowID);
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Remove Layer", GUILayout.Height(20)) &&
                EditorUtility.DisplayDialog("Warning", "Splatmap data changed by this layer will be lost.", "OK", "Cancel"))
            {
                RemoveLayerElement();
            }
            if (GUILayout.Button(Styles.RemoveLayersBtn, GUILayout.Height(20)) &&
                EditorUtility.DisplayDialog("Error", "Splatmap data changed by these layers will be lost.", "OK", "Cancel"))
            {
                RemoveSelectedLayerElements();
            }
            EditorGUILayout.EndHorizontal();

            // Create new layer
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            m_LayerName = EditorGUILayout.TextField(Styles.NewLayer, m_LayerName);
            if (GUILayout.Button(Styles.CreateLayersBtn, GUILayout.Width(85), GUILayout.Height(20)))
            {
                m_objPickerWindowID = EditorGUIUtility.GetControlID(FocusType.Passive) + 100; // had to bump this to make it unique
                EditorGUIUtility.ShowObjectPicker<Texture2D>(null, false, "", m_objPickerWindowID);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private void CreateLayersIfNeeded()
        {
            if( m_SelectedTerrain == null )
            {
                return;
            }
            
            for( int i = 0; i < m_PaletteLayers.Count && i < m_SelectedTerrain.terrainData.terrainLayers.Length; ++i )
            {
                if( m_PaletteLayers[ i ] == null )
                {
                    m_PaletteLayers[ i ] = ScriptableObject.CreateInstance< Layer >();
                    m_PaletteLayers[ i ].AssignedLayer = m_SelectedTerrain.terrainData.terrainLayers[ i ];
                }
            }
        }

        void DrawHeader(Rect rect)
        {
            var rectToggle = new Rect(rect.x + 16, rect.y, rect.width, rect.height);
            EditorGUI.BeginChangeCheck();
            m_ToggleAllElements = EditorGUI.Toggle(rectToggle, m_ToggleAllElements);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var layerElement in m_PaletteLayers)
                {
                    layerElement.IsSelected = m_ToggleAllElements;
                }
            }
            var rectLabel = new Rect(rectToggle.x + kElementToggleWidth + kElementPadding, rect.y, kElementObjectFieldWidth, kElementToggleWidth);
            EditorGUI.LabelField(rectLabel, "Layer Palette", EditorStyles.boldLabel);			
        }

        void DrawLayerElement(Rect rect, int index, bool selected, bool focused)
        {
            rect.y = rect.y + kElementPadding;
            var rectButton= new Rect((rect.x + kElementPadding), rect.y, kElementToggleWidth, kElementToggleWidth);
            var rectImage = new Rect((rectButton.x + kElementToggleWidth), rect.y, kElementImageWidth, kElementImageHeight);
            var rectObject = new Rect((rectImage.x + kElementImageWidth + 10), rect.y, kElementObjectFieldWidth, kElementObjectFieldHeight);

            if (m_PaletteLayers.Count > 0 && m_PaletteLayers.ElementAtOrDefault(index) != null)
            {
                m_PaletteLayers[index].IsSelected = EditorGUI.Toggle(rectButton, m_PaletteLayers[index].IsSelected);
                                
                EditorGUILayout.BeginHorizontal();
                List<TerrainLayer> existLayers = m_PaletteLayers.Select(l => l.AssignedLayer).ToList();
                TerrainLayer oldLayer = m_PaletteLayers[index].AssignedLayer;
                Texture2D icon = null;
                if (m_PaletteLayers[index].AssignedLayer != null)
                {
                    icon = AssetPreview.GetAssetPreview(m_PaletteLayers[index].AssignedLayer.diffuseTexture);
                }
                GUI.Box(rectImage, icon);
                EditorGUI.BeginChangeCheck();
                m_PaletteLayers[index].AssignedLayer = EditorGUI.ObjectField(rectObject, m_PaletteLayers[index].AssignedLayer, typeof(TerrainLayer), false) as TerrainLayer;
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    if(m_PaletteLayers[index].AssignedLayer == null)
                    {
                        m_PaletteLayers.RemoveAt(index);
                        TerrainToolboxLayer.RemoveLayerFromTerrain(m_SelectedTerrain.terrainData, index);
                        m_SelectedTerrainLayer = null;
                    }
                    else if (existLayers.Contains(m_PaletteLayers[index].AssignedLayer) && m_PaletteLayers[index].AssignedLayer != oldLayer)
                    {
                        EditorUtility.DisplayDialog("Error", "Layer exists. Please select a different layer.", "OK");
                        m_PaletteLayers[index].AssignedLayer = oldLayer;
                    }
                    else
                    {
                        if (m_SelectedTerrain.terrainData.terrainLayers.Length < m_PaletteLayers.Count)
                        {
                            TerrainToolboxLayer.AddLayerToTerrain(m_SelectedTerrain.terrainData, m_PaletteLayers[index].AssignedLayer);
                        }
                        else
                        {
                            int layersLength = m_SelectedTerrain.terrainData.terrainLayers.Length;
                            TerrainLayer[] layers = m_SelectedTerrain.terrainData.terrainLayers;
                            layers[index] = m_PaletteLayers[index].AssignedLayer;
                            m_SelectedTerrainLayer = m_PaletteLayers[index].AssignedLayer;
                            m_SelectedTerrain.terrainData.terrainLayers = layers;
                        }
                    }
                }
            }
        }
        
        void AddLayerElement(TerrainLayer layer)
        {
            if (LayerExists(layer))
                return;

            Layer newLayer = CreateInstance<Layer>();
            newLayer.AssignedLayer = layer;
            newLayer.IsSelected = m_ToggleAllElements;
            m_PaletteLayers.Add(newLayer);
            TerrainToolboxLayer.AddLayerToTerrain(m_SelectedTerrain.terrainData, newLayer.AssignedLayer);
            m_SelectedTerrainLayer = newLayer.AssignedLayer;
            m_LayerList.index = m_PaletteLayers.Count - 1;
#if UNITY_2019_2_OR_NEWER
            m_LayerRepaintFlag = true;
#endif
        }

        bool LayerExists(TerrainLayer layer)
        {
            List<TerrainLayer> existLayers = m_PaletteLayers.Select(l => l.AssignedLayer).ToList();
            if (existLayers.Count > 0 && existLayers.Contains(layer))
            {
                EditorUtility.DisplayDialog("Error", "Layer exists. Please select a different layer.", "OK");
                return true;
            }
            else
            {
                return false;
            }
        }

        void CreateNewLayerWithTexture(Texture2D texture)
        {
            Layer newLayer = ScriptableObject.CreateInstance<Layer>();
            newLayer.AssignedLayer = new TerrainLayer();
            newLayer.AssignedLayer.diffuseTexture = texture;
            m_PaletteLayers.Add(newLayer);
            m_LayerList.index = m_PaletteLayers.Count - 1;

            string path = "Assets";
            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }

            newLayer.AssignedLayer.name = m_LayerName;
            TerrainToolboxLayer.AddLayerToTerrain(m_SelectedTerrain.terrainData, newLayer.AssignedLayer);
            AssetDatabase.CreateAsset(newLayer.AssignedLayer, AssetDatabase.GenerateUniqueAssetPath($"{path}/{m_LayerName}.terrainlayer"));
        }

        void RemoveLayerElement()
        {
            if (m_PaletteLayers.ElementAtOrDefault(m_LayerList.index) == null)
            {
                return;
            }

            m_PaletteLayers.RemoveAt(m_LayerList.index);

            if (m_SelectedTerrain.terrainData.terrainLayers.Length > m_LayerList.index)
            {
                TerrainToolboxLayer.RemoveLayerFromTerrain(m_SelectedTerrain.terrainData, m_LayerList.index);
            }

            m_LayerList.index = m_PaletteLayers.Count - 1;
            if (m_LayerList.index >= 0 && m_LayerList.index < m_PaletteLayers.Count)
            {
                m_SelectedTerrainLayer = m_PaletteLayers[m_LayerList.index].AssignedLayer;
            }
            else
            {
                m_SelectedTerrainLayer = null;
            }
        }

        void RemoveSelectedLayerElements()
        {
            for (int i = m_PaletteLayers.Count - 1; i >= 0; i--)
            {
                if (m_PaletteLayers[i].IsSelected)
                {
                    if (m_PaletteLayers[i].AssignedLayer != null)
                    {
                        TerrainToolboxLayer.RemoveLayerFromTerrain(m_SelectedTerrain.terrainData, i);
                    }
                    m_PaletteLayers.RemoveAt(i);
                }
            }
            m_SelectedTerrainLayer = null;
            AssetDatabase.Refresh();
        }

        void OnSelectLayerElement(ReorderableList list)
        {
            if(m_SelectedTerrain.terrainData.terrainLayers.Length > list.index)
            {
                m_SelectedTerrainLayer = m_SelectedTerrain.terrainData.terrainLayers[list.index];
            }
        }

        void OnReorderLayerElement(ReorderableList list, int oldIndex, int newIndex)
        {
            
            TerrainLayer[] layers = m_SelectedTerrain.terrainData.terrainLayers;

            if(layers[oldIndex] != null)
            {
                TerrainLayer temp = layers[oldIndex];
                layers[oldIndex] = layers[newIndex];
                layers[newIndex] = temp;
                for (int i = 0; i < m_PaletteLayers.Count; i++)
                {
                    layers[i] = m_PaletteLayers[i].AssignedLayer;
                }
                m_SelectedTerrain.terrainData.SetTerrainLayersRegisterUndo(layers, "Reorder Terrain Layers");
            }
        }

        bool CanAddLayerElement()
        {
            return m_PaletteLayers.Count < m_MaxLayerCount;
        }

        void UpdateLayerPalette(Terrain terrain)
        {
            if (terrain == null || m_LayerList == null)
            {
                return;
            }

            bool[] selectedList = new bool[m_PaletteLayers.Count];
            for(int i = 0; i < m_PaletteLayers.Count; i++)
            {
                selectedList[i] = m_PaletteLayers[i].IsSelected;
            }

            m_PaletteLayers.Clear();
            m_LayerList.index = -1;
            List<TerrainLayer> terrainLayers = new List<TerrainLayer>();

            int index = 0;
            foreach (TerrainLayer layer in terrain.terrainData.terrainLayers)
            {
                if(layer != null)
                {
                    Layer paletteLayer = ScriptableObject.CreateInstance<Layer>();
                    paletteLayer.AssignedLayer = layer;
                    paletteLayer.IsSelected = selectedList.ElementAtOrDefault(index);
                    m_PaletteLayers.Add(paletteLayer);
                    terrainLayers.Add(layer);
                    if (layer == m_SelectedTerrainLayer)
                        m_LayerList.index = index;
                    index++;
                }
            }

            if(m_LayerList.index == -1)
            {
                m_SelectedTerrainLayer = null;
            }
            terrain.terrainData.terrainLayers = terrainLayers.ToArray();
        }

        bool GetPalette()
        {
            if (m_SelectedLayerPalette == null)
            {
                if (EditorUtility.DisplayDialog("Error", "No layer palette found, create a new one?", "OK", "Cancel"))
                {
                    return CreateNewPalette();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        void LoadPalette()
        {
            if (!GetPalette())
                return;

            m_PaletteLayers.Clear();
            List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
            foreach (var layer in m_SelectedLayerPalette.PaletteLayers)
            {
                if(layer != null)
                {
                    Layer newLayer = ScriptableObject.CreateInstance<Layer>();
                    newLayer.AssignedLayer = layer;
                    m_PaletteLayers.Add(newLayer);
                    terrainLayers.Add(layer);
                }
            }
            m_SelectedTerrain.terrainData.SetTerrainLayersRegisterUndo(terrainLayers.ToArray(), "Load Palette");
        }

        bool CreateNewPalette()
        {
            string filePath = EditorUtility.SaveFilePanelInProject("Create New Palette", "New Layer Palette.asset", "asset", "");
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }
            m_SelectedLayerPalette = CreateInstance<TerrainPalette>();
            foreach (var layer in m_PaletteLayers)
            {
                m_SelectedLayerPalette.PaletteLayers.Add(layer.AssignedLayer);
            }
            AssetDatabase.CreateAsset(m_SelectedLayerPalette, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return true;
        }

        void GetAndSetActiveRenderPipelineSettings()
        {
            ToolboxHelper.RenderPipeline m_ActiveRenderPipeline = ToolboxHelper.GetRenderPipeline();
            switch (m_ActiveRenderPipeline)
            {
                case ToolboxHelper.RenderPipeline.HD:
                    m_MaxLayerCount = kMaxLayerHD;
                    break;
                case ToolboxHelper.RenderPipeline.LW:
                    m_MaxLayerCount = kMaxNoLimit;
                    break;
                default:
                    m_MaxLayerCount = kMaxNoLimit;
                    break;
            }
        }

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters() => new TerrainToolsAnalytics.IBrushParameter[]{
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.targetStrengthTxt.text, Value = m_TargetStrength},
            new TerrainToolsAnalytics.BrushParameter<int>{Name = "Layers Count", Value = m_PaletteLayers.Count},
            };
        #endregion
    }
}
