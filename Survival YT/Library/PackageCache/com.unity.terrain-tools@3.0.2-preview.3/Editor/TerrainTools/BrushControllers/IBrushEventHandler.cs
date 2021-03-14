
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public interface IBrushEventHandler
	{
		/// <summary>
		/// Register a system event for processing later.
		/// </summary>
		/// <param name="newEvent"></param>
		void RegisterEvent(Event newEvent);
		
		/// <summary>
		/// Consume previously registered events.
		/// </summary>
		/// <param name="terrain"></param>
		/// <param name="editContext"></param>
		void ConsumeEvents(Terrain terrain, IOnSceneGUI editContext);

		/// <summary>
		/// Allows us to request a repaint of the GUI and scene-view.
		/// </summary>
		void RequestRepaint();
	}
}
