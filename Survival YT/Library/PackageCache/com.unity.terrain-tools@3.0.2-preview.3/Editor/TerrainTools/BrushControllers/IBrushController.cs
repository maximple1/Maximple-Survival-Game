
using System.Text;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public interface IBrushController
	{
		bool isInUse { get; }
		void OnEnterToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler);
		void OnExitToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler);
		
		void OnSceneGUI(Event currentEvent, int controlId, Terrain terrain, IOnSceneGUI editContext);
		void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext);
		bool OnPaint(Terrain terrain, IOnPaint editContext);
		
		void AppendBrushInfo(Terrain terrain, IOnSceneGUI editContext, StringBuilder builder);
	}
}
