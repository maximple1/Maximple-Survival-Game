////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System.IO;
using System.Text;
using System.Collections.Generic;
using U = UnityEngine;

namespace SETUtil.MeshExporter
{
	public static class OBJExporter
	{
		///<summary> Generate a data string compatible with the OBJ file format </summary>
		public static string MeshToString (U.Mesh mesh, U.Material[] materials)
		{
			StringBuilder _stringBuilder = new StringBuilder();

			_stringBuilder.Append("o ").Append(mesh.name).Append("\n");
			foreach(U.Vector3 v in mesh.vertices) {
				_stringBuilder.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
			}

			_stringBuilder.Append("\n");
			foreach(U.Vector3 v in mesh.normals) {
				_stringBuilder.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
			}
			
			_stringBuilder.Append("\n");
			foreach(U.Vector3 v in mesh.uv) {
				_stringBuilder.Append(string.Format("vt {0} {1}\n", v.x, v.y));
			}

			for (int m = 0; m < mesh.subMeshCount; m++) {
				_stringBuilder.Append("\n");
				_stringBuilder.Append("usemtl ").Append(materials[m].name).Append("\n");
				_stringBuilder.Append("usemap ").Append(materials[m].name).Append("\n");
	
				int[] _triangles = mesh.GetTriangles(m);
				for (int i = 0; i < _triangles.Length; i += 3) {
					_stringBuilder.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
						_triangles[i] + 1, _triangles[i + 1] + 1, _triangles[i + 2] + 1));
				}
			}
			return _stringBuilder.ToString();
		}

		public static void ExportMesh (string path, U.Mesh mesh, U.Material[] materials)
		{
			if(!Path.GetExtension(path).Equals(".obj", System.StringComparison.OrdinalIgnoreCase))
				throw new System.Exception("Target file extension must be .obj");

			FileUtil.WriteTextToFile(path, MeshToString(mesh, materials));
		}

		public static void ExportObject (string path, U.GameObject gameObject) 
		{
			U.Mesh _mesh = null;
			List<U.Material> _materials = new List<U.Material>();

			var _filter = gameObject.GetComponent<U.MeshFilter>();
			if(_filter != null) {
				_mesh = _filter.sharedMesh;

				var _renderer = gameObject.GetComponent<U.Renderer>();
				if(_renderer != null) {
					_materials.AddRange(_renderer.sharedMaterials);
				}
				ExportMesh(path, _mesh, _materials.ToArray());
			}else{
				EditorUtil.Debug("[ERROR ExportObject] Error while exporting object: No mesh found to export!");
			}
		}
	}
}