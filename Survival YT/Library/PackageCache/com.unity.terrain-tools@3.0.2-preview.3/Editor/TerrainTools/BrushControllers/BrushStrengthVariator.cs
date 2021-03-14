
using System.Text;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class BrushStrengthVariator : BaseBrushVariator, IBrushStrengthController {

        const float kMinBrushStrength = 0.0f;
        const float kMaxBrushStrength = 1.0f;
        const float kDefaultBrushStrength = kMaxBrushStrength;

        private float defaultBrushStrength;
        
        class Styles
        {
            public readonly GUIContent brushStrength = EditorGUIUtility.TrTextContent("Brush Strength", "Strength of the brush paint effect.");
        }
        
        static readonly Styles styles = new Styles();

        private readonly TerrainFloatMinMaxValue m_BrushStrength = new TerrainFloatMinMaxValue(styles.brushStrength, kDefaultBrushStrength, kMinBrushStrength, kMaxBrushStrength, true);
        private readonly BrushJitterHandler m_JitterHandler = new BrushJitterHandler(0.0f, kMinBrushStrength, kMaxBrushStrength);

        private bool m_AdjustingStrength;
        public override bool isInUse => m_AdjustingStrength;

        private RaycastHit m_LastRaycastHit;

        public float brushStrength
        {
            get { return m_JitterHandler.CalculateValue(m_BrushStrength.value); }
            set { m_BrushStrength.value = Mathf.Clamp(value, kMinBrushStrength, kMaxBrushStrength); }
        }
        public float brushStrengthUI => Mathf.Clamp(m_BrushStrength.value, kMinBrushStrength, kMaxBrushStrength);

        public BrushStrengthVariator(string toolName, IBrushEventHandler eventHandler, IBrushTerrainCache terrainCache, float defaultValue = kDefaultBrushStrength) : base(toolName, eventHandler, terrainCache) {
            this.defaultBrushStrength = defaultValue;
        }
        
        private void BeginAdjustingStrength()
        {
            LockTerrainUnderCursor(true);
            m_AdjustingStrength = true;
        }

        private void EndAdjustingStrength()
        {
            m_AdjustingStrength = false;
            UnlockTerrainUnderCursor();
        }

        #region IBrushController
        public override void OnEnterToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {
            base.OnEnterToolMode(shortcutHandler);
            
            shortcutHandler.AddActions(BrushShortcutType.Strength, BeginAdjustingStrength, EndAdjustingStrength);
            
            m_BrushStrength.value = GetEditorPrefs("TerrainBrushStrength", defaultBrushStrength);
            m_JitterHandler.jitter = GetEditorPrefs("TerrainBrushRandomStrength", 0.0f);
            m_BrushStrength.minValue = GetEditorPrefs("TerrainBrushStrengthMin", 0.0f);
            m_BrushStrength.maxValue = GetEditorPrefs("TerrainBrushStrengthMax", 1.0f);
        }
        
        public override void OnExitToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {            
            SetEditorPrefs("TerrainBrushStrength", m_BrushStrength.value);
            SetEditorPrefs("TerrainBrushStrengthMouseSensitivity", m_BrushStrength.mouseSensitivity);
            SetEditorPrefs("TerrainBrushRandomStrength", m_JitterHandler.jitter);
            SetEditorPrefs("TerrainBrushStrengthMin", m_BrushStrength.minValue);
            SetEditorPrefs("TerrainBrushStrengthMax", m_BrushStrength.maxValue);

            shortcutHandler.RemoveActions(BrushShortcutType.Strength);
            
            base.OnExitToolMode(shortcutHandler);
        }
        
        public override void OnSceneGUI(Event currentEvent, int controlId, Terrain terrain, IOnSceneGUI editContext) {
            Event e = Event.current;
 
            base.OnSceneGUI(currentEvent, controlId, terrain, editContext);
            
            m_JitterHandler.Update();
            
            if (m_AdjustingStrength)
            {
                float strength = m_BrushStrength.value;
                
                strength += 0.001f * e.delta.x;
                m_BrushStrength.value = strength;

                int strengthPct = Mathf.RoundToInt(100.0f * strength);

                GUIStyle style = new GUIStyle();
                style.normal.background = Texture2D.whiteTexture;
                style.fontSize = 12;
                Handles.Label(m_LastRaycastHit.point, $"Strength: {strengthPct}%", style);
                editContext.Repaint();
            } else {
                m_LastRaycastHit = editContext.raycastHit;
            }
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            m_JitterHandler.RequestRandomization();
            return base.OnPaint(terrain, editContext);
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) {
            base.OnInspectorGUI(terrain, editContext);
            
            m_BrushStrength.DrawInspectorGUI();
            m_JitterHandler.OnGuiLayout("Allow random variation of brush intensity");
        }

        public override void AppendBrushInfo(Terrain terrain, IOnSceneGUI editContext, StringBuilder builder)
        {
            base.AppendBrushInfo(terrain, editContext, builder);
            builder.AppendLine($"Strength = {m_BrushStrength.value:F3}");
        }
        #endregion

        #region Mouse Handling
        protected override bool OnModifierUsingMouseWheel(Event mouseEvent, Terrain terrain, IOnSceneGUI editContext)
        {
            base.OnModifierUsingMouseWheel(mouseEvent, terrain, editContext);

            Vector2 delta = CalculateMouseDelta(mouseEvent, m_BrushStrength.mouseSensitivity);
            float strength = m_BrushStrength.value;
            
            strength += delta.y;
            m_BrushStrength.value = strength;

            return true;
        }
        #endregion
    }
}
