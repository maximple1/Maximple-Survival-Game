using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEngine.TestTools;
using static UnityEditor.Experimental.TerrainAPI.BaseBrushUIGroup;

namespace UnityEditor.Experimental.TerrainAPI
{
    [TestFixture]
    public class BrushPlaybackTests
    {
        private const string k_TerrainToolsApiPrefix = "UnityEditor.Experimental.TerrainAPI.";
        const string k_TerrainToolsApiSuffix = ", Unity.TerrainTools.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"; 
        private Terrain terrainObj;
        private Bounds terrainBounds;
        private Queue<OnPaintOccurrence> onPaintHistory;
        private int m_PrevRTHandlesCount;
        private ulong m_PrevTextureMemory;

        private Type onSceneGUIContextType, terrainToolType, onPaintType;

        private static BindingFlags s_bindingFlags = BindingFlags.Public |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Static |
                                                        BindingFlags.Instance |
                                                        BindingFlags.FlattenHierarchy;
        private float[,] startHeightArr;

        private object terrainToolInstance;
        private MethodInfo onPaintMethod, onSceneGUIMethod;
        private Type baseBrushUIGroupType, brushRotationType, brushSizeType, brushStrengthType;
        private BaseBrushUIGroup commonUIInstance;
        private PropertyInfo brushRotationProperty, brushSizeProperty, brushStrengthProperty;

        private static string GetApiString(string str)
        {
            return $"{k_TerrainToolsApiPrefix}{str}{k_TerrainToolsApiSuffix}";
        }
        
