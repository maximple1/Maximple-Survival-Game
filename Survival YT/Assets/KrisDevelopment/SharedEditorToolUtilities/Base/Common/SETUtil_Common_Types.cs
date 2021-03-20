////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using U = UnityEngine;

//SETUtil.Common contains class names that might overlap with system or unity namespaces
namespace SETUtil.Common.Types
{
	public enum MouseButton
	{
		Left = 0,
		Right = 1,
		Middle = 2,
	}

	public class Vector3Int
	{
		public int x,y,z;

		public static Vector3Int one {
			get{
				return new Vector3Int(1,1,1);
			}
		}


		public Vector3Int()
		{
			x = 0;
			y = 0;
			z = 0;
		}

		public Vector3Int(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static Vector3Int operator +(Vector3Int lhs, Vector3Int rhs)
		{
			return new Vector3Int(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
		}

		public static Vector3Int operator -(Vector3Int lhs, Vector3Int rhs)
		{
			return new Vector3Int(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
		}
		
		public static Vector3Int operator *(Vector3Int lhs, int rhs)
		{
			return new Vector3Int(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
		}

		public static Vector3Int operator /(Vector3Int lhs, int rhs)
		{
			return new Vector3Int(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
		}

		public static explicit operator U.Vector3 (Vector3Int int3)
		{
			return new U.Vector3(int3.x, int3.y, int3.z);
		}
	}
}