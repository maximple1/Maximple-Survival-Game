
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public interface IBrushTerrainCache
	{
		void LockTerrainUnderCursor(bool cursorVisible);
		void UnlockTerrainUnderCursor();
		bool canUpdateTerrainUnderCursor { get; }
		
		Terrain terrainUnderCursor { get; }
		bool isRaycastHitUnderCursorValid { get; }
		RaycastHit raycastHitUnderCursor { get; }
	}
}
