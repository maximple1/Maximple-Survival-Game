
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
	public interface IBrushNoiseController
	{
		void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext);

		void Blit(BrushTransform brushXform, ref RenderTexture target);
	}
}
