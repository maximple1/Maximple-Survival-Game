//TODO: Find a way to allow users to save Filter Stacks for their brushes. For now the code is being commented
//until the feature is completely implemented.

//using UnityEditor;
//using UnityEngine;

//namespace UnityEditor.Experimental.TerrainAPI
//{
//    public class FilterStackFactory
//    {
//         [MenuItem("Assets/Create/Image Filter Stack")]
//         static void CreateAsset()
//         {
//             FilterStack fs = ScriptableObject.CreateInstance<FilterStack>();

//             AssetDatabase.CreateAsset(fs, "Assets/New Filter Stack.asset");
//             AssetDatabase.SaveAssets();

//             EditorGUIUtility.PingObject(fs);
//             Selection.activeObject = fs;
//         }
//    }
//}