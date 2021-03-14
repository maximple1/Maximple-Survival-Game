using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class MeshStampTool : TerrainPaintTool<MeshStampTool>
    {
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Terrain/Select Mesh Stamp Tool", typeof(TerrainToolShortcutContext))]                // tells shortcut manager what to call the shortcut and what to pass as args
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;              // gets interface to modify state of TerrainTools
            context.SelectPaintTool<MeshStampTool>();                                                   // set active tool
            TerrainToolsAnalytics.OnShortcutKeyRelease("Select Mesh Stamp Tool");
        }

        [ClutchShortcut("Terrain/Adjust Mesh Stamp Transform", typeof(TerrainToolShortcutContext), KeyCode.C)]
        static void StrengthBrushShortcut(ShortcutArguments args)
        {
            if(args.stage == ShortcutStage.Begin)
            {
                m_editTransform = true;
            }
            else if(args.stage == ShortcutStage.End)
            {
                m_editTransform = false;
                TerrainToolsAnalytics.OnShortcutKeyRelease("Adjust Mesh Stamp Transform");
            }
        }
#endif

        private enum ShaderPasses
        {
            BrushPreviewFrontFaces = 0,
            BrushPreviewBackFaces,
            DepthPassFrontFaces,
            DepthPassBackFaces,
            StampToHeightmap
        }

        [SerializeField]
        IBrushUIGroup m_brushUI;
        
        private IBrushUIGroup commonUI
        {
            get
            {
                if( m_brushUI == null )
                {
                    LoadSettings();
                    m_brushUI = new MeshBrushUIGroup( "MeshStampTool", UpdateAnalyticParameters );
                    m_brushUI.OnEnterToolMode();
                }

                return m_brushUI;
            }
        }

        static class RenderTextureIDs
        {
            public static int cameraView = "cameraView".GetHashCode();
            public static int meshStamp = "meshStamp".GetHashCode();
            public static int meshStampPreview = "meshStampPreview".GetHashCode();
            public static int meshStampMask = "meshStampMask".GetHashCode();
            public static int sourceHeight = "sourceHeight".GetHashCode();
            public static int combinedHeight = "combinedHeight".GetHashCode();
        }

        [System.Serializable]
        class ToolSettings
        {
            public Quaternion rotation;
            public Vector3 scale;
            public float stampHeight;
            public float blendAmount;
            public string meshAssetGUID;
            public bool showToolSettings;

            public void SetDefaults()
            {
                rotation = Quaternion.identity;
                scale = Vector3.one;
                blendAmount = 0.0f;
                stampHeight = 0.0f;
                meshAssetGUID = null;
                showToolSettings = true;
            }
        }

        private Mesh m_activeMesh;
        public Mesh activeMesh
        {
            get
            {
                if( m_activeMesh == null && !string.IsNullOrEmpty( toolSettings.meshAssetGUID ) )
                {
                    m_activeMesh = AssetDatabase.LoadAssetAtPath<Mesh>( AssetDatabase.GUIDToAssetPath( toolSettings.meshAssetGUID ) );
                }

                return m_activeMesh;
            }

            set
            {
                m_activeMesh = value;
            }
        }

        ToolSettings toolSettings = new ToolSettings();
        RTHandleCollection m_rtCollection;

        private Vector3 m_SceneRaycastHitPoint;
        private BrushTransform brushXformIdentity = new BrushTransform(Vector2.zero, Vector2.right, Vector2.up);
        [System.NonSerialized] private bool m_initialized = false;
        private float m_prevBrushRotation;
        private float m_prevBrushSize;
        private Vector3 m_baseHandlePos;
        private float m_handleHeightOffsetWS;
        private float m_handleHeightViewOffsetWS;
        static private bool m_prevEditTransform = false;
        static private bool m_editTransform = false;
        Bounds m_worldBounds;
        
        private Material m_Material = null;
        private Material GetMaterial()
        {
            if (m_Material == null)
            {
                m_Material = new Material(Shader.Find("Hidden/TerrainTools/MeshStamp"));
            }

            return m_Material;
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

        public override string GetName()
        {
            return Styles.nameString;
        }

        public override string GetDesc()
        {
            return Styles.descriptionString;
        }

        private void Init()
        {
            if( !m_initialized )
            {
                m_rtCollection = new RTHandleCollection();
                m_rtCollection.AddRTHandle( RenderTextureIDs.cameraView, "cameraView", GraphicsFormat.R8G8B8A8_SRGB );
                m_rtCollection.AddRTHandle( RenderTextureIDs.meshStamp, "meshStamp", GraphicsFormat.R16_SFloat );
                m_rtCollection.AddRTHandle( RenderTextureIDs.meshStampPreview, "meshStampPreview", GraphicsFormat.R16_SFloat );
                m_rtCollection.AddRTHandle( RenderTextureIDs.meshStampMask, "meshStampMask", GraphicsFormat.R16_UNorm );
                m_rtCollection.AddRTHandle( RenderTextureIDs.sourceHeight, "sourceHeight", GraphicsFormat.R16_UNorm );
                m_rtCollection.AddRTHandle( RenderTextureIDs.combinedHeight, "combinedHeight", GraphicsFormat.R16_UNorm );

                m_initialized = true;
            }
        }

        bool debugOrtho = true;

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            Init();

            // brush GUI
            commonUI.OnInspectorGUI(terrain, editContext);

            EditorGUI.BeginChangeCheck();
            {
                toolSettings.showToolSettings = TerrainToolGUIHelper.DrawHeaderFoldoutForBrush( Styles.settings, toolSettings.showToolSettings, toolSettings.SetDefaults);
                if( toolSettings.showToolSettings )
                {         
                    if (activeMesh == null)
                    {
                        EditorGUILayout.HelpBox(Styles.nullMeshString, MessageType.Warning);
                    }

                    activeMesh = EditorGUILayout.ObjectField(Styles.meshContent, activeMesh, typeof(Mesh), false) as Mesh;

                    GUILayout.Space(8f);

                    toolSettings.blendAmount = EditorGUILayout.Slider( Styles.blendAmount, toolSettings.blendAmount, 0, 1 );

                    GUILayout.Space(8f);
                    
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PrefixLabel( Styles.transformSettings );

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(Styles.resetTransformContent, GUILayout.ExpandWidth(false)))
                        {
                            toolSettings.rotation = Quaternion.identity;
                            toolSettings.stampHeight = 0;
                            toolSettings.scale = Vector3.one;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    toolSettings.stampHeight = EditorGUILayout.FloatField( Styles.stampHeightContent, toolSettings.stampHeight );

                    toolSettings.scale = EditorGUILayout.Vector3Field( Styles.stampScaleContent, toolSettings.scale );

                    toolSettings.rotation = Quaternion.Euler(EditorGUILayout.Vector3Field(Styles.stampRotationContent, toolSettings.rotation.eulerAngles));
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
                Save(true);
                TerrainToolsAnalytics.OnParameterChange();
            }
        }

        public override void OnSceneGUI( Terrain terrain, IOnSceneGUI editContext )
        {
            Init();

            // m_rtCollection.OnSceneGUI( EditorWindow.GetWindow<SceneView>().position.height / 4 );

            commonUI.OnSceneGUI2D( terrain, editContext );

            // only do the rest if user mouse hits valid terrain or they are using the
            // brush parameter hotkeys to resize, etc
            if ( !editContext.hitValidTerrain && !commonUI.isInUse && !m_editTransform && !debugOrtho )
            {
                return;
            }

            // update brush UI group
            commonUI.OnSceneGUI( terrain, editContext );

            bool justPressedEditKey = m_editTransform && !m_prevEditTransform;
            bool justReleaseEditKey = m_prevEditTransform && !m_editTransform;
            m_prevEditTransform = m_editTransform;

            if( justPressedEditKey )
            {
                ( commonUI as MeshBrushUIGroup).LockTerrainUnderCursor( true );
                m_baseHandlePos = commonUI.raycastHitUnderCursor.point;
                m_handleHeightOffsetWS = 0;
            }
            else if( justReleaseEditKey )
            {
                ( commonUI as MeshBrushUIGroup).UnlockTerrainUnderCursor();
                m_handleHeightOffsetWS = 0;
            }

            // don't render mesh previews, etc. if the mesh field has not been populated yet
            if ( activeMesh == null )
            {
                return;
            }

            // dont render preview if this isnt a repaint. losing performance if we do
            if ( Event.current.type == EventType.Repaint )
            {
                Terrain currTerrain = commonUI.terrainUnderCursor;
                Vector2 uv = commonUI.raycastHitUnderCursor.textureCoord;
                float brushSize = commonUI.brushSize;
                float brushRotation = commonUI.brushRotation;

                if ( /* debugOrtho || */ commonUI.isRaycastHitUnderCursorValid )
                {
                    // if(debugOrtho)
                    // {
                    //     uv = Vector2.one * .5f;
                    // }

                    BrushTransform brushTransform = TerrainPaintUtility.CalculateBrushTransform( currTerrain, uv, brushSize, brushRotation );
                    PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap( commonUI.terrainUnderCursor, brushTransform.GetBrushXYBounds(), 1 );
                    Material material = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();
                
                    // don't draw the brush mask preview
                    // but draw the resulting mesh stamp preview
                    {
                        ApplyBrushInternal( terrain, ctx, brushTransform );

                        TerrainPaintUtilityEditor.DrawBrushPreview( ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, 
                                                                    m_rtCollection[ RenderTextureIDs.meshStamp ], brushTransform, material, 0 );

                        RenderTexture.active = ctx.oldRenderTexture;

                        material.SetTexture( "_HeightmapOrig", ctx.sourceRenderTexture );
                        TerrainPaintUtility.SetupTerrainToolMaterialProperties( ctx, brushTransform, material );
                        TerrainPaintUtilityEditor.DrawBrushPreview( ctx, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture,
                                                                    m_rtCollection[ RenderTextureIDs.meshStamp ], brushTransform, material, 1 );

                        TerrainPaintUtility.ReleaseContextResources( ctx );
                        m_rtCollection.ReleaseRTHandles();
                    }
                }
            }

            if( m_editTransform )
            {
                EditorGUI.BeginChangeCheck();
                {
                    Vector3 prevHandlePosWS = m_baseHandlePos + Vector3.up * m_handleHeightOffsetWS;

                    // draw transform handles
                    float handleSize = HandleUtility.GetHandleSize( prevHandlePosWS );
                    Quaternion brushRotation = Quaternion.AngleAxis( commonUI.brushRotation, Vector3.up );
                    Matrix4x4 brushRotMat = Matrix4x4.Rotate( brushRotation );
                    Matrix4x4 toolRotMat = Matrix4x4.Rotate( toolSettings.rotation );
                    Quaternion handleRot = MeshUtils.QuaternionFromMatrix( brushRotMat * toolRotMat );
                    Quaternion newRot = Handles.RotationHandle( handleRot, prevHandlePosWS );
                    toolSettings.rotation = MeshUtils.QuaternionFromMatrix( brushRotMat.inverse * Matrix4x4.Rotate( newRot ) );
                    
                    toolSettings.scale = Handles.ScaleHandle( toolSettings.scale, prevHandlePosWS, handleRot, handleSize * 1.5f );

                    Vector3 currHandlePosWS = Handles.Slider( prevHandlePosWS, Vector3.up, handleSize, Handles.ArrowHandleCap, 1f );
                    float deltaHeight = ( currHandlePosWS.y - prevHandlePosWS.y );
                    m_handleHeightOffsetWS += deltaHeight;
                    toolSettings.stampHeight += deltaHeight;
                }
                
                if( EditorGUI.EndChangeCheck() )
                {
                    SaveSettings();
                    editContext.Repaint();
                }
            }
        }

        private void ApplyBrushInternal(Terrain terrain, PaintContext ctx, BrushTransform brushTransform)
        {
            Init();
            
            m_rtCollection.ReleaseRTHandles();
            m_rtCollection.GatherRTHandles( ctx.sourceRenderTexture.width, ctx.sourceRenderTexture.height, 16 );

            Graphics.Blit( ctx.sourceRenderTexture, m_rtCollection[ RenderTextureIDs.sourceHeight ] );

            Material mat = GetMaterial();
            var brushMask = RTUtils.GetTempHandle(ctx.sourceRenderTexture.width, ctx.sourceRenderTexture.height, 0, FilterUtility.defaultFormat);
            Utility.SetFilterRT(commonUI, ctx.sourceRenderTexture, brushMask, mat);

            Matrix4x4 toolMatrix = Matrix4x4.TRS( Vector3.zero, toolSettings.rotation, toolSettings.scale );

            Bounds modelBounds = activeMesh.bounds;
            float maxModelScale = Mathf.Max( Mathf.Max( modelBounds.size.x, modelBounds.size.y ), modelBounds.size.z );
            // maxModelScale *= Mathf.Sqrt( 2 + maxModelScale * maxModelScale / 4 ) * .5f; // mult so the mesh fits a little better within the camera / stamp texture bounds
            // maxModelScale /= 1.414f; 
            float x = .5f;
            float y = .5f;
            float xy = Mathf.Sqrt( x * x + y * y );
            float z = .5f;
            float xyz = Mathf.Sqrt( xy * xy + z * z );
            maxModelScale *= xyz;

            // build the model matrix to transform the mesh with. we want to scale it to fit in the brush bounds and also center it in the brush bounds
            Matrix4x4 model = toolMatrix * Matrix4x4.Scale( Vector3.one / maxModelScale ) * Matrix4x4.Translate( -modelBounds.center );

            // get the world bounds here so we can calculate the needed offset along the up axis
            // Bounds worldBounds = MeshUtility.TransformBounds( model, activeMesh.bounds );
            // float localHeightOffset = Mathf.Min( worldBounds.extents.y, toolSettings.stampHeight / brushUI.terrainUnderCursor.terrainData.size.y  * .5f );
            // Matrix4x4 localHeightOffsetMatrix = Matrix4x4.Translate( Vector3.up * localHeightOffset );
            // apply the local height offset
            // model = localHeightOffsetMatrix * model;

            Vector3 translate = Vector3.up * ( toolSettings.stampHeight ) / commonUI.terrainUnderCursor.terrainData.size.y;
            // translate = translate / brushUI.brushStrength * .5f;
            model = Matrix4x4.Translate( translate ) * model;

            // actually render the mesh to texture to be used with the tool shader
            MeshUtils.RenderTopdownProjection( activeMesh, model,
                                               m_rtCollection[ RenderTextureIDs.meshStamp ],
                                               MeshUtils.defaultProjectionMaterial,
                                               MeshUtils.ShaderPass.Height );
            // this doesn't actually apply any noise to the destination RT but will color the destination RT based on whether the fragment values are (+) or (-)
            NoiseUtils.BlitPreview2D( m_rtCollection[ RenderTextureIDs.meshStamp ], m_rtCollection[ RenderTextureIDs.meshStampPreview ] );

            // generate a mask for the mesh to be used in the compositing shader
            MeshUtils.RenderTopdownProjection( activeMesh, model,
                                               m_rtCollection[ RenderTextureIDs.meshStampMask ],
                                               MeshUtils.defaultProjectionMaterial,
                                               MeshUtils.ShaderPass.Mask );

            // perform actual composite of mesh stamp and terrain source heightmap
            float brushStrength = Event.current.control ? -commonUI.brushStrength : commonUI.brushStrength;
            Vector4 brushParams = new Vector4( brushStrength, toolSettings.blendAmount, ( commonUI.raycastHitUnderCursor.point.y - commonUI.terrainUnderCursor.GetPosition().y ) / commonUI.terrainUnderCursor.terrainData.size.y * .5f, toolSettings.stampHeight / commonUI.terrainUnderCursor.terrainData.size.y * .5f );
            mat.SetVector( "_BrushParams", brushParams );
            mat.SetTexture( "_MeshStampTex", m_rtCollection[ RenderTextureIDs.meshStamp ] );
            mat.SetTexture( "_MeshMaskTex", m_rtCollection[ RenderTextureIDs.meshStampMask ] );
            mat.SetFloat( "_TerrainHeight", commonUI.terrainUnderCursor.terrainData.size.y );
            TerrainPaintUtility.SetupTerrainToolMaterialProperties( ctx, brushTransform, mat );
            Graphics.Blit( ctx.sourceRenderTexture, ctx.destinationRenderTexture, mat, 0 );
            Graphics.Blit( ctx.destinationRenderTexture, m_rtCollection[ RenderTextureIDs.combinedHeight ] );

            // restore old render target
            RenderTexture.active = ctx.oldRenderTexture;
            RTUtils.Release(brushMask);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            Init();

            if (activeMesh == null || Event.current.type != EventType.MouseDown || Event.current.shift == true || m_editTransform)
            {
                return false;
            }

            commonUI.OnPaint( terrain, editContext );

            if ( commonUI.allowPaint )
            {
                Texture brushTexture = editContext.brushTexture;
                
                BrushTransform brushTransform = TerrainPaintUtility.CalculateBrushTransform( terrain, editContext.uv, commonUI.brushSize, commonUI.brushRotation );
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap( terrain, brushTransform.GetBrushXYBounds() );

                ApplyBrushInternal( terrain, ctx, brushTransform );

                TerrainPaintUtility.EndPaintHeightmap( ctx, "Mesh Stamp - Stamp Mesh" );
                
                m_rtCollection.ReleaseRTHandles();
            }

            return true;
        }

        private void SaveSettings()
        {
            toolSettings.meshAssetGUID = activeMesh == null ? "" : AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( activeMesh ) );
            string meshstampToolData = JsonUtility.ToJson( toolSettings );
            EditorPrefs.SetString("Unity.TerrainTools.MeshStamp", meshstampToolData);
        }

        private void LoadSettings()
        {
            string meshstampToolData = EditorPrefs.GetString("Unity.TerrainTools.MeshStamp");
            toolSettings.SetDefaults();
            JsonUtility.FromJsonOverwrite(meshstampToolData, toolSettings);
        }

        private static class Styles
        {
            public static readonly string nameString = "Mesh Stamp";
            public static readonly string descriptionString =
                    "Left Click to stamp the selected mesh into the heightmap (addition)." +
                    "\n\nHold Control + Left Click to indent the selected mesh into the heightmap (subtraction)." +
                    "\n\nHold 'C' to bring up the gizmos for rotation, scale, and height.";

            public static readonly GUIContent blendAmount = EditorGUIUtility.TrTextContent("Blend Amount",
                                    "Amount of blending to apply to the stamp. 0 means no blending. 1 means fully additive blending");
            public static readonly GUIContent stampHeightContent = EditorGUIUtility.TrTextContent("Height Offset", "The height to stamp the mesh into the terrain at.");
            public static readonly GUIContent stampScaleContent = EditorGUIUtility.TrTextContent("Scale", "The scale of the mesh.");
            public static readonly GUIContent stampRotationContent = EditorGUIUtility.TrTextContent("Rotation", "The rotation of the mesh.");
            public static readonly GUIContent meshContent = EditorGUIUtility.TrTextContent("Mesh", "The mesh to stamp.");
            public static readonly GUIContent settings = EditorGUIUtility.TrTextContent("Mesh Stamp Settings");
            public static readonly GUIContent transformSettings = EditorGUIUtility.TrTextContent("Transform Settings:");
            public static readonly GUIContent resetTransformContent = EditorGUIUtility.TrTextContent("Reset",
                                    "Resets the mesh's rotation, scale, and height to their default state.");
            public static readonly string nullMeshString = "Must assign a mesh to use with the Mesh Stamp Tool.";
        }

        #region Analytics
        private TerrainToolsAnalytics.IBrushParameter[] UpdateAnalyticParameters() => new TerrainToolsAnalytics.IBrushParameter[]{
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.blendAmount.text, Value = toolSettings.blendAmount},
            new TerrainToolsAnalytics.BrushParameter<float>{Name = Styles.stampHeightContent.text, Value = toolSettings.stampHeight},
            new TerrainToolsAnalytics.BrushParameter<Vector3>{Name = Styles.stampScaleContent.text, Value = toolSettings.scale},
            new TerrainToolsAnalytics.BrushParameter<Vector3>{Name = Styles.stampRotationContent.text, Value = toolSettings.rotation.eulerAngles},
        };
        #endregion
    }
}
