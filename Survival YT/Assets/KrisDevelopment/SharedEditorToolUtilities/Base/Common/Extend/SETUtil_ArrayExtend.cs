////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

namespace SETUtil.Common.Extend
{
	public static class ArrayExtend
	{
		public static int IndexOf<T> (this T[] array, System.Func<T, bool> predicate)
		{
			for(int i = 0; i < array.Length; i++){
				if(predicate.Invoke(array[i])){
					return i;
				}
			}

			return -1;
		}
	}
}