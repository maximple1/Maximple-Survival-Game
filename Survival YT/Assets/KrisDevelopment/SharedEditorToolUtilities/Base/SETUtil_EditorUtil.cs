////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using SETUtil.SceneUI;
using SETUtil.Types;
using SETUtil.UI;
using SETUtil.Extend;
using SETUtil.Common.Extend;

using U = UnityEngine;
using G = UnityEngine.GUI;
using Gl = UnityEngine.GUILayout;

#if UNITY_EDITOR
using E = UnityEditor;
using EGl = UnityEditor.EditorGUILayout;
#endif

namespace SETUtil
{
	//ENUMS:
	public enum ArrayFieldOption
	{
		Default = 0, //do nothing
		ShowAsLabels = (1 << 0), //show content as labels
		FixedSize = (1 << 2), //allow content modification, but no direct resizing through interface
		NoClearButton = (1 << 3), //content modification and array resizing but no "clear" button
		PreviewOnly = FixedSize | NoClearButton,
		NoIndex = (1 << 4),
		NoBoundries = (1 << 5), //hide the array drawer boundries
	}

	// -------------------------------------------
		
	//CLASSES:
	public static class EditorUtil
	{
		public delegate void DebugLogDelegate(object msg);

		public static DebugLogDelegate debugLog;

		private const int PROPERTY_DEPTH_LIMIT = 5;

		private static Stack<U.Color> colorStack = new Stack<U.Color>(2);
		private static ManagedList<iGUIElement> guiQueue = new ManagedList<iGUIElement>();
		private static NavigationFieldData lastDrawnNavField = null;

		// -------------------------------------------

		/// <summary>
		/// Prints the debug and the debugLog(msg) callback is invoked. Useful for custom console implementations.
		/// The pref allows you to specify a Debug Preference (message, warning, error)
		/// </summary>
		public static void Debug(object msg, DebugPreference pref = DebugPreference.Message)
		{
			//push to Unity console
			switch (pref) {
				case DebugPreference.Message:
					U.Debug.Log(msg);
					break;

				case DebugPreference.Warning:
					U.Debug.LogWarning(msg);
					break;

				case DebugPreference.Error:
					U.Debug.LogError(msg);
					break;
			}

			if (debugLog != null)
				debugLog(msg);
		}

		// -------------------------------------------

		public static void DrawSceneElement(iGUIElement element)
		{
			SubGUIDelegate();
			guiQueue.SmartPush(element);
		}

		public static void DrawSceneElement(iGUIElement element, U.Rect newRect)
		{
			element.rect = newRect;
			DrawSceneElement(element);
		}

		public static void DrawSceneElement(iGUIElement element, U.Vector3 newPos)
		{
			element.position = newPos;
			DrawSceneElement(element);
		}

		// -------------------------------------------
		
#if UNITY_EDITOR
		/// <summary>
		/// [EDITOR ONLY]
		/// Displays a unity unitliy window with less lines of code.
		/// </summary>
		public static T ShowUtilityWindow<T>(string title) where T : E.EditorWindow
		{
			var _s = U.ScriptableObject.CreateInstance<T>();
			_s.titleContent = new U.GUIContent(title);
			_s.ShowUtility();
			return _s;
		}

		/// <summary>
		/// [EDITOR ONLY]
		/// Displays a dialogue window with the log contents and an option to export the log to a file
		/// </summary>
		public static void ShowOperationLogWindow(string title, System.Text.StringBuilder log)
		{
			OperationLogWindow.ShowWindow(title, log);
		}
#endif

		///<summary> Draws simple horizontal GUI line </summary>
		public static void HorizontalRule()
		{
			Gl.Box("", Gl.ExpandWidth(true), Gl.Height(2));
		}

		/// <summary> 
		/// Draws simple vertical GUI line 
		/// </summary>
		public static void VerticalRule()
		{
			Gl.Box("", Gl.ExpandHeight(true), Gl.Width(2));
		}

		/// <summary>
		/// Draws expand (foldout type) button
		/// [ref override recommended]
		/// </summary>
		public static bool ExpandButton(bool b, string label, U.FontStyle fontStyle = U.FontStyle.Normal)
		{
			return ExpandButton(b, label, 20, fontStyle);
		}

		/// <summary>
		/// Draws expand (foldout type) button and allows you to set custom button height
		/// [ref override recommended]
		/// </summary>
		public static bool ExpandButton(bool b, string label, int height, U.FontStyle fontStyle = U.FontStyle.Normal)
		{
			bool _b = b;

			Gl.BeginHorizontal();
			U.GUIStyle _style = new U.GUIStyle(G.skin.button);
			_style.fontStyle = fontStyle;
			_style.richText = true;
			_style.alignment = U.TextAnchor.MiddleLeft;

			if (Gl.Button((b ? "▼ " : "► ") + label, _style, Gl.Height(height)))
				_b = !b;
			Gl.EndHorizontal();
			return _b;
		}
		
