////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

namespace SETUtil.Common.Extend
{
	public static class StringExtend
	{
		public static string ShowIf(this string left, bool condition)
		{
			return (condition ? left : string.Empty);
		}

		public static string[] ToPathArray (this string path)
		{
			return SETUtil.StringUtil.ToPathArray(path);
		}
	}
}