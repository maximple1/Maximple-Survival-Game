using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class CloneBrushTool : TerrainPaintTool<CloneBrushTool>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Clone Tool", typeof(TerrainToolShortcutContext))]                     // tells shortcut manager what to call the shortcut and what to pass as args
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;              // gets interface to modify state of TerrainTools
            context.SelectPaintTool<CloneBrushTool>();                                                  // set active tool
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Clone Tool");
        }
#endif

        private bool m_ShowControls = true;

        private enum ShaderPasses
        {
            CloneAlphamap = 0,
            CloneHeightmap
        }

        public enum MovementBehavior
        {
            FollowAlways = 0,   // clone location will move with the brush always
            Snap,               // clone snaps back to set sample location on mouse up
            FollowOnPaint,      // clone location will move with the brush only when painting
            Fixed,              // clone wont move at all and will sample same location always
        }

        [System.Serializable]
        struct BrushLocationData
        {
            public Terrain terrain;
            public Vector3 pos;

            public void Set(Terrain terrain, Vector3 pos)
            {
                this.terrain = terrain;
                this.pos = pos;
            }
        }

        [System.Serializable]
        class CloneToolSerializedProperties
        {
            public MovementBehavior m_MovementBehavior;
            public bool m_PaintHeightmap;
            public bool m_PaintAlphamap;
            public float m_StampingOffsetFromClone;

            public void SetDefaults()
            {
                m_MovementBehavior = MovementBehavior.FollowAlways;
                m_PaintHeightmap = true;
                m_PaintAlphamap = true;
                m_StampingOffsetFromClone = 0.0f;
            }
        }

        CloneToolSerializedProperties cloneToolProperties = new CloneToolSerializedProperties();


        [SerializeField]
        IBrushUIGroup m_commonUI;
        private IBrushUIGroup commonUI
        {
            get
            {
                if( m_commonUI == null )
                {
                    LoadSettings();
                    m_commonUI = new DefaultBrushUIGroup("CloneBrushTool", UpdateAnalyticParameters);
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }


        // variables for keeping track of mouse and key presses and painting states
        private bool m_lmb;
        private bool m_ctrl;
        private bool m_wasPainting;
        private bool m_isPainting;
        private bool m_HasDoneFirstPaint;

        // The current brush location data we are sampling/cloning from
        private BrushLocationData m_SampleLocation;
        // Brush location defined when user ctrl-clicks. Where the sample location should
        // "snap" back to when the user is not painting and clone behavior == Snap
        [SerializeField] private BrushLocationData m_SnapbackLocation;
        // brush location data used for determining how much the user brush moved in a frame
        private BrushLocationData m_PrevBrushLocation;

        private static Material m_Material = null;
        private static Material GetPaintMaterial()
        {
            if (m_Material == null)
            {
                m_Material = new Material(Shader.Find("Hidden/TerrainTools/CloneBrush"));
            }

            return m_Material;
        }

        public override string GetName()
        {
            return "Sculpt/Clone";
        }

        public override string GetDesc()
        {
            return Styles.descriptionString;
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
            EditorGUI.BeginChangeCheck();

            commonUI.OnInspectorGUI(terrain, editContext);

            m_ShowControls = TerrainToolGUIHelper.DrawHeaderFoldoutForBrush(Styles.controlHeader, m_ShowControls, cloneToolProperties.SetDefaults);

            if(m_ShowControls)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                {
                    // draw button-like toggles for choosing which terrain textures to sample
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PrefixLabel(Styles.cloneSourceContent);

                        cloneToolProperties.m_PaintAlphamap = GUILayout.Toggle(cloneToolProperties.m_PaintAlphamap, Styles.cloneTextureContent, GUI.skin.button);
                        cloneToolProperties.m_PaintHeightmap = GUILayout.Toggle(cloneToolProperties.m_PaintHeightmap, Styles.cloneHeightmapContent, GUI.skin.button);
                    }
                    EditorGUILayout.EndHorizontal();

                    cloneToolProperties.m_MovementBehavior = (MovementBehavior)EditorGUILayout.EnumPopup(Styles.cloneBehaviorContent, cloneToolProperties.m_MovementBehavior);
                    cloneToolProperties.m_StampingOffsetFromClone = EditorGUILayout.Slider(Styles.heightOffsetContent, cloneToolProperties.m_StampingOffsetFromClone,
                                                                    -terrain.terrainData.size.y, terrain.terrainData.size.y);
                }
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                // intentionally do not reset HasDoneFirstPaint here because then changing the brush mask will corrupt the clone position
                m_isPainting = false;
                Save(true);
                SaveSetting();
                TerrainToolsAnalytics.OnParameterChange();
            }
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

            ProcessInput(terrain, editContext);

            if(!commonUI.isInUse)
            {
                UpdateBrushLocations(terrain, editContext);
            }

            // dont render preview if this isnt a repaint. losing performance if we do
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            DrawBrushPreviews(terrain, editContext);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            commonUI.OnPaint(terrain, editContext);

            if (!m_isPainting || m_SampleLocation.terrain == null)
                return true;

            if (commonUI.allowPaint) {
                Vector2 uv = editContext.uv;

                if(commonUI.ScatterBrushStamp(ref terrain, ref uv)) {
                    // grab brush transforms for the sample location (where we are cloning from)
                    // and target location (where we are cloning to)
                    Vector2 sampleUV = TerrainUVFromBrushLocation(m_SampleLocation.terrain, m_SampleLocation.pos);
                    BrushTransform sampleBrushXform = TerrainPaintUtility.CalculateBrushTransform(m_SampleLocation.terrain, sampleUV, commonUI.brushSize, commonUI.brushRotation);
                    BrushTransform targetBrushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, uv, commonUI.brushSize, commonUI.brushRotation);

                    // set material props that will be used for both heightmap and alphamap painting
                    Material mat = GetPaintMaterial();
                    Vector4 brushParams = new Vector4(commonUI.brushStrength, cloneToolProperties.m_StampingOffsetFromClone * 0.5f, terrain.terrainData.size.y, 0f);
                    mat.SetTexture("_BrushTex", editContext.brushTexture);
                    mat.SetVector("_BrushParams", brushParams);

                    // apply texture modifications to terrain
                    if (cloneToolProperties.m_PaintAlphamap) PaintAlphamap(m_SampleLocation.terrain, terrain, sampleBrushXform, targetBrushXform, mat);
                    if (cloneToolProperties.m_PaintHeightmap) PaintHeightmap(m_SampleLocation.terrain, terrain, sampleBrushXform, targetBrushXform, editContext, mat);
                }
            }

            return false;
        }

        private void ProcessInput(Terrain terrain, IOnSceneGUI editContext)
        {
            // update Left Mouse Button state
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && commonUI.isRaycastHitUnderCursorValid)
                m_lmb = true;
            else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                m_lmb = false;
            
            if(!m_isPainting)
            {
                m_ctrl = Event.current.control;
            }
            
            m_wasPainting = m_isPainting;

            // xxx(jcowles): this logic is no good becuse it makes assumptions about what modifier keys will enable/disable painting,
            // however this cannot be known without querying other systems (e.g. orbiting the camera uses some combination of mouse
            // buttons and modifier keys, but the exact configuration is a user setting). For now, assume when ALT is pressed, we are 
            // not painting.
            m_isPainting = m_lmb && !m_ctrl && !Event.current.alt;
        }

        private void UpdateBrushLocations(Terrain terrain, IOnSceneGUI editContext)
        {
            if (!commonUI.isRaycastHitUnderCursorValid)
            {
                return;
            }

            if (!m_isPainting)
            {
                // check to see if the user is selecting a new location for the clone sample
                // and set the current sample location to that as well as the snap back location
                if(m_lmb && m_ctrl)
                {
                    m_HasDoneFirstPaint = false;
                    m_SampleLocation.Set(terrain, editContext.raycastHit.point);
                    m_SnapbackLocation.Set(terrain, editContext.raycastHit.point);
                }
                
                // snap the sample location back to the user-picked sample position
                if (cloneToolProperties.m_MovementBehavior == MovementBehavior.Snap)
                {
                    m_SampleLocation.Set(m_SnapbackLocation.terrain, m_SnapbackLocation.pos);
                }
            }
            else if (!m_wasPainting && m_isPainting) // first frame of user painting
            {
                m_HasDoneFirstPaint = true;
                // check if the user just started painting. do this so a delta pos
                // isn't applied to the sample location on the first paint operation
                m_PrevBrushLocation.Set(terrain, editContext.raycastHit.point);
            }

            bool updateClone = (m_isPainting && cloneToolProperties.m_MovementBehavior != MovementBehavior.Fixed) ||
                                (m_isPainting && cloneToolProperties.m_MovementBehavior == MovementBehavior.FollowOnPaint) ||
                                (m_HasDoneFirstPaint && cloneToolProperties.m_MovementBehavior == MovementBehavior.FollowAlways);
            
            if (updateClone)
            {
                HandleBrushCrossingSeams(ref m_SampleLocation, editContext.raycastHit.point, m_PrevBrushLocation.pos);
            }

            // update the previous paint location for use in the next frame (if the user is painting)
            m_PrevBrushLocation.Set(terrain, editContext.raycastHit.point);
        }

        // check to see if the sample brush is crossing any terrain seams/borders. have to do this manually
        // since TerrainPaintUtility only immediate neighbors and not manually created PaintContexts
        private void HandleBrushCrossingSeams(ref BrushLocationData brushLocation, Vector3 currBrushPos, Vector3 prevBrushPos)
        {
            if (brushLocation.terrain == null)
                return;

            Vector3 deltaPos = currBrushPos - prevBrushPos;
            brushLocation.Set(brushLocation.terrain, brushLocation.pos + deltaPos);

            Vector2 currUV = TerrainUVFromBrushLocation(brushLocation.terrain, brushLocation.pos);

            if (currUV.x >= 1.0f && brushLocation.terrain.rightNeighbor != null)
                brushLocation.terrain = brushLocation.terrain.rightNeighbor;
            else if (currUV.x < 0.0f && brushLocation.terrain.leftNeighbor != null)
                brushLocation.terrain = brushLocation.terrain.leftNeighbor;

            if (currUV.y >= 1.0f && brushLocation.terrain.topNeighbor != null)
                brushLocation.terrain = brushLocation.terrain.topNeighbor;
            else if (currUV.y < 0.0f && brushLocation.terrain.bottomNeighbor != null)
                brushLocation.terrain = brushLocation.terrain.bottomNeighbor;
        }

        private void DrawBrushPreviews(Terrain terrain, IOnSceneGUI editContext)
        {
            Vector2 sampleUV;
            BrushTransform sampleXform;
            PaintContext sampleContext = null;
            Material previewMat = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();
            // draw sample location brush and create context data to be used when drawing target brush previews
            if (m_SampleLocation.terrain != null)
            {
                sampleUV = TerrainUVFromBrushLocation(m_SampleLocation.terrain, m_SampleLocation.pos);
                sampleXform = TerrainPaintUtility.CalculateBrushTransform(m_SampleLocation.terrain, sampleUV, commonUI.brushSize, commonUI.brushRotation);
                sampleContext = TerrainPaintUtility.BeginPaintHeightmap(m_SampleLocation.terrain, sampleXform.GetBrushXYBounds());
                TerrainPaintUtilityEditor.DrawBrushPreview(sampleContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture,
                                                           editContext.brushTexture, sampleXform, previewMat, 0);
            }

            // draw brush preview and mesh preview for current mouse position
            if (commonUI.isRaycastHitUnderCursorValid)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, commonUI.raycastHitUnderCursor.textureCoord, commonUI.brushSize, commonUI.brushRotation);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                if (sampleContext != null && cloneToolProperties.m_PaintHeightmap) {
                    ApplyHeightmap(sampleContext, ctx, brushXform, terrain, editContext.brushTexture, commonUI.brushStrength);
                    RenderTexture.active = ctx.oldRenderTexture;
                    previewMat.SetTexture("_HeightmapOrig", ctx.sourceRenderTexture);
                    TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture,
                                                               editContext.brushTexture, brushXform, previewMat, 1);
                }

                // Restores RenderTexture.active
                ctx.Cleanup();
            }

            // Restores RenderTexture.active
            sampleContext?.Cleanup();
        }

        private Vector2 TerrainUVFromBrushLocation(Terrain terrain, Vector3 posWS)
        {
            // position relative to Terrain-space. doesnt handle rotations,
            // since that's not really supported at the moment
            Vector3 posTS = posWS - terrain.transform.position; 
            Vector3 size = terrain.terrainData.size;

            return new Vector2(posTS.x / size.x, posTS.z / size.z);
        }

        private void ApplyHeightmap(PaintContext sampleContext, PaintContext targetContext, BrushTransform targetXform,
                                    Terrain targetTerrain, Texture brushTexture, float brushStrength)
        {
            Material paintMat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(brushStrength, cloneToolProperties.m_StampingOffsetFromClone * 0.5f, targetTerrain.terrainData.size.y, 0f);
            paintMat.SetTexture("_BrushTex", brushTexture);
            paintMat.SetVector("_BrushParams", brushParams);
            paintMat.SetTexture("_CloneTex", sampleContext.sourceRenderTexture);
            paintMat.SetVector("_SampleUVScaleOffset", ComputeSampleUVScaleOffset(sampleContext, targetContext));

            var brushMask = RTUtils.GetTempHandle(sampleContext.sourceRenderTexture.width, sampleContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
            Utility.SetFilterRT(commonUI, sampleContext.sourceRenderTexture, brushMask, paintMat);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(targetContext, targetXform, paintMat);
            Graphics.Blit(targetContext.sourceRenderTexture, targetContext.destinationRenderTexture, paintMat, (int)ShaderPasses.CloneHeightmap);
            RTUtils.Release(brushMask);
        }

        private Vector4 ComputeSampleUVScaleOffset(PaintContext sampleContext, PaintContext targetContext) {
            Vector4 sampleUVScaleOffset;
            TerrainPaintUtility.BuildTransformPaintContextUVToPaintContextUV(targetContext, sampleContext, out sampleUVScaleOffset);

            var sampleUV = TerrainUVFromBrushLocation(m_SampleLocation.terrain, m_SampleLocation.pos);
            var paintContextUVOffset = sampleUV * (sampleContext.targetTextureHeight / (float)m_SampleLocation.terrain.terrainData.heightmapResolution);
            
            float deltaUPixels = (sampleUV.x - commonUI.raycastHitUnderCursor.textureCoord.x) * targetContext.targetTextureWidth;
            float deltaVPixels = (sampleUV.y - commonUI.raycastHitUnderCursor.textureCoord.y) * targetContext.targetTextureHeight;
            sampleUVScaleOffset.z += ((int) deltaUPixels) / (float)sampleContext.pixelRect.width;
            sampleUVScaleOffset.w += ((int) deltaVPixels) / (float)sampleContext.pixelRect.height;

            return sampleUVScaleOffset;
        }

        private void PaintHeightmap(Terrain sampleTerrain, Terrain targetTerrain, BrushTransform sampleXform,
                                    BrushTransform targetXform, IOnPaint editContext, Material mat)
        {
            PaintContext sampleContext = TerrainPaintUtility.BeginPaintHeightmap(sampleTerrain, sampleXform.GetBrushXYBounds());
            PaintContext targetContext = TerrainPaintUtility.BeginPaintHeightmap(targetTerrain, targetXform.GetBrushXYBounds());
            ApplyHeightmap(sampleContext, targetContext, targetXform, targetTerrain, editContext.brushTexture, commonUI.brushStrength);
            TerrainPaintUtility.EndPaintHeightmap(targetContext, "Terrain Paint - Clone Brush (Heightmap)");

            // Restores RenderTexture.active
            sampleContext.Cleanup();
            targetContext.Cleanup();
        }

        private void PaintAlphamap(Terrain sampleTerrain, Terrain targetTerrain, BrushTransform sampleXform, BrushTransform targetXform, Material mat)
        {
            Rect sampleRect = sampleXform.GetBrushXYBounds();
            Rect targetRect = targetXform.GetBrushXYBounds();
            int numSampleTerrainLayers = sampleTerrain.terrainData.terrainLayers.Length;
            for (int i = 0; i < numSampleTerrainLayers; ++i)
            {
                TerrainLayer layer = sampleTerrain.terrainData.terrainLayers[i];

                if (layer == null) continue; // nothing to paint if the layer is NULL

                PaintContext sampleContext = TerrainPaintUtility.BeginPaintTexture(sampleTerrain, sampleRect, layer);

                int layerIndex = TerrainPaintUtility.FindTerrainLayerIndex(sampleTerrain, layer);
                Texture2D layerTexture = TerrainPaintUtility.GetTerrainAlphaMapChecked(sampleTerrain, layerIndex >> 2);
                PaintContext targetContext = PaintContext.CreateFromBounds(targetTerrain, targetRect, layerTexture.width, layerTexture.height);
                targetContext.CreateRenderTargets(RenderTextureFormat.R8);
                targetContext.GatherAlphamap(layer, true);
                sampleContext.sourceRenderTexture.filterMode = FilterMode.Point;
                mat.SetTexture("_CloneTex", sampleContext.sourceRenderTexture);
                mat.SetVector("_SampleUVScaleOffset", ComputeSampleUVScaleOffset(sampleContext, targetContext));

                var brushMask = RTUtils.GetTempHandle(targetContext.sourceRenderTexture.width, targetContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
                Utility.SetFilterRT(commonUI, targetContext.sourceRenderTexture, brushMask, mat);
                TerrainPaintUtility.SetupTerrainToolMaterialProperties(targetContext, targetXform, mat);
                Graphics.Blit(targetContext.sourceRenderTexture, targetContext.destinationRenderTexture, mat, (int)ShaderPasses.CloneAlphamap);
                // apply texture modifications and perform cleanup. same thing as calling TerrainPaintUtility.EndPaintTexture
                targetContext.ScatterAlphamap("Terrain Paint - Clone Brush (Texture)");

                // Restores RenderTexture.active
                targetContext.Cleanup();
                RTUtils.Release(brushMask);
            }
        }

        private static class Styles
        {
            public static readonly string descriptionString =
                                            "Clones terrain from another area of the terrain map to the selected location.\n\n" +
                                            "Hold Ctrl and Left Click to assign the clone sample area.\n\n" +
                                            "Left Click to apply the cloned stamp.";
            public static readonly GUIContent cloneSourceContent = EditorGUIUtility.TrTextContent("Terrain sources to clone:",
                                            "Textures:\nBrush will gather and clone TerrainLayer data at Sample location\n\n" + 
                                            "Heightmap:\nBrush will gather and clone Heightmap data at Sample location");
            public static readonly GUIContent cloneTextureContent = EditorGUIUtility.TrTextContent("Textures", "Brush will gather and clone TerrainLayer data from Sample location");
            public static readonly GUIContent cloneHeightmapContent = EditorGUIUtility.TrTextContent("Heightmap", "Brush will gather and clone Heightmap data from Sample location");
            public static readonly GUIContent cloneBehaviorContent = EditorGUIUtility.TrTextContent("Clone Movement Behavior",
                                            "Snap:\nClone location will snap back to user-selected location on mouse-up\n\n" +
                                            "Follow On Paint:\nClone location will move with mouse position (only when painting) and not snap back\n\n" +
                                            "Follow Always:\nClone location will always move with mouse position (even when not painting) and not snap back\n\n" +
                                            "Fixed:\nClone location will always stay at the user-selected location");
            public static readonly GUIContent heightOffsetContent = EditorGUIUtility.TrTextContent("Height Offset", 
                                            "When stamping the heightmap, the cloned height will be added with this offset to raise or lower the cloned height at the stamp location.");
            public static readonly GUIContent controlHeader = EditorGUIUtility.TrTextContent("Clone Brush Controls");

            public static GUIStyle buttonActiveStyle = null;
            public static GUIStyle buttonNormalStyle = null;

            static Styles()
            {
                buttonNormalStyle = "Button";
                buttonActiveStyle = new GUIStyle("Button");
                buttonActiveStyle.normal.background = buttonNormalStyle.active.background;
            }

            public static GUIStyle GetButtonToggleStyle(bool isToggled)
            {
                return isToggled ? buttonActiveStyle : buttonNormalStyle;
            }
        }

        private void SaveSetting()
        {
            string cloneToolData = JsonUtility.ToJson(cloneToolProperties);
            EditorPrefs.SetString("Unity.TerrainTools.Clone", cloneToolData);
        }

        private void LoadSettings()
        {
            string cloneToolData = EditorPrefs.GetString("Unity.TerrainTools.Clone");
            cloneToolProperties.SetDefaults();
            JsonUtility.FromJsonOverwrite(cloneToolData, cloneToolProperties);
        }

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters() => new TerrainToolsAnalytics.IBrushParameter[]{
            new TerrainToolsAnalytics.BrushParameter<bool>{Name = Styles.cloneTextureContent.text, Value = cloneToolProperties.m_PaintAlphamap},
            new TerrainToolsAnalytics.BrushParameter<bool>{Name = Styles.cloneHeightmapContent.text, Value = cloneToolProperties.m_PaintHeightmap},
            new TerrainToolsAnalytics.BrushParameter<string>{Name = Styles.cloneBehaviorContent.text, Value = cloneToolProperties.m_MovementBehavior.ToString()},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.heightOffsetContent.text, Value = cloneToolProperties.m_StampingOffsetFromClone},
        };
        #endregion
    }
}
