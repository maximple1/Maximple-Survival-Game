////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System.Collections;
using System.Collections.Generic;
using U = UnityEngine;
using Gl = UnityEngine.GUILayout;

#if UNITY_EDITOR
using E = UnityEditor;
using EGl = UnityEditor.EditorGUILayout;

namespace SETUtil.EditorOnly
{
	///<summary> Intended to provide some form of asynchronous task support in the Unity Editor </summary>
	public class EditorCoroutine 
	{
		public static List<EditorCoroutine> coroutines = new List<EditorCoroutine>();

		private static List<EditorCoroutine> removeQueue = new List<EditorCoroutine>();
		private IEnumerator enumerator;
		private bool paused = false;
		
		private EditorCoroutine (IEnumerator enumerator)
		{
			this.enumerator = enumerator;
			this.paused = false;
		}

		private void Update ()
		{
			if(paused){
				return;
			}

			if(!enumerator.MoveNext()){
				removeQueue.Add(this);
			}
		}

		public void Pause ()
		{
			paused = true;
		}

		public void Resume ()
		{
			paused = false;
			Update();
		}

		public static EditorCoroutine Start (IEnumerator enumerator)
		{
			var _coroutine = new EditorCoroutine(enumerator);
			coroutines.Add(_coroutine);

			E.EditorApplication.update -= UpdateCoroutines;
			E.EditorApplication.update += UpdateCoroutines;

			UpdateCoroutines();

			return _coroutine;
		}

		public static void Stop (EditorCoroutine coroutine)
		{
			removeQueue.Add(coroutine);
			UpdateCoroutines();
		}

		private static void UpdateCoroutines ()
		{
			// Remove finished coroutines
			foreach(var toRemove in removeQueue){
				coroutines.Remove(toRemove);
			}
			removeQueue.Clear();

			// Update all active coroutines
			foreach(var coroutine in coroutines){
				coroutine.Update();
			}

			// Unsubscribe if there is nothing to do
			if(coroutines.Count == 0){
				E.EditorApplication.update -= UpdateCoroutines;
			}
		}
	}
}

#endif