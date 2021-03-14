using System.IO;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	internal class TerrainToolboxWindow : EditorWindow
	{
#if UNITY_2019_1_OR_NEWER
		[MenuItem("Window/Terrain/Terrain Toolbox", false, 1)]
		static void CreateMangerWindow()
		{
			TerrainToolboxWindow window = GetWindow<TerrainToolboxWindow>("Terrain Toolbox");
			window.minSize = new Vector2(200, 150);
			window.Show();
		}
#endif

		TerrainManagerMode m_SelectedMode = TerrainManagerMode.CreateTerrain;

		enum TerrainManagerMode
		{
			CreateTerrain = 0,
			Settings = 1,
			Utilities = 2,
			Visualization = 3
		}

		internal TerrainToolboxCreateTerrain m_CreateTerrainMode;
		internal TerrainToolboxSettings m_TerrainSettingsMode;
		internal TerrainToolboxUtilities m_TerrainUtilitiesMode;
		internal TerrainToolboxVisualization m_TerrainVisualizationMode;


		const string PrefName = "TerrainToolbox.Window.Mode";

		static class Styles
		{
			public static readonly GUIContent[] ModeToggles =
			{
				EditorGUIUtility.TrTextContent("Create New Terrain"),
				EditorGUIUtility.TrTextContent("Terrain Settings"),
				EditorGUIUtility.TrTextContent("Terrain Utilities"),
				EditorGUIUtility.TrTextContent("Terrain Visualization")
			};

			public static readonly GUIStyle ButtonStyle = "LargeButton";
		}

		public void OnEnable()
		{
			m_CreateTerrainMode = new TerrainToolboxCreateTerrain();
			m_TerrainSettingsMode = new TerrainToolboxSettings();
			m_TerrainUtilitiesMode = new TerrainToolboxUtilities();
			m_TerrainVisualizationMode = new TerrainToolboxVisualization();

			m_CreateTerrainMode.LoadSettings();
			m_TerrainSettingsMode.LoadSettings();
			m_TerrainUtilitiesMode.LoadSettings();
			m_TerrainUtilitiesMode.OnLoad();
			m_TerrainVisualizationMode.LoadSettings();
			LoadSettings();
		}

		public void OnDisable()
		{
			m_CreateTerrainMode.SaveSettings();
			m_TerrainSettingsMode.SaveSettings();
			m_TerrainUtilitiesMode.SaveSettings();
			m_TerrainVisualizationMode.SaveSettings();
			SaveSettings();
		}

		public void OnGUI()
		{
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			ToggleManagerMode();

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			this.autoRepaintOnSceneChange = false;
			switch (m_SelectedMode)
			{
				case TerrainManagerMode.CreateTerrain:
					this.autoRepaintOnSceneChange = true;
					m_CreateTerrainMode.OnGUI();
					break;

				case TerrainManagerMode.Settings:
					m_TerrainSettingsMode.OnGUI();
					break;

				case TerrainManagerMode.Utilities:
					m_TerrainUtilitiesMode.OnGUI();
					break;

				case TerrainManagerMode.Visualization:
					m_TerrainVisualizationMode.OnGUI();
					break;
			}
		}

		void ToggleManagerMode()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			EditorGUI.BeginChangeCheck();
			m_SelectedMode = (TerrainManagerMode)GUILayout.Toolbar((int)m_SelectedMode, Styles.ModeToggles, Styles.ButtonStyle, GUI.ToolbarButtonSize.FitToContents);
			if (EditorGUI.EndChangeCheck())
			{
				GUIUtility.keyboardControl = 0;
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		void OnLostFocus()
		{
			m_TerrainUtilitiesMode.OnLostFocus();
		}

		void OnDestroy()
		{
			m_TerrainVisualizationMode.RevertMaterial();
		}

		void SaveSettings()
		{
			string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsWindow);
			File.WriteAllText(filePath, ((int)m_SelectedMode).ToString());
		}

		void LoadSettings()
		{
			string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsWindow);
			if (File.Exists(filePath))
			{
				string windowSettingsData = File.ReadAllText(filePath);
				int value = 0;
				if (int.TryParse(windowSettingsData, out value))
				{
					m_SelectedMode = (TerrainManagerMode)value;
				}				
			}
		}
	}
}
