using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.Linq;

namespace UnityEditor.Experimental.TerrainAPI
{
    [ExecuteInEditMode]
    public class TerrainToolboxVisualization
    {
		TerrainVisualizationSettings m_Settings = ScriptableObject.CreateInstance<TerrainVisualizationSettings>();
        enum VISUALIZERMODE { None, AltitudeHeatmap };
        VISUALIZERMODE m_selectedMode = VISUALIZERMODE.None;
        VISUALIZERMODE m_previousMode = VISUALIZERMODE.None;

        List<Terrain> m_Terrains = new List<Terrain>();
        List<Material> m_TerrainMaterials = new List<Material>();
        Material m_VisualizationMaterial;

#if UNITY_2019_2_OR_NEWER
#else
        Terrain.MaterialType m_TerrainMaterialType;
        float m_TerrainLegacyShininess;
        Color m_TerrainLegacySpecular;
#endif
		bool m_ModeWarning = false;
		string m_PresetPath = string.Empty;
		float m_TerrainMaxHeight = 0f;

        // Preset
        TerrainVisualizationSettings m_SelectedPreset;

        static class Styles
        {
            //General
            public static readonly GUIContent VisualizationSettings = EditorGUIUtility.TrTextContent("Visualization Modes", "Select from visualization modes.");
            public static readonly string ModeWarningSettings = "There are no terrains within the scene. Add a terrain to use the visualization tool";
            public static readonly string CompatabilityWarningSettings = "Terrain Visualization isn't compatible with HDRP at this time";

            //Heatmap
            public static readonly GUIContent ReferenceSpace = EditorGUIUtility.TrTextContent("Reference Space", "Select to either visualize in world space or local space.");
            public static readonly GUIContent SeaLevel = EditorGUIUtility.TrTextContent("Sea Level", "Height of sea level.");
            public static readonly GUIContent HeatLevels = EditorGUIUtility.TrTextContent("Levels", "The number of heat levels.");
            public static readonly GUIContent MeasurementUnit = EditorGUIUtility.TrTextContent("Measurement Unit", "The measurement unit used to determine heat map altitude.");

            // Settings
            public static readonly GUIContent Preset = EditorGUIUtility.TrTextContent("Preset", "Preset used to visualize terrain. Select a pre-saved visualization preset asset or create a new preset.");
            public static readonly GUIContent SavePreset = EditorGUIUtility.TrTextContent("Save", "Save the current preset with current settings.");
            public static readonly GUIContent SaveAsPreset = EditorGUIUtility.TrTextContent("Save As", "Save the current preset as a new preset asset file.");
            public static readonly GUIContent RefreshPreset = EditorGUIUtility.TrTextContent("Refresh", "Load selected preset and apply to current visualization settings");
        }

        public void OnGUI()
        {
            // scroll view of settings
            EditorGUIUtility.hierarchyMode = true;
            TerrainToolboxUtilities.DrawSeperatorLine();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(Styles.VisualizationSettings, EditorStyles.boldLabel);
            m_selectedMode = (VISUALIZERMODE)EditorGUILayout.EnumPopup(m_selectedMode);
            if (EditorGUI.EndChangeCheck())
            {
                ToggleVisualization();
            }
            EditorGUILayout.EndHorizontal();

			if (m_selectedMode == VISUALIZERMODE.AltitudeHeatmap)
			{
				ShowHeatmapGUI();
			}

            if (m_ModeWarning)
            {
                EditorGUILayout.HelpBox(Styles.ModeWarningSettings, MessageType.Warning);
            }

            ShowPresetGUI();
        }

        public void ToggleVisualization()
        {
            if (GameObject.FindObjectOfType<Terrain>() == null)
            {
                m_selectedMode = VISUALIZERMODE.None;
                m_Settings.ModeWarning = true;
                return;
            }

            if (m_Terrains == null || GameObject.FindObjectsOfType<Terrain>().Length != m_Terrains.Count || m_Terrains[0] == null)
            {
                m_Terrains.Clear();
                m_Terrains.AddRange(ToolboxHelper.GetAllTerrainsInScene());
                m_Settings.TerrainMaxHeight = m_Terrains[0].terrainData.size.y;
            }


            switch (m_selectedMode)
            {
                case VISUALIZERMODE.AltitudeHeatmap:
                    RevertMaterial();
                    RefreshTerrainInScene();
                    UpdateHeatmapSettings();
                    break;
                case VISUALIZERMODE.None:
                    RevertMaterial();
                    break;
				default:
					break;
            }

            m_ModeWarning = false;
            m_previousMode = m_selectedMode;
        }

