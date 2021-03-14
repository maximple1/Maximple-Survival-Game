
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public interface IBrushSmoothController
	{
		bool active { get; }
        int kernelSize { get; set; }

		void OnEnterToolMode();
		void OnExitToolMode();
		void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext);
		void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext);
		bool OnPaint(Terrain terrain, IOnPaint editContext, float brushSize, float brushRotation, float brushStrength, Vector2 uv);
	}
}
