using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class BridgeTool : TerrainPaintTool<BridgeTool>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Bridge Tool", typeof(TerrainToolShortcutContext))]                // tells shortcut manager what to call the shortcut and what to pass as args
        static void SelectShortcut(ShortcutArguments args) {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;          // gets interface to modify state of TerrainTools
            context.SelectPaintTool<BridgeTool>();                                                  // set active tool
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Bridge Tool");
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
                    m_commonUI = new DefaultBrushUIGroup( "BridgeTool", UpdateAnalyticParameters, DefaultBrushUIGroup.Feature.NoScatter );
                    m_commonUI.OnEnterToolMode();
                }

                return m_commonUI;
            }
        }

        Terrain m_StartTerrain = null;
        private Vector3 m_StartPoint;

        Material m_Material = null;
        Material GetPaintMaterial() {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("Hidden/TerrainTools/SetExactHeight"));
            return m_Material;
        }

        [System.Serializable]
        class BridgeToolSerializedProperties
        {
            public AnimationCurve widthProfile;
            public AnimationCurve heightProfile;
            public AnimationCurve strengthProfile;
            public AnimationCurve jitterProfile;

            public void SetDefaults()
            {
                widthProfile = AnimationCurve.Linear(0, 1, 1, 1);
                heightProfile = AnimationCurve.Linear(0, 0, 1, 0);
                strengthProfile = AnimationCurve.Linear(0, 1, 1, 1);
                jitterProfile = AnimationCurve.Linear(0, 0, 1, 0);
            }
        }

        BridgeToolSerializedProperties bridgeToolProperties = new BridgeToolSerializedProperties();

        public override string GetName()
        {
            return "Sculpt/Bridge";
        }

        public override string GetDesc()
        {
            return "Control + Click to Set the start point, click to connect the bridge.";
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

            if (editContext.hitValidTerrain || commonUI.isInUse)
            {
                commonUI.OnSceneGUI(terrain, editContext);

                if (Event.current.type != EventType.Repaint)
                {
                    return;
                }

                float endWidth = Mathf.Abs(bridgeToolProperties.widthProfile.Evaluate(1.0f));

                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, commonUI.raycastHitUnderCursor.textureCoord, commonUI.brushSize * endWidth, commonUI.brushRotation);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            //display a brush preview at the bridge starting location, using starting size from width profile
            if (m_StartTerrain != null)
            {
                float startWidth = Mathf.Abs(bridgeToolProperties.widthProfile.Evaluate(0.0f));

                BrushTransform brushTransform = TerrainPaintUtility.CalculateBrushTransform(m_StartTerrain, m_StartPoint, commonUI.brushSize * startWidth, commonUI.brushRotation);
                PaintContext sampleContext = TerrainPaintUtility.BeginPaintHeightmap(m_StartTerrain, brushTransform.GetBrushXYBounds());
                TerrainPaintUtilityEditor.DrawBrushPreview(sampleContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture,
                                                           editContext.brushTexture, brushTransform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(sampleContext);
            }
        }

        bool m_ShowBridgeControls = true;
        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();

            commonUI.OnInspectorGUI(terrain, editContext);

            m_ShowBridgeControls = TerrainToolGUIHelper.DrawHeaderFoldoutForBrush(Styles.controlHeader, m_ShowBridgeControls, bridgeToolProperties.SetDefaults);

            if (m_ShowBridgeControls) {
                //"Controls the width of the bridge over the length of the stroke"
                bridgeToolProperties.widthProfile = EditorGUILayout.CurveField(Styles.widthProfileContent, bridgeToolProperties.widthProfile);
                bridgeToolProperties.heightProfile = EditorGUILayout.CurveField(Styles.heightProfileContent, bridgeToolProperties.heightProfile);
                bridgeToolProperties.strengthProfile = EditorGUILayout.CurveField(Styles.strengthProfileContent, bridgeToolProperties.strengthProfile);
                bridgeToolProperties.jitterProfile = EditorGUILayout.CurveField(Styles.jitterProfileContent, bridgeToolProperties.jitterProfile);
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveSetting();
                Save(true);
                TerrainToolsAnalytics.OnParameterChange();
            }
        }

        private Vector2 transformToWorld(Terrain t, Vector2 uvs)
        {
            Vector3 tilePos = t.GetPosition();
            return new Vector2(tilePos.x, tilePos.z) + uvs * new Vector2(t.terrainData.size.x, t.terrainData.size.z);
        }

        private Vector2 transformToUVSpace(Terrain originTile, Vector2 worldPos) {
            Vector3 originTilePos = originTile.GetPosition();
            Vector2 uvPos = new Vector2((worldPos.x - originTilePos.x) / originTile.terrainData.size.x,
                                        (worldPos.y - originTilePos.z) / originTile.terrainData.size.z);
            return uvPos;
        }

        private void ApplyBrushInternal(Terrain terrain, Vector2 uv, Texture brushTexture, float brushSpacing)
        {
            //get the target position & height
            float targetHeight = terrain.terrainData.GetInterpolatedHeight(uv.x, uv.y) / terrain.terrainData.size.y;
            Vector3 targetPos = new Vector3(uv.x, uv.y, targetHeight);

            if (terrain != m_StartTerrain) {
                //figure out the stroke vector in uv,height space
                Vector2 targetWorld = transformToWorld(terrain, uv);
                Vector2 targetUVs = transformToUVSpace(m_StartTerrain, targetWorld);
                targetPos.x = targetUVs.x;
                targetPos.y = targetUVs.y;
            }

            Vector3 stroke = targetPos - m_StartPoint;
            float strokeLength = stroke.magnitude;
            int numSplats = (int)(strokeLength / (0.1f * Mathf.Max(brushSpacing, 0.01f)));

            Terrain currTerrain = m_StartTerrain;
            Material mat = GetPaintMaterial();

            Vector2 posOffset = new Vector2(0.0f, 0.0f);
            Vector2 currUV = new Vector2();
            Vector4 brushParams = new Vector4();

            Vector2 jitterVec = new Vector2(-stroke.z, stroke.x); //perpendicular to stroke direction
            jitterVec.Normalize();

            

            for (int i = 0; i < numSplats; i++)
            {
                float pct = (float)i / (float)numSplats;

                float widthScale = bridgeToolProperties.widthProfile.Evaluate(pct);
                float heightOffset = bridgeToolProperties.heightProfile.Evaluate(pct) / currTerrain.terrainData.size.y;
                float strengthScale = bridgeToolProperties.strengthProfile.Evaluate(pct);
                float jitterOffset = bridgeToolProperties.jitterProfile.Evaluate(pct) / Mathf.Max(currTerrain.terrainData.size.x, currTerrain.terrainData.size.z);

                Vector3 currPos = m_StartPoint + pct * stroke;

                //add in jitter offset (needs to happen before tile correction)
                currPos.x += posOffset.x + jitterOffset * jitterVec.x;
                currPos.y += posOffset.y + jitterOffset * jitterVec.y;

                if (currPos.x >= 1.0f && (currTerrain.rightNeighbor != null)) {
                    currTerrain = currTerrain.rightNeighbor;
                    currPos.x -= 1.0f;
                    posOffset.x -= 1.0f;
                }
                if(currPos.x <= 0.0f && (currTerrain.leftNeighbor != null)) {
                    currTerrain = currTerrain.leftNeighbor;
                    currPos.x += 1.0f;
                    posOffset.x += 1.0f;
                }
                if(currPos.y >= 1.0f && (currTerrain.topNeighbor != null)) {
                    currTerrain = currTerrain.topNeighbor;
                    currPos.y -= 1.0f;
                    posOffset.y -= 1.0f;
                }
                if(currPos.y <= 0.0f && (currTerrain.bottomNeighbor != null)) {
                    currTerrain = currTerrain.bottomNeighbor;
                    currPos.y += 1.0f;
                    posOffset.y += 1.0f;
                }

                currUV.x = currPos.x;
                currUV.y = currPos.y;

                int finalBrushSize = (int)(widthScale * (float)commonUI.brushSize);
                float finalHeight =  (m_StartPoint + pct * stroke).z + heightOffset;

                using(IBrushRenderWithTerrain brushRenderWithTerrain = new BrushRenderWithTerrainUiGroup(commonUI, "BridgeTool", brushTexture))
                {
                    if(brushRenderWithTerrain.CalculateBrushTransform(currTerrain, currUV, finalBrushSize, out BrushTransform brushTransform))
                    {
                        Rect brushBounds = brushTransform.GetBrushXYBounds();
                        PaintContext paintContext = brushRenderWithTerrain.AcquireHeightmap(true, currTerrain, brushBounds);
                
                        mat.SetTexture("_BrushTex", brushTexture);

                        brushParams.x = commonUI.brushStrength * strengthScale;
                        brushParams.y = 0.5f * finalHeight;

                        mat.SetVector("_BrushParams", brushParams);

                        var brushMask = RTUtils.GetTempHandle(paintContext.sourceRenderTexture.width, paintContext.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
                        Utility.SetFilterRT(commonUI, paintContext.sourceRenderTexture, brushMask, mat);
                        brushRenderWithTerrain.SetupTerrainToolMaterialProperties(paintContext, brushTransform, mat);
                        brushRenderWithTerrain.RenderBrush(paintContext, mat, 0);
                        RTUtils.Release(brushMask);
                    }
                }
            }
        }
        
        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            commonUI.OnPaint(terrain, editContext);
            Vector2 uv = editContext.uv;

            if(Event.current.shift) { return true; }
            //grab the starting position & height
            if (Event.current.control)
            {
                TerrainData terrainData = terrain.terrainData;
                float height = terrainData.GetInterpolatedHeight(uv.x, uv.y) / terrainData.size.y;

                m_StartPoint = new Vector3(uv.x, uv.y, height);
                m_StartTerrain = terrain;
                return true;
            }
            else if (!m_StartTerrain || (Event.current.type == EventType.MouseDrag)) {
                return true;
            }
            else
            {
                ApplyBrushInternal(terrain, uv, editContext.brushTexture, commonUI.brushSpacing);
                return false;
            }
        }

        private static class Styles
        {
            public static readonly GUIContent controlHeader = EditorGUIUtility.TrTextContent("Bridge Tool Controls");
            public static readonly GUIContent widthProfileContent = EditorGUIUtility.TrTextContent("Width Profile", "A multiplier that controls the width of the bridge over the length of the stroke");
            public static readonly GUIContent heightProfileContent = EditorGUIUtility.TrTextContent("Height Offset Profile", "Adds a height offset to the bridge along the length of the stroke (World Units)");
            public static readonly GUIContent strengthProfileContent = EditorGUIUtility.TrTextContent("Strength Profile", "A multiplier that controls influence of the bridge along the length of the stroke");
            public static readonly GUIContent jitterProfileContent = EditorGUIUtility.TrTextContent("Horizontal Offset Profile", "Adds an offset perpendicular to the stroke direction (World Units)");

        }

        private void SaveSetting()
        {
            string bridgeToolData = JsonUtility.ToJson(bridgeToolProperties);
            EditorPrefs.SetString("Unity.TerrainTools.Bridge", bridgeToolData);
        }

        private void LoadSettings()
        {
            string bridgeToolData = EditorPrefs.GetString("Unity.TerrainTools.Bridge");
            bridgeToolProperties.SetDefaults();
            JsonUtility.FromJsonOverwrite(bridgeToolData, bridgeToolProperties);
        }

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters() => new TerrainToolsAnalytics.IBrushParameter[]{
            new TerrainToolsAnalytics.BrushParameter<Keyframe[]>{Name = Styles.widthProfileContent.text, Value = bridgeToolProperties.widthProfile.keys},
            new TerrainToolsAnalytics.BrushParameter<Keyframe[]>{Name = Styles.heightProfileContent.text, Value = bridgeToolProperties.heightProfile.keys},
            new TerrainToolsAnalytics.BrushParameter<Keyframe[]>{Name = Styles.strengthProfileContent.text, Value = bridgeToolProperties.strengthProfile.keys},
            new TerrainToolsAnalytics.BrushParameter<Keyframe[]>{Name = Styles.jitterProfileContent.text, Value = bridgeToolProperties.jitterProfile.keys},
        };
        #endregion
    }
}