        void ShowPresetGUI()
        {
            TerrainToolboxUtilities.DrawSeperatorLine();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.Preset, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            m_SelectedPreset = (TerrainVisualizationSettings)EditorGUILayout.ObjectField(m_SelectedPreset, typeof(TerrainVisualizationSettings), false);
            if (EditorGUI.EndChangeCheck() && m_SelectedPreset != null)
            {
                if (EditorUtility.DisplayDialog("Confirm", "Load terrain creation settings from selected preset?", "OK", "Cancel"))
                {
                    LoadVisualizationSettings();
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
					TransferSettings(m_Settings, m_SelectedPreset);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
                    ToggleVisualization();
                }
            }
            if (GUILayout.Button(Styles.SaveAsPreset))
            {
                CreateNewPreset();
            }
            if (GUILayout.Button(Styles.RefreshPreset))
            {
                LoadVisualizationSettings();
            }
            EditorGUILayout.EndHorizontal();
        }

		void ShowHeatmapGUI()
		{
			m_Settings.HeatLevels = m_Settings.DistanceSelection.Count;
            EditorGUILayout.BeginFadeGroup(Mathf.Clamp((int)m_selectedMode, 0, 1));

            //Color chooser GUI
            EditorGUI.indentLevel++;
            m_Settings.ReferenceSpace = (TerrainVisualizationSettings.REFERENCESPACE)EditorGUILayout.EnumPopup(Styles.ReferenceSpace, m_Settings.ReferenceSpace);
            if(m_Settings.ReferenceSpace == TerrainVisualizationSettings.REFERENCESPACE.WorldSpace)
            {
                m_Settings.SeaLevel = EditorGUILayout.FloatField(Styles.SeaLevel, m_Settings.SeaLevel, GUILayout.ExpandWidth(false));
            }

			// Measure GUI
            EditorGUI.BeginChangeCheck();
            m_Settings.CurrentMeasure = (TerrainVisualizationSettings.MEASUREMENTS)EditorGUILayout.EnumPopup(Styles.MeasurementUnit, m_Settings.CurrentMeasure);
            if (EditorGUI.EndChangeCheck())
            {
                ConvertUnits();
            }

			// Heat Levels GUI
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.HeatLevels, GUILayout.MaxWidth(100));
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginChangeCheck();
            m_Settings.HeatLevels = EditorGUILayout.IntSlider(m_Settings.HeatLevels, 1, 8);
			if (EditorGUI.EndChangeCheck())
			{
				ConfigureHeatLevels();
			}
            EditorGUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            EditorGUILayout.BeginVertical("Box");
			if (m_Settings.DistanceSelection.Count > 0 && m_Settings.HeatLevels > 0)
			{
				float min = m_Settings.DistanceSelection[0];
				float max = m_Settings.DistanceSelection[m_Settings.HeatLevels - 1];
				for (int i = 0; i < m_Settings.HeatLevels; i++)
				{
					float distance = (float)System.Math.Round(m_Settings.DistanceSelection[i], 2);

					EditorGUILayout.BeginHorizontal();
					if (m_Settings.ReferenceSpace == TerrainVisualizationSettings.REFERENCESPACE.WorldSpace)
						m_Settings.DistanceSelection[i] = EditorGUILayout.FloatField(distance, GUILayout.Width(70));
					else
						m_Settings.DistanceSelection[i] = Mathf.Clamp(EditorGUILayout.FloatField(distance, GUILayout.Width(70)), 0, m_TerrainMaxHeight);

					float height = m_Settings.DistanceSelection[i];

					//Compare the distances
					EditorGUI.indentLevel--;
					if (m_Settings.CurrentMeasure == TerrainVisualizationSettings.MEASUREMENTS.Feet)
					{
						EditorGUILayout.LabelField(new GUIContent("ft"), GUILayout.Width(30));
						height /= TerrainVisualizationSettings.CONVERSIONNUM;
					}
					else
					{
						EditorGUILayout.LabelField(new GUIContent("m"), GUILayout.Width(30));
					}
					m_Settings.MaxDistance = Mathf.Max(height, max);
					m_Settings.MinDistance = Mathf.Min(height, min);
					EditorGUI.indentLevel--;

					m_Settings.ColorSelection[i] = EditorGUILayout.ColorField(m_Settings.ColorSelection[i]);
					EditorGUI.indentLevel += 2;
					EditorGUILayout.EndHorizontal();
				}
            }
            if(EditorGUI.EndChangeCheck())
            {
                UpdateHeatmapSettings();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
        }

