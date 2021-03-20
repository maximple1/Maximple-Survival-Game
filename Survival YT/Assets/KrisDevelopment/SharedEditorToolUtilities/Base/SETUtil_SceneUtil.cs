////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System.Collections.Generic;
using U = UnityEngine;
using Gl = UnityEngine.GUILayout;

#if UNITY_EDITOR
using E = UnityEditor;
using EGl = UnityEditor.EditorGUILayout;
#endif

namespace SETUtil
{
	public static class SceneUtil
	{
		// OBJECT TERMINATION HANDLING --------------------------------------
		
		/// <summary>
		/// Calls appropriate destroy method based on the build conditions
		/// </summary>
		public static void SmartDestroy<T>(T instance) where T : U.Object
		{
			if (instance == null) {
				EditorUtil.Debug("[SmartDestroy ERROR] Target instance is NULL.");
				return;
			}

#if UNITY_EDITOR
			if (E.AssetDatabase.Contains(instance) && ValidatePrefabTermination(instance)) {
				E.AssetDatabase.DeleteAsset(E.AssetDatabase.GetAssetPath(instance));
			} else {
				if (U.Application.isPlaying)
					U.Object.Destroy(instance);
				else
					U.Object.DestroyImmediate(instance, ValidatePrefabTermination(instance));
			}
#else
				U.Object.Destroy(instance);
#endif
		}

		///<summary> Calls SmartDestroy for reach element in the array </summary>
		public static void DestroyArray<T>(ref T[] instances) where T : U.Object
		{
			foreach (T instance in instances)
				SmartDestroy(instance);

			instances = new T[0];
		}
		
		///<summary> Method meant to prevent unwanted asset termination </summary>
		public static bool ValidatePrefabTermination<T>(T obj) where T : U.Object
		{
			if (obj == null) {
				EditorUtil.Debug("[ValidatePrefabTermination] NULL object");
				return false;
			}

			bool _validTermination = false;

#if UNITY_EDITOR
			bool _isPrefab = false;
			U.Object _prefab = null;

#if UNITY_2018_3_OR_NEWER
			_prefab = E.PrefabUtility.GetPrefabInstanceHandle(obj);
#else
			_prefab = E.PrefabUtility.GetPrefabObject(obj);
#endif

			_isPrefab = _prefab != null && _prefab.Equals(obj);

			if (!_isPrefab || !E.EditorUtility.DisplayDialog("Confirm action", "Seems like object " + obj.name + " is a prefab! This action will permanently delete the asset from your project!\nContinue?", "Yes", "No"))
				_validTermination = true;
#else
				_validTermination = true;
#endif

			return _validTermination;
		}
		
		// GAME OBJECT CREATION -------------------------------------

		///<summary> Spawns a prefab-linked instance if in editor and normal instance during run-time, at default coordinates. </summary>
		public static U.GameObject Instantiate(U.GameObject prefab)
		{
			return Instantiate(prefab, U.Vector3.zero, U.Quaternion.identity);
		}

		///<summary> Spawns a prefab-linked instance if in editor and normal instance during run-time, with default rotation. </summary>
		public static U.GameObject Instantiate(U.GameObject prefab, U.Vector3 position)
		{
			return Instantiate(prefab, position, U.Quaternion.identity);
		}

		///<summary> Spawns a prefab-linked instance if in editor and normal instance during run-time </summary>
		public static U.GameObject Instantiate(U.GameObject prefab, U.Vector3 position, U.Quaternion rotation)
		{
			U.GameObject _instance = null;
			if (prefab != null) {
#if UNITY_EDITOR
				_instance = (U.GameObject) E.PrefabUtility.InstantiatePrefab(prefab);
				_instance.transform.position = position;
				_instance.transform.rotation = rotation;
#else
					_instance = U.GameObject.Instantiate(prefab, position, rotation);
#endif
			} else
				_instance = new U.GameObject();

			return _instance;
		}
		
		// GO MANAGEMENT ------------------------------------------------

		///<summary> Returns all children inside the given root transform. </summary>
		public static U.Transform[] CollectAllChildren(U.Transform root, bool includeParent = false)
		{
			if(includeParent){
				return root.GetComponentsInChildren<U.Transform>(true);
			}

			List<U.Transform> _children = new List<U.Transform>();
			IterateChildren(ref _children, root);
			if (!includeParent)
				_children.Remove(root);
			return _children.ToArray();
		}

		private static void IterateChildren(ref List<U.Transform> children, U.Transform currentNode)
		{
			//used by CollectAllChildren() to realize children iteration
			children.Add(currentNode);
			foreach (U.Transform child in currentNode)
				IterateChildren(ref children, child);
		}

		/// <summary> Tries to find the top-most root of the given GameObject </summary>
		public static U.GameObject FindTopRoot(U.GameObject gameObject)
		{
			U.Transform root = gameObject.transform;

			while (root.parent != null) {
				root = root.parent;
			}

			return root.gameObject;
		}
	}
}