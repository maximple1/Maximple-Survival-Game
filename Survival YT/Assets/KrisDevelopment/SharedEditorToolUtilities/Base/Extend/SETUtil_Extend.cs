////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System;
using SETUtil.Types;
using U = UnityEngine;
using Gl = UnityEngine.GUILayout;

#if UNITY_EDITOR
using E = UnityEditor;
#endif

namespace SETUtil.Extend
{
	public static class ExtendUtil
	{
		//GENERIC:
		public static bool TryCast<T>(this object obj, out T result)
		{
			if (obj is T) {
				result = (T) obj;
				return true;
			}

			result = default(T);
			return false;
		}

		public static T CopyTo<T>(this object source)
		{
			var output = U.JsonUtility.FromJson<T>(U.JsonUtility.ToJson(source));
			return output;
		}

		//OBJECTS - TransformData & UnityEngine.Transform:
		[Obsolete("TransformData.Copy(TransformData trdt) is obsolete, please use TransformData.Set(TransformData)")]
		public static void Copy(this TransformData t, TransformData trdt)
		{
			//keeping this method for backwards compatibility
			t.Set(trdt);
		}

		public static void Set(this U.Transform t, TransformData trdt)
		{
			//sets the position and rotation values of UnityEngine.Transform t to the given TransformData ones
			t.position = trdt.position;
			t.rotation = trdt.rotation;
		}

		public static TransformData ToTransformData(this U.Transform t)
		{
			return new TransformData(t.position, t.rotation);
		}

		public static U.GameObject[] ToGameObjectArray(this U.Transform[] t)
		{
			U.GameObject[] _objArray = new U.GameObject[t.Length];
			for (int i = 0; i < _objArray.Length; _objArray[i] = t[i].gameObject, i++) ;
			return _objArray;
		}

		public static void InitElements<T>(this T[] arr) where T : new()
		{
			for (int i = 0; i < arr.Length; arr[i] = new T(), i++) ;
		}

		//UI
		public static bool Contains(this U.Rect rect, U.Vector2 point)
		{
			if (rect.x < point.x && rect.width + rect.x > point.x)
				if (rect.y < point.y && rect.height + rect.y > point.y)
					return true;
			return false;
		}

		//EDITOR
		public static void Debug(this U.Transform[] tArr)
		{
			string _log = "Transform Array Contents:";
			//foreach(U.Transform t in tArr)
			//StringUtil.LogAdd(ref _log, t.name);
			EditorUtil.Debug(_log);
		}

		public static U.Color ToRGB(this U.Color clr)
		{
			return new U.Color(clr.r, clr.g, clr.b, 1f);
		}

		//EDITOR-ONLY SERIALIZATION:
#if UNITY_EDITOR
		public static E.SerializedProperty FindDeepProperty(this E.SerializedObject so, string path)
		{
			so.ApplyModifiedProperties();
			so.Update();

			string[] _pathArray = StringUtil.ToPathArray(path);

			E.SerializedProperty
				_so_parentDir = null,
				_so_targ = null;

			if (_pathArray.Length > 1) {
				_so_parentDir = so.FindProperty(_pathArray[0]);
				for (int i = 0; i < _pathArray.Length - 1 /*go only through the parents, hence -1*/; i++) {
					if (_so_parentDir != null) {
						if (i > 0) //iterate through the parents
							_so_parentDir = _so_parentDir.FindPropertyRelative(_pathArray[i]);
						_so_targ = _so_parentDir.FindPropertyRelative(_pathArray[i + 1]);
					} else
						EditorUtil.Debug("[SETUtil_EditorUtil.FindDeepProperty ERROR] Invalid path element: " + _so_parentDir);
				}
			} else if (_pathArray.Length == 1) {
				_so_targ = so.FindProperty(_pathArray[0]);
			} else {
				EditorUtil.Debug("[SETUtil_EditorUtil.FindDeepProperty ERROR] Empty target path!");
				return null;
			}

			if (_so_targ == null) {
				EditorUtil.Debug("[SETUtil_EditorUtil.FindDeepProperty ERROR] Nonexistent or inaccessible property: " + path + ". Make sure you are not targeting a MonoBehavior-derived class instance.");
				return null;
			}

			return _so_targ;
		}

		public static U.Vector3 GetScreenPosition(this E.SceneView sceneView, U.Vector3 position)
		{
			U.Vector3 _sPos = sceneView.camera.WorldToScreenPoint((U.Vector3) position);
			_sPos.y = sceneView.position.height - _sPos.y;
			return _sPos;
		}
#endif
	}
}