        void UpdateHeatmapSettings()
        {
			GetAndSetActiveRenderPipelineSettings();

			MaterialPropertyBlock heatmapBlock = new MaterialPropertyBlock();
            Vector4 shaderParams = new Vector4(m_Settings.MinDistance, m_Settings.MaxDistance, m_Settings.SeaLevel, 0);

            if (m_Settings.ReferenceSpace == TerrainVisualizationSettings.REFERENCESPACE.WorldSpace 
                && m_Settings.CurrentMeasure == TerrainVisualizationSettings.MEASUREMENTS.Feet)
            {
                shaderParams /= TerrainVisualizationSettings.CONVERSIONNUM;
            }

            m_VisualizationMaterial.EnableKeyword("_HEATMAP");
            m_VisualizationMaterial.DisableKeyword("_SPLATMAP_PREVIEW");
            m_VisualizationMaterial.SetTexture("_HeatmapGradient", CreateGradient());
            m_VisualizationMaterial.SetVector("_HeatmapData", shaderParams);
            if (m_Settings.ReferenceSpace == TerrainVisualizationSettings.REFERENCESPACE.WorldSpace)
            {
                m_VisualizationMaterial.DisableKeyword("LOCAL_SPACE");
                m_VisualizationMaterial.EnableKeyword("WORLD_SPACE");
            }
            else
            {
                m_VisualizationMaterial.DisableKeyword("WORLD_SPACE");
                m_VisualizationMaterial.EnableKeyword("LOCAL_SPACE");
            }

            foreach (Terrain terrain in m_Terrains)
            {
#if UNITY_2019_2_OR_NEWER
#else
                terrain.materialType = Terrain.MaterialType.Custom;
#endif
                terrain.materialTemplate = m_VisualizationMaterial;

                heatmapBlock.Clear();
                heatmapBlock.SetTexture("_HeatHeightmap", terrain.terrainData.heightmapTexture);
                terrain.SetSplatMaterialPropertyBlock(heatmapBlock);
				EditorUtility.SetDirty(terrain);
            }
        }

        void ConvertUnits()
        {
            if (m_Settings.CurrentMeasure == TerrainVisualizationSettings.MEASUREMENTS.Feet)
            {
                for (int i = 0; i < m_Settings.HeatLevels; i++)
                {
                    m_Settings.DistanceSelection[i] *= TerrainVisualizationSettings.CONVERSIONNUM;
                }
                m_TerrainMaxHeight *= TerrainVisualizationSettings.CONVERSIONNUM;

            }
            else
            {
                for (int i = 0; i < m_Settings.HeatLevels; i++)
                {
                    m_Settings.DistanceSelection[i] /= TerrainVisualizationSettings.CONVERSIONNUM;
                }
                m_TerrainMaxHeight /= TerrainVisualizationSettings.CONVERSIONNUM;
            }

        }

        void ConfigureHeatLevels()
        {
            int currentLevels = m_Settings.DistanceSelection.Count;
			if (currentLevels <= 0) return;

            float height;
            if (m_Settings.HeatLevels != currentLevels)
            {
                int num = m_Settings.HeatLevels - currentLevels;
                if (num > 0) //Add color and int field
                {
                    for (int i = 0; i < num; i++)
                    {
                        int index = (currentLevels - 1) + i;
                        height = m_Settings.DistanceSelection[index] + 100;
                        m_Settings.DistanceSelection.Add(height);

                        //Compare the distances
                        if (m_Settings.CurrentMeasure == TerrainVisualizationSettings.MEASUREMENTS.Feet)
                            height /= TerrainVisualizationSettings.CONVERSIONNUM;

                        m_Settings.MaxDistance = Mathf.Max(height, m_Settings.MaxDistance);
                        m_Settings.MinDistance = Mathf.Min(height, m_Settings.MinDistance);

                        m_Settings.ColorSelection.Add(Color.white);
                    }
                }
                else //Remove color and int field
                {
                    for (int i = -1; i >= num; i--)
                    {
                        int index = currentLevels + i;
                        m_Settings.DistanceSelection.RemoveAt(index);
                        m_Settings.ColorSelection.RemoveAt(index);
                    }
                }
            }
        }

