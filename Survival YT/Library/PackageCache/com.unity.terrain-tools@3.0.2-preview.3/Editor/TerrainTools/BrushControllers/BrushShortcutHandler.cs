
using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
	public class BrushShortcutHandler<TKey>
	{
		private readonly Dictionary<TKey, Action> m_OnPressedByKey = new Dictionary<TKey, Action>();
		private readonly Dictionary<TKey, Action> m_OnReleasedByKey = new Dictionary<TKey, Action>();
		private readonly HashSet<TKey> m_ActiveKeys = new HashSet<TKey>();

		public void AddActions(TKey key, Action onPressed = null, Action onReleased = null)
		{
			if(onPressed != null)
			{
				if(m_OnPressedByKey.TryGetValue(key, out Action existingOnPressed))
				{
					existingOnPressed += onPressed;
					m_OnPressedByKey[key] = existingOnPressed;
				}
				else
				{
					m_OnPressedByKey.Add(key, onPressed);
				}
			}
			
			if(onReleased != null)
			{
				if(m_OnReleasedByKey.TryGetValue(key, out Action existingOnReleased))
				{
					existingOnReleased += onReleased;
					m_OnReleasedByKey[key] = existingOnReleased;
				}
				else
				{
					m_OnReleasedByKey.Add(key, onReleased);
				}
			}
		}

		public void RemoveActions(TKey key)
		{
			m_OnPressedByKey.Remove(key);
			m_OnReleasedByKey.Remove(key);
			m_ActiveKeys.Remove(key);
		}
		
		public bool IsShortcutPressed(TKey key)
		{
			return m_ActiveKeys.Contains(key);
		}

		public void HandleShortcutChanged(ShortcutArguments args, TKey key)
		{
			switch(args.stage)
			{
				case ShortcutStage.Begin:
				{
					if(m_OnPressedByKey.TryGetValue(key, out Action onPressed))
					{
						m_ActiveKeys.Add(key);
						onPressed?.Invoke();
					}
					break;
				}

				case ShortcutStage.End:
				{
					if(m_OnReleasedByKey.TryGetValue(key, out Action onReleased))
					{
						onReleased?.Invoke();
						m_ActiveKeys.Remove(key);

						TerrainToolsAnalytics.OnShortcutKeyRelease(key.ToString());
						TerrainToolsAnalytics.OnParameterChange();
					}
					break;
				}

				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			} // End of switch.
		}
	}
}
