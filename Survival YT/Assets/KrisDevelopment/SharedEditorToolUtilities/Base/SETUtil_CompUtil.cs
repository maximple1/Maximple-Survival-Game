////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System.Collections.Generic;
using SETUtil.Types;

using U = UnityEngine;
using Gl = UnityEngine.GUILayout;

#if UNITY_EDITOR
using E = UnityEditor;
#endif

namespace SETUtil
{
	public static class CompUtil
	{
		/// <summary>
		/// Force component order in the given game object instance
		/// </summary>
		public static void ForceOrder(U.GameObject instance){
			ForceOrder(instance, new ComponentOrderList());
		}
		
		/// <summary>
		/// Force the component order for the selected instance, using a ComponentOrderList as a template
		/// </summary>
		public static void ForceOrder(U.GameObject instance, ComponentOrderList col)
		{			
			#if UNITY_EDITOR
			if (instance == null){
				EditorUtil.Debug("[SETUtil.CompUtil.Reorder ERROR] null reference.");
				return;
			}
			
			U.Component[] components = GatherComponents(instance);

			int cycle = 0;
			int ANTI_LOCK_LIMIT = 45;
			bool validOrder = false;
			
			do{
				for(uint i = 0; i < components.Length-1; i++){
					if(components[i].FindOrderIndex(ref col) > components[i+1].FindOrderIndex(ref col)){
						UnityEditorInternal.ComponentUtility.MoveComponentDown(components[i]);
						components = GatherComponents(instance);
					}
				}
				cycle++;
				validOrder = col.EvaluateOrder(components);
			}while (!validOrder && cycle < ANTI_LOCK_LIMIT);
			#endif
		}
		
		/// <summary>
		/// Return the components array as an array of their according types
		/// </summary>
		public static System.Type[] ComponentsToTypes (U.Component[] compArr){
			List<System.Type> _types = new List<System.Type>();
			for(int i = 0; i < compArr.Length; i++)
				if(compArr[i] != null)
					_types.Add(compArr[i].GetType());
			return _types.ToArray();
		}
		
		//private:

		/// <summary>
		/// returns true if all components are in order
		/// </summary>
		private static bool EvaluateOrder (this ComponentOrderList col, U.Component[] components) {
			bool valid = true;
			for(uint i = 0; i < components.Length - 1; i++){
				int _currentOrder = components[i].FindOrderIndex(ref col),
					_nextOrder = components[i+1].FindOrderIndex(ref col);
				if(_currentOrder > _nextOrder){
					valid = false;	
				}
			}

			return valid;
		}
		
		private static int FindOrderIndex(this U.Component comp, ref ComponentOrderList col){
			System.Type _comType = comp.GetType();
			int _order = 0;
			if(typeof(iOrderedComponent).IsAssignableFrom(_comType)){
				_order = ((iOrderedComponent) comp).OrderIndex();
			}
			else
				_order = col.EvaluateElement(_comType);
			
			return _order;
		}
		
		/// <summary>
		/// Returns an array of all components attached to the selected GameObject instance, except UnityEngine.Camera and UnityEngine.Transform
		/// </summary>
		private static U.Component[] GatherComponents (U.GameObject selected) {
			U.Component[] initialGather = selected.GetComponents<U.Component>();
			U.Component[] finalGather;
			uint count = 0;
			for(uint i = 0; i < initialGather.Length; i++){
				if(!(initialGather[i] is U.Camera) && !(initialGather[i] is U.Transform) && (initialGather[i] != null))
					count++;
			}
			finalGather = new U.Component[count];
			count = 0;
			for(uint i = 0; i < initialGather.Length; i++){
				if(!(initialGather[i] is U.Camera) && !(initialGather[i] is U.Transform) && (initialGather[i] != null)){
					finalGather[count] = initialGather[i];
					count++;
				}
			}
			return finalGather;
		}
	}
}