        Texture2D CreateGradient()
        {
            Color[] colors = m_Settings.ColorSelection.ToArray();
            if (colors.Length == 0 || colors == null) return null;

            int textureWidth = 256;
            int textureHeight = 1;
            int length = colors.Length;

            GradientColorKey[] colorKeys = new GradientColorKey[length];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[length];

            //Configure new gradient data
            for (int i = 0; i < length; i++)
            {
                float distance = m_Settings.DistanceSelection[i];
                float step;
                if (m_Settings.ReferenceSpace == TerrainVisualizationSettings.REFERENCESPACE.WorldSpace)
                {
                    step = (distance - m_Settings.MinDistance) / (m_Settings.MaxDistance - m_Settings.MinDistance);
                }
                else
                {
                    step = distance / m_TerrainMaxHeight;
                }

                colorKeys[i].color = colors[i];
                colorKeys[i].time = step;
                alphaKeys[i].alpha = colors[i].a;
                alphaKeys[i].time = step;
            }

            //Create gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);

            //Create texture
            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int i = 0; i < textureWidth; i++)
            {
                texture.SetPixel(i, 0, gradient.Evaluate((float)i / (float)textureWidth));
            }

            texture.Apply(false);
            return texture;
        }


        public void RevertMaterial()
        {
			GetAndSetActiveRenderPipelineSettings();

			if (m_VisualizationMaterial != null)
            {
				m_VisualizationMaterial.DisableKeyword("_HEATMAP");
			}

            for(int i = 0; i < m_Terrains.Count && m_TerrainMaterials.Count > 0; i++)
            {
                if (m_Terrains[i] != null)
                {
#if UNITY_2019_2_OR_NEWER
                    m_Terrains[i].materialTemplate = m_TerrainMaterials.Count > i ? m_TerrainMaterials[i] : m_TerrainMaterials.First();
#else
                    m_Terrains[i].materialType = m_TerrainMaterialType;
                    if (m_TerrainMaterialType == Terrain.MaterialType.Custom)
                    {
                        m_Terrains[i].materialTemplate = m_TerrainMaterials.Count > i ? m_TerrainMaterials[i] : m_TerrainMaterials[0];
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
            SceneView.RepaintAll();
        }

		void RefreshTerrainInScene()
		{
			m_Terrains.Clear();
			m_Terrains.AddRange(ToolboxHelper.GetAllTerrainsInScene());
			if (m_Terrains.Count == 0)
			{
				m_ModeWarning = true;
				return;
			}

			m_TerrainMaxHeight = m_Terrains.Max(t => t.terrainData.size.y);

            m_TerrainMaterials.Clear();
			foreach (Terrain terrain in m_Terrains)
			{
				m_TerrainMaterials.Add(terrain.materialTemplate);
			}

			m_ModeWarning = false;
		}

        void GetAndSetActiveRenderPipelineSettings()
        {
            ToolboxHelper.RenderPipeline m_ActiveRenderPipeline = ToolboxHelper.GetRenderPipeline();
            m_VisualizationMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.terrain-tools/editor/terraintoolbox/materials/terrainvisualization.mat");

            switch (m_ActiveRenderPipeline)
            {
                case ToolboxHelper.RenderPipeline.HD:
                    m_VisualizationMaterial.shader = Shader.Find("Hidden/HDRP_TerrainVisualization");
                    break;
                case ToolboxHelper.RenderPipeline.LW:
                    m_VisualizationMaterial.shader = Shader.Find("Hidden/LWRP_TerrainVisualization");
                    break;
				case ToolboxHelper.RenderPipeline.Universal:
					m_VisualizationMaterial.shader = Shader.Find("Hidden/Universal_TerrainVisualization");
					break;
                default:
                    if (m_Terrains == null || m_Terrains.Count == 0)
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
                    m_VisualizationMaterial.shader = Shader.Find("Hidden/Builtin_TerrainVisualization");
                    break;
            }
        }

        void CreateNewPreset()
        {
            string filePath = EditorUtility.SaveFilePanelInProject("Create Terrain Visualization Settings", "New Terrain Visualization.asset", "asset", "");
			if(string.IsNullOrEmpty(filePath))
			{
				return;
			}

            m_SelectedPreset = ScriptableObject.CreateInstance<TerrainVisualizationSettings>();
			TransferSettings(m_Settings, m_SelectedPreset);
			AssetDatabase.CreateAsset(m_SelectedPreset, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ToggleVisualization();
        }

        bool GetVisualizationSettingsPreset()
        {
            if (m_SelectedPreset == null)
            {
                if (EditorUtility.DisplayDialog("Error", "No terrain visualization setting found, create a new one?", "OK", "Cancel"))
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

        void LoadVisualizationSettings()
        {
            if (m_SelectedPreset == null)
            {
                EditorUtility.DisplayDialog("Error", "No selected preset found. Select one to continue.", "OK");
                return;
            }
            else
            {
				TransferSettings(m_SelectedPreset, m_Settings);
                UpdateHeatmapSettings();
            }
        }

        public void SaveSettings()
        {
            if (m_SelectedPreset != null)
            {
                m_PresetPath = AssetDatabase.GetAssetPath(m_SelectedPreset);
            }
            else
            {
                m_PresetPath = string.Empty;
            }

            string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsVisualization);
            string settingsData = JsonUtility.ToJson(m_Settings);
            File.WriteAllText(filePath, settingsData);
			SceneView.RepaintAll();

            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            Undo.undoRedoPerformed -= OnUndo;
        }

        public void LoadSettings()
        {
            string filePath = ToolboxHelper.GetPrefFilePath(ToolboxHelper.ToolboxPrefsVisualization);
            if (File.Exists(filePath))
            {
                string settingsData = File.ReadAllText(filePath);
                JsonUtility.FromJsonOverwrite(settingsData, m_Settings);
            }

            if (m_PresetPath == string.Empty)
            {
                m_SelectedPreset = null;
            }
            else
            {
                m_SelectedPreset = AssetDatabase.LoadAssetAtPath(m_PresetPath, typeof(TerrainVisualizationSettings)) as TerrainVisualizationSettings;
            }
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            Undo.undoRedoPerformed += OnUndo;
        }

        void TransferSettings(TerrainVisualizationSettings fromSettings, TerrainVisualizationSettings toSettings)
		{
			toSettings.ColorSelection.Clear();
			toSettings.DistanceSelection.Clear();
			toSettings.ColorSelection.AddRange(fromSettings.ColorSelection.ToArray());
			toSettings.DistanceSelection.AddRange(fromSettings.DistanceSelection.ToArray());
			toSettings.CurrentMeasure = fromSettings.CurrentMeasure;
			toSettings.HeatLevels = fromSettings.HeatLevels;
			toSettings.SeaLevel = fromSettings.SeaLevel;
			toSettings.MaxDistance = fromSettings.MaxDistance;
			toSettings.MinDistance = fromSettings.MinDistance;
			toSettings.ReferenceSpace = fromSettings.ReferenceSpace;
			toSettings.WorldSpace = fromSettings.WorldSpace;
		}

		void Reset()
		{
			m_selectedMode = VISUALIZERMODE.None;
			m_VisualizationMaterial = null;
			m_Terrains.Clear();
			m_TerrainMaterials.Clear();
		}

        void OnSceneSaving(Scene scene, string path)
        {
            if (m_selectedMode != VISUALIZERMODE.None)
            {
                RevertMaterial();
            }
        }

        void OnSceneSaved(Scene scene)
        {
			Reset();
		}

        void OnSceneOpened(Scene scene, OpenSceneMode open)
        {
			Reset();
        }

        void OnUndo()
        {
            if (m_selectedMode == VISUALIZERMODE.AltitudeHeatmap)
            {
                UpdateHeatmapSettings();
            }
        }

        void PlayModeChanged(PlayModeStateChange state)
        {
            if (m_selectedMode != VISUALIZERMODE.None)
            {
                RevertMaterial();
            }
        }
    }
}