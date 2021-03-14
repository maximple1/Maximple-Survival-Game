
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public interface IBrushSizeController : IBrushController
	{
		float brushSize { get; set; }
	}
}
