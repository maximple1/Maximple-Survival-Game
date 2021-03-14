using System.Collections.Generic;
using System.IO;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
	public class TerrainToolboxSettings
	{
		List<Terrain> m_Terrains = new List<Terrain>();
		Terrain m_SelectedTerrain = null;
		TerrainSettings m_Settings = ScriptableObject.CreateInstance<TerrainSettings>();
		TerrainSettings m_SelectedPreset = null;
		readonly TerrainSettings m_DefaultSettings = ScriptableObject.CreateInstance<TerrainSettings>();

		string m_MaterialPath = string.Empty;
		Vector2 m_ScrollPosition = Vector2.zero;
		AnimBool m_ShowCustomMaterial = new AnimBool();
		AnimBool m_ShowBuiltinSpecular = new AnimBool();
		AnimBool m_ShowReflectionProbes = new AnimBool();
		const int kBaseMapDistMax = 20000;

		static class Styles
		{
			public static readonly GUIContent BasicTerrainSettings = EditorGUIUtility.TrTextContent("Basic Settings", "Toggle to enable or disable changing the settings.");
			public static readonly GUIContent MeshResolutionSettings = EditorGUIUtility.TrTextContent("Mesh Resolutions", "Toggle to enable or disable changing the settings.");
			public static readonly GUIContent TextureResolutionSettings = EditorGUIUtility.TrTextContent("Texture Resolutions", "Toggle to enable or disable changing the settings.");
			public static readonly GUIContent TreeAndDetailSettings = EditorGUIUtility.TrTextContent("Trees & Details", "Toggle to enable or disable changing the settings.");
			public static readonly GUIContent GrassWindSettings = EditorGUIUtility.TrTextContent("Wind for Grass", "Toggle to enable or disable changing the settings.");

			public static readonly GUIStyle LableStyle = EditorStyles.wordWrappedMiniLabel;
			public static readonly GUIStyle ToggleButtonStyle = "LargeButton";

			public static readonly GUIContent LoadDefaultBtn = EditorGUIUtility.TrTextContent("Default", "Load settings from default values.");
			public static readonly GUIContent LoadSelectedBtn = EditorGUIUtility.TrTextContent("Selected", "Load settings from selected terrain.");
			public static readonly GUIContent LoadPresetBtn = EditorGUIUtility.TrTextContent("Preset", "Load settings from selected preset.");

			// Settings
			public static readonly GUIContent GroupingID = EditorGUIUtility.TrTextContent("Grouping ID", "Grouping ID for auto connection");
			public static readonly GUIContent AllowAutoConnect = EditorGUIUtility.TrTextContent("Auto connect", "Allow the current terrain tile automatically connect to neighboring tiles sharing the same grouping ID.");
			public static readonly GUIContent DrawTerrain = EditorGUIUtility.TrTextContent("Draw", "Toggle the rendering of terrain");
			public static readonly GUIContent DrawInstancedTerrain = EditorGUIUtility.TrTextContent("Draw Instanced", "Toggle terrain instancing rendering");
			public static readonly GUIContent PixelError = EditorGUIUtility.TrTextContent("Pixel Error", "The accuracy of the mapping between the terrain maps (heightmap, textures, etc) and the generated terrain; higher values indicate lower accuracy but lower rendering overhead.");
			public static readonly GUIContent BaseMapDist = EditorGUIUtility.TrTextContent("Base Map Dist.", "The maximum distance at which terrain textures will be displayed at full resolution. Beyond this distance, a lower resolution composite image will be used for efficiency.");
			public static readonly GUIContent CastShadows = EditorGUIUtility.TrTextContent("Cast Shadows", "Does the terrain cast shadows?");
			public static readonly GUIContent Material = EditorGUIUtility.TrTextContent("Material", "The material used to render the terrain. This will affect how the color channels of a terrain texture are interpreted.");
			public static readonly GUIContent ReflectionProbes = EditorGUIUtility.TrTextContent("Reflection Probes", "How reflection probes are used on terrain. Only effective when using built-in standard material or a custom material which supports rendering with reflection.");
			public static readonly GUIContent PreserveTreePrototypeLayers = EditorGUIUtility.TrTextContent("Preserve Tree Prototype Layers|Enable this option if you want your tree instances to take on the layer values of their prototype prefabs, rather than the terrain GameObject's layer.");
			public static readonly GUIContent DrawTrees = EditorGUIUtility.TrTextContent("Draw", "Should trees, grass and details be drawn?");
			public static readonly GUIContent DetailObjectDistance = EditorGUIUtility.TrTextContent("Detail Distance", "The distance (from camera) beyond which details will be culled.");
			public static readonly GUIContent CollectDetailPatches = EditorGUIUtility.TrTextContent("Collect Detail Patches", "Should detail patches in the Terrain be removed from memory when not visible?");
			public static readonly GUIContent DetailObjectDensity = EditorGUIUtility.TrTextContent("Detail Density", "The number of detail/grass objects in a given unit of area. The value can be set lower to reduce rendering overhead.");
			public static readonly GUIContent TreeDistance = EditorGUIUtility.TrTextContent("Tree Distance", "The distance (from camera) beyond which trees will be culled.");
			public static readonly GUIContent TreeBillboardDistance = EditorGUIUtility.TrTextContent("Billboard Start", "The distance (from camera) at which 3D tree objects will be replaced by billboard images.");
			public static readonly GUIContent TreeCrossFadeLength = EditorGUIUtility.TrTextContent("Fade Length", "Distance over which trees will transition between 3D objects and billboards.");
			public static readonly GUIContent TreeMaximumFullLODCount = EditorGUIUtility.TrTextContent("Max Mesh Trees", "The maximum number of visible trees that will be represented as solid 3D meshes. Beyond this limit, trees will be replaced with billboards.");
			public static readonly GUIContent Thickness = EditorGUIUtility.TrTextContent("Thickness", "How much the terrain collision volume should extend along the negative Y-axis. Objects are considered colliding with the terrain from the surface to a depth equal to the thickness. This helps prevent high-speed moving objects from penetrating into the terrain without using expensive continuous collision detection.");
			public static readonly GUIContent WavingGrassStrength = EditorGUIUtility.TrTextContent("Speed", "The speed of the wind as it blows grass.");
			public static readonly GUIContent WavingGrassSpeed = EditorGUIUtility.TrTextContent("Size", "The size of the 'ripples' on grassy areas as the wind blows over them.");
			public static readonly GUIContent WavingGrassAmount = EditorGUIUtility.TrTextContent("Bending", "The degree to which grass objects are bent over by the wind.");
			public static readonly GUIContent WavingGrassTint = EditorGUIUtility.TrTextContent("Grass Tint", "Overall color tint applied to grass objects.");
			public static readonly GUIContent BaseTextureRes = EditorGUIUtility.TrTextContent("Base Texture Resolution", "Resolution of the composite texture used on the terrain when viewed from a distance greater than the Basemap Distance.");
			public static readonly GUIContent ControlTextureRes = EditorGUIUtility.TrTextContent("Control Texture Resolution", "Resolution of the \"splatmap\" that controls the blending of the different terrain textures.");
			public static readonly GUIContent HeightTextureRes = EditorGUIUtility.TrTextContent("Heightmap Resolution", "Pixel resolution of the terrain's heightmap (should be a power of two plus one, eg, 513 = 512 + 1).");
			// warnings
			public static readonly GUIContent DetailResolutionWarning = EditorGUIUtility.TrTextContent("You may reduce CPU draw call overhead by setting the detail resolution per patch as high as possible, relative to detail resolution.");
			public static readonly GUIContent TerrainMaterialWarning = EditorGUIUtility.TrTextContent("Built in or default materials are not supported in scriptable rendering pipeline. Please use custom terrain material.");

			// presets
			public static readonly GUIContent Preset = EditorGUIUtility.TrTextContent("Preset", "Preset setting file in use. Select a pre-saved preset asset or create a new preset.");
			public static readonly GUIContent SavePreset = EditorGUIUtility.TrTextContent("Save", "Save current settings to selected preset.");
			public static readonly GUIContent SaveAsPreset = EditorGUIUtility.TrTextContent("Save As", "Save the current preset as a new preset asset file.");
			public static readonly GUIContent RefreshPreset = EditorGUIUtility.TrTextContent("Refresh", "Load selected preset and apply to current settings");

			// apply button
			public static readonly GUIContent ApplySettingsBtn = EditorGUIUtility.TrTextContent("Apply to Selected Terrain(s)", "Start applying enabled settings to selected terrain(s).");
			public static readonly GUIContent ApplySettingsToAllBtn = EditorGUIUtility.TrTextContent("Apply to All Terrain(s) in Scene", "Start applying enabled settings to all terrain(s) in scene.");
		}

		public void OnEnable()
		{
			m_Terrains.AddRange(GameObject.FindObjectsOfType<Terrain>());
		}

		void Refresh()
		{
			m_Terrains.AddRange(GameObject.FindObjectsOfType<Terrain>());
		}

		bool GetSelectedTerrain()
		{
			var result = false;

			var selectedTerrains = Selection.GetFiltered(typeof(Terrain), SelectionMode.Unfiltered);
			if (selectedTerrains != null && selectedTerrains.Length > 0)
			{
				m_SelectedTerrain = selectedTerrains[0] as Terrain;
				result = true;
			}

			return result;
		}

		public void OnDisable()
		{

		}

		public void OnGUI()
		{
			// scroll view of settings
			EditorGUIUtility.hierarchyMode = true;
			TerrainToolboxUtilities.DrawSeperatorLine();
			m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Load Settings", EditorStyles.boldLabel);
			if (GUILayout.Button(Styles.LoadDefaultBtn))
			{
				ResetToDefaultSettings();
			}
			if (GUILayout.Button(Styles.LoadSelectedBtn))
			{
				LoadSettingsFromSelectedTerrain();
			}
			if (GUILayout.Button(Styles.LoadPresetBtn))
			{
				LoadPresetToSettings();
			}
			EditorGUILayout.EndHorizontal();

			// Presets
			TerrainToolboxUtilities.DrawSeperatorLine();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(Styles.Preset, EditorStyles.boldLabel);
			EditorGUI.BeginChangeCheck();
			m_SelectedPreset = (TerrainSettings)EditorGUILayout.ObjectField(m_SelectedPreset, typeof(TerrainSettings), false);
			if (EditorGUI.EndChangeCheck() && m_SelectedPreset != null)
			{
				if (EditorUtility.DisplayDialog("Confirm", "Load terrain settings from selected preset?", "OK", "Cancel"))
				{
					LoadPresetToSettings();
				}
			}
			if (GUILayout.Button(Styles.SavePreset))
			{
				if (m_SelectedPreset == null)
				{
					if (EditorUtility.DisplayDialog("Confirm", "No preset selected. Create a new preset?", "Continue", "Cancel"))
					{
						CreateNewPreset();
					}
				}
				else
				{
					SavePresetSettings();
				}
			}
			if (GUILayout.Button(Styles.SaveAsPreset))
			{
				CreateNewPreset();
			}
			if (GUILayout.Button(Styles.RefreshPreset))
			{
				LoadPresetToSettings();
			}
			EditorGUILayout.EndHorizontal();
			
			// basic settings
			TerrainToolboxUtilities.DrawSeperatorLine();
			bool basicSettingToggled = m_Settings.EnableBasicSettings;
			m_Settings.ShowBasicTerrainSettings = TerrainToolGUIHelper.DrawToggleHeaderFoldout(Styles.BasicTerrainSettings, m_Settings.ShowBasicTerrainSettings, ref basicSettingToggled, 0f);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowBasicTerrainSettings)
			{
				EditorGUI.BeginDisabledGroup(!m_Settings.EnableBasicSettings);
				ShowBasicSettings();
				EditorGUI.EndDisabledGroup();
			}
			--EditorGUI.indentLevel;
			m_Settings.EnableBasicSettings = basicSettingToggled;

			// mesh resolution settings
			TerrainToolboxUtilities.DrawSeperatorLine();
			bool meshSettingToggled = m_Settings.EnableMeshResSettings;
			m_Settings.ShowMeshResolutionSettings = TerrainToolGUIHelper.DrawToggleHeaderFoldout(Styles.MeshResolutionSettings, m_Settings.ShowMeshResolutionSettings, ref meshSettingToggled, 0f);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowMeshResolutionSettings)
			{
				EditorGUI.BeginDisabledGroup(!m_Settings.EnableMeshResSettings);
				ShowMeshResolutionSettings();
				EditorGUI.EndDisabledGroup();
			}
			--EditorGUI.indentLevel;
			m_Settings.EnableMeshResSettings = meshSettingToggled;

			// texture resolution settings
			TerrainToolboxUtilities.DrawSeperatorLine();
			bool textureSettingToggled = m_Settings.EnableTextureResSettings;
			m_Settings.ShowTextureResolutionSettings = TerrainToolGUIHelper.DrawToggleHeaderFoldout(Styles.TextureResolutionSettings, m_Settings.ShowTextureResolutionSettings, ref textureSettingToggled, 0f);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowTextureResolutionSettings)
			{
				EditorGUI.BeginDisabledGroup(!m_Settings.EnableTextureResSettings);
				ShowTextureResolutionSettings();
				EditorGUI.EndDisabledGroup();
			}
			--EditorGUI.indentLevel;
			m_Settings.EnableTextureResSettings = textureSettingToggled;

			// trees and details
			TerrainToolboxUtilities.DrawSeperatorLine();
			bool treeSettingToggled = m_Settings.EnableTreeSettings;
			m_Settings.ShowTreeAndDetailSettings = TerrainToolGUIHelper.DrawToggleHeaderFoldout(Styles.TreeAndDetailSettings, m_Settings.ShowTreeAndDetailSettings, ref treeSettingToggled, 0f);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowTreeAndDetailSettings)
			{
				EditorGUI.BeginDisabledGroup(!m_Settings.EnableTreeSettings);
                ShowTreeAndDetailSettings();
				EditorGUI.EndDisabledGroup();
			}
			--EditorGUI.indentLevel;
			m_Settings.EnableTreeSettings = treeSettingToggled;

			// grass wind
			TerrainToolboxUtilities.DrawSeperatorLine();
			bool grassSettingToggled = m_Settings.EnableWindSettings;
			m_Settings.ShowGrassWindSettings = TerrainToolGUIHelper.DrawToggleHeaderFoldout(Styles.GrassWindSettings, m_Settings.ShowGrassWindSettings, ref grassSettingToggled, 0f);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowGrassWindSettings)
			{
				EditorGUI.BeginDisabledGroup(!m_Settings.EnableWindSettings);
				ShowGrassWindSettings();
				EditorGUI.EndDisabledGroup();
			}
			--EditorGUI.indentLevel;
			m_Settings.EnableWindSettings = grassSettingToggled;

			--EditorGUI.indentLevel;
			EditorGUILayout.Space();
			EditorGUILayout.EndScrollView();
			
			// buttons
			TerrainToolboxUtilities.DrawSeperatorLine();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(Styles.ApplySettingsBtn, GUILayout.Height(40)))
			{
				ApplySettingsToSelectedTerrains();
			}
			if (GUILayout.Button(Styles.ApplySettingsToAllBtn, GUILayout.Height(40)))
			{
				ApplySettingsToAllTerrains("This operation will apply settings to all terrains in scene.");
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}

		void ShowBasicSettings()
		{
			m_Settings.GroupingID = EditorGUILayout.IntField(Styles.GroupingID, m_Settings.GroupingID);
			m_Settings.AutoConnect = EditorGUILayout.Toggle(Styles.AllowAutoConnect, m_Settings.AutoConnect);
			m_Settings.DrawHeightmap = EditorGUILayout.Toggle(Styles.DrawTerrain, m_Settings.DrawHeightmap);
			m_Settings.DrawInstanced = EditorGUILayout.Toggle(Styles.DrawInstancedTerrain, m_Settings.DrawInstanced);
			m_Settings.PixelError = EditorGUILayout.Slider(Styles.PixelError, m_Settings.PixelError, 1, 200);
			m_Settings.BaseMapDistance = EditorGUILayout.Slider(Styles.BaseMapDist, m_Settings.BaseMapDistance, 0, kBaseMapDistMax);
			m_Settings.ShadowCastingMode = (ShadowCastingMode)EditorGUILayout.EnumPopup(Styles.CastShadows, m_Settings.ShadowCastingMode);
#if UNITY_2019_2_OR_NEWER
			m_Settings.MaterialTemplate = EditorGUILayout.ObjectField("Material", m_Settings.MaterialTemplate, typeof(Material), false) as Material;
			m_Settings.ReflectionProbeUsage = (ReflectionProbeUsage)EditorGUILayout.EnumPopup(Styles.ReflectionProbes, m_Settings.ReflectionProbeUsage);
#else
			m_Settings.MaterialType = (Terrain.MaterialType)EditorGUILayout.EnumPopup(Styles.Material, m_Settings.MaterialType);

			if (m_Settings.MaterialType == Terrain.MaterialType.Custom)
			{
				m_Settings.MaterialTemplate = EditorGUILayout.ObjectField("Material", m_Settings.MaterialTemplate, typeof(Material), false) as Material;
				m_Settings.ReflectionProbeUsage = (ReflectionProbeUsage)EditorGUILayout.EnumPopup(Styles.ReflectionProbes, m_Settings.ReflectionProbeUsage);
			}

			if (m_Settings.MaterialType == Terrain.MaterialType.BuiltInLegacySpecular)
			{
				m_Settings.LegacySpecular = EditorGUILayout.ColorField("Specular Color", m_Settings.LegacySpecular);
				m_Settings.LegacyShininess = EditorGUILayout.Slider("Shininess", m_Settings.LegacyShininess, 0, 1);
			}
#endif
		}

		void ShowMeshResolutionSettings()
		{
			// max size defined in TerrainInspector.cs
			const int maxTerrainSize = 100000;
			const int maxTerrainHeight = 10000;
			m_Settings.TerrainWidth = Mathf.Clamp(EditorGUILayout.FloatField("Terrain Width", m_Settings.TerrainWidth), 1.0f, maxTerrainSize);
			m_Settings.TerrainLength = Mathf.Clamp(EditorGUILayout.FloatField("Terrain Length", m_Settings.TerrainLength), 1.0f, maxTerrainSize);
			m_Settings.TerrainHeight = Mathf.Clamp(EditorGUILayout.FloatField("Terrain Height", m_Settings.TerrainHeight), 1.0f, maxTerrainHeight);
			m_Settings.DetailResolutionPerPatch = EditorGUILayout.IntField("Detail Resolution Per Patch", m_Settings.DetailResolutionPerPatch);
			m_Settings.DetailResolutionPerPatch = Mathf.Clamp(m_Settings.DetailResolutionPerPatch, 8, 128);
			m_Settings.DetailResolutaion = EditorGUILayout.IntField("Detail Resolution", m_Settings.DetailResolutaion);
			m_Settings.DetailResolutaion = Mathf.Clamp(m_Settings.DetailResolutaion, 0, 4048);
		}

		void ShowTextureResolutionSettings()
		{
			m_Settings.BaseTextureResolution = EditorGUILayout.IntPopup(Styles.BaseTextureRes.text, m_Settings.BaseTextureResolution, ToolboxHelper.GUITextureResolutionNames, ToolboxHelper.GUITextureResolutions);
            m_Settings.AlphaMapResolution = EditorGUILayout.IntPopup(Styles.ControlTextureRes.text, m_Settings.AlphaMapResolution, ToolboxHelper.GUITextureResolutionNames, ToolboxHelper.GUITextureResolutions);
            m_Settings.HeightMapResolution = EditorGUILayout.IntPopup(Styles.HeightTextureRes.text, m_Settings.HeightMapResolution, ToolboxHelper.GUIHeightmapResolutionNames, ToolboxHelper.GUIHeightmapResolutions);
		}

		void ShowTreeAndDetailSettings()
		{
			m_Settings.DrawTreesAndFoliage = EditorGUILayout.Toggle(Styles.DrawTrees, m_Settings.DrawTreesAndFoliage);
			m_Settings.BakeLightProbesForTrees = EditorGUILayout.Toggle("Bake Light Probes For Trees", m_Settings.BakeLightProbesForTrees);
			m_Settings.DeringLightProbesForTrees = EditorGUILayout.Toggle("Remove Light Probe Ringing", m_Settings.DeringLightProbesForTrees);
			m_Settings.PreserveTreePrototypeLayers = EditorGUILayout.Toggle("Preserve Tree Prototype Layers", m_Settings.PreserveTreePrototypeLayers);
			m_Settings.DetailObjectDistance = EditorGUILayout.Slider("Detail Distance", m_Settings.DetailObjectDistance, 0, 250);
			m_Settings.DetailObjectDensity = EditorGUILayout.Slider("Detail Density", m_Settings.DetailObjectDensity, 0.0f, 1.0f);
			m_Settings.TreeDistance = EditorGUILayout.Slider("Tree Distance", m_Settings.TreeDistance, 0, 5000);
			m_Settings.TreeBillboardDistance = EditorGUILayout.Slider("Billboard Start", m_Settings.TreeBillboardDistance, 5, 2000);
			m_Settings.TreeCrossFadeLength = EditorGUILayout.Slider("Fade Length", m_Settings.TreeCrossFadeLength, 0, 200);
			m_Settings.TreeMaximumFullLODCount = EditorGUILayout.IntSlider("Max Mesh Trees", m_Settings.TreeMaximumFullLODCount, 0, 10000);
		}

		void ShowGrassWindSettings()
		{
			m_Settings.WavingGrassStrength = EditorGUILayout.Slider("Speed", m_Settings.WavingGrassStrength, 0, 1);
			m_Settings.WavingGrassSpeed = EditorGUILayout.Slider("Size", m_Settings.WavingGrassSpeed, 0, 1);
			m_Settings.WavingGrassAmount = EditorGUILayout.Slider("Bending", m_Settings.WavingGrassAmount, 0, 1);
			m_Settings.WavingGrassTint = EditorGUILayout.ColorField("Grass Tint", m_Settings.WavingGrassTint);
		}

        void ResetToDefaultSettings()
        {
            // Copy everything to our current settings.
            // Since UI settings are not copied, they remain unchanged.
            m_Settings.CopySettingsFrom(m_DefaultSettings);
        }

		void ApplySettingsToAllTerrains(string errorContext)
		{
			var terrains = Object.FindObjectsOfType<Terrain>();
			if (terrains != null && terrains.Length > 0)
			{
				ApplySettingsToTerrains(terrains, errorContext);
			}
		}

		void ApplySettingsToSelectedTerrains()
		{
			var terrainObjs = Selection.GetFiltered(typeof(Terrain), SelectionMode.Unfiltered);			
			if (terrainObjs != null && terrainObjs.Length > 0)
			{
				Terrain[] terrains = new Terrain[terrainObjs.Length];
				for (var i = 0; i < terrainObjs.Length; i++)
				{
					terrains[i] = terrainObjs[i] as Terrain;
				}

				ApplySettingsToTerrains(terrains);
			}
		}

		void ApplySettingsToTerrains(Terrain[] terrains, string errorContext="")
		{			
			int index = 0;
			try
			{
				bool continueEdit = true;
				
				// Only show this warning if the user has Mesh Settings enabled.
				if (m_Settings.EnableMeshResSettings)
				{
					var newSize = new Vector3(m_Settings.TerrainWidth, m_Settings.TerrainHeight,
						m_Settings.TerrainLength);
					foreach (var terrain in terrains)
					{
						// If any terrain has a size that's different from the specified settings, let's confirm 
						// the action.
						if (terrain.terrainData.size != newSize)
						{
							var message =
								"Some terrains have different sizes than the settings specified. This operation will resize the terrains potentially resulting in gaps and/or overlaps.";
							if (string.IsNullOrEmpty(errorContext))
							{
								continueEdit = EditorUtility.DisplayDialog("Confirm",
									$"{message}\nAre you sure you want to continue?",
									"Continue", "Cancel");
							}
							else
							{
								continueEdit = EditorUtility.DisplayDialog("Confirm",
									$"1. {errorContext}\n2. {message}\n\nAre you sure you want to continue?",
									"Continue", "Cancel");
							}
							break;
						}
					}
				}

				if (continueEdit)
				{
					foreach (var terrain in terrains)
					{
						EditorUtility.DisplayProgressBar("Applying settings changes on terrains",
							string.Format("Updating terrain tile {0}", terrain.name),
							((float) index / terrains.Length));

						if (m_Settings.EnableBasicSettings)
						{
							Undo.RecordObject(terrain, "Terrain property change");

							terrain.groupingID = m_Settings.GroupingID;
							terrain.allowAutoConnect = m_Settings.AutoConnect;
							terrain.drawHeightmap = m_Settings.DrawHeightmap;
							terrain.drawInstanced = m_Settings.DrawInstanced;
							terrain.heightmapPixelError = m_Settings.PixelError;
							terrain.basemapDistance = m_Settings.BaseMapDistance;
							terrain.shadowCastingMode = m_Settings.ShadowCastingMode;
							terrain.materialTemplate = m_Settings.MaterialTemplate;
							terrain.reflectionProbeUsage = m_Settings.ReflectionProbeUsage;
#if UNITY_2019_2_OR_NEWER
#else
						terrain.materialType = m_Settings.MaterialType;
						if (m_Settings.MaterialType != Terrain.MaterialType.Custom)
						{
							terrain.legacySpecular = m_Settings.LegacySpecular;
							terrain.legacyShininess = m_Settings.LegacyShininess;
						}
#endif
						}

						if (m_Settings.EnableMeshResSettings)
						{
							Undo.RecordObject(terrain.terrainData, "TerrainData property change");
							Vector3 size = new Vector3(m_Settings.TerrainWidth, m_Settings.TerrainHeight,
								m_Settings.TerrainLength);
							terrain.terrainData.size = size;
							terrain.terrainData.SetDetailResolution(m_Settings.DetailResolutaion,
								m_Settings.DetailResolutionPerPatch);
						}

						if (m_Settings.EnableTextureResSettings)
						{
							terrain.terrainData.baseMapResolution = m_Settings.BaseTextureResolution;
							if (m_Settings.AlphaMapResolution != terrain.terrainData.alphamapResolution)
							{
								ToolboxHelper.ResizeControlTexture(terrain.terrainData, m_Settings.AlphaMapResolution);
							}

							if (m_Settings.HeightMapResolution != terrain.terrainData.heightmapResolution)
							{
								ToolboxHelper.ResizeHeightmap(terrain.terrainData, m_Settings.HeightMapResolution);
							}
						}

						if (m_Settings.EnableTreeSettings)
						{
							Undo.RecordObject(terrain, "Terrain property change");

							terrain.drawTreesAndFoliage = m_Settings.DrawTreesAndFoliage;
							terrain.bakeLightProbesForTrees = m_Settings.BakeLightProbesForTrees;
							terrain.deringLightProbesForTrees = m_Settings.DeringLightProbesForTrees;
							terrain.preserveTreePrototypeLayers = m_Settings.PreserveTreePrototypeLayers;
							terrain.detailObjectDistance = m_Settings.DetailObjectDistance;
							terrain.collectDetailPatches = m_Settings.CollectDetailPatches;
							terrain.detailObjectDensity = m_Settings.DetailObjectDensity;
							terrain.treeDistance = m_Settings.TreeDistance;
							terrain.treeBillboardDistance = m_Settings.TreeBillboardDistance;
							terrain.treeCrossFadeLength = m_Settings.TreeCrossFadeLength;
							terrain.treeMaximumFullLODCount = m_Settings.TreeMaximumFullLODCount;
						}

						if (m_Settings.EnableWindSettings)
						{
							Undo.RecordObject(terrain.terrainData, "TerrainData property change");
							terrain.terrainData.wavingGrassStrength = m_Settings.WavingGrassStrength;
							terrain.terrainData.wavingGrassSpeed = m_Settings.WavingGrassSpeed;
							terrain.terrainData.wavingGrassAmount = m_Settings.WavingGrassAmount;
							terrain.terrainData.wavingGrassTint = m_Settings.WavingGrassTint;
						}

						index++;
					}
				}
			}
			finally
			{
				AssetDatabase.Refresh();
				EditorUtility.ClearProgressBar();
			}		
		}

		void SavePresetSettings()
		{
			m_SelectedPreset.CopySettingsFrom(m_Settings);
			m_SelectedPreset.CopyUISettingsFrom(m_Settings);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		void CreateNewPreset()
		{			
			string filePath = EditorUtility.SaveFilePanelInProject("Save Terrain Setting Preset", "New Terrain Settings Preset.asset", "asset", "");
			if (string.IsNullOrEmpty(filePath))
			{
				return;
			}

			if (!File.Exists(filePath))
			{
				m_SelectedPreset = null;
				var newPreset = ScriptableObject.CreateInstance<TerrainSettings>();
				newPreset.CopySettingsFrom(m_Settings);
				newPreset.CopyUISettingsFrom(m_Settings);
				AssetDatabase.CreateAsset(newPreset, filePath);
			}
			
			m_SelectedPreset = AssetDatabase.LoadAssetAtPath<TerrainSettings>(filePath);
			SavePresetSettings();
		}

		bool GetSettingsPreset()
		{
			if (m_SelectedPreset == null)
			{
				if (EditorUtility.DisplayDialog("Error", "No terrain settings preset found, create a new one?", "OK", "Cancel"))
				{
					CreateNewPreset();
					return true;
				}
				return false;
			}

			return true;
		}

		void LoadPresetToSettings()
		{
			if (m_SelectedPreset == null)
			{
				EditorUtility.DisplayDialog("Error", "No selected preset found. Select one to continue.", "OK");
				return;
			}

			m_Settings.CopySettingsFrom(m_SelectedPreset);
			m_Settings.CopyUISettingsFrom(m_SelectedPreset);
		}

		void LoadSettingsFromSelectedTerrain()
		{
			if (!GetSelectedTerrain())
			{
				return;
			}
			
			m_Settings.CopySettingsFrom(m_SelectedTerrain);
		}

		public void SaveSettings()
		{
			if (m_SelectedPreset != null)
			{
				m_Settings.PresetPath = AssetDatabase.GetAssetPath(m_SelectedPreset);
			}
			else
			{
				m_Settings.PresetPath = string.Empty;
			}

			string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsSettings);
			string settingsData = JsonUtility.ToJson(m_Settings);
			File.WriteAllText(filePath, settingsData);
		}

		public void LoadSettings()
		{
			string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsSettings);
			if (File.Exists(filePath))
			{
				string settingsData = File.ReadAllText(filePath);
				JsonUtility.FromJsonOverwrite(settingsData, m_Settings);
			}

			if (m_Settings.PresetPath == string.Empty)
			{
				m_SelectedPreset = null;
			}
			else
			{
				m_SelectedPreset = AssetDatabase.LoadAssetAtPath(m_Settings.PresetPath, typeof(TerrainSettings)) as TerrainSettings;
			}
		}
	}
}