        private void InitTerrainTypesWithReflection(string paintToolName) {

            terrainToolType = Type.GetType("UnityEditor.Experimental.TerrainAPI." + paintToolName + ", " +
                                           "Unity.TerrainTools.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            onPaintType = Type.GetType("UnityEditor.Experimental.TerrainAPI.OnPaintContext, " +
                                       "UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            onSceneGUIContextType = Type.GetType("UnityEditor.Experimental.TerrainAPI.OnSceneGUIContext, " +
                                                 "UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

            // Get the method and instance for the current tool being tested
            PropertyInfo propertyInfo = terrainToolType.GetProperty("instance", s_bindingFlags);
            MethodInfo methodInfo = propertyInfo.GetGetMethod();
            terrainToolInstance = methodInfo.Invoke(null, null);

            onPaintMethod = terrainToolType.GetMethod("OnPaint");
            onSceneGUIMethod = terrainToolType.GetMethod("OnSceneGUI");

            MethodInfo loadSettingsInfo = terrainToolType.GetMethod("LoadSettings", s_bindingFlags);

            if (loadSettingsInfo != null)
            {
                loadSettingsInfo.Invoke(terrainToolInstance, null);
            }

            // LOAD TOOL SETTINGS
            baseBrushUIGroupType = typeof(BaseBrushUIGroup);
            brushSizeType = typeof(IBrushSizeController);
            brushStrengthType = typeof(IBrushStrengthController);
            brushRotationType = typeof(IBrushRotationController);

            FieldInfo baseBrushUIGroupFieldInfo = terrainToolType.GetField("commonUI", BindingFlags.NonPublic | BindingFlags.Instance);
            if (baseBrushUIGroupFieldInfo == null)
            {
                PropertyInfo baseBrushUIGroupPropertyInfo = terrainToolType.GetProperty("commonUI", BindingFlags.NonPublic | BindingFlags.Instance);
                if (baseBrushUIGroupPropertyInfo != null)
                {
                    commonUIInstance = baseBrushUIGroupPropertyInfo.GetValue(terrainToolInstance) as BaseBrushUIGroup;
                }
            }
            else
            {
                commonUIInstance = baseBrushUIGroupFieldInfo.GetValue(terrainToolInstance) as BaseBrushUIGroup;
            }
            
            if (commonUIInstance == null)
            {
                throw new Exception("The commonUI of the brush can't be found - does it have one?");
            }

            brushSizeProperty = baseBrushUIGroupType.GetProperty("brushSize", BindingFlags.Public | BindingFlags.Instance);
            brushStrengthProperty = baseBrushUIGroupType.GetProperty("brushStrength", BindingFlags.Public | BindingFlags.Instance);
            brushRotationProperty = baseBrushUIGroupType.GetProperty("brushRotation", BindingFlags.Public | BindingFlags.Instance);
        }

        // Triggered once per frame while the test is running
        void OnSceneGUI(SceneView sceneView)
        {
            if (onPaintHistory == null || onPaintHistory.Count == 0 || terrainObj == null)
            {
                return;
            }

            OnPaintOccurrence paintOccurrence = onPaintHistory.Dequeue();

            // Generate a raycast from the relative UV and terrain size
            Vector3 rayOrigin = new Vector3(
                Mathf.Lerp(terrainBounds.min.x, terrainBounds.max.x, paintOccurrence.xPos),
                1000,
                Mathf.Lerp(terrainBounds.min.z, terrainBounds.max.z, paintOccurrence.yPos)
            );

            Physics.Raycast(new Ray(rayOrigin, Vector3.down), out RaycastHit hit);

            Texture brushTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(paintOccurrence.brushTextureAssetPath) as Texture;

            // Instantiate a null SceneGUIContext with the above raycast
            object onSceneGUIContextInstance = Activator.CreateInstance(
                onSceneGUIContextType,
                null, hit, brushTexture, paintOccurrence.brushStrength, paintOccurrence.brushSize
            );

            // set context info in case tool uses that instead of brush ui group
            MethodInfo setInfo = onSceneGUIContextType.GetMethod("Set", BindingFlags.Public | BindingFlags.Instance);
            setInfo.Invoke(onSceneGUIContextInstance,
                           new object[]
                           {
                                sceneView, true, hit,
                                brushTexture,
                                paintOccurrence.brushStrength,
                                paintOccurrence.brushSize
                           });

            brushSizeProperty.SetValue(commonUIInstance, paintOccurrence.brushSize);
            brushStrengthProperty.SetValue(commonUIInstance, paintOccurrence.brushStrength);
            brushRotationProperty.SetValue(commonUIInstance, paintOccurrence.brushRotation);

            onSceneGUIMethod.Invoke(terrainToolInstance, new object[] { terrainObj, onSceneGUIContextInstance });

            // Set the brush strength via commonUI
            commonUIInstance.brushStrength = paintOccurrence.brushStrength;
            commonUIInstance.brushSize = paintOccurrence.brushSize;

            object onPaintContext = Activator.CreateInstance(
                onPaintType,
                hit,
                brushTexture,
                new Vector2(paintOccurrence.xPos, paintOccurrence.yPos),
                paintOccurrence.brushStrength,
                paintOccurrence.brushSize
            );
            onPaintMethod.Invoke(terrainToolInstance, new object[] { terrainObj, onPaintContext });
        }

        private void ResetTerrainHeight(Terrain terrain)
        {
            float[,] heights = GetFullTerrainHeights(terrain);

            for (int x = 0; x < terrain.terrainData.heightmapResolution; x++) {
                for (int y = 0; y < terrain.terrainData.heightmapResolution; y++) {
                    heights[x, y] = 0;
                }
            }

            terrain.terrainData.SetHeights(0, 0, heights);
        }

        private Queue<OnPaintOccurrence> LoadDataFile(string recordingFileName, bool expectNull = false) {
            // Discover path to data file
            string[] assets = AssetDatabase.FindAssets(recordingFileName);
            if (assets.Length == 0) {
                Debug.LogError("No asset with name " + recordingFileName + " found");
            }
            string assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);

            // Load data file as a List<paintHistory>
            FileStream file = File.OpenRead(assetPath);
            BinaryFormatter bf = new BinaryFormatter();
            Queue<OnPaintOccurrence> paintHistory = new Queue<OnPaintOccurrence>(bf.Deserialize(file) as List<OnPaintOccurrence>);

            file.Close();

            if (paintHistory.Count == 0 && !expectNull)
            {
                throw new InconclusiveException("The loaded file contains no recordings");
            }

            return paintHistory;
        }
        
        private float[,] GetFullTerrainHeights(Terrain terrain)
        {
            int terrainWidth = terrain.terrainData.heightmapResolution;
            int terrainHeight = terrain.terrainData.heightmapResolution;
            return terrain.terrainData.GetHeights(
                0, 0,
                terrainWidth,
                terrainHeight
            );
        }

