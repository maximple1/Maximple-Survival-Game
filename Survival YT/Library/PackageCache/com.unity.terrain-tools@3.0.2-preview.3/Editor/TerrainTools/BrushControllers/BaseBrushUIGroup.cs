using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    public abstract class BaseBrushUIGroup : IBrushUIGroup, IBrushEventHandler, IBrushTerrainCache
    {
        private bool m_ShowBrushTextures = true;
        private bool m_ShowModifierControls = true;

        private static readonly BrushShortcutHandler<BrushShortcutType> s_ShortcutHandler = new BrushShortcutHandler<BrushShortcutType>();

        private readonly string m_Name;
        private readonly HashSet<Event> m_ConsumedEvents = new HashSet<Event>();
        private readonly List<IBrushController> m_Controllers = new List<IBrushController>();

        private IBrushSizeController m_BrushSizeController = null;
        private IBrushRotationController m_BrushRotationController = null;
        private IBrushStrengthController m_BrushStrengthController = null;
        private IBrushSpacingController m_BrushSpacingController = null;
        private IBrushScatterController m_BrushScatterController = null;
        private IBrushModifierKeyController m_BrushModifierKeyController = null;
        private IBrushSmoothController m_BrushSmoothController = null;

        [ SerializeField ]
        private FilterStack m_BrushMaskFilterStack = null;
        public FilterStack brushMaskFilterStack
        {
            get
            {
                if( m_BrushMaskFilterStack == null )
                {
                    if( File.Exists( getFilterStackFilePath ) )
                    {
                        m_BrushMaskFilterStack = LoadFilterStack();
                    }
                    else
                    {
                        // create the first filterstack if this is the first time this tool is being used
                        // because a save file has not been made yet for the filterstack
                        m_BrushMaskFilterStack = ScriptableObject.CreateInstance< FilterStack >();
                    }
                }

                return m_BrushMaskFilterStack;
            }
        }

        private FilterStackView m_BrushMaskFilterStackView = null;
        public FilterStackView brushMaskFilterStackView
        {
            get
            {
                // need to make the UI if the view hasnt been created yet or if the reference to the FilterStack SerializedObject has
                // been lost, like when entering and exiting Play Mode
                if( m_BrushMaskFilterStackView == null || m_BrushMaskFilterStackView.serializedFilterStack.targetObject == null )
                {
                    m_BrushMaskFilterStackView = new FilterStackView(new GUIContent("Brush Mask Filters"), new SerializedObject( brushMaskFilterStack ) );
                    m_BrushMaskFilterStackView.FilterContext = filterContext;
                    m_BrushMaskFilterStackView.onChanged += SaveFilterStack;
                }

                return m_BrushMaskFilterStackView;
            }
        }

        FilterContext m_FilterContext;
        private FilterContext filterContext
        {
            get
            {
                if (m_FilterContext != null) return m_FilterContext;
                
                m_FilterContext = new FilterContext(FilterUtility.defaultFormat, Vector3.zero, 1f, 0f);
                return m_FilterContext;
            }
        }

        public void GetBrushMask(Terrain terrain, RenderTexture sourceRenderTexture,
            RenderTexture destinationRenderTexture,
            Vector3 position, float scale, float rotation)
        {
            filterContext.ReleaseRTHandles();

            using(new ActiveRenderTextureScope(null))
            {
                // set the filter context properties
                filterContext.brushPos = position;
                filterContext.brushSize = scale;
                filterContext.brushRotation = rotation;

                // bind properties for filters to read/write to
                var terrainData = terrain.terrainData;
                filterContext.floatProperties[FilterContext.Keywords.TerrainScale] = Mathf.Sqrt(terrainData.size.x * terrainData.size.x + terrainData.size.z * terrainData.size.z);
                filterContext.vectorProperties["_TerrainSize"] = new Vector4(terrainData.size.x, terrainData.size.y, terrainData.size.z, 0.0f);
                
                // bind terrain texture data
                filterContext.rtHandleCollection.AddRTHandle(0, FilterContext.Keywords.Heightmap, sourceRenderTexture.graphicsFormat);
                filterContext.rtHandleCollection.GatherRTHandles(sourceRenderTexture.width, sourceRenderTexture.height);
                Graphics.Blit(sourceRenderTexture, filterContext.rtHandleCollection[FilterContext.Keywords.Heightmap]);
                brushMaskFilterStack.Eval(filterContext, sourceRenderTexture, destinationRenderTexture);
            }
            
            filterContext.ReleaseRTHandles();
        }

        public void GetBrushMask(Terrain terrain, RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            GetBrushMask(terrain, sourceRenderTexture, destinationRenderTexture, raycastHitUnderCursor.point, brushSize, brushRotation);
        }
        
        public void GetBrushMask(RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            GetBrushMask(terrainUnderCursor, sourceRenderTexture, destinationRenderTexture);
        }

        public string brushName => m_Name;

        public float brushSize
        {
            get { return m_BrushSizeController?.brushSize ?? 25.0f; }
            set { m_BrushSizeController.brushSize = value; }
        }
        public float brushRotation
        {
            get { return m_BrushRotationController?.brushRotation ?? 0.0f; }
            set { m_BrushRotationController.brushRotation = value; }
        }
        public float brushStrength
        {
            get { return m_BrushStrengthController?.brushStrength ?? 1.0f; }
            set { m_BrushStrengthController.brushStrength = value; }
        }
        public float brushSpacing => m_BrushSpacingController?.brushSpacing ?? 0.0f;

        public float brushScatter => m_BrushScatterController?.brushScatter ?? 0.0f;

        private bool isSmoothing
        {
            get
            {
                if (m_BrushSmoothController != null)
                {
                    return Event.current.shift;
                }
                return false;
            }
        }
        public bool allowPaint => (m_BrushSpacingController?.allowPaint ?? true) && !isSmoothing;
        public bool InvertStrength => m_BrushModifierKeyController?.ModifierActive(BrushModifierKey.BRUSH_MOD_INVERT) ?? false;
        
        public bool isInUse
        {
            get
            {
                foreach(IBrushController c in m_Controllers)
                {
                    if(c.isInUse)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool s_MultipleControlShortcuts = true;

        #region GUIStyles
        private static class Styles
        {
            public static GUIStyle Box { get; private set; }
            public static readonly GUIContent brushMask = EditorGUIUtility.TrTextContent("Brush Mask");
            public static readonly GUIContent multipleControls = EditorGUIUtility.TrTextContent("Multiple Controls");
            public static readonly GUIContent stroke = EditorGUIUtility.TrTextContent("Stroke");

            public static readonly string kGroupBox = "GroupBox";

            static Styles()
            {
                Box = new GUIStyle(EditorStyles.helpBox);
                Box.normal.textColor = Color.white;
            }
        }
        #endregion

        Func<TerrainToolsAnalytics.IBrushParameter[]> m_analyticsCallback;
        protected BaseBrushUIGroup(string name, Func<TerrainToolsAnalytics.IBrushParameter[]> analyticsCall = null)
        {
            m_Name = name;
            m_analyticsCallback = analyticsCall;
        }

        #region Shortcut Handling
        #if UNITY_2019_1_OR_NEWER
        [ClutchShortcut("Terrain/Adjust Brush Strength (SceneView)", typeof(TerrainToolShortcutContext), KeyCode.A)]
        static void StrengthBrushShortcut(ShortcutArguments args) {
            if(s_MultipleControlShortcuts)
            {
                s_ShortcutHandler.HandleShortcutChanged(args, BrushShortcutType.Strength);
            }
        }

        [ClutchShortcut("Terrain/Adjust Brush Size (SceneView)", typeof(TerrainToolShortcutContext), KeyCode.S)]
        static void ResizeBrushShortcut(ShortcutArguments args) {
            if(s_MultipleControlShortcuts)
            {
                s_ShortcutHandler.HandleShortcutChanged(args, BrushShortcutType.Size);
            }
            else
            {
                s_ShortcutHandler.HandleShortcutChanged(args, BrushShortcutType.RotationSizeStrength);
            }
        }

        [ClutchShortcut("Terrain/Adjust Brush Rotation (SceneView)", typeof(TerrainToolShortcutContext), KeyCode.D)]
        private static void RotateBrushShortcut(ShortcutArguments args) {
            if(s_MultipleControlShortcuts)
            {
                s_ShortcutHandler.HandleShortcutChanged(args, BrushShortcutType.Rotation);
            }
        }
        #endif
        #endregion

        #region Add Controllers
        protected TController AddController<TController>(TController newController) where TController: IBrushController
        {
            m_Controllers.Add(newController);
            return newController;
        }

        protected TController AddRotationController<TController>(TController newController) where TController : IBrushRotationController
        {
            m_BrushRotationController = AddController(newController);
            return newController;
        }
        
        protected TController AddSizeController<TController>(TController newController) where TController : IBrushSizeController
        {
            m_BrushSizeController = AddController(newController);
            return newController;
        }
        
        protected TController AddStrengthController<TController>(TController newController) where TController : IBrushStrengthController
        {
            m_BrushStrengthController = AddController(newController);
            return newController;
        }
        
        protected TController AddSpacingController<TController>(TController newController) where TController : IBrushSpacingController
        {
            m_BrushSpacingController = AddController(newController);
            return newController;
        }
        
        protected TController AddScatterController<TController>(TController newController) where TController : IBrushScatterController
        {
            m_BrushScatterController = AddController(newController);
            return newController;
        }
        
        protected TController AddModifierKeyController<TController>(TController newController) where TController : IBrushModifierKeyController
        {
            m_BrushModifierKeyController = newController;
            return newController;
        }
        
        protected TController AddSmoothingController<TController>(TController newController) where TController : IBrushSmoothController
        {
            m_BrushSmoothController = newController;
            return newController;
        }
        #endregion

        #region Remove Controllers
        protected void RemoveController<TController>(TController controller) where TController: IBrushController
        {
            m_Controllers.Remove(controller);
        }

        protected void RemoveAllControllers()
        {
            m_BrushSizeController = null;
            m_BrushRotationController = null;
            m_BrushStrengthController = null;
            m_BrushSpacingController = null;
            m_BrushScatterController = null;
            m_BrushModifierKeyController = null;
            m_BrushSmoothController = null;
            
            m_Controllers.Clear();
        }
        #endregion

        #region IBrushEventHandler
        private bool m_RepaintRequested;
        
        public void RegisterEvent(Event newEvent)
        {
            m_ConsumedEvents.Add(newEvent);
        }
        
        public void ConsumeEvents(Terrain terrain, IOnSceneGUI editContext)
        {
            // Consume all of the events we've handled...
            foreach(Event currentEvent in m_ConsumedEvents)
            {
                currentEvent.Use();
            }
            m_ConsumedEvents.Clear();

            // Repaint everything if we need to...
            if(m_RepaintRequested)
            {
                EditorWindow view = EditorWindow.GetWindow<SceneView>();

                editContext.Repaint();
                view.Repaint();
                
                m_RepaintRequested = false;
            }
        }

        public void RequestRepaint()
        {
            m_RepaintRequested = true;
        }
        #endregion
        
        #region IBrushUIGroup
        public virtual void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            m_ShowBrushTextures = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.brushMask, m_ShowBrushTextures);
            if (m_ShowBrushTextures)
            {
                editContext.ShowBrushesGUI(0, BrushGUIEditFlags.SelectAndInspect);
            }

            EditorGUI.BeginChangeCheck();
            brushMaskFilterStackView.OnGUI();
            
            m_ShowModifierControls = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.stroke, m_ShowModifierControls);
            if (m_ShowModifierControls)
            {
                if(m_BrushStrengthController != null)
                {
                    EditorGUILayout.BeginVertical(Styles.kGroupBox);
                    m_BrushStrengthController.OnInspectorGUI(terrain, editContext);
                    EditorGUILayout.EndVertical();
                }

                if(m_BrushSizeController != null)
                {
                    EditorGUILayout.BeginVertical(Styles.kGroupBox);
                    m_BrushSizeController.OnInspectorGUI(terrain, editContext);
                    EditorGUILayout.EndVertical();
                }

                if(m_BrushRotationController != null)
                {
                    EditorGUILayout.BeginVertical(Styles.kGroupBox);
                    m_BrushRotationController?.OnInspectorGUI(terrain, editContext);
                    EditorGUILayout.EndVertical();
                }

                if((m_BrushSpacingController != null) || (m_BrushScatterController != null))
                {
                    EditorGUILayout.BeginVertical(Styles.kGroupBox);
                    m_BrushSpacingController?.OnInspectorGUI(terrain, editContext);
                    m_BrushScatterController?.OnInspectorGUI(terrain, editContext);
                    EditorGUILayout.EndVertical();
                }
            }

            if (EditorGUI.EndChangeCheck())
                TerrainToolsAnalytics.OnParameterChange();
        }

        private string getFilterStackFilePath
        {
            get { return Application.persistentDataPath + "/TerrainTools_" + m_Name + "_FilterStack.filterstack"; }
        }

        private FilterStack LoadFilterStack()
        {
            UnityEngine.Object[] obs = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget( getFilterStackFilePath );

            if( obs != null && obs.Length > 0 )
            {
                return obs[ 0 ] as FilterStack;
            }

            return null;
        }

        private void SaveFilterStack( FilterStack filterStack )
        {
            List< UnityEngine.Object > objList = new List< UnityEngine.Object >();
            objList.Add( filterStack );
            objList.AddRange( filterStack.filters );

            filterStack.filters.ForEach( ( f ) =>
            {
                var l = f.GetObjectsToSerialize();
                
                if( l != null && l.Count > 0 )
                {
                    objList.AddRange( l );
                }
            } );

            // write to the file
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(objList.ToArray(), getFilterStackFilePath, true );
        }

        public virtual void OnEnterToolMode()
        {
            s_MultipleControlShortcuts = EditorPrefs.GetBool("TerrainTools.MultipleControlShortcuts", true);

            m_BrushModifierKeyController?.OnEnterToolMode();
            m_Controllers.ForEach((controller) => controller.OnEnterToolMode(s_ShortcutHandler));
            
            TerrainToolsAnalytics.m_OriginalParameters = m_analyticsCallback?.Invoke();
        }

        public virtual void OnExitToolMode()
        {
            m_Controllers.ForEach((controller) => controller.OnExitToolMode(s_ShortcutHandler));
            m_BrushModifierKeyController?.OnExitToolMode();
            
            EditorPrefs.SetBool("TerrainTools.MultipleControlShortcuts", s_MultipleControlShortcuts);

            SaveFilterStack(brushMaskFilterStack);
        }

        public static bool isRecording = false;

        [Serializable]
        public class OnPaintOccurrence
        {
            [NonSerialized] internal static List<OnPaintOccurrence> history = new List<OnPaintOccurrence>();
            [NonSerialized] private static float prevRealTime;

            public OnPaintOccurrence(Texture brushTexture, float brushSize,
                                    float brushStrength, float brushRotation,
                                    float uvX, float uvY)
            {
                this.xPos = uvX;
                this.yPos = uvY;
                this.brushTextureAssetPath = AssetDatabase.GetAssetPath(brushTexture);
                this.brushStrength = brushStrength;
                this.brushSize = brushSize;

                if (history.Count == 0)
                {
                    duration = 0;
                }
                else
                {
                    duration = Time.realtimeSinceStartup - prevRealTime;
                }

                prevRealTime = Time.realtimeSinceStartup;
            }

            [SerializeField] public float xPos;
            [SerializeField] public float yPos;
            [SerializeField] public string brushTextureAssetPath;
            [SerializeField] public float brushStrength;
            [SerializeField] public float brushRotation;
            [SerializeField] public float brushSize;
            [SerializeField] public float duration;
        }

        public virtual void OnPaint(Terrain terrain, IOnPaint editContext)
        {
            filterContext.ReleaseRTHandles();

            // Manage brush capture history for playback in tests
            if (isRecording)
            {
                OnPaintOccurrence.history.Add(new OnPaintOccurrence(editContext.brushTexture, brushSize,
                                                                    brushStrength, brushRotation,
                                                                    editContext.uv.x, editContext.uv.y));
            }

            m_Controllers.ForEach((controller) => controller.OnPaint(terrain, editContext));

            if (isSmoothing)
            {
                Vector2 uv = editContext.uv;

                m_BrushSmoothController.kernelSize = (int)Mathf.Max(1, 0.1f * m_BrushSizeController.brushSize);
                m_BrushSmoothController.OnPaint(terrain, editContext, brushSize, brushRotation, brushStrength, uv);
            }

            /// Ensure that we re-randomize where the next scatter operation will place the brush,
            /// that way we can render the preview in a representative manner.
            m_BrushScatterController?.RequestRandomisation();

            TerrainToolsAnalytics.UpdateAnalytics(this, m_analyticsCallback);
            
            filterContext.ReleaseRTHandles();
        }

        public virtual void OnSceneGUI2D(Terrain terrain, IOnSceneGUI editContext)
        {
            StringBuilder builder = new StringBuilder();

            Handles.BeginGUI();
            {
                AppendBrushInfo(terrain, editContext, builder);
                string text = builder.ToString();
                string trimmedText = text.Trim('\n', '\r', ' ', '\t');
                GUILayout.Box(trimmedText, Styles.Box, GUILayout.ExpandWidth(false));
                Handles.EndGUI();
            }
        }

        public virtual void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            filterContext.ReleaseRTHandles();

            Event currentEvent = Event.current;
            int controlId = GUIUtility.GetControlID(BrushUITools.s_TerrainEditorHash, FocusType.Passive);

            if(canUpdateTerrainUnderCursor)
            {
                isRaycastHitUnderCursorValid = editContext.hitValidTerrain;
                terrainUnderCursor = terrain;
                raycastHitUnderCursor = editContext.raycastHit;
            }
            
            m_Controllers.ForEach((controller) => controller.OnSceneGUI(currentEvent, controlId, terrain, editContext));
            
            ConsumeEvents(terrain, editContext);

            if (!isRecording && OnPaintOccurrence.history.Count != 0) {
                SaveBrushData();
            }

            brushMaskFilterStackView.OnSceneGUI(editContext.sceneView);

            if( editContext.hitValidTerrain && Event.current.keyCode == KeyCode.F && Event.current.type != EventType.Layout )
            {
                SceneView.currentDrawingSceneView.Frame( new Bounds() { center = raycastHitUnderCursor.point, size = new Vector3( brushSize, 1, brushSize ) }, false );
                Event.current.Use();
            }
            
            filterContext.ReleaseRTHandles();
        }

        private void SaveBrushData() {
            // Copy paintOccurrenceHistory to temp variable to prevent re-activating this condition
            List<OnPaintOccurrence> tmpPaintOccurrenceHistory = new List<OnPaintOccurrence>(OnPaintOccurrence.history);
            OnPaintOccurrence.history.Clear();

            string fileName = EditorUtility.SaveFilePanelInProject("Save input playback", "PaintHistory", "txt", "");
            if (fileName == "") {
                return;
            }

            FileStream file;
            if (File.Exists(fileName)) file = File.OpenWrite(fileName);
            else file = File.Create(fileName);

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(file, tmpPaintOccurrenceHistory);
            file.Close();
        }

        public virtual void AppendBrushInfo(Terrain terrain, IOnSceneGUI editContext, StringBuilder builder)
        {

            builder.AppendLine($"Brush: {m_Name}");
            builder.AppendLine();
            
            m_Controllers.ForEach((controller) => controller.AppendBrushInfo(terrain, editContext, builder));
            builder.AppendLine();
            builder.AppendLine(validationMessage);
        }

        public bool ScatterBrushStamp(ref Terrain terrain, ref Vector2 uv)
        {
            if(m_BrushScatterController == null) {
                bool invalidTerrain = terrain == null;
                
                return !invalidTerrain;
            }
            else {
                Vector2 scatteredUv = m_BrushScatterController.ScatterBrushStamp(uv, brushSize);
                Terrain scatteredTerrain = terrain;
                
                // Ensure that our UV is over a valid terrain AND in the range 0-1...
                while((scatteredTerrain != null) && (scatteredUv.x < 0.0f)) {
                    scatteredTerrain = scatteredTerrain.leftNeighbor;
                    scatteredUv.x += 1.0f;
                }
                while((scatteredTerrain != null) && (scatteredUv.x > 1.0f)) {
                    scatteredTerrain = scatteredTerrain.rightNeighbor;
                    scatteredUv.x -= 1.0f;
                }
                while((scatteredTerrain != null) && scatteredUv.y < 0.0f) {
                    scatteredTerrain = scatteredTerrain.bottomNeighbor;
                    scatteredUv.y += 1.0f;
                }
                while((scatteredTerrain != null) && (scatteredUv.y > 1.0f)) {
                    scatteredTerrain = scatteredTerrain.topNeighbor;
                    scatteredUv.y -= 1.0f;
                }

                // Did we run out of terrains?
                if(scatteredTerrain == null) {
                    return false;
                }
                else {
                    terrain = scatteredTerrain;
                    uv = scatteredUv;
                    return true;
                }
            }
        }

        public bool ModifierActive(BrushModifierKey k)
        {
            return m_BrushModifierKeyController?.ModifierActive(k) ?? false;
        }

        #endregion

        #region IBrushTerrainCache
        private int m_TerrainUnderCursorLockCount = 0;
        
        public void LockTerrainUnderCursor(bool cursorVisible)
        {
            if(m_TerrainUnderCursorLockCount == 0)
            {
                Cursor.visible = cursorVisible;
            }
            m_TerrainUnderCursorLockCount++;
        }

        public void UnlockTerrainUnderCursor()
        {
            m_TerrainUnderCursorLockCount--;
            if(m_TerrainUnderCursorLockCount == 0)
            {
                // Last unlock enables the cursor...
                Cursor.visible = true;
            }
            else if(m_TerrainUnderCursorLockCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(m_TerrainUnderCursorLockCount), "Cannot reduce m_TerrainUnderCursorLockCount below zero. Possible mismatch between lock/unlock calls.");
            }
        }

        public bool canUpdateTerrainUnderCursor => m_TerrainUnderCursorLockCount == 0;
        
        public Terrain terrainUnderCursor { get; private set; }
        public bool isRaycastHitUnderCursorValid { get; private set; }
        public RaycastHit raycastHitUnderCursor { get; private set; }
        public virtual string validationMessage { get; set; }
        #endregion
    }
}
