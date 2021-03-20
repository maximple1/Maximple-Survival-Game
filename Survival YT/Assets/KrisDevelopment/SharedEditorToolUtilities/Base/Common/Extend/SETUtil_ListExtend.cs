////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System;
using System.Collections.Generic;
using U = UnityEngine;

namespace SETUtil.Common.Extend
{
	public static class ListExtend
	{
		public static T SelectRandom<T>(this List<T> list)
		{
			return list[U.Random.Range(0, list.Count)];
		}

		public static int IndexOf<T>(this List<T> list, Func<T, bool> predicate)
		{
			for(int i = 0; i < list.Count; i++){
				if(predicate.Invoke(list[i])){
					return i;
				}
			}

			return -1;
		}
	}
}