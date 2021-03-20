////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System;
using System.Collections.Generic;
using System.Text;
using U = UnityEngine;
using Gl = UnityEngine.GUILayout;

#if UNITY_EDITOR
using E = UnityEditor;
#endif

namespace SETUtil.Types
{
	//INTERFACES:
	
	public interface iOrderedComponent
	{
		int OrderIndex();
	}

	public interface iDrawableProperty
	{
		void DrawAsProperty();
	}

	//CLASSES:
	
	/// <summary>
	/// Struct representation for the orientation information stored in a Unity Transform
	/// </summary>
	[Serializable]
	public struct TransformData
	{
		public U.Vector3 position;
		public U.Quaternion rotation;

		public U.Vector3 right { get { return rotation * U.Vector3.right; } }
		public U.Vector3 up { get { return rotation * U.Vector3.up; } }
		public U.Vector3 forward { get { return rotation * U.Vector3.forward; } }
		public U.Vector3 left { get { return rotation * -U.Vector3.right; } }
		public U.Vector3 down { get { return rotation * -U.Vector3.up; } }
		public U.Vector3 back { get { return rotation * -U.Vector3.forward; } }

		//CONSTRUCTORS
		public TransformData(U.Vector3 position, U.Quaternion rotation)
		{
			this.position = position;
			this.rotation = rotation;
		}

		public TransformData(U.Transform tr)
		{
			position = tr.position;
			rotation = tr.rotation;
		}

		public TransformData(TransformData dt)
		{
			position = dt.position;
			rotation = dt.rotation;
		}

		//INSTANCE METHODS:
		public void Debug()
		{
			EditorUtil.Debug("[SETUtil.TransformData] Position: " + position + " Rotation: " + rotation);
		}

		public void Set(U.Vector3 position, U.Quaternion rotation)
		{
			this.position = position;
			this.rotation = rotation;
		}

		public void Set(U.Transform tr)
		{
			position = tr.position;
			rotation = tr.rotation;
		}

		public void Set(TransformData trdt)
		{
			position = trdt.position;
			rotation = trdt.rotation;
		}

		//STATIC METHODS:
		public static TransformData Lerp(TransformData t1, TransformData t2, float lerp)
		{
			TransformData _t3 = new TransformData();
			_t3.position = U.Vector3.Lerp(t1.position, t2.position, lerp);
			_t3.rotation = U.Quaternion.Lerp(t1.rotation, t2.rotation, lerp);
			return _t3;
		}
	}

	/// <summary>
	/// Stores conditions and returns true if any one of them is met
	/// </summary>
	[Serializable]
	public class ConditionCapacitor
	{
		//Can store many boolean conditions and if one of them is true, it returns true when doing implicit boolean check

		public bool this[int index] { get { return Get(index); } set { Set(value, index); } }
		public int Length { get { return conditions.Length; } }
		public bool[] conditions;

		//CONSTRUCTORS:
		
		public ConditionCapacitor()
		{
			conditions = new bool[0];
		}

		public ConditionCapacitor(params bool[] conditions)
		{
			Set(conditions);
		}

		//OPERATORS:
		
		public static implicit operator bool(ConditionCapacitor cc)
		{
			for (int i = 0; i < cc.Length; i++)
				if (cc[i])
					return true;

			return false;
		}

		//METHODS:
		public void Set(params bool[] conditions)
		{
			this.conditions = new bool[conditions.Length];
			for (int i = 0; i < conditions.Length; this.conditions[i] = conditions[i], i++) ;
		}

		public void Set(bool condition, int index)
		{
			if (index < conditions.Length && index >= 0)
				conditions[index] = condition;
			EditorUtil.Debug("[ConditionCapacitor.Set ERROR] Index out of bounds!", DebugPreference.Error);
		}

		public bool[] Get()
		{
			return conditions;
		}