		/// <summary>
		/// Draws expand (foldout type) button and modify the provided boolean value
		/// </summary>
		public static bool ExpandButton(ref bool b, string label, U.FontStyle fontStyle = U.FontStyle.Normal)
		{
			return b = ExpandButton(b, label, 20, fontStyle);
		}
		
		/// <summary>
		/// Draws expand (foldout type) button and modify the provided boolean value. Allows custom button height.
		/// </summary>
		public static bool ExpandButton(ref bool b, string label, int height, U.FontStyle fontStyle = U.FontStyle.Normal)
		{
			return b = ExpandButton(b, label, height, fontStyle);
		}

		/// <summary> 
		/// Pushes current GUI colors to the stack. 
		/// </summary>
		public static void BeginColorPocket()
		{
			colorStack.Push(G.color);
			colorStack.Push(G.contentColor);
			colorStack.Push(G.backgroundColor);
		}

		/// <summary> 
		/// Opens the color pocked and pushes current GUI colors to the stack. 
		/// </summary>
		public static void BeginColorPocket(U.Color clr)
		{
			BeginColorPocket();
			G.color = clr;
		}

		/// <summary>
		/// Returns the GUI colors to their previous state. 
		/// </summary>
		public static void EndColorPocket()
		{
			G.backgroundColor = colorStack.Pop();
			G.contentColor = colorStack.Pop();
			G.color = colorStack.Pop();
		}


		/// <summary> 
		/// Opens the navigation field drawer. 
		/// </summary>
		public static void BeginNavigationField(ref NavigationFieldData navField, U.Vector2? restriction = null)
		{
			lastDrawnNavField = navField;

			U.Rect _fieldRect = FindLayoutAreaRect(ref navField.backupRect, navField.border);

			BeginColorPocket(navField.backgroundColor);
			Gl.BeginArea(_fieldRect, "", "Box");
			EndColorPocket();

			navField.DrawBackground();
			navField.DragUpdate();

			U.Vector2 _restriction = restriction ?? (new U.Vector2(_fieldRect.width, _fieldRect.height) - navField.scrollView);
			Gl.BeginArea(new U.Rect(navField.scrollView.x, navField.scrollView.y, _restriction.x, _restriction.y));
		}

		/// <summary> 
		/// Closes the Navigation Field. (Optional) Draws additional controls on top of the content. 
		/// </summary>
		public static void EndNavigationField(bool showNativeControls = true)
		{
			Gl.EndArea(); //end offset area
			if (lastDrawnNavField != null) {
				if (showNativeControls) {
					if (G.Button(new U.Rect(lastDrawnNavField.backupRect.width - lastDrawnNavField.border - 23, 5, 20, 20), new U.GUIContent("+", "Center View")))
						lastDrawnNavField.CenterView();
				}

#if UNITY_EDITOR
				G.Button(new U.Rect(0, 0, lastDrawnNavField.backupRect.width, lastDrawnNavField.backupRect.height), "", "Label"); //force hot control
#endif
			}

			Gl.EndArea(); //end field viewport area

			if (lastDrawnNavField != null) {
				lastDrawnNavField.DragUpdate();
			}

			lastDrawnNavField = null;
		}

		/// <summary>
		/// Draws a dummy GUI layout element and measures its rect.
		/// If the current event is not the repaint event, then use the backup rect reference.
		/// </summary>
		public static U.Rect FindLayoutAreaRect(ref U.Rect backupRect, int border = 0)
		{
			//DRAW DUMMY LAYOUT GROUP TO GET THE RECT FROM
			Gl.BeginVertical(Gl.MaxWidth(U.Screen.width), Gl.MaxHeight(U.Screen.height));
			Gl.Label(""); //<- layout dummy
			Gl.EndVertical();
			U.Rect _fieldRect = U.GUILayoutUtility.GetLastRect();

			//rect update handling (ignore dummy rect at layout event)
			if (U.Event.current.type != U.EventType.Repaint)
				_fieldRect = backupRect;
			else
				backupRect = _fieldRect;

			_fieldRect.x += border;
			_fieldRect.y += border;
			_fieldRect.width -= border * 2;
			_fieldRect.height -= border * 2;

			return _fieldRect;
		}

		// Build-Compatible GUI Utilities:

