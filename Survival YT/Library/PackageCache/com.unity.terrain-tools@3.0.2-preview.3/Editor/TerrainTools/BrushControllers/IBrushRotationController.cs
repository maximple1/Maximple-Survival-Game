
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public interface IBrushRotationController : IBrushController
	{
		float brushRotation { get; set; }
		float currentRotation { get; }
	}
}
