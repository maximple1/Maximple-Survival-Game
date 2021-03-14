
using System.Text;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public class DefaultBrushStrength : IBrushStrengthController {
		internal float m_BrushStrength = 0.5f;
		const float kMinBrushStrength = (1.1F / ushort.MaxValue) / 0.01f;
		private readonly string m_NamePrefix;

		public bool isInUse => false;

		private class Styles {
			public readonly GUIContent brushStrength = EditorGUIUtility.TrTextContent("Brush Strength", "Strength of the brush paint effect.");
		}

		static readonly Styles styles = new Styles();

		public float brushStrength
		{
			get { return m_BrushStrength; }
			set { m_BrushStrength = value; }
		}

		public DefaultBrushStrength(string toolName) {
			m_NamePrefix = toolName;
		}

		public void OnEnterToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {
			m_BrushStrength = EditorPrefs.GetFloat(m_NamePrefix + ".TerrainBrushStrength", 0.5f);
		}
		public void OnExitToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {
			EditorPrefs.SetFloat(m_NamePrefix + ".TerrainBrushStrength", m_BrushStrength);
		}
		public void OnSceneGUI(Event currentEvent, int controlId, Terrain terrain, IOnSceneGUI editContext) {
		}
		public void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) {
			m_BrushStrength = BrushUITools.PercentSlider(styles.brushStrength, m_BrushStrength, kMinBrushStrength, 1.0f);
		}
		public bool OnPaint(Terrain terrain, IOnPaint editContext) {
			return true;
		}
		public void AppendBrushInfo(Terrain terrain, IOnSceneGUI editContext, StringBuilder builder) {
		}
	}
}