		public bool Get(int index)
		{
			if (index < conditions.Length && index >= 0)
				return conditions[index];
			EditorUtil.Debug("[ConditionCapacitor.Get ERROR] Index out of bounds!", DebugPreference.Error);
			return false;
		}
	}


	public class OrderElement
	{
		public Type type;
		public int order = 0;

		public OrderElement(Type type, int order)
		{
			this.type = type;
			this.order = order;
		}
	}

	public class ComponentOrderList
	{
		static readonly string ADD_ERROR_PREFIX = "[ComponentOrderList.AddElement ERROR]";

		List<OrderElement> list;

		public ComponentOrderList()
		{
			//EMPTY LIST
			list = new List<OrderElement>();
		}

		public ComponentOrderList(List<OrderElement> list)
		{
			this.list = list;
		}

		public void Print()
		{
			StringBuilder _log = new StringBuilder();
			_log.Append("[ComponentOrderList] Component Order List:\n");
			for(int i = 0; i < list.Count; _log.Append(i).Append(' ').Append(list[i].type).Append('\n'), i++);
			EditorUtil.Debug(_log.ToString());
		}

		public void AddElements(U.Component[] components)
		{
			for (int i = 0; i < components.Length; i++) {
				AddElement(components[i]);
			}
		}

		public void AddElement(U.Component component)
		{
			if (component == null) {
				EditorUtil.Debug(ADD_ERROR_PREFIX + " null component.");
				return;
			}

			AddElement(component.GetType(), list[list.Count - 1].order + 1);
		}

		public void AddElement(Type type, int order)
		{
			//Define a new order position in the list for the specified type
			for (int i = 0; i < list.Count; i++)
				if (list[i].type == type) {
					EditorUtil.Debug(ADD_ERROR_PREFIX + " Element of type " + type + " already exists. If it were added to the list, it would make element " + i + " obsolete. Aborting.");
					return;
				}

			list.Add(new OrderElement(type, order));
		}

		public int EvaluateElement(Type type)
		{
			//return the order index of the type
			int _orderIndex = (list.Count > 0) ? list[list.Count - 1].order + 1 : 0;
			bool _foundMatch = false;

			for (int i = 0; i < list.Count; i++) {
				if (list[i].type == type) {
					_orderIndex = list[i].order;
					_foundMatch = true;
				} else if (!_foundMatch)
					if (list[i].type.IsAssignableFrom(type))
						_orderIndex = list[i].order;
			}

			return _orderIndex;
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Shows an editor window with some log.
	/// That's here so it can be called from non-editor code!
	/// </summary>
	internal class OperationLogWindow : E.EditorWindow
	{
		private string log;
		private string outputPath;
		private U.Vector2 scrollView = U.Vector2.zero;
		
		public static OperationLogWindow ShowWindow(string title, StringBuilder log)
		{
			var _win = EditorUtil.ShowUtilityWindow<OperationLogWindow>(title);
			_win.log = log.ToString();
			_win.outputPath = U.Application.dataPath + "/operation_log.txt";
			return _win;
		}

		private void OnGUI()
		{
			scrollView = Gl.BeginScrollView(scrollView);
			{
				Gl.TextArea(log);
			}
			Gl.EndScrollView();
			
			EditorUtil.HorizontalRule();
			
			Gl.BeginHorizontal();
			{
				Gl.Label("Save Log:", Gl.ExpandWidth(false));
				outputPath = Gl.TextField(outputPath);
			}
			Gl.EndHorizontal();

			Gl.BeginHorizontal();
			{
				if (Gl.Button("Save")) {
					PrintLog();
				}

				if (Gl.Button("Close")) {
					Close();
				}
			}
			Gl.EndHorizontal();
		}

		private void PrintLog()
		{
			FileUtil.WriteTextToFile(outputPath, log);
			E.EditorUtility.DisplayDialog("Done!", "File saved to " + outputPath, "Ok");
		}
	}
#endif
}