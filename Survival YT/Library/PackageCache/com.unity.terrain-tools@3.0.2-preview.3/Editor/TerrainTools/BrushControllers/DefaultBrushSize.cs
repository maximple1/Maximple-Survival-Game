
using System.Text;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public class DefaultBrushSize : IBrushSizeController {
		private readonly TerrainFloatMinMaxValue m_BrushSize = new TerrainFloatMinMaxValue(styles.brushSize, 25.0f, 0.0f, 500.0f);

		private class Styles {
			public readonly GUIContent brushSize = EditorGUIUtility.TrTextContent("Brush Size", "Size of the brush used to paint.");
		}

		private static readonly Styles styles = new Styles();
		private readonly string m_NamePrefix;

		public float brushSize
		{
			get { return m_BrushSize.value; }
			set { m_BrushSize.value = value; }
		}

		public bool isInUse => false;

		public DefaultBrushSize(string toolName) {
			m_NamePrefix = toolName;
		}

		public void OnEnterToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {
			m_BrushSize.value = EditorPrefs.GetFloat(m_NamePrefix + ".TerrainBrushSize", 25.0f);
		}
		public void OnExitToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {
			EditorPrefs.SetFloat(m_NamePrefix + ".TerrainBrushSize", m_BrushSize.value);
		}
		public void OnSceneGUI(Event currentEvent, int controlId, Terrain terrain, IOnSceneGUI editContext) {
		}
		public void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) {
			TerrainData terrainData = terrain.terrainData;
			float maxBrushSize = Mathf.Min(terrainData.size.x, terrainData.size.z) - 1;
			m_BrushSize.maxValue = Mathf.Min(maxBrushSize, m_BrushSize.maxValue);
			m_BrushSize.DrawInspectorGUI();
			//m_BrushSize = Mathf.RoundToInt(EditorGUILayout.Slider(styles.brushSize, m_BrushSize, 2, maxBrushSize));
		}
		public bool OnPaint(Terrain terrain, IOnPaint editContext) {
			return true;
		}
		public void AppendBrushInfo(Terrain terrain, IOnSceneGUI editContext, StringBuilder builder) {
		}
	}
}
