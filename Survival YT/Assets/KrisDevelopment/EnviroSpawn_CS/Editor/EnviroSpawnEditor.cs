using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EnvSpawn;

[CustomEditor(typeof(EnviroSpawn_CS))]
[CanEditMultipleObjects]
public class EnviroSpawnEditor : Editor
{
	string[] scatterModeOption = Enum.GetNames(typeof(EnviroSpawn_CS.ScatterMode));

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EnviroSpawn_CS script = (EnviroSpawn_CS) target;

		script.scatterMode = (EnviroSpawn_CS.ScatterMode) EditorGUILayout.Popup("Scatter Mode", (int) script.scatterMode, scatterModeOption);

		if (script.scatterMode == EnviroSpawn_CS.ScatterMode.FixedGrid) {
			//script.offsetInEachCell = EditorGUILayout.Toggle("Offset In Each Cell", script.offsetInEachCell);
			script.fixedGridScale = EditorGUILayout.FloatField("Grid Scale ", script.fixedGridScale);
		}

		GUILayout.BeginVertical(EditorStyles.helpBox);
		{
			GUILayout.Label("Prefabs:", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			{
				script.prefabs = SETUtil.EditorUtil.ArrayFieldGUI(script.prefabs);
			}
			if (EditorGUI.EndChangeCheck()) {
				EditorUtility.SetDirty(script);
			}
		}
		GUILayout.EndVertical();

		if (GUILayout.Button("Generate")) {
			script.Generate();
		}

		if (GUILayout.Button("Re-Generate All In Scene")) {
			script.MassInstantiateNew();
		}
		
		EditorGUILayout.HelpBox("Important: Do not add children to this game object manually, they will get deleted upon update!", MessageType.Info);
	}

	public void OnInspectorUpdate()
	{
		Repaint();
	}
}