////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System;

namespace SETUtil.Common.Extend
{
	public static class EnumExtend
	{
		public static bool ContainsFlag<T>(this T lhs, T flag) where T : struct, IConvertible
		{
			var _flag = (int)(object)flag;
			return ((int)(object)lhs & _flag) == _flag;
		}

		public static bool ContainsAnyFlag<T>(this T lhs, T flags) where T : struct, IConvertible
		{
			var _lhs = (int)(object)lhs;
			var _flags = (int)(object)flags;

			var _and = (_lhs & _flags);
			var _result = (~(_lhs ^ _flags) & _and);
			return _result != 0 && _result == _and;
		}
	}
}