        private bool AreHeightsEqual(float[,] arr1, float[,] arr2)
        {
            if(arr1.Rank != arr2.Rank)
            {
                return false;
            }

            if(arr1.Rank > 1 && arr2.Rank > 1)
            {
                if(arr1.GetLength(0) != arr2.GetLength(0) ||
                   arr1.GetLength(1) != arr2.GetLength(1))
                {
                    return false;
                }
            }

            int xlen = arr1.GetLength(0);
            int ylen = arr1.GetLength(1);

            for(int x = 0; x < xlen; ++x)
            {
                for(int y = 0; y < ylen; ++y)
                {
                    if(arr1[x,y] != arr2[x,y])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool AreHeightsNotEqual(float[,] arr1, float[,] arr2)
        {
            return !AreHeightsEqual(arr1, arr2);
        }

        public void SetupTerrain(string terrainName) {
            TerrainData td = new TerrainData();
            td.size = new Vector3(1000, 600, 1000);
            td.heightmapResolution = 513;
            td.baseMapResolution = 1024;
            td.SetDetailResolution(1024, 32);

            // Generate terrain
            GameObject terrainGo = Terrain.CreateTerrainGameObject(td);
            terrainObj = terrainGo.GetComponent<Terrain>();
            terrainBounds = terrainGo.GetComponent<TerrainCollider>().bounds;
            Selection.activeObject = terrainGo;

            ResetTerrainHeight(terrainObj);

            Selection.activeObject = terrainGo;
            
            startHeightArr = GetFullTerrainHeights(terrainObj);
        }

        [SetUp]
        public void SetUp()
        {
            EditorWindow.GetWindow<SceneView>().Focus();

            m_PrevTextureMemory = Texture.totalTextureMemory;
            m_PrevRTHandlesCount = RTUtils.GetHandleCount();
            
            // Enables the core brush playback on OnSceneGUI
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [TearDown]
        public void Cleanup()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.activeObject = null;
            if (onPaintHistory != null)
                onPaintHistory.Clear();
            
            // delete test resources
            commonUIInstance?.brushMaskFilterStack?.Clear(true);
            PaintContext.ApplyDelayedActions(); // apply before destroying terrain and terrainData
            if (terrainObj != null)
            {
                UnityEngine.Object.DestroyImmediate(terrainObj.terrainData);
                UnityEngine.Object.DestroyImmediate(terrainObj.gameObject);
            }

            // check Texture memory and RTHandle count
            // var currentTextureMemory = Texture.totalTextureMemory;
            // Assert.True(m_PrevTextureMemory == currentTextureMemory, $"Texture memory leak. Was {m_PrevTextureMemory} but is now {currentTextureMemory}. Diff = {currentTextureMemory - m_PrevTextureMemory}");
            var currentRTHandlesCount = RTUtils.GetHandleCount();
            Assert.True(m_PrevRTHandlesCount == RTUtils.GetHandleCount(), $"RTHandle leak. Was {m_PrevRTHandlesCount} but is now {currentRTHandlesCount}. Diff = {currentRTHandlesCount - m_PrevRTHandlesCount}");
        }

        [UnityTest]
        [TestCase("PaintHeightHistory", "Terrain", ExpectedResult = null)]
        public IEnumerator Test_PaintHeight_Playback(string recordingFilePath, string targetTerrainName) {
            yield return null;
            SetupTerrain(targetTerrainName);
            InitTerrainTypesWithReflection("PaintHeightTool");
            onPaintHistory = LoadDataFile(recordingFilePath);
            SetupTerrain(targetTerrainName);

            while (onPaintHistory.Count > 0)
            {
                // Force a SceneView update for OnSceneGUI to be triggered
                SceneView.RepaintAll();
                yield return null;
            }

            if(terrainObj.drawInstanced)
            {
                terrainObj.terrainData.SyncHeightmap();
            }
            
            Assert.That(AreHeightsNotEqual(startHeightArr, GetFullTerrainHeights(terrainObj)), Is.True, "Brush didn't make changes to terrain heightmap");
        }
        
        [UnityTest]
        [TestCase("SetHeightHistory", 204f, ExpectedResult = null)]
        public IEnumerator Test_SetHeight_Playback(string recordingFilePath, float targetHeight) {
            yield return null;

            SetupTerrain("Terrain");
            InitTerrainTypesWithReflection("SetHeightTool");
            onPaintHistory = LoadDataFile(recordingFilePath);

            // Set the height parameter
            FieldInfo heightField = terrainToolType.GetField("m_TargetHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            heightField.SetValue(terrainToolInstance, targetHeight);

            while (onPaintHistory.Count > 0)
            {
                // Force a SceneView update for OnSceneGUI to be triggered
                SceneView.RepaintAll();
                yield return null;
            }

            if(terrainObj.drawInstanced)
            {
                terrainObj.terrainData.SyncHeightmap();
            }
            
            Assert.That(AreHeightsNotEqual(startHeightArr, GetFullTerrainHeights(terrainObj)), Is.True, "Brush didn't make changes to terrain heightmap");
        }


        [UnityTest]
        [TestCase("StampToolHistory", 500.0f, ExpectedResult = null)]
        public IEnumerator Test_StampTerrain_Playback(string recordingFilePath, float stampHeight) {
            yield return null;

            SetupTerrain("Terrain");
            InitTerrainTypesWithReflection("StampTool");
            onPaintHistory = LoadDataFile(recordingFilePath);

            // Set the height parameter
            FieldInfo propertiesField = terrainToolType.GetField("stampToolProperties", BindingFlags.NonPublic | BindingFlags.Instance);
            object props = propertiesField.GetValue(terrainToolInstance);
            FieldInfo heightField = props.GetType().GetField("m_StampHeight");
            heightField.SetValue(props, stampHeight);  // Use 20 b/c why not

            while (onPaintHistory.Count > 0)
            {
                // Force a SceneView update for OnSceneGUI to be triggered
                SceneView.RepaintAll();
                yield return null;
            }

            if(terrainObj.drawInstanced)
            {
                terrainObj.terrainData.SyncHeightmap();
            }
            
            Assert.That(AreHeightsNotEqual(startHeightArr, GetFullTerrainHeights(terrainObj)), Is.True, "Brush didn't make changes to terrain heightmap");
        }
        
        [UnityTest]
        [TestCase("NoiseHeightHistory", "Terrain", ExpectedResult = null)]
        public IEnumerator Test_PaintNoiseHeight_Playback(string recordingFilePath, string targetTerrainName) {
            yield return null;

            SetupTerrain(targetTerrainName);
            InitTerrainTypesWithReflection("NoiseHeightTool");
            onPaintHistory = LoadDataFile(recordingFilePath);

            while (onPaintHistory.Count > 0)
            {
                // Force a SceneView update for OnSceneGUI to be triggered
                SceneView.RepaintAll();
                yield return null;
            }

            if(terrainObj.drawInstanced)
            {
                terrainObj.terrainData.SyncHeightmap();
            }
            
            Assert.That(AreHeightsNotEqual(startHeightArr, GetFullTerrainHeights(terrainObj)), Is.True, "Brush didn't make changes to terrain heightmap");
        }

        // Used to check for texture matrix regressions
        [UnityTest]
        [TestCase("NoiseHeightHistory", "Terrain", ExpectedResult = null)]
        public IEnumerator Test_PaintTexture_Playback(string recordingFilePath, string targetTerrainName) {
            yield return null;

            SetupTerrain(targetTerrainName);
            InitTerrainTypesWithReflection("PaintTextureTool");
            onPaintHistory = LoadDataFile(recordingFilePath);

            TerrainLayer tl1 = new TerrainLayer(), tl2 = new TerrainLayer();
            tl1.diffuseTexture = Resources.Load<Texture2D>("testGradientCircle");
            tl2.diffuseTexture = Resources.Load<Texture2D>("testGradientCircle");
            terrainObj.terrainData.terrainLayers = new TerrainLayer[] { tl1, tl2 };

            PaintTextureTool paintTextureTool = terrainToolInstance as PaintTextureTool;
            FieldInfo selectedTerrainLayerInfo = typeof(PaintTextureTool).GetField("m_SelectedTerrainLayer", 
               s_bindingFlags);
            selectedTerrainLayerInfo.SetValue(paintTextureTool, tl2);

            while (onPaintHistory.Count > 0) {
                // Force a SceneView update for OnSceneGUI to be triggered
                SceneView.RepaintAll();
                yield return null;
            }

            Assert.Pass("Matrix stack regression not found!");
        }

        [UnityTest]
        [TestCase("PaintHeightHistory", "Terrain", ExpectedResult = null)]
        public IEnumerator Test_PaintHeight_With_BrushMaskFilters_Playback(string recordingFilePath, string targetTerrainName)
        {
            yield return null;

            InitTerrainTypesWithReflection("PaintHeightTool");
            onPaintHistory = LoadDataFile(recordingFilePath);
            SetupTerrain(targetTerrainName);

            commonUIInstance.brushMaskFilterStack.Clear(true);

            var filterCount = FilterUtility.GetFilterTypeCount();
            for(int i = 0; i < filterCount; ++i)
            {
                commonUIInstance.brushMaskFilterStack.Add(FilterUtility.CreateInstance(FilterUtility.GetFilterType(i)));
            }

            while (onPaintHistory.Count > 0)
            {
                // Force a SceneView update for OnSceneGUI to be triggered
                SceneView.RepaintAll();
                PaintContext.ApplyDelayedActions();
                yield return null;
            }

            if(terrainObj.drawInstanced)
            {
                terrainObj.terrainData.SyncHeightmap();
            }
        }
        
        [UnityTest]
        public IEnumerator Test_MemoryLeaks()
        {
            yield return null;
            InitTerrainTypesWithReflection("PaintHeightTool");
            SetupTerrain("Terrain");
        }
        
        [UnityTest]
        public IEnumerator Test_SetHeight_FlattenTile()
        {
            yield return null;

            InitTerrainTypesWithReflection("SetHeightTool");
            SetupTerrain("Terrain");

            var fillHeightFunc = terrainToolType.GetMethod("Flatten", BindingFlags.Instance | BindingFlags.NonPublic);
            fillHeightFunc.Invoke(terrainToolInstance, new[] {terrainObj});
        }
    }
}
