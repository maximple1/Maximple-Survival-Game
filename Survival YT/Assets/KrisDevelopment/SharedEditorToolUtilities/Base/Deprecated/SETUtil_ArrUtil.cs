////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System.Linq;
using System.Collections.Generic;

namespace SETUtil.Deprecated {
	public static class ArrUtil{		
		public static int Resize<T> (ref T[] arr, int addSize){
			if(arr == null)
				arr = new T[0];
			
			if(addSize == 0)
				return arr.Length;
			
			//resize the array
			if(0 > arr.Length + addSize){
				EditorUtil.Debug("[ArrUtil.Resize ERROR] Cannot resize to a negative number!");
				return arr.Length;
			}
			
			T[] _tempArr = arr;
			arr = new T[arr.Length + addSize];
			for(int i = 0; i < _tempArr.Length && i < arr.Length; arr[i] = _tempArr[i], i++);
			
			return arr.Length;
		}
		
		public static bool AutoResize<T> (ref T[] arr, int i) where T : new(){
			//will check and if the index is within range and expand the array if needed
			if(i >= arr.Length){
				int resizeAmount = (i + 1) - arr.Length;
				Resize<T>(ref arr, resizeAmount);
				return true;
			}
			return false;
		}
		
		public static T AddElement<T> (ref T[] arr, T element){
			//Add element to the array (expands the array with 1 element)
			List<T> _list = new List<T>();
			_list.AddRange(arr);
			_list.Add(element);
			arr = _list.ToArray();
			return _list.Last();
		}
		
		public static void Combine<T> (ref T[] arr1, T[] arr2) {
			List<T> _list = new List<T>();
			_list.AddRange(arr1);
			
			foreach(T t in arr2)
				_list.Add(t);
			
			arr1 = _list.ToArray();
		}
		
		public static void RemoveElement<T> (ref T[] arr, int? id = null){
			//remove selected element from the array. If an id has not been specified, the last element is removed.
			if(id == null)
				id = arr.Length - 1;
			
			if(id >= arr.Length || id < 0){
				EditorUtil.Debug("[ArrUtil.RemoveElement ERROR] id " + id + " outside array bounds!");
				return;
			}
			
			List<T> _list = new List<T>();
			_list.AddRange(arr);
			_list.RemoveAt((int)id);
			arr = _list.ToArray();
		}
		
		public static void Swap<T> (ref T[] arr, int a, int b) {
			if(!(a < arr.Length && b < arr.Length)){
				EditorUtil.Debug("[SETUtil.ArrUtil.Swap ERROR] Array index out of range 0 - " + arr.Length + ", aborting.");
				return;
			}
			
			T _temp = arr[a];
			arr[a] = arr[b];
			arr[b] = _temp;
		}
	}
}
