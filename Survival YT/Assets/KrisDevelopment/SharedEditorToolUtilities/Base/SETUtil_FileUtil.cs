////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using System.IO;
using U = UnityEngine;

namespace SETUtil
{
	public static class FileUtil
	{
		///<summary>
		/// Creates a normalized file path
		/// </summary>
		public static string CreateFilePathString (string fileName, string fileExtension, bool relativePath = true) 
		{
			return (relativePath ? ParseToLocalUnityPath(fileName) : ParseToAbsolutePath(fileName)) + "." + fileExtension;
		}

		///<summary>
		/// Creates a normalized folder path
		/// </summary>
		public static string CreateFolderPathString (string folderName, bool relativePath = true) 
		{
			return relativePath ? ParseToLocalUnityPath(folderName) : ParseToAbsolutePath(folderName);
		}

		public static void WriteTextToFile (string path, string content)
		{
			string _filePath = ParseToAbsolutePath(path);
			FileInfo _fileInfo = new FileInfo(_filePath);

			_fileInfo.Directory.Create();
			StreamWriter _writer = File.CreateText(_filePath);
			_writer.Close();
			File.WriteAllText(_filePath, content);
		}

		public static bool ReadTextFromFile (string path, out string content)
		{
			string _filePath = ParseToAbsolutePath(path);
			content = string.Empty;

			if(File.Exists(_filePath)){
				content = File.ReadAllText(_filePath);
				return true; //read success
			}

			return false; //read failed
		}

		///<summary>
		/// Local paths in unity start with "Assets/".
		/// This method adds that to the path if it isn't already there.
		///</summary>
		public static string NormalizeToLocalUnityPath (string localPath)
		{
			if(localPath.StartsWith("/")){
				localPath = localPath.TrimStart('/');
			}

			if(localPath.StartsWith("Assets/")){
				return localPath;
			}

			return string.Format("Assets/{0}", localPath);
		}

		///<summary>
		/// Makes the given path compatible with unity local path operations such as AssetDatabase ones
		///</summary>
		public static string ParseToLocalUnityPath (string path)
		{
			var _applicationPath = U.Application.dataPath;
			if(path.StartsWith(_applicationPath)){
				return NormalizeToLocalUnityPath(path.Remove(0, _applicationPath.Length));
			}

			return NormalizeToLocalUnityPath(path);
		}

		///<summary> Creates an absolute path given any path </summary>
		public static string ParseToAbsolutePath (string path)
		{
			// if absolute system path
			if(path.Length > 2 && path.Remove(0, 1).StartsWith(":/")){
				return path;
			}

			// if local path
			var _applicationPath = U.Application.dataPath;
			const string _assetsStr = "Assets/";

			path = ParseToLocalUnityPath(path);
			return string.Format("{0}/{1}", _applicationPath, path.Remove(0, _assetsStr.Length));
		}
	}
}