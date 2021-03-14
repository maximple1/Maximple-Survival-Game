using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGroup))]
public class TerrainGroupInspector : Editor
{
	Terrain[] childTerrains;

	public override void OnInspectorGUI()
	{
		TerrainGroup groupTarget = (TerrainGroup)target;

		EditorGUILayout.Separator();
		groupTarget.GroupID = EditorGUILayout.IntField("Group ID", groupTarget.GroupID);
		EditorGUILayout.Separator();
		if (GUILayout.Button("Update Child Terrains"))
		{
			groupTarget.UpdateChildTerrains();
		}		
	}
}