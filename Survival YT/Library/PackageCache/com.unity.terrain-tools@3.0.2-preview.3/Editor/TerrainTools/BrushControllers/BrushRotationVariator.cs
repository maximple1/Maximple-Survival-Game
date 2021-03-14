
using System.Text;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class BrushRotationVariator : BaseBrushVariator, IBrushRotationController {

        private const float kMinBrushRotation = -180.0f;
        private const float kMaxBrushRotation = 180.0f;
        private const float kDefaultBrushRotation = 0.0f;
        private const float kDefaultMouseSensitivity = 1.0f;

        private readonly TerrainFloatMinMaxValue m_BrushRotation = new TerrainFloatMinMaxValue(styles.brushRotation, kDefaultBrushRotation, kMinBrushRotation, kMaxBrushRotation, false);
        private readonly TerrainFloatMinMaxValue m_SmoothJitterFreq = new TerrainFloatMinMaxValue(styles.jitterFreq, 1.0f, 0.0001f, 10.0f);
        private bool m_FollowMouse;
        private bool m_AdjustingRotation;
        public override bool isInUse => m_AdjustingRotation;

        private readonly BrushJitterHandler m_JitterHandler;

        // Values below are not modifiable directly -- they are means of caching the current state of the brush and/or used in calculations
        private float m_PreviousRotation;
        private RaycastHit m_PreviousRaycastHit;

        class Styles {
            public readonly GUIContent brushRotation = EditorGUIUtility.TrTextContent("Brush Rotation", "Rotation of the brush used to paint.");
            public readonly GUIContent brushRotOffset = EditorGUIUtility.TrTextContent("Offset", "Rotation relative to mouse movement. Use Ctrl+Shift (Command+Shift on Mac) to control rotation in view.");
            public readonly GUIContent brushFollowMouse = EditorGUIUtility.TrTextContent("Rotation Follows Mouse", "Brush rotation follows mouse movement. (No Randomization)");
            public readonly GUIContent jitterFreq = EditorGUIUtility.TrTextContent("Jitter Frequency", "Controls the frequency of the smooth noise function controlling the jitter.");
        }

        static readonly Styles styles = new Styles();

        public float brushRotation
        {
            get { return CalculateRotation(m_BrushRotation.value); }
            set
            {
                m_BrushRotation.value = Mathf.Clamp(value, kMinBrushRotation, kMaxBrushRotation);
                m_PreviousRotation = m_BrushRotation.value;
            }
        }
        public float currentRotation => CalculateRotation(m_BrushRotation.value);

        public BrushRotationVariator(string toolName, IBrushEventHandler eventHandler, IBrushTerrainCache terrainCache, bool smoothJitter = false) : base(toolName, eventHandler, terrainCache)
        {
            m_JitterHandler = new BrushJitterHandler(0.0f, kMinBrushRotation, kMaxBrushRotation, smoothJitter);
            m_BrushRotation.wrapValue = true;
        }

        private float CalculateRotation(float initialRotation) {
            return m_JitterHandler.CalculateValue(initialRotation);
        }
        
        public float GetMouseFollowAngle(RaycastHit hit) {
            Vector3 direction = hit.point - m_PreviousRaycastHit.point;

            float theAngle = Mathf.Rad2Deg * -Mathf.Atan2(direction.z, direction.x);

            // Our assumption here is that if the mouse is being dragged, we are doing a follow-mouse action
            // Which is also why we are always saving previous rotation as an offset relative to m_BrushRotation
            // That's how this function is used in the mouse drag case, which is the only time we care about
            // m_PreviousRotation.  The goal here is to keep the rotation changes as smooth as possible.
            if (Event.current.type == EventType.MouseDrag) {
                // This is the extra step to account for the instabilities you get with atan2...
                float angleDelta = theAngle - m_PreviousRotation;
                if (angleDelta < -90) {
                    theAngle += 180;
                } else if (angleDelta > 90) {
                    theAngle -= 180;
                }

                Vector2 mouseDelta = CalculateMouseDeltaFromInitialPosition(Event.current);
                // There should be a heavy weight to the previous angle if there is very little mouse movement
                // and also we assume that a very large delta in the angle in one frame is probably a sensitivity error,
                // so we should take that to mean weight more heavily to the previous angle.
                float weight = Mathf.Exp(-0.05f * mouseDelta.sqrMagnitude);
                theAngle = weight * m_PreviousRotation + (1 - weight) * theAngle;

                angleDelta = (theAngle - m_PreviousRotation) * Mathf.Deg2Rad;
                weight = Mathf.Exp(angleDelta * angleDelta);
                theAngle = weight * theAngle + (1 - weight) * m_PreviousRotation;
            }
            return theAngle;
        }

        private void UpdateCurrentRotation(float rotValue) {
            // Just a quicky to make sure that both the current and previous rotation are up to date
            m_PreviousRotation = m_BrushRotation.value;
            m_BrushRotation.value = rotValue;
        }

        private void BeginAdjustingRotation()
        {
            LockTerrainUnderCursor(true);
            m_AdjustingRotation = true;
        }

        private void EndAdjustingRotation()
        {
            m_AdjustingRotation = false;
            UnlockTerrainUnderCursor();
        }

        #region IBrushController
        public override void OnEnterToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {
            base.OnEnterToolMode(shortcutHandler);
            
            m_BrushRotation.value = GetEditorPrefs("TerrainBrushRotation", kDefaultBrushRotation);
            m_BrushRotation.mouseSensitivity = GetEditorPrefs("TerrainBrushRotMouseSensitivity",kDefaultMouseSensitivity);
            m_JitterHandler.jitter = GetEditorPrefs("TerrainBrushRotJitter", 0.0f);
            m_FollowMouse = GetEditorPrefs("TerrainBrushRotFollowMouse", false);

            shortcutHandler.AddActions(BrushShortcutType.Rotation, BeginAdjustingRotation, EndAdjustingRotation);

            UpdateCurrentRotation(m_BrushRotation.value);
        }

        public override void OnExitToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {
            shortcutHandler.RemoveActions(BrushShortcutType.Rotation);
            
            SetEditorPrefs("TerrainBrushRotation", m_BrushRotation.value);
            SetEditorPrefs("TerrainBrushRotMouseSensitivity",m_BrushRotation.mouseSensitivity);
            SetEditorPrefs("TerrainBrushRotJitter", m_JitterHandler.jitter);
            SetEditorPrefs("TerrainBrushRotFollowMouse", m_FollowMouse);
            
            base.OnExitToolMode(shortcutHandler);
        }

        public override void OnSceneGUI(Event currentEvent, int controlId, Terrain terrain, IOnSceneGUI editContext) {
            RaycastHit raycastHit = editContext.raycastHit;

            base.OnSceneGUI(currentEvent, controlId, terrain, editContext);

            m_JitterHandler.frequency = m_SmoothJitterFreq.value;
            m_JitterHandler.Update();

            if(m_AdjustingRotation)
            {
                float rotation = GetMouseFollowAngle(raycastHit);
                int rotationInDegrees = Mathf.RoundToInt(rotation);
                GUIStyle style = new GUIStyle();
                
                style.normal.background = Texture2D.whiteTexture;
                style.fontSize = 12;
                Handles.Label(raycastHit.point, $"Rotation: {rotationInDegrees}°", style);
                UpdateCurrentRotation(rotation);
                RequestRepaint();
            }
            else
            {
                m_PreviousRaycastHit = raycastHit;
            }
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) {
            base.OnInspectorGUI(terrain, editContext);
            
            m_BrushRotation.DrawInspectorGUI();
            m_JitterHandler.OnGuiLayout("Randomly vary the brush rotation between the values in the slider.");
            if(m_JitterHandler.smoothJitter) {
                m_SmoothJitterFreq.DrawInspectorGUI();
            }
            
            //m_FollowMouse = EditorGUILayout.Toggle(styles.brushFollowMouse, m_FollowMouse, GUILayout.ExpandWidth(false));

            UpdateCurrentRotation(m_BrushRotation.value);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            m_JitterHandler.RequestRandomization();
            return base.OnPaint(terrain, editContext);
        }

        public override void AppendBrushInfo(Terrain terrain, IOnSceneGUI editContext, StringBuilder builder)
        {
            base.AppendBrushInfo(terrain, editContext, builder);
            builder.AppendLine($"Rotation = {brushRotation:F3}");
        }
        #endregion

        #region Mouse Handling
		protected override bool OnBeginModifier()
        {
			base.OnBeginModifier();

            LockTerrainUnderCursor(false);
            return true;
		}

		protected override bool OnModifierUsingMouseMove(Event mouseEvent, Terrain terrain, IOnSceneGUI editContext)
		{
			base.OnModifierUsingMouseMove(mouseEvent, terrain, editContext);

            Vector2 delta = CalculateMouseDelta(mouseEvent, m_BrushRotation.mouseSensitivity);
			
            m_BrushRotation.value -= delta.x;
            UpdateCurrentRotation(m_BrushRotation.value);
            return true;
		}

		protected override bool OnEndModifier()
		{
			base.OnEndModifier();

            UnlockTerrainUnderCursor();
            return true;
		}
        #endregion
    }
}
