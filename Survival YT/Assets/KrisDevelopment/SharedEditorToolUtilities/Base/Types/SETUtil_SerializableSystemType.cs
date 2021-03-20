////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System;
using System.Reflection;
using U = UnityEngine;
using Gl = UnityEngine.GUILayout;

#if UNITY_EDITOR
using E = UnityEditor;
#endif

namespace SETUtil.Types
{
	/// <summary>
	/// Wrapper for System.Type that supports Unity's serialization
	/// </summary>
	[System.Serializable]
	public class SerializableSystemType
	{
		private Type type;
		[U.SerializeField] private string typeName;
		[U.SerializeField] private string assemblyName;

		public Type value
		{
			get { return type ?? (type = LoadTypeFromStringName()); }
			private set
			{
				typeName = value.ToString(); 
				assemblyName = value.Assembly.FullName;
				type = value;
			}
		}

		// short access
		public string Name { get { return value.Name; } }
		public string FullName { get { return value.FullName; } }


		// For that json support
		public SerializableSystemType(){}

		public SerializableSystemType(Type type)
		{
			value = type;
		}

		private Type LoadTypeFromStringName()
		{
			if (string.IsNullOrEmpty(assemblyName)) {
				return null;
			}

			var _assembly = Assembly.Load(new AssemblyName(assemblyName));
			if (_assembly == null) {
				return null;
			}

			var _type = _assembly.GetType(typeName);
			return _type; 
		}
	}
}