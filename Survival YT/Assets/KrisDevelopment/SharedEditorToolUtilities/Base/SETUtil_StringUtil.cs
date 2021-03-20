////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

namespace SETUtil
{
	public static class StringUtil
	{
		/// <summary>
		/// Default separator values for string.Split()
		/// </summary>
		public static readonly char[] defaultPathSeparators = {'.', '/', '\\'};

		/// <summary>
		/// Splits a string into a readable format (someWord -> some Word)
		/// </summary>
		public static string WordSplit(string str)
		{
			return WordSplit(str, (str.Length > 0) ? char.IsUpper(str[0]) : false);
		}

		/// <summary> Splits a string into a readable format
		/// with option to make the first character upper case (someWord -> Some Word)
		/// </summary>
		public static string WordSplit(string str, bool firstIsUpper)
		{
			string _str2 = "";

			if (str.Length > 0)
				for (int i = 0; i < str.Length; i++) {
					string st = str[i].ToString();
					_str2 += (i == 0 && firstIsUpper) ? st.ToUpper() : st;
					if (i < str.Length - 1)
						if (char.IsUpper(str[i + 1]))
							if (char.IsLower(str[i]))
								_str2 += " ";
				}

			return _str2;
		}

		/// <summary>
		/// Uses string.Split with the values defined in defPathSeparators and removes empty entries
		/// </summary>
		public static string[] ToPathArray(string path)
		{
			return path.Split(defaultPathSeparators, System.StringSplitOptions.RemoveEmptyEntries);
		}
	}
}