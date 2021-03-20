////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System.Collections.Generic;

namespace SETUtil.Common.Extend
{
	public static class HashSetExtend
	{
		public static void AddRange<T> (this HashSet<T> hashSet, IEnumerable<T> elements)
		{
			foreach(var _element in elements){
				hashSet.Add(_element);
			}
		}
	}
}