
using System.Text;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public class BrushScatterVariator : BaseBrushVariator, IBrushScatterController {
		
		private float m_BrushScatter;
		public float brushScatter => m_BrushScatter;
				
		private bool m_UseNewRandomValue;
		private Vector2 m_RandomValues;

		public Vector2 ScatterBrushStamp(Vector2 uv, float brushSize)
		{
			float scatterRadius = 0.5f * m_BrushScatter;
			Vector2 r = scatterRadius * m_RandomValues;

			return r + uv;
		}

		class Styles {
			public readonly GUIContent brushScatter = EditorGUIUtility.TrTextContent("Brush Scatter", "Randomized scattering perpendicular to the stroke direction");
		}

		static readonly Styles styles = new Styles();

		public BrushScatterVariator(string toolName, IBrushEventHandler eventHandler, IBrushTerrainCache terrainCache) : base(toolName, eventHandler, terrainCache) {
		}

		#region IBrushScatterController
		public void RequestRandomisation()
		{
			m_UseNewRandomValue = true;
		}

		public override void OnEnterToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {
			base.OnEnterToolMode(shortcutHandler);		
			m_BrushScatter = GetEditorPrefs("TerrainBrushScatter", 0.0f);
		}

		public override void OnExitToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler) {
			SetEditorPrefs("TerrainBrushScatter", m_BrushScatter);
			base.OnExitToolMode(shortcutHandler);
		}

		public override void OnSceneGUI(Event currentEvent, int controlId, Terrain terrain, IOnSceneGUI editContext)
		{
			if(m_UseNewRandomValue)
			{
				m_RandomValues = Random.insideUnitCircle;
				m_UseNewRandomValue = false;
			}

			base.OnSceneGUI(currentEvent, controlId, terrain, editContext);
		}

		public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) {
			base.OnInspectorGUI(terrain, editContext);
			
			m_BrushScatter = BrushUITools.PercentSlider(styles.brushScatter, m_BrushScatter, 0.0f, 1.0f);
		}

		public override void AppendBrushInfo(Terrain terrain, IOnSceneGUI editContext, StringBuilder builder)
		{
			base.AppendBrushInfo(terrain, editContext, builder);
			builder.AppendLine($"Scatter = {m_BrushScatter:F3}");
		}
		#endregion
	}
}
