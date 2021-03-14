
using System.Text;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public abstract class BaseBrushVariator : IBrushController, IBrushTerrainCache
	{
		private readonly string m_NamePrefix;
		private readonly IBrushEventHandler m_EventHandler;
		private readonly IBrushTerrainCache m_TerrainCache;

		public virtual bool isInUse => m_ModifierActive;
		
		protected BaseBrushVariator(string toolName, IBrushEventHandler eventHandler, IBrushTerrainCache terrainCache)
		{
			m_NamePrefix = toolName;
			m_EventHandler = eventHandler;
			m_TerrainCache = terrainCache;
		}

		protected void RequestRepaint()
		{
			m_EventHandler.RequestRepaint();
		}

		private void OnModifierKeyPressed()
		{
			m_ModifierActive = true;
			HandleBeginModifier();
		}

		private void OnModifierKeyReleased()
		{
			HandleEndModifier();
			m_ModifierActive = false;
		}

		#region Editor Preferences
		protected bool GetEditorPrefs(string name, bool defaultValue)
		{
			return EditorPrefs.GetBool($"{m_NamePrefix}.{name}", defaultValue);
		}

		protected void SetEditorPrefs(string name, bool currentValue)
		{
			EditorPrefs.SetBool($"{m_NamePrefix}.{name}", currentValue);
		}
		
		protected float GetEditorPrefs(string name, float defaultValue)
		{
			return EditorPrefs.GetFloat($"{m_NamePrefix}.{name}", defaultValue);
		}

		protected void SetEditorPrefs(string name, float currentValue)
		{
			EditorPrefs.SetFloat($"{m_NamePrefix}.{name}", currentValue);
		}
		#endregion

		#region Mouse Handling
		private bool m_ModifierActive;
		private Vector2 m_InitialMousePosition;

		protected Vector2 CalculateMouseDeltaFromInitialPosition(Event mouseEvent, float scale = 1.0f)
		{
			Vector2 mousePosition = mouseEvent.mousePosition;
			Vector2 delta = m_InitialMousePosition - mousePosition;
			Vector2 scaledDelta = delta * scale;

			return scaledDelta;
		}

		protected static Vector2 CalculateMouseDelta(Event mouseEvent, float scale = 1.0f)
		{
			Vector2 delta = mouseEvent.delta;
			Vector2 scaledDelta = delta * scale;

			return scaledDelta;
		}
		
		protected virtual bool OnBeginModifier()
		{
			return false;
		}
        
		protected virtual bool OnModifierUsingMouseMove(Event mouseEvent, Terrain terrain, IOnSceneGUI editContext)
		{
			if(!m_ModifierActive)
			{
				m_InitialMousePosition = mouseEvent.mousePosition;
			}
			return false;
		}

		protected virtual bool OnModifierUsingMouseWheel(Event mouseEvent, Terrain terrain, IOnSceneGUI editContext)
		{
			return false;
		}

		protected virtual bool OnEndModifier()
		{
			return false;
		}
		
		private bool HandleBeginModifier()
		{			
			bool consumeEvent = OnBeginModifier();

			return consumeEvent;
		}

		private bool HandleModifierUsingMouseMove(Event mouseEvent, Terrain terrain, IOnSceneGUI editContext)
		{
			bool consumeEvent = OnModifierUsingMouseMove(mouseEvent, terrain, editContext);

			return consumeEvent;
		}

		private bool HandleModifierUsingMouseWheel(Event mouseEvent, Terrain terrain, IOnSceneGUI editContext)
		{
			bool consumeEvent = OnModifierUsingMouseWheel(mouseEvent, terrain, editContext);

			return consumeEvent;
		}

		private bool HandleEndModifier()
		{
			bool consumeEvent = OnEndModifier();
			
			return consumeEvent;
		}

		private bool ProcessMouseEvent(Event mouseEvent, int controlId, Terrain terrain, IOnSceneGUI editContext)
		{
			bool consumeEvent = false;
			
			if(m_ModifierActive)
			{
				EventType eventType = mouseEvent.GetTypeForControl(controlId);

				switch(eventType)
				{
					case EventType.MouseMove:
					{
						consumeEvent |= HandleModifierUsingMouseMove(mouseEvent, terrain, editContext);
						break;
					}
	
					case EventType.ScrollWheel:
					{
						consumeEvent |= HandleModifierUsingMouseWheel(mouseEvent, terrain, editContext);
						break;
					}
				} // End of switch.
			}

			if(consumeEvent)
			{
				// We changed something - time to repaint...
				RequestRepaint();
				return true;
			}
			else
			{
				return false;
			}
		}
		#endregion

		#region IBrushController
		public virtual void OnEnterToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler)
		{
			shortcutHandler.AddActions(BrushShortcutType.RotationSizeStrength, OnModifierKeyPressed, OnModifierKeyReleased);
		}

		public virtual void OnExitToolMode(BrushShortcutHandler<BrushShortcutType> shortcutHandler)
		{
			shortcutHandler.RemoveActions(BrushShortcutType.RotationSizeStrength);
		}

		public virtual void OnSceneGUI(Event currentEvent, int controlId, Terrain terrain, IOnSceneGUI editContext)
		{
			if(currentEvent.isMouse || currentEvent.isScrollWheel)
			{
				if(ProcessMouseEvent(currentEvent, controlId, terrain, editContext))
				{
					m_EventHandler.RegisterEvent(currentEvent);
				}
			}
		}

		public virtual void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
		{
		}

		public virtual bool OnPaint(Terrain terrain, IOnPaint editContext)
		{
			return true;
		}

		public virtual void AppendBrushInfo(Terrain terrain, IOnSceneGUI editContext, StringBuilder builder)
		{
		}
		#endregion

		#region IBrushTerrainCache
		public Terrain terrainUnderCursor => m_TerrainCache.terrainUnderCursor;
		public bool isRaycastHitUnderCursorValid => m_TerrainCache.isRaycastHitUnderCursorValid;
		public RaycastHit raycastHitUnderCursor => m_TerrainCache.raycastHitUnderCursor;

		public bool canUpdateTerrainUnderCursor => m_TerrainCache.canUpdateTerrainUnderCursor;

		public void LockTerrainUnderCursor(bool cursorVisible)
		{
			m_TerrainCache.LockTerrainUnderCursor(cursorVisible);
		}

		public void UnlockTerrainUnderCursor()
		{
			m_TerrainCache.UnlockTerrainUnderCursor();
		}
		#endregion
	}
}
