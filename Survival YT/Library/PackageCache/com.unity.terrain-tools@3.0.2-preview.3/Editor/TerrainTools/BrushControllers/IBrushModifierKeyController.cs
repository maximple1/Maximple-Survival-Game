
using System;

namespace UnityEditor.Experimental.TerrainAPI
{
	[Flags]
	public enum BrushModifierKey {
		BRUSH_MOD_INVERT = 0,
		BRUSH_MOD_1 = 1,
		BRUSH_MOD_2 = 2,
		BRUSH_MOD_3 = 3
	}
	
	public interface IBrushModifierKeyController
	{
		event Action<BrushModifierKey> OnModifierPressed;
		event Action<BrushModifierKey> OnModifierReleased;
		
		void OnEnterToolMode();
		void OnExitToolMode();

		bool ModifierActive(BrushModifierKey k);
	}
}
