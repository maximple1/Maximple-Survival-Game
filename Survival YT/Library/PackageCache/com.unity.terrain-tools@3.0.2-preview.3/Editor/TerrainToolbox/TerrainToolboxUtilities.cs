using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
	[Serializable]
	public class UtilitySettings : ScriptableObject
	{
		// Terrain Split
		public int TileXAxis = 2;
		public int TileZAxis = 2;
		public bool AutoUpdateSettings = true;
		public bool KeepOldTerrains = true;
		public string TerrainAssetDir = "Assets/Terrain";

		// Layers
		public string PalettePath = string.Empty;
		public bool ClearExistLayers = true;
		public bool ApplyAllTerrains = true;

		// Replace splatmap
		public Terrain SplatmapTerrain;
		public Texture2D SplatmapOld0;
		public Texture2D SplatmapNew0;
		public Texture2D SplatmapOld1;
		public Texture2D SplatmapNew1;
		public ImageFormat SelectedFormat = ImageFormat.TGA;
		public RotationAdjustment RotationAdjust = RotationAdjustment.Clockwise;
		public FlipAdjustment FlipAdjust = FlipAdjustment.Horizontal;
		public string SplatFolderPath = "Assets/Splatmaps/";
		public bool AdjustAllSplats = false;

		// Import heightmap
		public Texture2D HeightmapImport;
		public int HeightmapResolution;
		public float ImportHeightRemapMin;
		public float ImportHeightRemapMax;
		public Heightmap.Flip HeightmapFlipMode = Heightmap.Flip.None;

		// Export heightmaps
		public string HeightmapFolderPath = "Assets/Heightmaps/";
		public Heightmap.Format HeightFormat = Heightmap.Format.RAW;
		public Heightmap.Depth HeightmapDepth = Heightmap.Depth.Bit16;
		public ToolboxHelper.ByteOrder HeightmapByteOrder = ToolboxHelper.ByteOrder.Windows;
		public float ExportHeightRemapMin = 0.0f;
		public float ExportHeightRemapMax = 1.0f;
		public bool FlipVertically = false;

		// Enums
		public enum ImageFormat { TGA, PNG }
		public enum SplatmapChannel { R, G, B, A }
		public enum RotationAdjustment { Clockwise, Counterclockwise }
		public enum FlipAdjustment { Horizontal, Vertical }

		// GUI
		public bool ShowTerrainEdit = false;
		public bool ShowTerrainLayers = false;
		public bool ShowReplaceSplatmaps = false;
		public bool ShowExportSplatmaps = false;
		public bool ShowExportHeightmaps = false;
	}

	public class TerrainToolboxUtilities
	{
		Vector2 m_ScrollPosition = Vector2.zero;
		internal UtilitySettings m_Settings = ScriptableObject.CreateInstance<UtilitySettings>();

		// Splatmaps
		int m_SplatmapResolution = 0;
		Terrain[] m_SplatExportTerrains;
		// Terrain Edit
		Terrain[] m_Terrains;
		List<Material> m_TerrainMaterials = new List<Material>();
		// Terrain Split
		Terrain[] m_SplitTerrains;
		// Layers
		List<TerrainLayer> m_CopiedLayers = new List<TerrainLayer>();
		List<Layer> m_PaletteLayers = new List<Layer>();
		ReorderableList m_LayerList;
		TerrainPalette m_SelectedLayerPalette = ScriptableObject.CreateInstance<TerrainPalette>();
		// Heightmap export
		Dictionary<string, Heightmap.Depth> m_DepthOptions = new Dictionary<string, Heightmap.Depth>()
		{
			{ "16 bit", Heightmap.Depth.Bit16 },
			{ "8 bit", Heightmap.Depth.Bit8 }
		};
		internal int m_SelectedDepth = 0;

		// Splatmaps
		internal List<Texture2D> m_Splatmaps = new List<Texture2D>();
		HashSet<Texture2D> m_SplatmapHasCopy = new HashSet<Texture2D>();
		internal ReorderableList m_SplatmapList;
		int m_SelectedSplatMap = 0;
		bool m_PreviewIsDirty = false;
		MaterialPropertyBlock m_PreviewMaterialPropBlock = new MaterialPropertyBlock();
		ToolboxHelper.RenderPipeline m_ActiveRenderPipeline = ToolboxHelper.RenderPipeline.None;

		//Visualization
		Material m_PreviewMaterial;
		bool m_ShowSplatmapPreview = false;
#if UNITY_2019_2_OR_NEWER
#else
		Terrain.MaterialType m_TerrainMaterialType;
		float m_TerrainLegacyShininess;
		Color m_TerrainLegacySpecular;
#endif

		int m_MaxLayerCount = 0;
		int m_MaxSplatmapCount = 0;

		const int kMaxLayerHD = 8; // HD allows up to 8 layers with 2 splat alpha maps
		const int kMaxLayerFeatureLW = 4; // LW allows up to 4 layers when having density or height-based blending enabled
		const int kMaxSplatmapHD = 2;
		const int kMaxSplatmapFeatureLW = 1;
		const int kMaxNoLimit = 20;
		const int kMinHeightmapRes = 32;

		static class Styles
		{
			public static readonly GUIContent TerrainLayers = EditorGUIUtility.TrTextContent("Terrain Layers");
			public static readonly GUIContent ImportSplatmaps = EditorGUIUtility.TrTextContent("Terrain Splatmaps");
			public static readonly GUIContent ExportSplatmaps = EditorGUIUtility.TrTextContent("Export Splatmaps");
			public static readonly GUIContent ExportHeightmaps = EditorGUIUtility.TrTextContent("Export Heightmaps");
			public static readonly GUIContent TerrainEdit = EditorGUIUtility.TrTextContent("Terrain Edit");
			public static readonly GUIContent DuplicateTerrain = EditorGUIUtility.TrTextContent("Duplicate");
			public static readonly GUIContent RemoveTerrain = EditorGUIUtility.TrTextContent("Clean Remove");
			public static readonly GUIContent SplitTerrain = EditorGUIUtility.TrTextContent("Split");

			public static readonly GUIContent DuplicateTerrainBtn = EditorGUIUtility.TrTextContent("Duplicate", "Start duplicating selected terrain(s) and create new terrain data.");
			public static readonly GUIContent RemoveTerrainBtn = EditorGUIUtility.TrTextContent("Remove", "Start removing selected terrain(s) and delete terrain data asset files.");

			public static readonly GUIContent PalettePreset = EditorGUIUtility.TrTextContent("Palette Preset:", "Select or make a palette preset asset.");
			public static readonly GUIContent SavePalette = EditorGUIUtility.TrTextContent("Save", "Save the current palette asset file on disk.");
			public static readonly GUIContent SaveAsPalette = EditorGUIUtility.TrTextContent("Save As", "Save the current palette asset as a new file on disk.");
			public static readonly GUIContent RefreshPalette = EditorGUIUtility.TrTextContent("Refresh", "Load selected palette and apply to list of layers.");
			public static readonly GUIContent ClearExistingLayers = EditorGUIUtility.TrTextContent("Clear Existing Layers", "Remove existing layers on selected terrain(s).");
			public static readonly GUIContent ApplyToAllTerrains = EditorGUIUtility.TrTextContent("All Terrains in Scene", "When unchecked only apply layer changes to selected terrain(s).");
			public static readonly GUIContent ImportTerrainLayersBtn = EditorGUIUtility.TrTextContent("Import From Terrain", "Import layers from the selected terrain.");
			public static readonly GUIContent AddLayersBtn = EditorGUIUtility.TrTextContent("Add to Terrain(s)", "Start adding layers to either all or selected terrain(s).");
			public static readonly GUIContent RemoveLayersBtn = EditorGUIUtility.TrTextContent("Remove All Layers", "Start removing all layers from either all or selected terrain(s)");

			public static readonly GUIContent TerrainToReplaceSplatmap = EditorGUIUtility.TrTextContent("Terrain", "Select a terrain to replace splatmaps on.");
			public static readonly GUIContent SplatmapResolution = EditorGUIUtility.TrTextContent("Splatmap Resolution: ", "The control texture resolution setting of selected terrain.");
			public static readonly GUIContent SplatAlpha0 = EditorGUIUtility.TrTextContent("Old SplatAlpha0", "The SplatAlpha 0 texture from selected terrain.");
			public static readonly GUIContent SplatAlpha1 = EditorGUIUtility.TrTextContent("Old SplatAlpha1", "The SplatAlpha 1 texture from selected terrain.");
			public static readonly GUIContent SplatAlpha0New = EditorGUIUtility.TrTextContent("New SplatAlpha0", "Select a texture to replace the SplatAlpha 0 texture on selected terrain.");
			public static readonly GUIContent SplatAlpha1New = EditorGUIUtility.TrTextContent("New SplatAlpha1", "Select a texture to replace the SplatAlpha 1 texture on selected terrain.");
			public static readonly GUIContent ImportFromSplatmapBtn = EditorGUIUtility.TrTextContent("Import from Selected Terrain", "Import splatmaps from the selected terrain.");
			public static readonly GUIContent ReplaceSplatmapsBtn = EditorGUIUtility.TrTextContent("Replace Splatmaps", "Replace splatmaps with new splatmaps on selected terrain.");
			public static readonly GUIContent ResetSplatmapsBtn = EditorGUIUtility.TrTextContent("Reset Splatmaps", "Clear splatmap textures on selected terrain(s).");
			public static readonly GUIContent ExportToTerrSplatmapBtn = EditorGUIUtility.TrTextContent("Apply to Terrain", "Export splatmaps to selected terrains.");
			public static readonly GUIContent PreviewSplatMapTogg = EditorGUIUtility.TrTextContent("Preview Splatmap", "Preview a splatmap on selected terrains.");
			public static readonly GUIContent RotateSplatmapLabel = EditorGUIUtility.TrTextContent("Rotate Adjustment", "Select whether to rotate clockwise or counterclockwise.");
			public static readonly GUIContent FlipSplatmapLabel = EditorGUIUtility.TrTextContent("Flip Adjustment", "Select whether to flip horizontally or vertically.");
			public static readonly GUIContent RotateSplatmapBtn = EditorGUIUtility.TrTextContent("Rotate", "Rotate splatmap in the selected direction.");
			public static readonly GUIContent FlipSplatmapBtn = EditorGUIUtility.TrTextContent("Flip", "Flip splatmaps in the selected direction.");
			public static readonly GUIContent MultiAdjustTogg = EditorGUIUtility.TrTextContent("Adjust All Splatmaps", "Make changes to all splatmaps.");
			public static readonly GUIContent ExportSplatmapFolderPath = EditorGUIUtility.TrTextContent("Export Folder Path", "Select or input a folder path where splatmap textures will be saved.");
			public static readonly GUIContent ExportSplatmapFormat = EditorGUIUtility.TrTextContent("Splatmap Format", "Texture format of exported splatmap(s).");
			public static readonly GUIContent ExportSplatmapsBtn = EditorGUIUtility.TrTextContent("Export Splatmaps", "Start exporting splatmaps into textures as selected format from selected terrain(s).");

			public static readonly GUIContent OriginalTerrain = EditorGUIUtility.TrTextContent("Original Terrain", "Select a terrain to split into smaller tiles.");
			public static readonly GUIContent TilesX = EditorGUIUtility.TrTextContent("Tiles X Axis", "Number of tiles along X axis.");
			public static readonly GUIContent TilesZ = EditorGUIUtility.TrTextContent("Tiles Z Axis", "Number of tiles along Z axis.");
			public static readonly GUIContent AutoUpdateSetting = EditorGUIUtility.TrTextContent("Auto Update Terrain Settings", "Automatically copy terrain settings to new tiles from original tiles upon create.");
			public static readonly GUIContent KeepOldTerrains = EditorGUIUtility.TrTextContent("Keep Original Terrain", "Keep original terrain while splitting.");
			public static readonly GUIContent SplitTerrainBtn = EditorGUIUtility.TrTextContent("Split", "Start splitting original terrain into small tiles.");

			public static readonly GUIContent ExportHeightmapsBtn = EditorGUIUtility.TrTextContent("Export Heightmaps", "Start exporting raw heightmaps for selected terrain(s).");
			public static readonly GUIContent HeightmapSelectedFormat = EditorGUIUtility.TrTextContent("Heightmap Format", "Select the image format for exported heightmaps.");
			public static readonly GUIContent ExportHeightmapFolderPath = EditorGUIUtility.TrTextContent("Export Folder Path", "Select or input a folder path where heightmaps will be saved.");
			public static readonly GUIContent HeightmapBitDepth = EditorGUIUtility.TrTextContent("Heightmap Depth", "Select heightmap depth option from 8 bit or 16 bit.");
			public static readonly GUIContent HeightmapByteOrder = EditorGUIUtility.TrTextContent("Heightmap Byte Order", "Select heightmap byte order from Windows or Mac.");
			public static readonly GUIContent HeightmapRemap = EditorGUIUtility.TrTextContent("Levels Correction", "Remap the height range before export.");
			public static readonly GUIContent HeightmapRemapMin = EditorGUIUtility.TrTextContent("Min", "Minimum input height");
			public static readonly GUIContent HeightmapRemapMax = EditorGUIUtility.TrTextContent("Max", "Maximum input height");
			public static readonly GUIContent FlipVertically = EditorGUIUtility.TrTextContent("Flip Vertically", "Flip heights vertically when export. Enable this if using heightmap in external program like World Machine. Or use the Flip Y Axis option in World Machine instead.");

			public static readonly GUIStyle ToggleButtonStyle = "LargeButton";
		}

		public static void DrawSeperatorLine()
		{
			Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(12));
			rect.height = 1;
			rect.y = rect.y + 5;
			rect.x = 2;
			rect.width += 6;
			EditorGUI.DrawRect(rect, new Color(0.35f, 0.35f, 0.35f));
		}

		public void OnLoad()
		{
			if (m_Settings.PalettePath != string.Empty && File.Exists(m_Settings.PalettePath))
			{
				LoadPalette();
			}
		}

		public void OnGUI()
		{
			// scroll view of settings
			EditorGUIUtility.hierarchyMode = true;
			DrawSeperatorLine();
			m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

			// Terrain Edit			
			m_Settings.ShowTerrainEdit = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.TerrainEdit, m_Settings.ShowTerrainEdit);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowTerrainEdit)
			{
				ShowTerrainEditGUI();
			}
			--EditorGUI.indentLevel;
			DrawSeperatorLine();

			// Terrain Layers
			m_Settings.ShowTerrainLayers = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.TerrainLayers, m_Settings.ShowTerrainLayers);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowTerrainLayers)
			{
				ShowTerrainLayerGUI();
			}
			--EditorGUI.indentLevel;
			DrawSeperatorLine();

			// Terrain Splatmaps
			m_Settings.ShowReplaceSplatmaps = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.ImportSplatmaps, m_Settings.ShowReplaceSplatmaps);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowReplaceSplatmaps)
			{
				ShowSplatmapImportGUI();
			}
			--EditorGUI.indentLevel;
			DrawSeperatorLine();

			// Export Spaltmaps
			m_Settings.ShowExportSplatmaps = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.ExportSplatmaps, m_Settings.ShowExportSplatmaps);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowExportSplatmaps)
			{
				ShowExportSplatmapGUI();
			}
			--EditorGUI.indentLevel;
			DrawSeperatorLine();

			// Export Heightmaps
			m_Settings.ShowExportHeightmaps = TerrainToolGUIHelper.DrawHeaderFoldout(Styles.ExportHeightmaps, m_Settings.ShowExportHeightmaps);
			++EditorGUI.indentLevel;
			if (m_Settings.ShowExportHeightmaps)
			{
				ShowExportHeightmapGUI();
			}
			--EditorGUI.indentLevel;

			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}

		void ShowTerrainEditGUI()
		{
			// Duplicate Terrain
			EditorGUILayout.LabelField(Styles.DuplicateTerrain, EditorStyles.boldLabel);
			++EditorGUI.indentLevel;
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Select terrain(s) to make a copy from with new terrain data assets: ");
			if (GUILayout.Button(Styles.DuplicateTerrain, GUILayout.Height(30), GUILayout.Width(200)))
			{
				DuplicateTerrains();
			}
			EditorGUILayout.EndHorizontal();

			// Clean Delete
			--EditorGUI.indentLevel;
			EditorGUILayout.LabelField(Styles.RemoveTerrain, EditorStyles.boldLabel);
			++EditorGUI.indentLevel;
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Select terrain(s) to remove and delete associated terrain data assets: ");
			if (GUILayout.Button(Styles.RemoveTerrainBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				RemoveTerrains();
			}
			EditorGUILayout.EndHorizontal();

			// Split Terrain
			--EditorGUI.indentLevel;
			EditorGUILayout.LabelField(Styles.SplitTerrain, EditorStyles.boldLabel);
			++EditorGUI.indentLevel;
			EditorGUILayout.LabelField("Select terrain(s) to split: ");
			m_Settings.TileXAxis = EditorGUILayout.IntField(Styles.TilesX, m_Settings.TileXAxis);
			m_Settings.TileZAxis = EditorGUILayout.IntField(Styles.TilesZ, m_Settings.TileZAxis);
			m_Settings.AutoUpdateSettings = EditorGUILayout.Toggle(Styles.AutoUpdateSetting, m_Settings.AutoUpdateSettings);
			m_Settings.KeepOldTerrains = EditorGUILayout.Toggle(Styles.KeepOldTerrains, m_Settings.KeepOldTerrains);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Styles.SplitTerrainBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				SplitTerrains();
			}
			EditorGUILayout.EndHorizontal();
			--EditorGUI.indentLevel;
		}

		void ShowTerrainLayerGUI()
		{
			// Layer Palette preset
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(Styles.PalettePreset);
			EditorGUI.BeginChangeCheck();
			m_SelectedLayerPalette = (TerrainPalette)EditorGUILayout.ObjectField(m_SelectedLayerPalette, typeof(TerrainPalette), false);
			if (EditorGUI.EndChangeCheck() && m_SelectedLayerPalette != null)
			{
				if (EditorUtility.DisplayDialog("Confirm", "Load palette from selected?", "OK", "Cancel"))
				{
					LoadPalette();
				}
			}
			if (GUILayout.Button(Styles.SavePalette))
			{
				if (GetPalette())
				{
					m_SelectedLayerPalette.PaletteLayers.Clear();
					foreach (var layer in m_PaletteLayers)
					{
						m_SelectedLayerPalette.PaletteLayers.Add(layer.AssignedLayer);
					}
					AssetDatabase.SaveAssets();
				}
			}
			if (GUILayout.Button(Styles.SaveAsPalette))
			{
				CreateNewPalette();
			}
			if (GUILayout.Button(Styles.RefreshPalette))
			{
				if (GetPalette())
				{
					LoadPalette();
				}
			}
			EditorGUILayout.EndHorizontal();

			// layer reorderable list
			ShowLayerListGUI();

			// Apply button			
			m_Settings.ClearExistLayers = EditorGUILayout.Toggle(Styles.ClearExistingLayers, m_Settings.ClearExistLayers);
			m_Settings.ApplyAllTerrains = EditorGUILayout.Toggle(Styles.ApplyToAllTerrains, m_Settings.ApplyAllTerrains);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Styles.AddLayersBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				AddLayersToSelectedTerrains();
			}
			// Clear button
			if (GUILayout.Button(Styles.RemoveLayersBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				RemoveLayersFromSelectedTerrains();
			}
			EditorGUILayout.EndHorizontal();
		}

		// layer list view
		const int kElementHeight = 70;
		const int kElementObjectFieldHeight = 16;
		const int kElementPadding = 2;
		const int kElementObjectFieldWidth = 240;
		const int kElementToggleWidth = 20;
		const int kElementImageWidth = 64;
		const int kElementImageHeight = 64;

		void ShowLayerListGUI()
		{
			EditorGUILayout.BeginVertical("Box");
			if (GUILayout.Button(Styles.ImportTerrainLayersBtn, GUILayout.Width(200)))
			{
				ImportLayersFromTerrain();
			}
			// List View
			if (m_LayerList == null)
			{
				m_LayerList = new ReorderableList(m_PaletteLayers, typeof(Layer), true, true, true, true);
			}

			m_LayerList.elementHeight = kElementHeight;
			m_LayerList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Material Layer Palette");
			m_LayerList.drawElementCallback = DrawLayerElement;
			m_LayerList.onAddCallback = OnAddLayerElement;
			m_LayerList.onRemoveCallback = OnRemoveLayerElement;
			m_LayerList.onCanAddCallback = OnCanAddLayerElement;
			m_LayerList.DoLayoutList();
			EditorGUILayout.EndVertical();
		}

		void DrawLayerElement(Rect rect, int index, bool selected, bool focused)
		{
			rect.y = rect.y + kElementPadding;
			var rectImage = new Rect((rect.x + kElementPadding), rect.y, kElementImageWidth, kElementImageHeight);
			var rectObject = new Rect((rectImage.x + kElementImageWidth), rect.y, kElementObjectFieldWidth, kElementObjectFieldHeight);

			if (m_PaletteLayers.Count > 0 && m_PaletteLayers[index] != null)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.BeginHorizontal();
				List<TerrainLayer> existLayers = m_PaletteLayers.Select(l => l.AssignedLayer).ToList();
				TerrainLayer oldLayer = m_PaletteLayers[index].AssignedLayer;
				Texture2D icon = null;
				if (m_PaletteLayers[index].AssignedLayer != null)
				{
					icon = AssetPreview.GetAssetPreview(m_PaletteLayers[index].AssignedLayer.diffuseTexture);
				}
				GUI.Box(rectImage, icon);
				m_PaletteLayers[index].AssignedLayer = EditorGUI.ObjectField(rectObject, m_PaletteLayers[index].AssignedLayer, typeof(TerrainLayer), false) as TerrainLayer;
				EditorGUILayout.EndHorizontal();
				if (EditorGUI.EndChangeCheck())
				{
					if (existLayers.Contains(m_PaletteLayers[index].AssignedLayer) && m_PaletteLayers[index].AssignedLayer != oldLayer)
					{
						EditorUtility.DisplayDialog("Error", "Layer exists. Please select a different layer.", "OK");
						m_PaletteLayers[index].AssignedLayer = oldLayer;
					}
				}
			}
		}

		bool OnCanAddLayerElement(ReorderableList list)
		{
			return list.count < m_MaxLayerCount;
		}

		void OnAddLayerElement(ReorderableList list)
		{
			Layer newLayer = ScriptableObject.CreateInstance<Layer>();
			newLayer.IsSelected = true;
			m_PaletteLayers.Add(newLayer);
			m_LayerList.index = m_PaletteLayers.Count - 1;
		}

		void OnRemoveLayerElement(ReorderableList list)
		{
			m_PaletteLayers.RemoveAt(list.index);
			list.index = 0;
		}

		void ShowSplatmapImportGUI()
		{
			EditorGUILayout.BeginVertical("Box");
			if (GUILayout.Button(Styles.ImportFromSplatmapBtn, GUILayout.Width(200)))
			{
				ImportSplatmapsFromTerrain();
			}
			if (m_SplatmapList == null)
			{
				m_SplatmapList = new ReorderableList(m_Splatmaps, typeof(Texture2D), true, false, true, true);
			}
			m_SplatmapList.elementHeight = 70;
			m_SplatmapList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Splatmaps");
			m_SplatmapList.drawElementCallback = DrawSplatmapElement;
			m_SplatmapList.onAddCallback = OnAddSplatmapElement;
			m_SplatmapList.onRemoveCallback = OnRemoveSplatmapElement;
			m_SplatmapList.onCanAddCallback = OnCanAddSplatmapElement;
			m_SplatmapList.onSelectCallback = OnSelectSplatmapElement;

			EditorGUI.BeginChangeCheck();
			m_SplatmapList.DoLayoutList();
			if (EditorGUI.EndChangeCheck())
			{
				// check to see if any of the splatmaps in the list need to be copied
				for (int i = 0; i < m_Splatmaps.Count; i++)
				{
					var splatmap = m_Splatmaps[i];
					if (splatmap != null && !m_SplatmapHasCopy.Contains(splatmap))
					{
						var textureCopy = GetTextureCopy(splatmap);
						m_Splatmaps[i] = textureCopy;
						m_SplatmapHasCopy.Add(textureCopy);
					}
				}
				// clean up the hashset
				m_SplatmapHasCopy.Clear();
				foreach (var splatmap in m_Splatmaps)
				{
					if (splatmap != null)
					{
						m_SplatmapHasCopy.Add(splatmap);
					}
				}
			}

			//Splatmap Preview and Adjustment
			EditorStyles.label.fontStyle = FontStyle.Bold;
			EditorGUI.BeginChangeCheck();
			m_ShowSplatmapPreview = EditorGUILayout.Toggle(Styles.PreviewSplatMapTogg, m_ShowSplatmapPreview);
			if (EditorGUI.EndChangeCheck())
			{
				m_PreviewIsDirty = true;
				if(m_ShowSplatmapPreview)
				{
					GetAndSetActiveRenderPipelineSettings();
				}
			}
			EditorStyles.label.fontStyle = FontStyle.Normal;

			++EditorGUI.indentLevel;
			m_Settings.AdjustAllSplats = EditorGUILayout.Toggle(Styles.MultiAdjustTogg, m_Settings.AdjustAllSplats);

			EditorGUILayout.BeginHorizontal();
			m_Settings.RotationAdjust = (UtilitySettings.RotationAdjustment)EditorGUILayout.EnumPopup(Styles.RotateSplatmapLabel, m_Settings.RotationAdjust);
			if (GUILayout.Button(Styles.RotateSplatmapBtn, GUILayout.Width(150)))
			{
				RotateSplatmap();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			m_Settings.FlipAdjust = (UtilitySettings.FlipAdjustment)EditorGUILayout.EnumPopup(Styles.FlipSplatmapLabel, m_Settings.FlipAdjust);
			if (GUILayout.Button(Styles.FlipSplatmapBtn, GUILayout.Width(150)))
			{
				FlipSplatmap();
			}
			EditorGUILayout.EndHorizontal();

			--EditorGUI.indentLevel;
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();

			if (GUILayout.Button(Styles.ExportToTerrSplatmapBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				ExportSplatmapsToTerrain();
			}
			if (GUILayout.Button(Styles.ResetSplatmapsBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				ResetSplatmaps();
			}

			EditorGUILayout.EndHorizontal();

			if (m_PreviewIsDirty)
			{
				if (!m_ShowSplatmapPreview)
				{
					RevertPreviewMaterial();
				}
				else if (ValidatePreviewTexture())
				{
					m_PreviewMaterial.DisableKeyword("_HEATMAP");
					m_PreviewMaterial.EnableKeyword("_SPLATMAP_PREVIEW");
					UpdateAdjustedSplatmaps();
				}
				m_PreviewIsDirty = false;
			}
		}

		bool OnCanAddSplatmapElement(ReorderableList list)
		{
			return list.count < m_MaxSplatmapCount;
		}

		void OnAddSplatmapElement(ReorderableList list)
		{
			Texture2D newSplatmap = null;
			m_Splatmaps.Add(newSplatmap);
			m_SplatmapList.index = m_Splatmaps.Count - 1;
		}

		void OnRemoveSplatmapElement(ReorderableList list)
		{
			m_Splatmaps.RemoveAt(list.index);
			list.index = 0;
			m_SelectedSplatMap = 0;
			if (list.count == 0)
			{
				RevertPreviewMaterial();
			}
		}

		void OnSelectSplatmapElement(ReorderableList list)
		{
			m_SelectedSplatMap = list.index;
			m_PreviewIsDirty = true;
		}

		const int kSplatmapElementHeight = 64;
		const int kSplatmapLabelWidth = 100;
		const int kSplatmapFieldWidth = 75;
		void DrawSplatmapElement(Rect rect, int index, bool selected, bool focused)
		{
			rect.height = rect.height + kElementPadding;
			var rectLabel = new Rect(rect.x, rect.y, kSplatmapLabelWidth, kSplatmapElementHeight);
			var rectObject = new Rect((rectLabel.x + kSplatmapLabelWidth), rect.y + kElementPadding, kSplatmapFieldWidth, kSplatmapElementHeight);
			if (m_Splatmaps.Count > 0)
			{
				// label is the built-in splatmap name that gets auto assigned
				string label = "SplatAlpha " + index;
				EditorGUI.LabelField(rectLabel, label);
				m_Splatmaps[index] = EditorGUI.ObjectField(rectObject, m_Splatmaps[index], typeof(Texture2D), false) as Texture2D;
			}
		}

		void ShowReplaceSplatmapGUI()
		{
			// Replace Splatmap
			EditorGUI.BeginChangeCheck();
			m_Settings.SplatmapTerrain = EditorGUILayout.ObjectField(Styles.TerrainToReplaceSplatmap, m_Settings.SplatmapTerrain, typeof(Terrain), true) as Terrain;
			if (EditorGUI.EndChangeCheck())
			{
				if (m_Settings.SplatmapTerrain != null)
				{
					TerrainData terrainData = m_Settings.SplatmapTerrain.terrainData;
					if (terrainData.alphamapTextureCount == 1)
					{
						m_Settings.SplatmapOld0 = terrainData.alphamapTextures[0];
						m_Settings.SplatmapOld1 = null;
					}
					if (terrainData.alphamapTextureCount == 2)
					{
						m_Settings.SplatmapOld0 = terrainData.alphamapTextures[0];
						m_Settings.SplatmapOld1 = terrainData.alphamapTextures[1];
					}
					m_SplatmapResolution = terrainData.alphamapResolution;
				}
				else
				{
					m_Settings.SplatmapOld0 = null;
					m_Settings.SplatmapOld1 = null;
					m_SplatmapResolution = 0;
				}
			}
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(Styles.SplatmapResolution.text + m_SplatmapResolution.ToString());
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			m_Settings.SplatmapOld0 = EditorGUILayout.ObjectField(Styles.SplatAlpha0, m_Settings.SplatmapOld0, typeof(Texture2D), false) as Texture2D;
			m_Settings.SplatmapNew0 = EditorGUILayout.ObjectField(Styles.SplatAlpha0New, m_Settings.SplatmapNew0, typeof(Texture2D), false) as Texture2D;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			m_Settings.SplatmapOld1 = EditorGUILayout.ObjectField(Styles.SplatAlpha1, m_Settings.SplatmapOld1, typeof(Texture2D), false) as Texture2D;
			m_Settings.SplatmapNew1 = EditorGUILayout.ObjectField(Styles.SplatAlpha1New, m_Settings.SplatmapNew1, typeof(Texture2D), false) as Texture2D;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Styles.ReplaceSplatmapsBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				ReplaceSplatmaps();
			}
			if (GUILayout.Button(Styles.ResetSplatmapsBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				ResetSplatmaps();
			}
			EditorGUILayout.EndHorizontal();
		}

		void ShowExportSplatmapGUI()
		{
			// Export Splatmaps
			EditorGUILayout.BeginHorizontal();
			m_Settings.SplatFolderPath = EditorGUILayout.TextField(Styles.ExportSplatmapFolderPath, m_Settings.SplatFolderPath);
			if (GUILayout.Button("...", GUILayout.Width(25)))
			{
				m_Settings.SplatFolderPath = EditorUtility.OpenFolderPanel("Select a folder...", m_Settings.SplatFolderPath, "");
			}
			EditorGUILayout.EndHorizontal();
			m_Settings.SelectedFormat = (UtilitySettings.ImageFormat)EditorGUILayout.EnumPopup(Styles.ExportSplatmapFormat, m_Settings.SelectedFormat);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Styles.ExportSplatmapsBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				var selectedTerrains = Selection.GetFiltered(typeof(Terrain), SelectionMode.Unfiltered);
				ExportSplatmaps(selectedTerrains);
			}
			EditorGUILayout.EndHorizontal();
		}

		void ShowExportHeightmapGUI()
		{
			EditorGUILayout.BeginHorizontal();
			m_Settings.HeightmapFolderPath = EditorGUILayout.TextField(Styles.ExportHeightmapFolderPath, m_Settings.HeightmapFolderPath);
			if (GUILayout.Button("...", GUILayout.Width(25)))
			{
				m_Settings.HeightmapFolderPath = EditorUtility.OpenFolderPanel("Select a folder...", m_Settings.HeightmapFolderPath, "");
			}
			EditorGUILayout.EndHorizontal();
			//EditorGUILayout.LabelField("Heightmap Format: .raw");
			//EditorGUILayout.BeginHorizontal();
			//m_SelectedDepth = EditorGUILayout.Popup(Styles.HeightmapBitDepth, m_SelectedDepth, m_DepthOptions.Keys.ToArray());
			//EditorGUILayout.EndHorizontal();
			//EditorGUILayout.BeginHorizontal();
			//m_Settings.HeightmapByteOrder = (ToolboxHelper.ByteOrder)EditorGUILayout.EnumPopup(Styles.HeightmapByteOrder, m_Settings.HeightmapByteOrder);
			//EditorGUILayout.EndHorizontal();

			//Future to support PNG and TGA. 
			m_Settings.HeightFormat = (Heightmap.Format)EditorGUILayout.EnumPopup(Styles.HeightmapSelectedFormat, m_Settings.HeightFormat);
			if (m_Settings.HeightFormat == Heightmap.Format.RAW)
			{
				EditorGUILayout.BeginHorizontal();
				m_Settings.HeightmapDepth = (Heightmap.Depth)EditorGUILayout.EnumPopup(Styles.HeightmapBitDepth, m_Settings.HeightmapDepth);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				m_Settings.HeightmapByteOrder = (ToolboxHelper.ByteOrder)EditorGUILayout.EnumPopup(Styles.HeightmapByteOrder, m_Settings.HeightmapByteOrder);
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.MinMaxSlider(Styles.HeightmapRemap, ref m_Settings.ExportHeightRemapMin, ref m_Settings.ExportHeightRemapMax, 0f, 1.0f);
			EditorGUILayout.LabelField(Styles.HeightmapRemapMin, GUILayout.Width(40.0f));
			m_Settings.ExportHeightRemapMin = EditorGUILayout.FloatField(m_Settings.ExportHeightRemapMin, GUILayout.Width(75.0f));
			EditorGUILayout.LabelField(Styles.HeightmapRemapMax, GUILayout.Width(40.0f));
			m_Settings.ExportHeightRemapMax = EditorGUILayout.FloatField(m_Settings.ExportHeightRemapMax, GUILayout.Width(75.0f));
			EditorGUILayout.EndHorizontal();
			m_Settings.FlipVertically = EditorGUILayout.Toggle(Styles.FlipVertically, m_Settings.FlipVertically);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Styles.ExportHeightmapsBtn, GUILayout.Height(30), GUILayout.Width(200)))
			{
				var selectedTerrains = Selection.GetFiltered(typeof(Terrain), SelectionMode.Unfiltered);
				ExportHeightmaps(selectedTerrains);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();
		}



		void UpdateAdjustedSplatmaps()
		{
			List<Terrain> terrains = m_Terrains.ToList();
			if (terrains.Count == 0)
			{
				EditorUtility.DisplayDialog("Error", "Select a terrain before previewing the splatmap.", "OK");
				RevertPreviewMaterial();
				return;
			}

			List<Terrain> sortedTerrains = terrains.OrderBy(t => t.gameObject.transform.position.x).ThenBy(t => t.gameObject.transform.position.z).ToList();

			Texture2D splatMap = m_Splatmaps[m_SelectedSplatMap];
			Vector2Int tileOffset = Vector2Int.zero;
			int tilesX = terrains.Select(t => t.gameObject.transform.position.x).Distinct().Count();
			int tilesZ = terrains.Select(t => t.gameObject.transform.position.z).Distinct().Count();
			int expectedCount = tilesX * tilesZ;
			int tilesCount = terrains.Count;
			int index = 0;

			Vector2Int resolution = new Vector2Int(splatMap.width / tilesX, splatMap.height / tilesZ);
			Texture2D texture = new Texture2D(resolution.x, resolution.y);

			if (!ValidateSplatmap(terrains, resolution, expectedCount, tilesCount))
			{
				RevertPreviewMaterial();
				return;
			}

			for (int x = 0; x < tilesX; x++, tileOffset.x += resolution.x)
			{
				tileOffset.y = 0;
				for (int y = 0; y < tilesZ; y++, tileOffset.y += resolution.y)
				{
					texture = new Texture2D(resolution.x, resolution.y);
					var newPixels = splatMap.GetPixels(tileOffset.x, tileOffset.y, resolution.x, resolution.y);
#if UNITY_2019_2_OR_NEWER
#else
					sortedTerrains[index].materialType = Terrain.MaterialType.Custom;
#endif
					sortedTerrains[index].materialTemplate = m_PreviewMaterial;
					texture.SetPixels(newPixels);
					texture.Apply();

					m_PreviewMaterialPropBlock.Clear();
					m_PreviewMaterialPropBlock.SetTexture("_SplatmapTex", texture);
					sortedTerrains[index].SetSplatMaterialPropertyBlock(m_PreviewMaterialPropBlock);
					index++;
				}
			}

		}

		void DrawLayerIcon(Texture icon, int index)
		{
			if (icon == null)
				return;

			int width = icon.width;
			Rect position = new Rect(0, width * index, width, width);
			int size = Mathf.Min((int)position.width, (int)position.height);
			if (size >= icon.width * 2)
				size = icon.width * 2;

			FilterMode filterMode = icon.filterMode;
			icon.filterMode = FilterMode.Point;
			EditorGUILayout.BeginVertical("Box", GUILayout.Width(140));
			GUILayout.Label(icon);

			if (m_PaletteLayers[index] != null && m_PaletteLayers[index].AssignedLayer != null)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(m_PaletteLayers[index].AssignedLayer.name, GUILayout.Width(90));
				GUILayout.EndHorizontal();
			}

			EditorGUILayout.EndVertical();
			icon.filterMode = filterMode;
		}

		void AddLayersToSelectedTerrains()
		{
			Terrain[] terrains;
			if (m_Settings.ApplyAllTerrains)
			{
				terrains = ToolboxHelper.GetAllTerrainsInScene();
			}
			else
			{
				terrains = ToolboxHelper.GetSelectedTerrainsInScene();
			}

			if (terrains == null || terrains.Length == 0)
			{
				EditorUtility.DisplayDialog("Warning", "No selected terrain found. Please select to continue.", "OK");
				return;
			}

			int index = 0;
			if (terrains.Length > 0 && m_PaletteLayers.Count > 0)
			{
				foreach (var terrain in terrains)
				{
					if (!terrain || !terrain.terrainData)
					{
						continue;
					}

					EditorUtility.DisplayProgressBar("Applying terrain layers", string.Format("Updating terrain tile ({0})", terrain.name), ((float)index / (terrains.Count())));
					TerrainToolboxLayer.AddLayersToTerrain(terrain.terrainData, m_PaletteLayers.Select(l => l.AssignedLayer).ToList(), m_Settings.ClearExistLayers);

					index++;
				}

				AssetDatabase.SaveAssets();
				EditorUtility.ClearProgressBar();
			}
		}

		void RemoveLayersFromSelectedTerrains()
		{
			Terrain[] terrains;
			if (m_Settings.ApplyAllTerrains)
			{
				terrains = ToolboxHelper.GetAllTerrainsInScene();
			}
			else
			{
				terrains = ToolboxHelper.GetSelectedTerrainsInScene();
			}

			if (terrains == null || terrains.Length == 0)
			{
				EditorUtility.DisplayDialog("Warning", "No selected terrain found. Please select to continue.", "OK");
				return;
			}

			if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to remove all existing layers from terrain(s)?", "Continue", "Cancel"))
			{
				int index = 0;
				if (terrains.Length > 0)
				{
					foreach (var terrain in terrains)
					{
						EditorUtility.DisplayProgressBar("Removing terrain layers", string.Format("Updating terrain tile ({0})", terrain.name), ((float)index / (terrains.Count())));
						if (!terrain || !terrain.terrainData)
						{
							continue;
						}

						var layers = terrain.terrainData.terrainLayers;
						if (layers == null || layers.Length == 0)
						{
							continue;
						}

						TerrainToolboxLayer.RemoveAllLayers(terrain.terrainData);
						index++;
					}

					AssetDatabase.SaveAssets();
					EditorUtility.ClearProgressBar();
				}
			}
		}


		void ImportLayersFromTerrain()
		{
			m_Terrains = ToolboxHelper.GetSelectedTerrainsInScene();
			if (m_Terrains.Length != 1)
			{
				EditorUtility.DisplayDialog("Warning", "Layers can only be imported from 1 terrain.", "OK");
			}
			else
			{
				Terrain terrain = m_Terrains[0];
				m_PaletteLayers.Clear();
				m_CopiedLayers.Clear();
				foreach (TerrainLayer layer in terrain.terrainData.terrainLayers)
				{
					Layer paletteLayer = ScriptableObject.CreateInstance<Layer>();
					paletteLayer.AssignedLayer = layer;
					m_PaletteLayers.Add(paletteLayer);
				}
				m_CopiedLayers.AddRange(terrain.terrainData.terrainLayers);
			}
		}

		internal void ImportSplatmapsFromTerrain(bool autoAcceptWarning = false)
		{
			m_Terrains = ToolboxHelper.GetSelectedTerrainsInScene();
			if (m_Terrains.Length != 1)
			{
				if(!autoAcceptWarning)
					EditorUtility.DisplayDialog("Warning", "Splatmaps can only be imported from 1 terrain.", "OK");
			}
			else
			{
				Terrain terrain = m_Terrains[0];
				m_Splatmaps.Clear();
				foreach (Texture2D alphamap in terrain.terrainData.alphamapTextures)
				{
					var textureCopy = GetTextureCopy(alphamap);
					m_SplatmapHasCopy.Add(textureCopy);
					m_Splatmaps.Add(textureCopy);
				}

                UpdateCachedTerrainMaterials();
            }
        }

		Texture2D GetTextureCopy(Texture2D texture)
		{
			var creationFlags = texture.mipmapCount > 0
				? TextureCreationFlags.MipChain
				: TextureCreationFlags.None;
			var textureCopy = new Texture2D(texture.width, texture.height, texture.graphicsFormat,
				creationFlags);
			Graphics.CopyTexture(texture, textureCopy);
			return textureCopy;
		}

		void DuplicateTerrains()
		{
			m_Terrains = ToolboxHelper.GetSelectedTerrainsInScene();

			if (m_Terrains == null || m_Terrains.Length == 0)
			{
				EditorUtility.DisplayDialog("Error", "No terrain(s) selected. Please select and try again.", "OK");
				return;
			}

			foreach (var terrain in m_Terrains)
			{
				// copy terrain data asset to be the new terrain data asset
				var dataPath = AssetDatabase.GetAssetPath(terrain.terrainData);
				var dataPathNew = AssetDatabase.GenerateUniqueAssetPath(dataPath);
				AssetDatabase.CopyAsset(dataPath, dataPathNew);
				TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPathNew);
				// clone terrain from old terrain
				GameObject newGO = UnityEngine.Object.Instantiate(terrain.gameObject);
				newGO.transform.localPosition = terrain.gameObject.transform.position;
				newGO.GetComponent<Terrain>().terrainData = terrainData;
				// parent to parent if any
				if (terrain.gameObject.transform.parent != null)
				{
					newGO.transform.SetParent(terrain.gameObject.transform.parent);
				}
				// update terrain data reference in terrain collider 
				TerrainCollider collider = newGO.GetComponent<TerrainCollider>();
				collider.terrainData = terrainData;

				Undo.RegisterCreatedObjectUndo(newGO, "Duplicate terrain");
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		void RemoveTerrains()
		{
			m_Terrains = ToolboxHelper.GetSelectedTerrainsInScene();

			if (m_Terrains == null || m_Terrains.Length == 0)
			{
				EditorUtility.DisplayDialog("Error", "No terrain(s) selected. Please select and try again.", "OK");
				return;
			}

			if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to delete selected terrain(s) And their data assets? This process is not undoable.", "Continue", "Cancel"))
			{
				foreach (var terrain in m_Terrains)
				{
					if (terrain.terrainData)
					{
						var path = AssetDatabase.GetAssetPath(terrain.terrainData);
						AssetDatabase.DeleteAsset(path);
					}

					UnityEngine.Object.DestroyImmediate(terrain.gameObject);
				}

				AssetDatabase.Refresh();
			}
		}

		bool MultipleIDExist(List<Terrain> terrains)
		{
			int[] ids = terrains.Select(t => t.groupingID).ToArray();
			if (ids.Distinct().ToArray().Length > 1)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		internal void SplitTerrains(bool isTest=false)
		{
			var terrainsFrom = ToolboxHelper.GetSelectedTerrainsInScene();

			if (terrainsFrom == null || terrainsFrom.Length == 0)
			{
				EditorUtility.DisplayDialog("Error", "No terrain(s) selected. Please select and try again.", "OK");
				return;
			}

			if (!m_Settings.KeepOldTerrains)
			{
				if (!EditorUtility.DisplayDialog("Warning", "About to split selected terrain(s), and this process is not undoable! You can enable Keep Original Terrain option to keep a copy of selected terrain(s). Are you sure to continue without a copy?", "Continue","Cancel"))
				{
					return;
				}
			}

			// check if multiple grouping ids selected
			if (MultipleIDExist(terrainsFrom.ToList()))
			{
				EditorUtility.DisplayDialog("Error", "The terrains selected have inconsistent Grouping IDs.", "OK");
				return;
			}

			int new_id = GetGroupIDForSplittedNewTerrain(terrainsFrom);

			try
			{
				foreach (var terrain in terrainsFrom)
				{
					SplitTerrain(terrain, new_id, isTest);
				}
			}
			finally
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.ClearProgressBar();

				if (!m_Settings.KeepOldTerrains)
				{
					foreach (var t in terrainsFrom)
					{
						GameObject.DestroyImmediate(t.gameObject);
					}
				}
			}
		}

		internal void SplitTerrain(Terrain terrain, int new_id, bool isTest=false)
		{
			TerrainData terrainData = terrain.terrainData;
			Vector3 startPosition = terrain.transform.position;
			float tileWidth = terrainData.size.x / m_Settings.TileXAxis;
			float tileLength = terrainData.size.z / m_Settings.TileZAxis;
			float tileHeight = terrainData.size.y;
			Vector2Int tileResolution = new Vector2Int((int)(terrainData.size.x / m_Settings.TileXAxis), (int)(terrainData.size.z / m_Settings.TileZAxis));
			Vector2Int heightOffset = Vector2Int.zero;
			Vector2Int detailOffset = Vector2Int.zero;
			Vector2Int controlOffset = Vector2Int.zero;
			Vector3 tilePosition = terrain.transform.position;

			// get terrain group
			GameObject groupGO = null;
			if (terrain.transform.parent != null && terrain.transform.parent.gameObject != null)
			{
				var parent = terrain.transform.parent.gameObject;
				var groupComp = parent.GetComponent<TerrainGroup>();
				if (parent != null && groupComp != null)
				{
					groupGO = parent;
				}
			}

			int originalHeightmapRes = terrainData.heightmapResolution;
			int newHeightmapRes = (originalHeightmapRes - 1) / m_Settings.TileXAxis;
			int newDetailmapRes = terrainData.detailResolution / m_Settings.TileXAxis;

			if (!ToolboxHelper.IsPowerOfTwo(newHeightmapRes))
			{
				EditorUtility.DisplayDialog("Error", "Heightmap resolution of new tiles is not power of 2 with current settings.", "OK");
				return;
			}

			if (newHeightmapRes < kMinHeightmapRes)
			{
				if (!isTest && !EditorUtility.DisplayDialog("Warning",
					$"The heightmap resolution of the newly split tiles is {newHeightmapRes + 1}; "+
					$"this is smaller than the minimum supported value of {kMinHeightmapRes + 1}.\n\n" +
					$"Would you like to split terrain into {m_Settings.TileXAxis}x{m_Settings.TileZAxis} " +
					$"tiles of heightmap resolution {kMinHeightmapRes + 1}?",
					"OK",
					"Cancel"))
				{
					return;
				}

				ToolboxHelper.ResizeHeightmap(terrainData, kMinHeightmapRes * Math.Max(m_Settings.TileXAxis, m_Settings.TileZAxis));
				newHeightmapRes = kMinHeightmapRes;
			}

			// control map resolution
			int newControlRes = terrainData.alphamapResolution / m_Settings.TileXAxis;
			if (!ToolboxHelper.IsPowerOfTwo(newControlRes))
			{
				EditorUtility.DisplayDialog("Error", "Splat control map resolution of new tiles is not power of 2 with current settings.", "OK");
				return;
			}

			int tileIndex = 0;
			int tileCount = m_Settings.TileXAxis * m_Settings.TileZAxis;
			Terrain[] terrainsNew = new Terrain[tileCount];
#if UNITY_2019_3_OR_NEWER

			// holes render texture
			RenderTexture rt = RenderTexture.GetTemporary(terrainData.holesTexture.width, terrainData.holesTexture.height);
			Graphics.Blit(terrainData.holesTexture, rt);
			rt.filterMode = FilterMode.Point;
#endif

			for (int x = 0; x < m_Settings.TileXAxis; x++, heightOffset.x += newHeightmapRes, detailOffset.x += newDetailmapRes, controlOffset.x += newControlRes, tilePosition.x += tileWidth)
			{
				heightOffset.y = 0;
				detailOffset.y = 0;
				controlOffset.y = 0;
				tilePosition.z = startPosition.z;

				for (int y = 0; y < m_Settings.TileZAxis; y++, heightOffset.y += newHeightmapRes, detailOffset.y += newDetailmapRes, controlOffset.y += newControlRes, tilePosition.z += tileLength)
				{
					EditorUtility.DisplayProgressBar("Creating terrains", string.Format("Updating terrain tile ({0}, {1})", x, y), ((float)tileIndex / tileCount));

					TerrainData terrainDataNew = new TerrainData();
					GameObject newGO = Terrain.CreateTerrainGameObject(terrainDataNew);
					Terrain newTerrain = newGO.GetComponent<Terrain>();

					Guid newGuid = Guid.NewGuid();
					string terrainName = $"Terrain_{x}_{y}_{newGuid}";
					newGO.name = terrainName;
					newTerrain.transform.position = tilePosition;
					newTerrain.groupingID = new_id;
					newTerrain.allowAutoConnect = true;
					newTerrain.drawInstanced = terrain.drawInstanced;
					if (groupGO != null)
					{
						newTerrain.transform.SetParent(groupGO.transform);
					}

					// get and set heights
					terrainDataNew.heightmapResolution = newHeightmapRes + 1;
					var heightData = terrainData.GetHeights(heightOffset.x, heightOffset.y, (newHeightmapRes + 1), (newHeightmapRes + 1));
					terrainDataNew.SetHeights(0, 0, heightData);
					terrainDataNew.size = new Vector3(tileWidth, tileHeight, tileLength);

					string assetPath = $"{m_Settings.TerrainAssetDir}/{terrainName}.asset";
					if (!Directory.Exists(m_Settings.TerrainAssetDir))
					{
						Directory.CreateDirectory(m_Settings.TerrainAssetDir);
					}

					AssetDatabase.CreateAsset(terrainDataNew, assetPath);

					// note that add layers and alphamap operations need to happen after terrain data asset being created, so cached splat 0 and 1 data gets cleared to avoid bumping to splat 2 map.
					// get and set terrain layers
					TerrainToolboxLayer.AddLayersToTerrain(terrainDataNew, terrainData.terrainLayers.ToList(), true);

					// get and set alphamaps
					float[,,] alphamap = terrainData.GetAlphamaps(controlOffset.x, controlOffset.y, newControlRes, newControlRes);
					terrainDataNew.alphamapResolution = newControlRes;
					terrainDataNew.SetAlphamaps(0, 0, alphamap);

					// get and set detailmap
					int newDetailPatch = terrainData.detailResolutionPerPatch / m_Settings.TileXAxis;
					terrainDataNew.SetDetailResolution(newDetailmapRes, newDetailPatch);
					terrainDataNew.detailPrototypes = terrainData.detailPrototypes;

					for (int i = 0; i < terrainDataNew.detailPrototypes.Length; i++)
					{
						int[,] detailLayer = terrainData.GetDetailLayer(detailOffset.x, detailOffset.y, newDetailmapRes, newDetailmapRes, i);
						terrainDataNew.SetDetailLayer(0, 0, i, detailLayer);
					}

          // get and set treemap
          float treeOffsetXMin = x / (float)m_Settings.TileXAxis;
          float treeOffsetZMin = y / (float)m_Settings.TileZAxis;
          float treeOffsetXMAX = treeOffsetXMin + (1 / (float)m_Settings.TileXAxis);
          float treeOffsetZMAX = treeOffsetZMin + (1 / (float)m_Settings.TileZAxis);
          terrainDataNew.treePrototypes = terrainData.treePrototypes;
          List<TreeInstance> treeInstances = new List<TreeInstance>();
          for (int i = 0; i < terrainData.treeInstances.Length; i++)
          {
            TreeInstance tree = terrainData.treeInstances[i];
            if(treeOffsetXMin <= tree.position.x && tree.position.x <= treeOffsetXMAX &&
              treeOffsetZMin <= tree.position.z && tree.position.z <= treeOffsetZMAX)
            {
              tree.position.x = (tree.position.x - treeOffsetXMin) * m_Settings.TileXAxis;
              tree.position.z = (tree.position.z - treeOffsetZMin) * m_Settings.TileZAxis;
              treeInstances.Add(tree);
            }
          }
          terrainDataNew.SetTreeInstances(treeInstances.ToArray(), true);

#if UNITY_2019_3_OR_NEWER
          // get and set holes, however there's currently a bug in GetHoles() so using render texture blit instead
          //var holes = terrainData.GetHoles(heightOffset.x, heightOffset.y, newHeightmapRes, newHeightmapRes);
          //terrainDataNew.SetHoles(0, 0, holes);							
          float divX = 1f / m_Settings.TileXAxis;
          float divZ = 1f / m_Settings.TileZAxis;
          Vector2 scale = new Vector2(divX, divZ);
          Vector2 offset = new Vector2(divX * x, divZ * y);
          Graphics.Blit(rt, (RenderTexture)terrainDataNew.holesTexture, scale, offset);							
          terrainDataNew.DirtyTextureRegion(TerrainData.HolesTextureName, new RectInt(0, 0, terrainDataNew.holesTexture.width, terrainDataNew.holesTexture.height), false);
#endif
          // update other terrain settings
          if (m_Settings.AutoUpdateSettings)
          {
            ApplySettingsFromSourceToTargetTerrain(terrain, newTerrain);
          }

          terrainsNew[tileIndex] = newTerrain;
          tileIndex++;

          Undo.RegisterCreatedObjectUndo(newGO, "Split terrain");
				}
			}

			m_SplitTerrains = terrainsNew;
			ToolboxHelper.CalculateAdjacencies(m_SplitTerrains, m_Settings.TileXAxis, m_Settings.TileZAxis);
#if UNITY_2019_3_OR_NEWER
			RenderTexture.ReleaseTemporary(rt);
#endif
			if (terrainData.heightmapResolution != originalHeightmapRes)
			{
				ToolboxHelper.ResizeHeightmap(terrainData, originalHeightmapRes);
			}
		}

		int GetGroupIDForSplittedNewTerrain(Terrain[] exclude_terrains)
		{
			// check all other terrains in scene to see if group ID exists
			Terrain[] all_terrains = ToolboxHelper.GetAllTerrainsInScene();
			Terrain[] remaining_terrains = all_terrains.Except(exclude_terrains).ToArray();
			List<int> ids = new List<int>();
			int original_id = exclude_terrains[0].groupingID;
			ids.Add(original_id);
			bool exist = false;
			foreach (var terrain in remaining_terrains)
			{
				if (terrain.groupingID == original_id)
				{
					exist = true;
				}

				ids.Add(terrain.groupingID);
			}
			List<int> unique_ids = ids.Distinct().ToList();
			int max_id = unique_ids.Max();

			// if found id exist in scene, give a new id with largest id + 1, otherwise use original terrain's id
			if (exist)
			{
				return max_id + 1;
			}
			else
			{
				return original_id;
			}
		}

		void ApplySettingsFromSourceToTargetTerrain(Terrain sourceTerrain, Terrain targetTerrain)
		{
			targetTerrain.allowAutoConnect = sourceTerrain.allowAutoConnect;
			targetTerrain.drawHeightmap = sourceTerrain.drawHeightmap;
			targetTerrain.drawInstanced = sourceTerrain.drawInstanced;
			targetTerrain.heightmapPixelError = sourceTerrain.heightmapPixelError;
			targetTerrain.basemapDistance = sourceTerrain.basemapDistance;
			targetTerrain.shadowCastingMode = sourceTerrain.shadowCastingMode;
			targetTerrain.materialTemplate = sourceTerrain.materialTemplate;
			targetTerrain.reflectionProbeUsage = sourceTerrain.reflectionProbeUsage;
#if UNITY_2019_2_OR_NEWER
#else
			targetTerrain.materialType = sourceTerrain.materialType;			
			targetTerrain.legacySpecular = sourceTerrain.legacySpecular;
			targetTerrain.legacyShininess = sourceTerrain.legacyShininess;
#endif
			targetTerrain.terrainData.baseMapResolution = sourceTerrain.terrainData.baseMapResolution;

			targetTerrain.drawTreesAndFoliage = sourceTerrain.drawTreesAndFoliage;
			targetTerrain.bakeLightProbesForTrees = sourceTerrain.bakeLightProbesForTrees;
			targetTerrain.deringLightProbesForTrees = sourceTerrain.deringLightProbesForTrees;
			targetTerrain.preserveTreePrototypeLayers = sourceTerrain.preserveTreePrototypeLayers;
			targetTerrain.detailObjectDistance = sourceTerrain.detailObjectDistance;
			targetTerrain.collectDetailPatches = sourceTerrain.collectDetailPatches;
			targetTerrain.detailObjectDensity = sourceTerrain.detailObjectDistance;
			targetTerrain.treeDistance = sourceTerrain.treeDistance;
			targetTerrain.treeBillboardDistance = sourceTerrain.treeBillboardDistance;
			targetTerrain.treeCrossFadeLength = sourceTerrain.treeCrossFadeLength;
			targetTerrain.treeMaximumFullLODCount = sourceTerrain.treeMaximumFullLODCount;

			targetTerrain.terrainData.wavingGrassStrength = sourceTerrain.terrainData.wavingGrassStrength;
			targetTerrain.terrainData.wavingGrassSpeed = sourceTerrain.terrainData.wavingGrassSpeed;
			targetTerrain.terrainData.wavingGrassAmount = sourceTerrain.terrainData.wavingGrassAmount;
			targetTerrain.terrainData.wavingGrassTint = sourceTerrain.terrainData.wavingGrassTint;
		}

		void ReplaceSplatmaps()
		{
			if (m_Settings.SplatmapNew0 == null && m_Settings.SplatmapNew1 == null)
			{
				if (EditorUtility.DisplayDialog("Confirm", "You don't have new splatmaps assigned. Would you like to reset splatmaps to defaults on selected terrain?", "OK", "Cancel"))
				{
					// reset splatmaps
					ResetSplatmapsOnTerrain(m_Settings.SplatmapTerrain);
					return;
				}
				return;
			}

			if (m_Settings.SplatmapOld0 != null && m_Settings.SplatmapNew0 != null)
			{
				ReplaceSplatmapTexture(m_Settings.SplatmapOld0, m_Settings.SplatmapNew0);
			}

			if (m_Settings.SplatmapOld1 != null && m_Settings.SplatmapNew1 != null)
			{
				ReplaceSplatmapTexture(m_Settings.SplatmapOld1, m_Settings.SplatmapNew1);
			}

			AssetDatabase.SaveAssets();
		}

		void ReplaceSplatmapTexture(Texture2D oldTexture, Texture2D newTexture)
		{
			if (newTexture.width != newTexture.height)
			{
				EditorUtility.DisplayDialog("Error", "Could not replace splatmap. Non-square sized splatmap found.", "OK");
				return;
			}

			var undoObjects = new List<UnityEngine.Object>();
			undoObjects.Add(m_Settings.SplatmapTerrain.terrainData);
			undoObjects.AddRange(m_Settings.SplatmapTerrain.terrainData.alphamapTextures);
			Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), "Replace splatmaps");

			// set new texture to be readable through Import Settings, so we can use GetPixels() later
			if (!newTexture.isReadable)
			{
				var newPath = AssetDatabase.GetAssetPath(newTexture);
				var newImporter = AssetImporter.GetAtPath(newPath) as TextureImporter;
				if (newImporter != null)
				{
					newImporter.isReadable = true;
					AssetDatabase.ImportAsset(newPath);
					AssetDatabase.Refresh();
				}
			}

			if (newTexture.width != oldTexture.width)
			{
				if (EditorUtility.DisplayDialog("Confirm", "Mismatched splatmap resolution found.", "Use New Resolution", "Use Old Resolution"))
				{
					// resize to new texture size
					oldTexture.Resize(newTexture.width, newTexture.height, oldTexture.format, true);
					// update splatmap resolution on terrain settings as well
					m_Settings.SplatmapTerrain.terrainData.alphamapResolution = newTexture.width;
					m_SplatmapResolution = newTexture.width;
				}
				else
				{
					// resize to old texture size
					newTexture.Resize(oldTexture.width, oldTexture.height, newTexture.format, true);
				}
			}

			var pixelsNew = newTexture.GetPixels();
			oldTexture.SetPixels(pixelsNew);
			oldTexture.Apply();
		}

		internal void ExportSplatmapsToTerrain(bool autoAcceptWarning = false)
		{
			// validate settings
			// all splatmaps same resolution
			// terrains same control map resolution

			// get selected tiles and sort by position along X and Z
			List<Terrain> terrains = ToolboxHelper.GetSelectedTerrainsInScene().ToList();
			List<Terrain> sortedTerrains = terrains.OrderBy(t => t.gameObject.transform.position.x).ThenBy(t => t.gameObject.transform.position.z).ToList();

			var undoObjects = new List<UnityEngine.Object>();
			foreach (var terrain in terrains)
			{
				undoObjects.Add(terrain.terrainData);
				undoObjects.AddRange(terrain.terrainData.alphamapTextures);
			}
			Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), "Reset terrains");

			int tilesX = terrains.Select(t => t.gameObject.transform.position.x).Distinct().Count();
			int tilesZ = terrains.Select(t => t.gameObject.transform.position.z).Distinct().Count();
			int expectedCount = tilesX * tilesZ;
			if (expectedCount == 0)
			{
				if (!autoAcceptWarning)
				{
					EditorUtility.DisplayDialog("Error", "No terrain(s) selected. Please select terrain tile(s) to continue.", "OK");
				}
				return;
            }

            int tilesCount = terrains.Count;
			Vector2Int tileOffset = Vector2Int.zero;
			int index = 0;

			try
			{
				for (int z = 0; z < m_Splatmaps.Count; z++)
				{
					index = 0;
					tileOffset = Vector2Int.zero;

					if (m_Splatmaps[z] != null)
					{
						Vector2Int resolution = new Vector2Int(m_Splatmaps[z].width / tilesX, m_Splatmaps[z].height / tilesZ);
						if (ValidateSplatmap(terrains, resolution, expectedCount, tilesCount))
						{
							RenderTexture oldRT = RenderTexture.active;
							RenderTexture[] rts = new RenderTexture[m_Splatmaps.Count];
							rts[z] = RenderTexture.GetTemporary(resolution.x, resolution.x, 0, SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.HDR));
							Graphics.Blit(m_Splatmaps[z], rts[z]);
							RenderTexture.active = rts[z];

							for (int x = 0; x < tilesX; x++, tileOffset.x += resolution.x)
							{
								tileOffset.y = 0;
								for (int y = 0; y < tilesZ; y++, tileOffset.y += resolution.y)
								{
									EditorUtility.DisplayProgressBar("Applying splatmaps", string.Format("Updating terrain tile {0}", sortedTerrains[index].name), ((float)index / tilesCount));
									
									ToolboxHelper.ResizeControlTexture(sortedTerrains[index].terrainData, resolution.x);
									if (sortedTerrains[index].terrainData.alphamapTextures[z] != null)
									{									
										ToolboxHelper.CopyActiveRenderTextureToTexture(sortedTerrains[index].terrainData.alphamapTextures[z], new RectInt(tileOffset.x, tileOffset.y, resolution.x, resolution.x), Vector2Int.zero, false);
									}

									index++;
								}
							}

							RenderTexture.active = oldRT;
							for (int i = 0; i < m_Splatmaps.Count; i++)
							{
								RenderTexture.ReleaseTemporary(rts[i]);
							}
						}
					}
				}
			}
			finally
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.ClearProgressBar();
			}
		}

		bool ValidateSplatmap(List<Terrain> terrains, Vector2Int resolution, int expectedCount, int tilesCount)
		{
			foreach (Terrain terrain in terrains)
			{
				if (terrain.terrainData.alphamapTextures.Length < m_SplatmapList.count)
				{
					EditorUtility.DisplayDialog("Error", "You've selected more splatmaps to import than each terrain can hold. Either select less splatmaps or add more to your terrain.", "OK"); //string.Format("The selected amount of {0} splatmap textures, dosen't match that of the average splatmaps of {1:0.##} per terrain ", m_SplatmapList.count, averageSplatmaps)
					return false;
				}
			}

			if (!ToolboxHelper.IsPowerOfTwo(resolution.x))
			{
				EditorUtility.DisplayDialog("Error", "The selected splatmap resolutions aren't a power of two.", "OK");
				return false;
			}
			else if (resolution.x != resolution.y)
			{
				EditorUtility.DisplayDialog("Error", "The selected splatmaps resolution isn't square.", "OK");
				return false;
			}
			else if (expectedCount > tilesCount)
			{
				EditorUtility.DisplayDialog("Error", "The terrains selected aren't square.", "OK");
				return false;
			}
			return true;
		}

		bool ValidatePreviewTexture()
		{
			if (m_Splatmaps.Count == 0)
			{
				EditorUtility.DisplayDialog("Error", "Add and select a splatmap before previewing the splatmap.", "OK");
				RevertPreviewMaterial();
				return false;
			}
			else if (m_Splatmaps[m_SelectedSplatMap] == null)
			{
				EditorUtility.DisplayDialog("Error", "Select a splatmap before previewing the splatmap.", "OK");

				if (m_Splatmaps[0] == null)
				{
					RevertPreviewMaterial();
					return false;
				}
				m_SelectedSplatMap = 0;
			}

			Texture2D texture = m_Splatmaps[m_SelectedSplatMap];
			TextureFormat format = texture.format;
			if ((format != TextureFormat.RGBA32 && format != TextureFormat.ARGB32 && format != TextureFormat.RGB24) || !texture.isReadable)
			{
				EditorUtility.DisplayDialog("Error", "The Texture format isn't compatable. Please change it to either RGBA32, ARGB32, or RGB24 and enable Read/Write.", "OK");
				RevertPreviewMaterial();
				return false;
			}

			return true;
		}

		void ResetSplatmaps()
		{
			var terrains = ToolboxHelper.GetSelectedTerrainsInScene();
			int index = 0;
			foreach (var terrain in terrains)
			{
				EditorUtility.DisplayProgressBar("Resetting Splatmaps", string.Format("Resetting splatmaps on terrain {0}", terrain.name), (index / (terrains.Count())));
				ResetSplatmapsOnTerrain(terrain);
				index++;
			}
			EditorUtility.ClearProgressBar();
		}

		void ResetSplatmapsOnTerrain(Terrain terrain)
		{
			TerrainData terrainData = terrain.terrainData;
			if (terrainData.alphamapTextureCount < 1) return;

			var undoObjects = new List<UnityEngine.Object>();
			undoObjects.Add(terrainData);
			undoObjects.AddRange(terrainData.alphamapTextures);
			Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), "Reset splatmaps");

			Color splatDefault = new Color(1, 0, 0, 0); // red
			Color splatZero = new Color(0, 0, 0, 0);

			var pixelsFirst = terrainData.alphamapTextures[0].GetPixels();
			for (int p = 0; p < pixelsFirst.Length; p++)
			{
				pixelsFirst[p] = splatDefault;
			}
			terrainData.alphamapTextures[0].SetPixels(pixelsFirst);
			terrainData.alphamapTextures[0].Apply();

			for (int i = 1; i < terrainData.alphamapTextureCount; i++)
			{
				var pixels = terrainData.alphamapTextures[i].GetPixels();
				for (int j = 0; j < pixels.Length; j++)
				{
					pixels[j] = splatZero;
				}
				terrainData.alphamapTextures[i].SetPixels(pixels);
				terrainData.alphamapTextures[i].Apply();
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		void ExportSplatmaps(UnityEngine.Object[] terrains)
		{
			if (terrains == null || terrains.Length == 0)
			{
				EditorUtility.DisplayDialog("Error", "No terrain(s) selected. Please select terrain tile(s) to continue.", "OK");
				return;
			}

			if (!Directory.Exists(m_Settings.SplatFolderPath))
			{
				Directory.CreateDirectory(m_Settings.SplatFolderPath);
			}

			var fileExtension = m_Settings.SelectedFormat == UtilitySettings.ImageFormat.TGA ? ".tga" : ".png";
			int index = 0;

			foreach (var t in terrains)
			{
				var terrain = t as Terrain;
				EditorUtility.DisplayProgressBar("Exporting Splatmaps", string.Format("Exporting splatmaps on terrain {0}", terrain.name), (index / (terrains.Count())));
				TerrainData data = terrain.terrainData;
				for (var i = 0; i < data.alphamapTextureCount; i++)
				{
					Texture2D tex = data.alphamapTextures[i];
					byte[] bytes;
					if (m_Settings.SelectedFormat == UtilitySettings.ImageFormat.TGA)
					{
						bytes = tex.EncodeToTGA();
					}
					else
					{
						bytes = tex.EncodeToPNG();
					}
					string filename = terrain.name + "_splatmap_" + i + fileExtension;
					File.WriteAllBytes($"{m_Settings.SplatFolderPath}/{filename}", bytes);
				}

				index++;
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.ClearProgressBar();
		}

		void ImportHeightmap()
		{

		}

		internal void ExportHeightmaps(UnityEngine.Object[] terrains)
		{
			if (terrains == null || terrains.Length == 0)
			{
				EditorUtility.DisplayDialog("Error", "No terrain(s) selected. Please select terrain tile(s) to continue.", "OK");
				return;
			}

			if (!Directory.Exists(m_Settings.HeightmapFolderPath))
			{
				Directory.CreateDirectory(m_Settings.HeightmapFolderPath);
			}

			int index = 0;

			foreach (var t in terrains)
			{
				var terrain = t as Terrain;
				EditorUtility.DisplayProgressBar("Exporting Heightmaps", string.Format("Exporting heightmap on terrain {0}", terrain.name), (index / (terrains.Count())));
				TerrainData terrainData = terrain.terrainData;
				string fileName = terrain.name + "_heightmap";
				string path = Path.Combine(m_Settings.HeightmapFolderPath, fileName);

				switch (m_Settings.HeightFormat)
				{
					case Heightmap.Format.RAW:
						ToolboxHelper.ExportTerrainHeightsToRawFile(terrainData, path, m_Settings.HeightmapDepth, m_Settings.FlipVertically, m_Settings.HeightmapByteOrder, new Vector2(m_Settings.ExportHeightRemapMin, m_Settings.ExportHeightRemapMax));
						break;
					default:
						ToolboxHelper.ExportTerrainHeightsToTexture(terrainData, m_Settings.HeightFormat, path, m_Settings.FlipVertically, new Vector2(m_Settings.ExportHeightRemapMin, m_Settings.ExportHeightRemapMax));
						break;
				}

				index++;
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.ClearProgressBar();
		}

		internal void RotateSplatmap()
		{
			if (!ValidatePreviewTexture())
				return;

			if (m_Settings.AdjustAllSplats)
			{
				for (int i = 0; i < m_SplatmapList.count; i++)
				{
					RotateTexture(m_Splatmaps[i]);
				}
			}
			else
			{
				RotateTexture(m_Splatmaps[m_SelectedSplatMap]);
			}

			if (m_ShowSplatmapPreview)
				m_PreviewIsDirty = true;
		}

		void RotateTexture(Texture2D texture)
		{
			Undo.RegisterCompleteObjectUndo(texture, "Rotate Texture");
			Color32[] originalPixels;
			Color32[] rotatedPixels;
			originalPixels = texture.GetPixels32();
			rotatedPixels = new Color32[originalPixels.Length];
			//bool clockwise = m_Settings.RotationAdjust == UtilitySettings.RotationAdjustment.Clockwise ? true : false;

			int width = texture.width;
			int height = texture.height;
			int rotatedIndex, originalIndex;
			for (int row = 0; row < height; row++)
			{
				for (int col = 0; col < width; col++)
				{
					rotatedIndex = (col + 1) * height - row - 1;
					if (m_Settings.RotationAdjust == UtilitySettings.RotationAdjustment.Clockwise)
					{
						originalIndex = originalPixels.Length - 1 - (row * width + col);
					}
					else
					{
						originalIndex = row * width + col;
					}
					//originalIndex = clockwise ? originalPixels.Length - 1 - (row * width + col) : row * width + col;

					rotatedPixels[rotatedIndex] = originalPixels[originalIndex];
				}
			}
			texture.SetPixels32(rotatedPixels);
			texture.Apply();
		}

		internal void FlipSplatmap()
		{
			if (!ValidatePreviewTexture())
				return;

			bool horizontal = m_Settings.FlipAdjust == UtilitySettings.FlipAdjustment.Horizontal ? true : false;

			if (m_Settings.AdjustAllSplats)
			{
				for (int i = 0; i < m_SplatmapList.count; i++)
				{
					ToolboxHelper.FlipTexture(m_Splatmaps[i], horizontal);
				}
			}
			else
			{
				ToolboxHelper.FlipTexture(m_Splatmaps[m_SelectedSplatMap], horizontal);
			}

			if (m_ShowSplatmapPreview)
				m_PreviewIsDirty = true;
		}

		void FlipTexture(Texture2D texture)
		{
			Undo.RegisterCompleteObjectUndo(texture, "Flip Texture");
			Color32[] originalPixels;
			Color32[] flippedPixels;
			bool horizontal = m_Settings.FlipAdjust == UtilitySettings.FlipAdjustment.Horizontal ? true : false;
			int difference;
			int width;
			int height;

			int flippedIndex, originalIndex;

			originalPixels = texture.GetPixels32();
			flippedPixels = new Color32[originalPixels.Length];
			width = texture.width;
			height = texture.height;
			difference = width - height;
			for (int row = 0; row < height; row++)
			{
				for (int col = 0; col < width; col++)
				{
					flippedIndex = horizontal ? (((width - 1) - row) * height + col) - difference : (height - 1) + (row * width) - col - difference;
					originalIndex = row * width + col;
					if (flippedIndex < 0) continue;
					flippedPixels[flippedIndex] = originalPixels[originalIndex];
				}
			}
			texture.SetPixels32(flippedPixels);
			texture.Apply();
		}
		
		internal void RevertPreviewMaterial()
		{
			if(m_PreviewMaterial == null)
			{
				GetAndSetActiveRenderPipelineSettings();
			}
			m_PreviewMaterial.DisableKeyword("_SPLATMAP_PREVIEW");
			for (int i = 0; i < m_Terrains.Length; i++)
			{
				if(m_Terrains[i] != null)
				{
#if UNITY_2019_2_OR_NEWER
					m_Terrains[i].materialTemplate = m_TerrainMaterials[i];
#else
					m_Terrains[i].materialType = m_TerrainMaterialType;
					if (m_TerrainMaterialType == Terrain.MaterialType.Custom)
					{
						m_Terrains[i].materialTemplate = m_TerrainMaterials[i];
					}
					else if (m_TerrainMaterialType == Terrain.MaterialType.BuiltInLegacySpecular)
					{
						m_Terrains[i].legacyShininess = m_TerrainLegacyShininess;
						m_Terrains[i].legacySpecular = m_TerrainLegacySpecular;
						m_Terrains[i].materialTemplate = null;
					}
					else
					{
						m_Terrains[i].materialTemplate = null;
					}
#endif
				}
			}
			m_ShowSplatmapPreview = false;
		}

		void GetAndSetActiveRenderPipelineSettings()
		{
			m_PreviewMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.terrain-tools/editor/terraintoolbox/materials/terrainvisualization.mat");
			m_Terrains = ToolboxHelper.GetSelectedTerrainsInScene();
			UpdateCachedTerrainMaterials();

			ToolboxHelper.RenderPipeline currentPipeline = ToolboxHelper.GetRenderPipeline();
			if (m_ActiveRenderPipeline == currentPipeline)
				return;

            m_ActiveRenderPipeline = currentPipeline;
            switch (m_ActiveRenderPipeline)
            {
				case ToolboxHelper.RenderPipeline.HD:
					m_MaxLayerCount = kMaxLayerHD;
					m_MaxSplatmapCount = kMaxSplatmapHD;
					m_PreviewMaterial.shader = Shader.Find("Hidden/HDRP_TerrainVisualization");
					break;
				case ToolboxHelper.RenderPipeline.LW:
					// this is a temp setting, in LW if height based blending or opacity as density enabled, 
					// we only support 4 layers and 1 splatmap
					// this will get checked when applying changes to each terrain
					// To-do: update max allowance check once LW terrain checked in
					m_MaxLayerCount = kMaxNoLimit;
					m_MaxSplatmapCount = kMaxNoLimit;
					m_PreviewMaterial.shader = Shader.Find("Hidden/LWRP_TerrainVisualization");
					break;
				case ToolboxHelper.RenderPipeline.Universal:
					m_MaxLayerCount = kMaxNoLimit;
					m_MaxSplatmapCount = kMaxNoLimit;
					m_PreviewMaterial.shader = Shader.Find("Hidden/Universal_TerrainVisualization");
					break;
				default:
					m_MaxLayerCount = kMaxNoLimit;
					m_MaxSplatmapCount = kMaxNoLimit;
					if (m_Terrains == null || m_Terrains.Length == 0)
					{
						break;
					}
#if UNITY_2019_2_OR_NEWER
#else
					m_TerrainMaterialType = m_Terrains[0].materialType;
					if (m_TerrainMaterialType == Terrain.MaterialType.BuiltInLegacySpecular)
					{
						m_TerrainLegacyShininess = m_Terrains[0].legacyShininess;
						m_TerrainLegacySpecular = m_Terrains[0].legacySpecular;
					}
#endif
					m_PreviewMaterial.shader = Shader.Find("Hidden/Builtin_TerrainVisualization");
					break;
			}
		}

        /// <summary>
        /// Updates an array of materials used to revert the selected terrain material from
        /// the preview material back to its original Terrain material.
        /// </summary>
        void UpdateCachedTerrainMaterials()
        {
            m_TerrainMaterials.Clear();

			foreach(Terrain terrain in m_Terrains)
			{
				m_TerrainMaterials.Add(terrain.materialTemplate);
			}
        }

        void CreateNewPalette()
		{
			string filePath = EditorUtility.SaveFilePanelInProject("Create New Palette", "New Layer Palette.asset", "asset", "");
			if (string.IsNullOrEmpty(filePath))
			{
				return;
			}
			m_SelectedLayerPalette = null;
			var newPalette = ScriptableObject.CreateInstance<TerrainPalette>();			
			foreach (var layer in m_PaletteLayers)
			{
				newPalette.PaletteLayers.Add(layer.AssignedLayer);
			}
			AssetDatabase.CreateAsset(newPalette, filePath);
			m_SelectedLayerPalette = newPalette;

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		void LoadPalette()
		{
			if (!GetPalette())
				return;

			m_PaletteLayers.Clear();
			foreach (var layer in m_SelectedLayerPalette.PaletteLayers)
			{
				Layer newLayer = ScriptableObject.CreateInstance<Layer>();
				newLayer.AssignedLayer = layer;
				newLayer.IsSelected = true;
				m_PaletteLayers.Add(newLayer);
			}
		}

		bool GetPalette()
		{
			if (m_SelectedLayerPalette == null)
			{
				if (EditorUtility.DisplayDialog("Error", "No layer palette found, create a new one?", "OK", "Cancel"))
				{
					CreateNewPalette();
					return true;
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		public void SaveSettings()
		{
			if (m_SelectedLayerPalette != null)
			{
				m_Settings.PalettePath = AssetDatabase.GetAssetPath(m_SelectedLayerPalette);
			}
			else
			{
				m_Settings.PalettePath = string.Empty;
			}

			string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsUtility);
			string utilitySettings = JsonUtility.ToJson(m_Settings);
			File.WriteAllText(filePath, utilitySettings);
			RevertPreviewMaterial();
			SceneView.RepaintAll();
		}

		public void LoadSettings()
		{
			string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsUtility);
			if (File.Exists(filePath))
			{
				string utilitySettingsData = File.ReadAllText(filePath);
				JsonUtility.FromJsonOverwrite(utilitySettingsData, m_Settings);
			}

			if (m_Settings.PalettePath == string.Empty)
			{
				m_SelectedLayerPalette = null;
			}
			else
			{
				m_SelectedLayerPalette = AssetDatabase.LoadAssetAtPath(m_Settings.PalettePath, typeof(TerrainPalette)) as TerrainPalette;
			}

			GetAndSetActiveRenderPipelineSettings();
			EditorSceneManager.sceneSaving += OnSceneSaving;
			EditorSceneManager.sceneOpened += OnSceneOpened;
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}

		public void OnLostFocus()
		{
			if (!m_ShowSplatmapPreview)
				return;

			string mouseOverWindow;
			try
			{
				mouseOverWindow = EditorWindow.mouseOverWindow.ToString();
			}
			catch
			{
				mouseOverWindow = null;
			}

			if (mouseOverWindow == null
				|| mouseOverWindow != " (UnityEditor.Experimental.TerrainAPI.TerrainToolboxWindow)"
				&& mouseOverWindow != " (UnityEditor.SceneView)")
			{
				RevertPreviewMaterial();
			}
		}

		void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
		{
			if(m_ShowSplatmapPreview)
			{
				RevertPreviewMaterial();
			}
		}

		void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode open)
		{
			m_PaletteLayers.Clear();
		}

		void OnPlayModeChanged(PlayModeStateChange state)
		{
			RevertPreviewMaterial();
		}
	}
}
