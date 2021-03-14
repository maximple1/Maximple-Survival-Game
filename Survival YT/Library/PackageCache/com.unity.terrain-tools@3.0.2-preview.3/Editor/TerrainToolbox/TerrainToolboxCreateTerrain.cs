using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
	public class TerrainToolboxCreateTerrain
	{
		Vector2 m_ScrollPosition = Vector2.zero;
		TerrainCreationSettings m_Settings = ScriptableObject.CreateInstance<TerrainCreationSettings>();

		// create terrain properties
		string m_TerrainMessage = string.Empty;
		Terrain[] m_Terrains;
		TerrainGroup[] m_TerrainGroups;
		GameObject m_CurrentGroup = null;
        const int kPixelErrorMax = 200;
        const int kBaseMapDistMax = 20000;

        // Preset
        TerrainCreationSettings m_SelectedPreset;

		// Heightmap
		Texture2D m_HeightmapGlobal = null;		

		static class Styles
		{
			public static readonly GUIContent GroupSettings = EditorGUIUtility.TrTextContent("Group Settings");
			public static readonly GUIContent HeightmapSettings = EditorGUIUtility.TrTextContent("Heightmap Settings");
			public static readonly GUIContent ImportHeightmap = EditorGUIUtility.TrTextContent("Import Heightmap", "Toggle to enable or disable import heightmap(s) on new terrains.");
			public static readonly GUIContent Options = EditorGUIUtility.TrTextContent("Options");

			public static readonly GUIContent TerrainWidth = EditorGUIUtility.TrTextContent("Total Terrain Width(m)", "Total width of the new terrain, along X axis.");
			public static readonly GUIContent TerrainLength = EditorGUIUtility.TrTextContent("Total Terrain Length(m)", "Total length of the new terrain, along Z axis.");
			public static readonly GUIContent TerrainHeight = EditorGUIUtility.TrTextContent("Terrain Height(m)", "Height of the new terrain, along Y axis.");
			public static readonly GUIContent StartPosition = EditorGUIUtility.TrTextContent("Start Position", "The starting position of the new terrain.");
			public static readonly GUIContent TilesXAxis = EditorGUIUtility.TrTextContent("Tiles X Axis", "Number of tiles along X axis.");
			public static readonly GUIContent TilesZAxis = EditorGUIUtility.TrTextContent("Tiles Z Axis", "Number of tiles along Z axis.");
			public static readonly GUIContent GroupingID = EditorGUIUtility.TrTextContent("Grouping ID", "Terrain grouping ID for auto connection.");
			public static readonly GUIContent AutoConnect = EditorGUIUtility.TrTextContent("Auto Connect", "Allow the current terrain tile automatically connect to neighboring tiles sharing the same grouping ID.");
			public static readonly GUIContent DrawInstanced = EditorGUIUtility.TrTextContent("Draw Instanced", "Toggle terrain instancing rendering");
			public static readonly GUIContent PixelError = EditorGUIUtility.TrTextContent("Pixel Error", "The accuracy of the mapping between the terrain maps (heightmap, textures, etc) and the generated terrain; higher values indicate lower accuracy but lower rendering overhead.");
			public static readonly GUIContent BaseMapDistance = EditorGUIUtility.TrTextContent("Base Map Distance", "The maximum distance at which terrain textures will be displayed at full resolution. Beyond this distance, a lower resolution composite image will be used for efficiency.");
			public static readonly GUIContent ShareMaterial = EditorGUIUtility.TrTextContent("Material Override", "Apply a custom material to terrain tiles");
			public static readonly GUIContent HeightmapResolution = EditorGUIUtility.TrTextContent("Heightmap Resolution", "The heightmap resolution of new terrain tile(s).");

			public static readonly GUIContent EnableHeightmapImport = EditorGUIUtility.TrTextContent("", "Enable/disable importing heightmap(s) for creating new terrain(s). If disabled, will generate terrain tiles with empty height values.");
			public static readonly GUIContent HeightmapMode = EditorGUIUtility.TrTextContent("Heightmap Mode", "Select a heightmap import mode.");
			public static readonly GUIContent HeightmapFormatUseRaw = EditorGUIUtility.TrTextContent("Use Raw File", "Enable to use raw file(s).");
			public static readonly GUIContent SelectRawHeightmap = EditorGUIUtility.TrTextContent("Select Raw File", "Select a raw file to import as global heightmap for the terrain(s).");
			public static readonly GUIContent SelectTextureHeightmap = EditorGUIUtility.TrTextContent("Select Texture", "Select a heightmap texture to import as global heightmap for the terrain(s).");
			public static readonly GUIContent SelectBatchHeightmapFolder = EditorGUIUtility.TrTextContent("Select Heightmap Folder", "Select the folder where heightmaps are. Heightmap files need to be named as NAME_INDEX-X-AXIS_INDEX-Z-AXIS. For example, heightmap_00_01");
			public static readonly GUIContent HeightmapWidth = EditorGUIUtility.TrTextContent("Heightmap Width", "Width of the selected heightmap(s).");
			public static readonly GUIContent HeightmapHeight = EditorGUIUtility.TrTextContent("Heightmap Height", "Width of the selected heightmap(s).");
			public static readonly GUIContent TileHeightResolution = EditorGUIUtility.TrTextContent("Tile Heightmap Resolution", "The heightmap resolution of each individual tile.");
			public static readonly GUIContent HeightmapRemap = EditorGUIUtility.TrTextContent("Height Remap", "Remap heightmap height to terrain height.");
			public static readonly GUIContent HeightmapRemapMin = EditorGUIUtility.TrTextContent("Min", "The terrain height that maps to the 0 value of the heightmap.");
			public static readonly GUIContent HeightmapRemapMax = EditorGUIUtility.TrTextContent("Max", "The terrain height that maps to the 1 value of the heightmap.");
			public static readonly GUIContent HeightmapDepth = EditorGUIUtility.TrTextContent("Depth", "Bit depth of selected heightmap(s).");
			public static readonly GUIContent FlipAxis = EditorGUIUtility.TrTextContent("Flip Axis", "Flip heightmap along selected axis when imports.");

			public static readonly GUIContent Preset = EditorGUIUtility.TrTextContent("Preset", "Preset used to create new terrain. Select a pre-saved creation preset asset or create a new preset.");
			public static readonly GUIContent SavePreset = EditorGUIUtility.TrTextContent("Save", "Save the current preset with current settings.");
			public static readonly GUIContent SaveAsPreset = EditorGUIUtility.TrTextContent("Save As", "Save the current preset as a new preset asset file.");
			public static readonly GUIContent RefreshPreset = EditorGUIUtility.TrTextContent("Refresh", "Load selected preset and apply to current creation settings");

			public static readonly GUIContent Gizmo = EditorGUIUtility.TrTextContent("Gizmo", "In-scene view gizmo to help visualize the scale of the terrain to be created.");
			public static readonly GUIContent EnableGizmo = EditorGUIUtility.TrTextContent("", "Enable/disable Gizmo");
			public static readonly GUIContent ShowGizmo = EditorGUIUtility.TrTextContent("Show", "Make gizmo object visible.");
			public static readonly GUIContent HideGizmo = EditorGUIUtility.TrTextContent("Hide", "Make gizmo object invisible.");
			public static readonly GUIContent GizmoSettings = EditorGUIUtility.TrTextContent("Gizmo Settings");
			public static readonly GUIContent CubeColor = EditorGUIUtility.TrTextContent("Cube Color");
			public static readonly GUIContent CubeWireColor = EditorGUIUtility.TrTextContent("Cube Wire Color");

			public static readonly GUIContent TerrainDataFolderPath = EditorGUIUtility.TrTextContent("TerrainData Directory", "Select or input a folder path where the new terrain data asset files will be saved.");
			public static readonly GUIContent TerrainDataGuidEnable = EditorGUIUtility.TrTextContent("TerrainData Name Enable Guid", "Enable/disable adding guid to new terrain data asset files. Adding guid will make sure naming is unique.");
			public static readonly GUIContent ClearExistingTerrainData = EditorGUIUtility.TrTextContent("Replace Terrains", "Enable to replace existing terrains of the same terrain group. Disable will keep existing terrains.");
			public static readonly GUIContent LightingAutobakeEnable = EditorGUIUtility.TrTextContent("Enable Auto Generate Lighting", "Enable auto generate lighting will auto bake lighting once terrain created.");

			public static readonly GUIContent CreateBtn = EditorGUIUtility.TrTextContent("Create", "Start creating new terrain(s) with current settings.");

			public static readonly GUIContent[] HeightmapToggles =
			{
				EditorGUIUtility.TrTextContent("Global", "Import one raw heightmap that used on the entire terrain tiles."),
				EditorGUIUtility.TrTextContent("Batch", "Import heightmap(s) from a folder and assign to each individual terrain tile automatically. Requires heightmap(s) sharing a name convention."),
				EditorGUIUtility.TrTextContent("Tiles", "Assign heightmap to individual terrain tile to import.")				
			};

			public static readonly GUIStyle ToggleButtonStyle = "LargeButton";
		}

		public void OnEnable()
		{

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

			// General Settings
			ShowGeneralGUI();

			// Import Heightmap
			TerrainToolboxUtilities.DrawSeperatorLine();
			bool importHeightmapToggle = m_Settings.EnableHeightmapImport;
			m_Settings.ShowHeightmapSettings = TerrainToolGUIHelper.DrawToggleHeaderFoldout(Styles.ImportHeightmap, m_Settings.ShowHeightmapSettings, ref importHeightmapToggle, 0f);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowHeightmapSettings)
			{
				EditorGUI.BeginDisabledGroup(!m_Settings.EnableHeightmapImport);
				ShowImportHeightmapGUI();
				EditorGUI.EndDisabledGroup();
			}
			m_Settings.EnableHeightmapImport = importHeightmapToggle;
			--EditorGUI.indentLevel;

			// Gizmos
			ShowGizmoGUI();

			// Presets
			++EditorGUI.indentLevel;
			ShowPresetGUI();
			EditorGUILayout.EndScrollView();

			// Options
			ShowOptionsGUI();

			// Terrain info box
			--EditorGUI.indentLevel;
			string sizeMsg = string.Format("Terrain Size: {0}m x {1}m		", m_Settings.TerrainWidth, m_Settings.TerrainLength);
			string tileMsg = string.Format("Number of Tiles: {0} x {1} \n", m_Settings.TilesX, m_Settings.TilesZ);
			string heightMsg = string.Format("Terrain Height: {0}m		", m_Settings.TerrainHeight);			
			string tileSizeMsg = string.Format("Tile Size: {0} x {1} ", (m_Settings.TerrainWidth / m_Settings.TilesX), (m_Settings.TerrainLength / m_Settings.TilesZ));
			m_TerrainMessage = sizeMsg + tileMsg + heightMsg + tileSizeMsg;
			EditorGUILayout.HelpBox(m_TerrainMessage, MessageType.Info);

			// Create			
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(Styles.CreateBtn, GUILayout.Height(40)))
			{
				if (!RunCreateValidations())
				{
					EditorUtility.DisplayDialog("Error", "There are incompatible terrain creation settings that need to be resolved to continue. Please check Console for details.", "OK");
				}
				else
				{
					if (m_Settings.EnableHeightmapImport && m_Settings.HeightmapMode == Heightmap.Mode.Global 
						&& File.Exists(m_Settings.GlobalHeightmapPath))
					{
						m_Settings.UseGlobalHeightmap = true;
					}
					else
					{
						m_Settings.UseGlobalHeightmap = false;
					}

					if (m_Settings.HeightmapMode == Heightmap.Mode.Global && m_HeightmapGlobal != null && m_Settings.FlipMode != Heightmap.Flip.None)
					{
						bool horizontal = m_Settings.FlipMode == Heightmap.Flip.Horizontal ? true : false;
						ToolboxHelper.FlipTexture(m_HeightmapGlobal, horizontal);
					}

					Create();
				}				
			}
			EditorGUILayout.EndHorizontal();
		}

		bool RunCreateValidations()
		{
			// check if same grouping ID terrain exists
			if (GroupExists(m_Settings.GroupID) && !m_Settings.EnableClearExistingData)
			{
				if (!EditorUtility.DisplayDialog("Warning", "You are trying to create same grouping ID terrains. This may result seam when sculpting across terrain tiles. Are you sure you want to continue?", "Continue", "Cancel"))
				{
					Debug.LogWarning("TerrainToobox: Trying to create same grouping ID terrains. You may want to create a different ID.");
					return false;
				}
			}

			// check heightmap settings
			if (m_Settings.EnableHeightmapImport && !ValidateHeightmap())
			{
				return false;
			}

			return true;
		}

		bool ValidateHeightmap()
		{			
			if (!ToolboxHelper.IsPowerOfTwo(m_Settings.HeightmapWidth) || !ToolboxHelper.IsPowerOfTwo(m_Settings.HeightmapHeight))
			{
				// texture is not power of two
				Debug.LogError("TerrainToolbox: Imported heightmap resolution is not power of two.");
				return false;
			}

			if (m_Settings.TilesX != 0 && m_Settings.TilesZ != 0)
			{
				float tileHeightX = (float)m_Settings.HeightmapWidth / (float)m_Settings.TilesX;
				float tileHeightZ = (float)m_Settings.HeightmapHeight / (float)m_Settings.TilesZ;
				if (tileHeightX != tileHeightZ)
				{
					// heights per tile is non-square
					Debug.LogError("TerrainToolbox: Heightmap resolution per tile is not square size with current settings.");
					return false;
				}
				if (!ToolboxHelper.IsInteger(tileHeightX) || !ToolboxHelper.IsInteger(tileHeightZ))
				{
					// heights per tile is not integer
					Debug.LogError("TerrainToolbox: Heightmap resolution per tile is not integer with current settings. You will get seams between tiles.");
					return false;
				}
				if (tileHeightX > 4096 || tileHeightX < 32)
				{
					// heightmap size per tile out of range
					Debug.LogError("TerrainToolbox: Heightmap resolution per tile is out of range. Supported resolution is from 32 to 4096.");
					return false;
				}
			}			

			if (m_Settings.HeightmapMode == Heightmap.Mode.Batch)
			{
				int tiles = m_Settings.TilesX * m_Settings.TilesZ;
				if (m_Settings.TileHeightmapPaths.Count != tiles)
				{
					// missing tile heightmap files
					Debug.LogError("TerrainToolbox: Number of heightmaps does not match number of terrain tiles.");
					return false;
				}
			}

			return true;
		}

		void ShowGeneralGUI()
		{
			EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			++EditorGUI.indentLevel;
			// Terrain Sizing
			EditorGUI.BeginChangeCheck();
            m_Settings.TerrainWidth = Mathf.Max(1, EditorGUILayout.IntField(Styles.TerrainWidth, m_Settings.TerrainWidth));
			m_Settings.TerrainLength = Mathf.Max(1, EditorGUILayout.IntField(Styles.TerrainLength, m_Settings.TerrainLength));
            m_Settings.TerrainHeight = Mathf.Max(1, EditorGUILayout.IntField(Styles.TerrainHeight, m_Settings.TerrainHeight));
            m_Settings.StartPosition = EditorGUILayout.Vector3IntField(Styles.StartPosition, Vector3Int.RoundToInt(m_Settings.StartPosition));
            if (EditorGUI.EndChangeCheck())
			{
                m_Settings.HeightmapRemapMax = m_Settings.TerrainHeight;
			}

            EditorGUI.BeginChangeCheck();
			m_Settings.TilesX = Mathf.Max(1, EditorGUILayout.IntField(Styles.TilesXAxis, m_Settings.TilesX));
			m_Settings.TilesZ = Mathf.Max(1, EditorGUILayout.IntField(Styles.TilesZAxis, m_Settings.TilesZ));
			if (EditorGUI.EndChangeCheck() && m_Settings.EnableHeightmapImport)
			{
				UpdateHeightmapInformation(m_Settings.GlobalHeightmapPath);
			}			

			// Terrain Group Settings
			m_Settings.GroupID = EditorGUILayout.IntField(Styles.GroupingID, m_Settings.GroupID);
			m_Settings.ShowGroupSettings = EditorGUILayout.Foldout(m_Settings.ShowGroupSettings, Styles.GroupSettings, true);
			if (m_Settings.ShowGroupSettings)
			{
                m_Settings.PixelError = EditorGUILayout.IntSlider(Styles.PixelError, m_Settings.PixelError, 0, kPixelErrorMax);
                m_Settings.BaseMapDistance = EditorGUILayout.IntSlider(Styles.BaseMapDistance, m_Settings.BaseMapDistance, 0, kBaseMapDistMax);
				m_Settings.MaterialOverride = EditorGUILayout.ObjectField(Styles.ShareMaterial, m_Settings.MaterialOverride, typeof(Material), false) as Material;
				m_Settings.HeightmapResolution = EditorGUILayout.IntPopup("Tile Height Resolution", m_Settings.HeightmapResolution, ToolboxHelper.GUIHeightmapResolutionNames, ToolboxHelper.GUIHeightmapResolutions);
			}
			--EditorGUI.indentLevel;
		}

		void ShowImportHeightmapGUI()
		{
			// Heightmap Mode
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(Styles.HeightmapMode);
			ToggleHightmapMode();
			EditorGUILayout.EndHorizontal();
			
			// Heightmap selector			
			if (m_Settings.HeightmapMode == Heightmap.Mode.Global)
			{
				m_Settings.UseRawFile = EditorGUILayout.Toggle(Styles.HeightmapFormatUseRaw, m_Settings.UseRawFile);
				if (m_Settings.UseRawFile)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(Styles.SelectRawHeightmap);
					EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
					EditorGUILayout.LabelField(m_Settings.GlobalHeightmapPath);
					EditorGUI.BeginChangeCheck();
					if (GUILayout.Button("...", GUILayout.Width(25.0f)))
					{
						m_Settings.GlobalHeightmapPath = EditorUtility.OpenFilePanelWithFilters("Select raw image file...", "Assets", new string[] { "Raw Image File", "raw" });
					}
					if (EditorGUI.EndChangeCheck())
					{
						UpdateHeightmapInformation(m_Settings.GlobalHeightmapPath);
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndHorizontal();
				}
				else
				{
					// use heightmap texture2D
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.BeginHorizontal();
					m_HeightmapGlobal = EditorGUILayout.ObjectField(Styles.SelectTextureHeightmap, m_HeightmapGlobal, typeof(Texture2D), false) as Texture2D;
					EditorGUILayout.EndHorizontal();
					if (EditorGUI.EndChangeCheck())
					{
						UpdateHeightmapInformation(string.Empty);
					}
				}				
			}
			else if (m_Settings.HeightmapMode == Heightmap.Mode.Tiles)
			{
				int fileIndex = 0;
				int numFiles = m_Settings.TilesX * m_Settings.TilesZ;
				for (int i = 0; i < numFiles; i++)
				{
					m_Settings.TileHeightmapPaths.Add(string.Empty);
				}

				for (int x = 0; x < m_Settings.TilesZ; x++)
				{
					for (int y = 0; y < m_Settings.TilesX; y++)
					{
						EditorGUILayout.BeginHorizontal();
						string tileIndex = "X-" + x + " | " + "Y-" + y;
						EditorGUILayout.LabelField(tileIndex);

						EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
						EditorGUILayout.LabelField(m_Settings.TileHeightmapPaths[fileIndex]);
						EditorGUI.BeginChangeCheck();
						if (GUILayout.Button("...", GUILayout.Width(25.0f)))
						{
							m_Settings.TileHeightmapPaths[fileIndex] = EditorUtility.OpenFilePanelWithFilters("Select raw image file...", "Assets", new string[] { "Raw Image File", "raw" });
						}
						if (EditorGUI.EndChangeCheck())
						{
							UpdateHeightmapInformation(m_Settings.TileHeightmapPaths[fileIndex]);
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.EndHorizontal();

						fileIndex++;
					}
				}
			}
			else if (m_Settings.HeightmapMode == Heightmap.Mode.Batch)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(Styles.SelectBatchHeightmapFolder);
				EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
				EditorGUILayout.LabelField(m_Settings.BatchHeightmapFolder);
				EditorGUI.BeginChangeCheck();
				if (GUILayout.Button("...", GUILayout.Width(25.0f)))
				{
					m_Settings.BatchHeightmapFolder = EditorUtility.OpenFolderPanel("Select heightmaps folder...", "", "");					
				}
				if (EditorGUI.EndChangeCheck() && Directory.Exists(m_Settings.BatchHeightmapFolder))
				{
					List<string> heightFiles = Directory.GetFiles(m_Settings.BatchHeightmapFolder, "*.raw").ToList();					
					m_Settings.TileHeightmapPaths = SortBatchHeightmapFiles(heightFiles);
					UpdateHeightmapInformation(m_Settings.TileHeightmapPaths[0]);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndHorizontal();
			}
			// Heightmap settings
			string infoMsg = string.Empty;
			string depthMsg = string.Empty;
			string sizeMsg = string.Format("Resolution: {0} x {1} \n", m_Settings.HeightmapWidth, m_Settings.HeightmapHeight);
			string tileMsg = string.Format("Number of Tiles: {0} x {1} \n", m_Settings.TilesX, m_Settings.TilesZ);
			string msg = string.Empty;
			if (m_Settings.UseRawFile)
			{
				infoMsg = "Heightmap(s) must use a single channel and be either 8 or 16 bit in RAW format. Resolution must be a power of two. \n";
				depthMsg = string.Format("Bit depth: {0}", ToolboxHelper.GetBitDepth(m_Settings.HeightmapDepth));
				msg = infoMsg + sizeMsg + tileMsg + depthMsg;
			}
			else
			{
				msg = sizeMsg + tileMsg;
			}			
			
			EditorGUILayout.HelpBox(msg, MessageType.Info);
			m_Settings.HeightmapResolution = EditorGUILayout.IntPopup("Tile Height Resolution", m_Settings.HeightmapResolution, ToolboxHelper.GUIHeightmapResolutionNames, ToolboxHelper.GUIHeightmapResolutions);
			EditorGUILayout.BeginHorizontal();			
			EditorGUILayout.MinMaxSlider(Styles.HeightmapRemap, ref m_Settings.HeightmapRemapMin, ref m_Settings.HeightmapRemapMax, 0f, (float)m_Settings.TerrainHeight);
			EditorGUILayout.LabelField(Styles.HeightmapRemapMin, GUILayout.Width(40.0f));			
			m_Settings.HeightmapRemapMin = EditorGUILayout.FloatField(m_Settings.HeightmapRemapMin, GUILayout.Width(75.0f));
			EditorGUILayout.LabelField(Styles.HeightmapRemapMax, GUILayout.Width(40.0f));
			m_Settings.HeightmapRemapMax = EditorGUILayout.FloatField(m_Settings.HeightmapRemapMax, GUILayout.Width(75.0f));
			EditorGUILayout.EndHorizontal();
			m_Settings.FlipMode = (Heightmap.Flip)EditorGUILayout.EnumPopup(Styles.FlipAxis, m_Settings.FlipMode);
		}

		void ShowPresetGUI()
		{
			TerrainToolboxUtilities.DrawSeperatorLine();
			--EditorGUI.indentLevel;
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(Styles.Preset, EditorStyles.boldLabel);
			EditorGUI.BeginChangeCheck();
			m_SelectedPreset = (TerrainCreationSettings)EditorGUILayout.ObjectField(m_SelectedPreset, typeof(TerrainCreationSettings), false);
			if (EditorGUI.EndChangeCheck() && m_SelectedPreset != null)
			{
				if (EditorUtility.DisplayDialog("Confirm", "Load terrain creation settings from selected preset?", "OK", "Cancel"))
				{
					LoadCreationSettings();
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
					UpdateCreationSettings();
					AssetDatabase.SaveAssets();
				}
			}
			if (GUILayout.Button(Styles.SaveAsPreset))
			{
				CreateNewPreset();
			}
			if (GUILayout.Button(Styles.RefreshPreset))
			{
				LoadCreationSettings();
			}
			EditorGUILayout.EndHorizontal();
		}

		void ShowOptionsGUI()
		{
			TerrainToolboxUtilities.DrawSeperatorLine();			
			m_Settings.ShowOptions = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.Options, m_Settings.ShowOptions);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowOptions)
			{				
				EditorGUILayout.BeginHorizontal();
				m_Settings.TerrainAssetDirectory = EditorGUILayout.TextField(Styles.TerrainDataFolderPath, m_Settings.TerrainAssetDirectory);
				if (GUILayout.Button("...", GUILayout.Width(25)))
				{
					m_Settings.TerrainAssetDirectory = EditorUtility.OpenFolderPanel("Select a folder...", m_Settings.TerrainAssetDirectory, "");
				}
				EditorGUILayout.EndHorizontal();
				m_Settings.EnableGuid = EditorGUILayout.Toggle(Styles.TerrainDataGuidEnable, m_Settings.EnableGuid);
				EditorGUILayout.BeginHorizontal();
				m_Settings.EnableClearExistingData = EditorGUILayout.Toggle(Styles.ClearExistingTerrainData, m_Settings.EnableClearExistingData);
				EditorGUILayout.LabelField(string.Format("Group ID: {0}", m_Settings.GroupID));
				EditorGUILayout.EndHorizontal();
				m_Settings.EnableLightingAutoBake = EditorGUILayout.Toggle(Styles.LightingAutobakeEnable, m_Settings.EnableLightingAutoBake);
			}
		}

		void ShowGizmoGUI()
		{
			TerrainToolboxUtilities.DrawSeperatorLine();
			bool gizmoToggle = m_Settings.EnableGizmo && ToolboxHelper.GizmoGO != null ? true : false;
			m_Settings.ShowGizmoSettings = TerrainToolGUIHelper.DrawToggleHeaderFoldout(Styles.Gizmo, m_Settings.ShowGizmoSettings, ref gizmoToggle, 0f);

			if (gizmoToggle && !ToolboxHelper.GizmoEnabled)
			{
				ToolboxHelper.ShowGizmo();
				ToolboxHelper.UpdateGizmos(m_Settings.TerrainWidth, m_Settings.TerrainHeight, m_Settings.TerrainLength, m_Settings.StartPosition, m_Settings.GroupID);
			}
			else if (!gizmoToggle && ToolboxHelper.GizmoEnabled)
			{
				ToolboxHelper.HideGizmo();
			}

			if (ToolboxHelper.GizmoEnabled && ToolboxHelper.GizmoGO != null)
			{
				if (GUI.changed)
				{
					ToolboxHelper.UpdateGizmos(m_Settings.TerrainWidth, m_Settings.TerrainHeight, m_Settings.TerrainLength, m_Settings.StartPosition, m_Settings.GroupID);
				}
				else if (ToolboxHelper.GizmoGO.transform.hasChanged)
				{
					Transform gizmoTransform = ToolboxHelper.GizmoGO.transform;
					Vector3Int gizmoScale = Vector3Int.RoundToInt(gizmoTransform.localScale);
					m_Settings.TerrainWidth = gizmoScale.x;
					m_Settings.TerrainLength = gizmoScale.z;
					m_Settings.TerrainHeight = gizmoScale.y;
					m_Settings.StartPosition = ToolboxHelper.GetGizmoPosition();
				}
			}

			// Update gizmo colors
			Color previousColor = m_Settings.GizmoCubeColor + m_Settings.GizmoWireColor;
			if (m_Settings.ShowGizmoSettings)
			{
				EditorGUI.BeginDisabledGroup(!m_Settings.EnableGizmo);
				EditorGUILayout.Space();
				m_Settings.GizmoCubeColor = EditorGUILayout.ColorField(Styles.CubeColor, m_Settings.GizmoCubeColor);
				m_Settings.GizmoWireColor = EditorGUILayout.ColorField(Styles.CubeWireColor, m_Settings.GizmoWireColor);
				EditorGUI.EndDisabledGroup();
			}
			m_Settings.EnableGizmo = gizmoToggle;

			if (previousColor != m_Settings.GizmoCubeColor + m_Settings.GizmoWireColor)
			{
				UnityEditor.SceneView.RepaintAll();
			}

			ToolboxHelper.SetGizmoColor(m_Settings.GizmoCubeColor, m_Settings.GizmoWireColor);

		}

		void UpdateHeightmapInformation(string path)
		{
			if (path == string.Empty)
			{
				if (m_HeightmapGlobal != null)
				{
					m_Settings.HeightmapWidth = m_HeightmapGlobal.width;
					m_Settings.HeightmapHeight = m_HeightmapGlobal.height;
				}
				else
				{
					m_Settings.HeightmapWidth = 0;
					m_Settings.HeightmapHeight = 0;
					m_Settings.HeightmapDepth = Heightmap.Depth.Bit8;
					return;
				}				
			}

			if (!File.Exists(path)) return;			

			// TODO - cache height data so we do not open file stream so often
			FileStream file = File.Open(path, FileMode.Open, FileAccess.Read);
			int fileSize = (int)file.Length;
			file.Close();

			m_Settings.HeightmapDepth = Heightmap.Depth.Bit16; 

			int pixels = fileSize / (int)m_Settings.HeightmapDepth;
			int width = Mathf.RoundToInt(Mathf.Sqrt(pixels));
			int height = Mathf.RoundToInt(Mathf.Sqrt(pixels));
			if ((width * height * (int)m_Settings.HeightmapDepth) == fileSize)
			{
				m_Settings.HeightmapWidth = width;
				m_Settings.HeightmapHeight = height;
			}
			else
			{
				m_Settings.HeightmapDepth = Heightmap.Depth.Bit8;
				pixels = fileSize / (int)m_Settings.HeightmapDepth;
				width = Mathf.RoundToInt(Mathf.Sqrt(pixels));
				height = Mathf.RoundToInt(Mathf.Sqrt(pixels));
				if ((width * height * (int)m_Settings.HeightmapDepth) == fileSize)
				{
					m_Settings.HeightmapWidth = width;
					m_Settings.HeightmapHeight = height;
				}
				else
				{
					m_Settings.HeightmapDepth = Heightmap.Depth.Bit16;
				}
			}		
		}

		void ToggleHightmapMode()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			m_Settings.HeightmapMode = (Heightmap.Mode)GUILayout.Toolbar((int)m_Settings.HeightmapMode, Styles.HeightmapToggles, Styles.ToggleButtonStyle, GUI.ToolbarButtonSize.Fixed);
			if (EditorGUI.EndChangeCheck())
			{
				GUIUtility.keyboardControl = 0;
			}
			EditorGUILayout.EndHorizontal();
		}

		void ClearExistingTerrainGroup(int groupID)
		{
			if (GroupExists(groupID))
			{
				TerrainGroup[] terrainGroups = UnityEngine.Object.FindObjectsOfType<TerrainGroup>().Where(g => g.GroupID == groupID).ToArray();
				foreach (var group in terrainGroups)
				{
					Terrain[] childTerrains = group.GetComponentsInChildren<Terrain>();
					List<string> dataPaths = new List<string>();
					if (childTerrains != null && childTerrains.Length > 0)
					{
						foreach (var t in childTerrains)
						{
							TerrainData data = t.terrainData;
							dataPaths.Add(AssetDatabase.GetAssetPath(data));
						}
					}

					foreach (var path in dataPaths)
					{
						AssetDatabase.DeleteAsset(path);
					}

					UnityEngine.Object groupObject = group.gameObject;
					UnityEngine.Object.DestroyImmediate(groupObject);
				}
				
				AssetDatabase.SaveAssets();
			}
		}

		void Create()
		{
			// check lighting auto bake
			if (m_Settings.EnableLightingAutoBake)
			{
				UnityEditor.Lightmapping.giWorkflowMode = UnityEditor.Lightmapping.GIWorkflowMode.Iterative;
			}
			else
			{
				UnityEditor.Lightmapping.giWorkflowMode = UnityEditor.Lightmapping.GIWorkflowMode.OnDemand;
			}

			if (m_Settings.EnableClearExistingData)
			{
				ClearExistingTerrainGroup(m_Settings.GroupID);
			}

			// create tiles
			int tileCount = m_Settings.TilesX * m_Settings.TilesZ;
			Vector2Int tileOffset = Vector2Int.zero;
			Vector2Int tileOffsetSource = Vector2Int.zero;
			Vector2Int tileResolution = new Vector2Int(m_Settings.TerrainWidth / m_Settings.TilesX, m_Settings.TerrainLength / m_Settings.TilesZ);
			Vector3 tileSize = new Vector3(tileResolution.x, m_Settings.TerrainHeight, tileResolution.y);
			Vector3 tilePosition = m_Settings.StartPosition;
			Terrain[] terrains = new Terrain[tileCount];

			string assetFolderPath = GetAssetPathFromFullPath(m_Settings.TerrainAssetDirectory);
			int tileIndex = 0;			

			try
			{
				// create terrain grouping object
				string groupName = "TerrainGroup_" + m_Settings.GroupID;				
				GameObject terrainGroup = new GameObject(groupName);
				TerrainGroup groupComp = terrainGroup.AddComponent<TerrainGroup>();	
				terrainGroup.transform.position = m_Settings.StartPosition;
				Heightmap globalHeightmap = null;

				Undo.RegisterCreatedObjectUndo(terrainGroup, "Create terrain");

				// heightmap offset
				if (m_Settings.UseGlobalHeightmap)
				{
					if (m_HeightmapGlobal != null && !m_Settings.UseRawFile)
					{
						tileOffsetSource = new Vector2Int(m_HeightmapGlobal.width / m_Settings.TilesX, m_HeightmapGlobal.height / m_Settings.TilesZ);
					}
					else
					{
						byte[] rawData = File.ReadAllBytes(m_Settings.GlobalHeightmapPath);
						globalHeightmap = new Heightmap(rawData, m_Settings.FlipMode);
						tileOffsetSource = new Vector2Int(globalHeightmap.Width / m_Settings.TilesX, globalHeightmap.Height / m_Settings.TilesZ);
					}					
				}
				else
				{
					tileOffsetSource = tileResolution;
				}

				for (int x = 0; x < m_Settings.TilesX; x++, tileOffset.x += tileOffsetSource.x, tilePosition.x += tileResolution.x)
				{
					tileOffset.y = 0;
					tilePosition.z = m_Settings.StartPosition.z;

					for (int y = 0; y < m_Settings.TilesZ; y++, tileOffset.y += tileOffsetSource.y, tilePosition.z += tileResolution.y)
					{
						EditorUtility.DisplayProgressBar("Creating terrains", string.Format("Updating terrain tile ({0}, {1})", x, y), ((float)tileIndex / tileCount));

						TerrainData terrainData = new TerrainData();
						terrainData.alphamapResolution = m_Settings.ControlTextureResolution;
						terrainData.baseMapResolution = m_Settings.BaseTextureResolution;
						terrainData.SetDetailResolution(m_Settings.DetailResolution, m_Settings.DetailResolutionPerPatch);						
						
						GameObject newGO = Terrain.CreateTerrainGameObject(terrainData);
						Terrain newTerrain = newGO.GetComponent<Terrain>();
						newTerrain.groupingID = m_Settings.GroupID;
						newTerrain.allowAutoConnect = m_Settings.AutoConnect;
						newTerrain.drawInstanced = m_Settings.DrawInstanced;
						newTerrain.heightmapPixelError = m_Settings.PixelError;
						newTerrain.basemapDistance = m_Settings.BaseMapDistance;
						if (m_Settings.MaterialOverride != null)
						{
							newTerrain.materialTemplate = m_Settings.MaterialOverride;
#if UNITY_2019_2_OR_NEWER
#else
							newTerrain.materialType = Terrain.MaterialType.Custom;
#endif
						}

						string terrainName = $"Terrain_{x}_{y}"; ;
						if (m_Settings.EnableGuid)
						{
							Guid newGuid = Guid.NewGuid();
							terrainName = $"Terrain_{x}_{y}_{newGuid}";
						}
						newGO.name = terrainName;
						newTerrain.transform.position = tilePosition;
						newTerrain.transform.SetParent(terrainGroup.transform);

						// heightmap remap
						var remap = (m_Settings.HeightmapRemapMax - m_Settings.HeightmapRemapMin) / m_Settings.TerrainHeight;
						var baseLevel = m_Settings.HeightmapRemapMin / m_Settings.TerrainHeight;

						// import height
						if (m_Settings.HeightmapMode == Heightmap.Mode.Global && m_Settings.UseRawFile && globalHeightmap != null)
						{
							Heightmap tileHeightmap = GetTileHeightmapFromGlobalHeightmap(globalHeightmap, tileOffset);
							tileHeightmap.ApplyTo(newTerrain);
						}

						// global texture2d
						if (m_Settings.HeightmapMode == Heightmap.Mode.Global && !m_Settings.UseRawFile && m_HeightmapGlobal != null)
						{							
							ToolboxHelper.CopyTextureToTerrainHeight(terrainData, m_HeightmapGlobal, new Vector2Int(x, y), (m_HeightmapGlobal.width / m_Settings.TilesX), m_Settings.TilesX, baseLevel, remap);
						}

						if (m_Settings.HeightmapMode == Heightmap.Mode.Tiles || m_Settings.HeightmapMode == Heightmap.Mode.Batch)
						{
							if (File.Exists(m_Settings.TileHeightmapPaths[tileIndex]))
							{								
								byte[] rawTileData = File.ReadAllBytes(m_Settings.TileHeightmapPaths[tileIndex]);
								Heightmap tileHeight = new Heightmap(rawTileData, m_Settings.FlipMode);
								Heightmap tileMap = new Heightmap(tileHeight, Vector2Int.zero, new Vector2Int(tileHeight.Width, tileHeight.Height), remap, baseLevel);
								tileMap.ApplyTo(newTerrain);
							}
						}

						terrains[tileIndex] = newTerrain;
						tileIndex++;

						// save terrain data asset
						terrainData.size = tileSize; // set terrain size after heightmap process
						string assetPath = $"{assetFolderPath}/{terrainName}.asset";
						if (!Directory.Exists(assetFolderPath))
						{
							Directory.CreateDirectory(assetFolderPath);
						}
						AssetDatabase.CreateAsset(terrainData, assetPath);

						// finally, resize height resolution if needed
						if (terrainData.heightmapResolution != (m_Settings.HeightmapResolution))
						{
							ToolboxHelper.ResizeHeightmap(terrainData, (m_Settings.HeightmapResolution));
						}						

						Undo.RegisterCreatedObjectUndo(newGO, "Create terrain");
					}
				}

				m_Terrains = terrains;
				m_CurrentGroup = terrainGroup;

				UpdateGroupSettings(groupComp);
				ToolboxHelper.CalculateAdjacencies(m_Terrains, m_Settings.TilesX, m_Settings.TilesZ);
			}
			finally
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.ClearProgressBar();
			}
		}

		string GetAssetPathFromFullPath(string fullPath)
		{
			string newPath = fullPath;
			string assetsFolder = "Assets";
			string assetsDataPath = Application.dataPath;
			if (fullPath.Contains(assetsDataPath))
			{ 
				newPath = assetsFolder + fullPath.Replace(assetsDataPath, string.Empty);
			}

			return newPath;
		}

		List<string> SortBatchHeightmapFiles(List<string> files)
		{
			// sort by the tiles index after "_"
			return files.OrderBy(x => x.Substring(x.IndexOf("_"))).ToList();
		}

		bool GroupExists(int id)
		{
			var groups = UnityEngine.Object.FindObjectsOfType<TerrainGroup>();

			foreach (var group in groups)
			{
				var comp = group.GetComponent<TerrainGroup>();
				if (comp.GroupID == id)
				{
					return true;
				}
			}

			return false;
		}

		Heightmap GetTileHeightmapFromGlobalHeightmap(Heightmap heightmap, Vector2Int tileOffset)
		{
			var remap = (m_Settings.HeightmapRemapMax - m_Settings.HeightmapRemapMin) / m_Settings.TerrainHeight;
			var baseLevel = m_Settings.HeightmapRemapMin / m_Settings.TerrainHeight;
			Heightmap tileHeightmap = null;
			Vector2Int numHeightsPerTile = new Vector2Int(heightmap.Width / m_Settings.TilesX, heightmap.Height / m_Settings.TilesZ);
			tileHeightmap = new Heightmap(heightmap, tileOffset, numHeightsPerTile, remap, baseLevel);

			return tileHeightmap;
		}		

		void CreateNewPreset()
		{
			string filePath = EditorUtility.SaveFilePanelInProject("Create Terrain Creation Settings", "New Terrain Creation.asset", "asset", "");
			if (string.IsNullOrEmpty(filePath))
			{
				return;
			}
			m_SelectedPreset = null;
			var newPreset = ScriptableObject.CreateInstance<TerrainCreationSettings>();
			newPreset = m_Settings;
			AssetDatabase.CreateAsset(newPreset, filePath);
			m_SelectedPreset = newPreset;

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		bool GetCreationSettingPreset()
		{
			if (m_SelectedPreset == null)
			{
				if (EditorUtility.DisplayDialog("Error", "No terrain creation settings found, create a new one?", "OK", "Cancel"))
				{
					CreateNewPreset();
					return true;
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		void UpdateCreationSettings()
		{
			if (!GetCreationSettingPreset()) return;

			m_SelectedPreset.TerrainWidth = m_Settings.TerrainWidth;
			m_SelectedPreset.TerrainLength = m_Settings.TerrainLength;
			m_SelectedPreset.TerrainHeight = m_Settings.TerrainHeight;
			m_SelectedPreset.StartPosition = m_Settings.StartPosition;
			m_SelectedPreset.TilesX = m_Settings.TilesX;
			m_SelectedPreset.TilesZ = m_Settings.TilesZ;
			m_SelectedPreset.GroupID = m_Settings.GroupID;
			m_SelectedPreset.AutoConnect = m_Settings.AutoConnect;
			m_SelectedPreset.DrawInstanced = m_Settings.DrawInstanced;
			m_SelectedPreset.PixelError = m_Settings.PixelError;
			m_SelectedPreset.BaseMapDistance = m_Settings.BaseMapDistance;
			m_SelectedPreset.BaseTextureResolution = m_Settings.BaseTextureResolution;
			m_SelectedPreset.ControlTextureResolution = m_Settings.ControlTextureResolution;
			m_SelectedPreset.DetailResolution = m_Settings.DetailResolution;
			m_SelectedPreset.DetailResolutionPerPatch = m_Settings.DetailResolutionPerPatch;
			m_SelectedPreset.HeightmapResolution = m_Settings.HeightmapResolution;
			m_SelectedPreset.UseGlobalHeightmap = m_Settings.UseGlobalHeightmap;
			m_SelectedPreset.HeightmapMode = m_Settings.HeightmapMode;
			m_SelectedPreset.HeightmapDepth = m_Settings.HeightmapDepth;
			m_SelectedPreset.FlipMode = m_Settings.FlipMode;
			m_SelectedPreset.GizmoCubeColor = m_Settings.GizmoCubeColor;
			m_SelectedPreset.GizmoWireColor = m_Settings.GizmoWireColor;
			
			if (m_Settings.HeightmapMode == Heightmap.Mode.Global)
			{
				m_SelectedPreset.GlobalHeightmapPath = m_Settings.GlobalHeightmapPath;
			}

			if (m_Settings.HeightmapMode == Heightmap.Mode.Batch)
			{
				m_SelectedPreset.BatchHeightmapFolder = m_Settings.BatchHeightmapFolder;
			}

			if (m_Settings.HeightmapMode == Heightmap.Mode.Tiles)
			{
				m_SelectedPreset.TileHeightmapPaths.Clear();
				m_SelectedPreset.TileHeightmapPaths = m_Settings.TileHeightmapPaths.ToList();
			}			
		}

		void LoadCreationSettings()
		{
			if (!GetCreationSettingPreset()) return;

			m_Settings.TerrainWidth = m_SelectedPreset.TerrainWidth;
			m_Settings.TerrainHeight = m_SelectedPreset.TerrainHeight;
			m_Settings.TerrainLength = m_SelectedPreset.TerrainLength;
			m_Settings.StartPosition = m_SelectedPreset.StartPosition;
			m_Settings.TilesX = m_SelectedPreset.TilesX;
			m_Settings.TilesZ = m_SelectedPreset.TilesZ;
			m_Settings.GroupID = m_SelectedPreset.GroupID;
			m_Settings.AutoConnect = m_SelectedPreset.AutoConnect;
			m_Settings.DrawInstanced = m_SelectedPreset.DrawInstanced;
			m_Settings.PixelError = m_SelectedPreset.PixelError;
			m_Settings.BaseMapDistance = m_SelectedPreset.BaseMapDistance;
			m_Settings.BaseTextureResolution = m_SelectedPreset.BaseTextureResolution;
			m_Settings.ControlTextureResolution = m_SelectedPreset.ControlTextureResolution;
			m_Settings.DetailResolution = m_SelectedPreset.DetailResolution;
			m_Settings.DetailResolutionPerPatch = m_SelectedPreset.DetailResolutionPerPatch;
			m_Settings.HeightmapResolution = m_SelectedPreset.HeightmapResolution;
			m_Settings.UseGlobalHeightmap = m_SelectedPreset.UseGlobalHeightmap;
			m_Settings.HeightmapMode = m_SelectedPreset.HeightmapMode;
			m_Settings.HeightmapDepth = m_SelectedPreset.HeightmapDepth;
			m_Settings.FlipMode = m_SelectedPreset.FlipMode;
			m_Settings.GlobalHeightmapPath = m_SelectedPreset.GlobalHeightmapPath;
			m_Settings.BatchHeightmapFolder = m_SelectedPreset.BatchHeightmapFolder;
			m_Settings.GizmoCubeColor = m_SelectedPreset.GizmoCubeColor;
			m_Settings.GizmoWireColor = m_SelectedPreset.GizmoWireColor;
			m_Settings.TileHeightmapPaths.Clear();
			m_Settings.TileHeightmapPaths = m_SelectedPreset.TileHeightmapPaths.ToList();
			
		}

		void UpdateGroupSettings(TerrainGroup group)
		{
			group.GroupID = m_Settings.GroupID;
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

			string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsCreate);
			string createSettingsData = JsonUtility.ToJson(m_Settings);
			File.WriteAllText(filePath, createSettingsData);			
		}

		public void LoadSettings()
		{
			string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsCreate);
			if (File.Exists(filePath))
			{
				string createSettingsData = File.ReadAllText(filePath);
				JsonUtility.FromJsonOverwrite(createSettingsData, m_Settings);
			}

			if (m_Settings.PresetPath == string.Empty)
			{
				m_SelectedPreset = null;
			}
			else
			{
				m_SelectedPreset = AssetDatabase.LoadAssetAtPath(m_Settings.PresetPath, typeof(TerrainCreationSettings)) as TerrainCreationSettings;
			}
		}
	}
}