		/// <summary> 
		/// List GUI drawer utility (Will modify the source list) 
		/// </summary>
		public static List<T> ArrayFieldGUI<T>(List<T> list, ArrayFieldOption option = ArrayFieldOption.Default)
		{
			var _listCopy = new List<T>(list);

			U.GUIStyle _style = new U.GUIStyle("Box");

			if (!option.ContainsFlag(ArrayFieldOption.NoBoundries)) {
				Gl.BeginVertical(_style, Gl.ExpandWidth(true));
			}

			var _clearButtonContent = new U.GUIContent("Clr", "Clear Field");
			var _removeButtonContent = new U.GUIContent("X", "Remove Element");

			for (int i = 0; i < _listCopy.Count; i++) {
				if (!option.ContainsFlag(ArrayFieldOption.NoBoundries)) {
					Gl.BeginHorizontal(_style, Gl.ExpandWidth(true));
				}

				if (!option.ContainsFlag(ArrayFieldOption.NoIndex)) {
					Gl.Label(i.ToString(), Gl.ExpandWidth(false));
				}

				Gl.BeginVertical();
				{
					_listCopy[i] = DrawPropertyField(_listCopy[i]);
				}
				Gl.EndVertical();

				if (!option.ContainsFlag(ArrayFieldOption.NoClearButton)) {
					if (Gl.Button(_clearButtonContent, Gl.ExpandWidth(false))) {
						_listCopy[i] = default(T);
					}
				}

				if (!option.ContainsFlag(ArrayFieldOption.FixedSize)) {
					if (Gl.Button(_removeButtonContent, Gl.ExpandWidth(false))) {
						_listCopy.Remove(_listCopy[i]);
						i--;
					}
				}

				if (!option.ContainsFlag(ArrayFieldOption.NoBoundries)) {
					Gl.EndHorizontal();
				}
			}

			if (!option.ContainsFlag(ArrayFieldOption.FixedSize)) {
				if (Gl.Button("Add Element")) {
					_listCopy.Add(default(T));
				}
			}

			if (!option.ContainsFlag(ArrayFieldOption.NoBoundries))
				Gl.EndVertical();

			return _listCopy;
		}

		///<summary> 
		/// Array GUI drawer utility (Will not modify the source array) 
		/// </summary>
		public static T[] ArrayFieldGUI<T>(T[] arr, ArrayFieldOption option = ArrayFieldOption.Default)
		{
			return ArrayFieldGUI(arr.ToList(), option).ToArray();
		}

		[Obsolete("\"allowModify\" parameter has been deprecated")]
		private static object DrawPropertyObject(object property, Type type, bool allowModify, int indent = 0)
		{
			return DrawPropertyObject(property, type, indent);
		}

		/// <summary> 
		/// Field drawing utility used when a field of an object needs to be drawn 
		/// </summary>
		private static object DrawPropertyObject(object property, Type type, int indent = 0)
		{
			var _val = property;

			// UnityEngine.Object
			if (typeof(U.Object).IsAssignableFrom(type)) {
				U.Object _elementAsObject = null;
				_val.TryCast<U.Object>(out _elementAsObject);

#if UNITY_EDITOR
				_elementAsObject = EGl.ObjectField(_elementAsObject, type, true);
#else
				Gl.Label(_elementAsObject.ToString());
#endif

				_elementAsObject.TryCast<object>(out _val);

				return _val;
			}

			// Initialize new if null (and newable).
			// Doing this after the Unity Object check will assure no GameObjects are spawned in the current scene
			if (_val == null) {
				var _constructorInfo = type.GetConstructor(Type.EmptyTypes);
				if (_constructorInfo != null) {
					_val = _constructorInfo.Invoke(null);
				} else {
					_val = default(object);
				}
			}

			// Implements the iDrawableProperty
			if (_val is iDrawableProperty) {
				iDrawableProperty _asDrawable = (iDrawableProperty) _val;
				_asDrawable.DrawAsProperty();
				return _val;
			}

			// Bool
			if (_val is bool) {
				bool _elementAsBool = default(bool);

				if (_val.TryCast<bool>(out _elementAsBool)) {
					_elementAsBool = Gl.Toggle(_elementAsBool, "");
				}

				_elementAsBool.TryCast<object>(out _val);

				return _val;
			}

			// Int
			if (_val is int) {
				int _elementAsInt = default(int);

				if (_val.TryCast<int>(out _elementAsInt)) {
#if UNITY_EDITOR
					_elementAsInt = EGl.IntField(_elementAsInt);
#else
					int.TryParse(Gl.TextField(_elementAsInt.ToString()), out _elementAsInt);
#endif
				}

				_elementAsInt.TryCast<object>(out _val);

				return _val;
			}

			// Float
			if (_val is float) {
				float _elementAsFloat = default(float);

				if (_val.TryCast<float>(out _elementAsFloat)) {
#if UNITY_EDITOR
					_elementAsFloat = EGl.FloatField(_elementAsFloat);
#else
					float.TryParse(Gl.TextField(_elementAsFloat.ToString()), out _elementAsFloat);
#endif
				}

				_elementAsFloat.TryCast<object>(out _val);

				return _val;
			}

			// String
			if (_val is string || typeof(string).IsAssignableFrom(type)) {
				string _elementAsString = string.Empty;

				if (_val != null) {
					if (_val.TryCast<string>(out _elementAsString)) {
						_elementAsString = Gl.TextField(_elementAsString);
					}
				} else {
					Gl.Label("EMPTY STRING");
				}

				_elementAsString.TryCast<object>(out _val);

				return _val;
			}

			// Try drawing using reflection,
			// expecting that it is a newable type that is already initialized in the code above
			if (_val != null) {
				var _valType = _val.GetType();
				if (indent == 0) {
					Gl.Label(_valType.Name);
				}

				var _fieldInfo = _valType.GetFields(BindingFlags.Public | BindingFlags.Instance);

				if (indent < PROPERTY_DEPTH_LIMIT) {
					indent++;

					OpenIndent(indent);
					foreach (var _field in _fieldInfo) {
						Gl.BeginHorizontal();
						Gl.Label(StringUtil.WordSplit(_field.Name, true), Gl.ExpandWidth(false));
						Gl.BeginVertical();
						var _fieldValue = _field.GetValue(_val);
						_field.SetValue(_val, DrawPropertyObject(_fieldValue, _field.FieldType, indent));
						Gl.EndVertical();
						Gl.EndHorizontal();
					}

					CloseIndent();
				} else {
					Gl.Label(string.Format("[!] MAX DRAWING DEPTH ({0}) REACHED", PROPERTY_DEPTH_LIMIT));
				}

				return _val;
			}

			Gl.Label("[ERROR] Unknown Type");
			return null;
		}

		/// <summary>
		/// Open auto property drawer indent
		/// </summary>
		private static void OpenIndent(int depth)
		{
			Gl.BeginHorizontal();
			for (int i = 0; i < depth; i++) {
				Gl.Space(8);
			}

			Gl.BeginVertical();
		}

		/// <summary>
		/// Close auto property drawer indent
		/// </summary>
		private static void CloseIndent()
		{
			Gl.EndVertical();
			Gl.EndHorizontal();
		}

		/// <summary> 
		/// Auto field drawing utility 
		/// </summary>
		public static T DrawPropertyField<T>(T property)
		{
			T _val = (T) DrawPropertyObject(property, typeof(T), 0);
			return _val;
		}

		// -------------------------------------------

		/// <summary>
		/// Method intended for internal use by the library.
		/// Subscribes the scene GUI drawer to the onSceneGUIDelegate.
		/// </summary>
		private static void SubGUIDelegate()
		{
#if UNITY_EDITOR
			E.SceneView.onSceneGUIDelegate -= DrawQueuedSceneGUI;
			E.SceneView.onSceneGUIDelegate += DrawQueuedSceneGUI;
#endif
		}

		public static void UnsubGUIDelegate()
		{
#if UNITY_EDITOR
			E.SceneView.onSceneGUIDelegate -= DrawQueuedSceneGUI;
			AgeQueueIterator();
#endif
		}

		/// <summary>
		/// Forcibly clear the scene gui rendering queue
		/// </summary>
		public static void ClearSceneGUI()
		{
			UnsubGUIDelegate();
			AgeQueueIterator();
			guiQueue = new ManagedList<iGUIElement>();
		}

#if UNITY_EDITOR
		/// <summary>
		/// [EDITOR ONLY]
		/// Draws the queued scene GUI elements and ages the queue iterator
		/// </summary>
		private static void DrawQueuedSceneGUI(E.SceneView sceneView)
		{
			if (sceneView != null) {
				for (int i = 0; i < guiQueue.Count; i++) {
					if (guiQueue[i] != null) {
						E.Handles.BeginGUI();
						guiQueue[i].Draw(sceneView);
						E.Handles.EndGUI();
					}
				}
			}

			AgeQueueIterator();
		}
#endif


		/// <summary>
		/// Age the scene drawer iterator
		/// </summary>
		private static void AgeQueueIterator()
		{
			guiQueue.Age();
		}
	}
}