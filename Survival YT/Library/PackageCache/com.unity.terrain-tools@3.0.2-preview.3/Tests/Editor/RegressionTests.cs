using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class RegressionTests
    {
        GameObject m_TerrainGO;
        Terrain m_TerrainComponent;
        private int m_NumTerrains;

        [Test]
        [TestCase(0f, 1f, Heightmap.Format.PNG, Ignore = "Failing on Ubuntu")]
        [TestCase(.25f, .75f, Heightmap.Format.PNG, Ignore = "Failing on Ubuntu")]
        [TestCase(0f, 1f, Heightmap.Format.TGA, Ignore = "Failing on Ubuntu")]
        [TestCase(.25f, .75f, Heightmap.Format.TGA, Ignore = "Failing on Ubuntu")]
        [TestCase(0f, 1f, Heightmap.Format.RAW)]
        [TestCase(.25f, .75f, Heightmap.Format.RAW)]
        [TestCase(0f, 1f, Heightmap.Format.RAW, Heightmap.Depth.Bit8)]
        [TestCase(.25f, .75f, Heightmap.Format.RAW, Heightmap.Depth.Bit8)]
        public void TerrainToolboxUtilites_WhenExportHeightmap_LevelCorrectionWorks(float min, float max, Heightmap.Format format, Heightmap.Depth depth = Heightmap.Depth.Bit16)
        {
            TerrainToolboxWindow toolboxWindow = EditorWindow.GetWindow(typeof(TerrainToolboxWindow)) as TerrainToolboxWindow;
            Texture2D gradientTexture = CreateGradientTexture();

            int heightmapResolution = 513;
            int numberOfTiles = 1;
            int baseLevel = 0;
            int remapLevel = 1;
            ToolboxHelper.CopyTextureToTerrainHeight(m_TerrainComponent.terrainData, gradientTexture, Vector2Int.zero, heightmapResolution, numberOfTiles, baseLevel, remapLevel);

            Selection.activeGameObject = m_TerrainGO;
            m_TerrainGO.name = "TestTerrain";
            m_TerrainComponent.name = "TestComponent";

            RenderTexture oldRT = RenderTexture.active;
            RenderTexture.active = m_TerrainComponent.terrainData.heightmapTexture;

            //Run Tests and Cleanup files
            string fileName = m_TerrainGO.name + "_heightmap";
            string path = Path.Combine(toolboxWindow.m_TerrainUtilitiesMode.m_Settings.HeightmapFolderPath, fileName);
            switch (format)
            {
                case Heightmap.Format.PNG:
                    path += ".png";
                    Assert.IsTrue(TestLevelCorrection(toolboxWindow, new Vector2(min, max), path, format));
                    FileUtil.DeleteFileOrDirectory(path);
                    FileUtil.DeleteFileOrDirectory(path + ".meta");
                    break;
                case Heightmap.Format.TGA:
                    path += ".tga";
                    Assert.IsTrue(TestLevelCorrection(toolboxWindow, new Vector2(min, max), path, format));
                    FileUtil.DeleteFileOrDirectory(path);
                    FileUtil.DeleteFileOrDirectory(path + ".meta");
                    break;
                case Heightmap.Format.RAW:
                    path += ".raw";
                    Assert.IsTrue(TestLevelCorrection(toolboxWindow, new Vector2(min, max), path, depth));
                    FileUtil.DeleteFileOrDirectory(path);
                    FileUtil.DeleteFileOrDirectory(path + ".meta");
                    break;
            }

            AssetDatabase.Refresh();
            RenderTexture.active = oldRT;
            toolboxWindow.Close();
        }

        /// <summary>
        /// This overloaded method deals specifically with testing the level correction of the raw format 
        /// </summary>
        /// <param name="toolboxWindow">Window where the Export Heightmap Utilities live</param>
        /// <param name="minMaxRemap">Min and Max values of the remap</param>
        /// <param name="format">Heightmap File Format</param>
        /// <param name="path">String path of the files directory location</param>
        /// <param name="depth">Heightmap Bit Depth</param>
        /// <returns></returns>
        bool TestLevelCorrection(TerrainToolboxWindow toolboxWindow, Vector2 minMaxRemap, string path, Heightmap.Depth depth)
        {
            //Execute the repro steps in code
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.ExportHeightRemapMin = minMaxRemap.x;
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.ExportHeightRemapMax = minMaxRemap.y;

            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.HeightFormat = Heightmap.Format.RAW;
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.HeightmapDepth = depth;
            toolboxWindow.m_TerrainUtilitiesMode.m_SelectedDepth = (depth == Heightmap.Depth.Bit16) ? 0 : 1;
            toolboxWindow.m_TerrainUtilitiesMode.ExportHeightmaps(new Object[] { m_TerrainComponent });

            //Get byte data of the terrain's heightmap
            TerrainData terrainData = m_TerrainComponent.terrainData;
#if UNITY_2019_3_OR_NEWER
            int heightmapWidth = terrainData.heightmapResolution - 1;
            int heightmapHeight = terrainData.heightmapResolution - 1;
#else
			int heightmapWidth = terrainData.heightmapWidth - 1;
			int heightmapHeight = terrainData.heightmapHeight - 1;
#endif
            float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);
            byte[] data = new byte[heightmapWidth * heightmapHeight * (int)depth];

            if (depth == Heightmap.Depth.Bit16)
            {
                float normalize = (1 << 16);
                for (int y = 0; y < heightmapHeight; ++y)
                {
                    for (int x = 0; x < heightmapWidth; ++x)
                    {
                        //Remapping the heightmap data
                        int index = x + y * heightmapWidth;
                        float remappedHeight = heights[y, x] * (minMaxRemap.y - minMaxRemap.x) + minMaxRemap.x;

                        int height = Mathf.RoundToInt(remappedHeight * normalize);
                        ushort compressedHeight = (ushort)Mathf.Clamp(height, 0, ushort.MaxValue);

                        byte[] byteData = System.BitConverter.GetBytes(compressedHeight);
                        if ((toolboxWindow.m_TerrainUtilitiesMode.m_Settings.HeightmapByteOrder == ToolboxHelper.ByteOrder.Mac) == System.BitConverter.IsLittleEndian)
                        {
                            data[index * 2 + 0] = byteData[1];
                            data[index * 2 + 1] = byteData[0];
                        }
                        else
                        {
                            data[index * 2 + 0] = byteData[0];
                            data[index * 2 + 1] = byteData[1];
                        }
                    }
                }
            }
            else
            {
                float normalize = (1 << 8);
                for (int y = 0; y < heightmapHeight; ++y)
                {
                    for (int x = 0; x < heightmapWidth; ++x)
                    {
                        //Remapping the heightmap data
                        int index = x + y * heightmapWidth;
                        float remappedHeight = heights[y, x] * (minMaxRemap.y - minMaxRemap.x) + minMaxRemap.x;

                        int height = Mathf.RoundToInt(remappedHeight * normalize);
                        byte compressedHeight = (byte)Mathf.Clamp(height, 0, byte.MaxValue);
                        data[index] = compressedHeight;
                    }
                }
            }

            //Compare both the original and regression test data
            byte[] rawByteData = File.ReadAllBytes(path);
            return data.SequenceEqual(rawByteData);
        }

        /// <summary>
        /// This overloaded method deals specifically with testing the level correction of the png and tga format 
        /// </summary>
        /// <param name="toolboxWindow">Window where the Export Heightmap Utilities live</param>
        /// <param name="minMaxRemap">Min and Max values of the remap</param>
        /// <param name="path">String path of the files directory location</param>
        /// <param name="format">Heightmap File Format</param>
        /// <returns></returns>
        bool TestLevelCorrection(TerrainToolboxWindow toolboxWindow, Vector2 minMaxRemap, string path, Heightmap.Format format)
        {
            //Execute the repro steps in code
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.ExportHeightRemapMin = minMaxRemap.x;
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.ExportHeightRemapMax = minMaxRemap.y;

            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.HeightFormat = format;
            toolboxWindow.m_TerrainUtilitiesMode.ExportHeightmaps(new Object[] { m_TerrainComponent });

            //Get heightmap data to compare
            int width = RenderTexture.active.width - 1;
            int height = RenderTexture.active.height - 1;
            var texture = new Texture2D(width, height, RenderTexture.active.graphicsFormat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            //Remap Texture
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i].r = (pixels[i].r * 2) * (minMaxRemap.y - minMaxRemap.x) + minMaxRemap.x;
                pixels[i + 1].r = (pixels[i + 1].r * 2) * (minMaxRemap.y - minMaxRemap.x) + minMaxRemap.x;
                pixels[i + 2].r = (pixels[i + 2].r * 2) * (minMaxRemap.y - minMaxRemap.x) + minMaxRemap.x;
                pixels[i + 3].r = (pixels[i + 3].r * 2) * (minMaxRemap.y - minMaxRemap.x) + minMaxRemap.x;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            //Compare both the original and regression test data
            byte[] byteData = File.ReadAllBytes(path);
            return format == Heightmap.Format.PNG ?
                texture.EncodeToPNG().SequenceEqual(byteData) :
                texture.EncodeToTGA().SequenceEqual(byteData);
        }

        [Test]
        public void TerrainToolboxUtilites_WhenSelectSplatmap_DoesNotIndexOutOfRange()
        {
            //Collect data, create needed objects
            Texture2D texture = new Texture2D(512, 512);
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = texture;
            TerrainLayer[] terrainLayers = { layer };

            //Create gameobject with terrain component
            m_TerrainGO = new GameObject();
            Terrain terrain = m_TerrainGO.AddComponent<Terrain>();
            terrain.terrainData = new TerrainData();

            //Add splatmap to terrain in order to import into the Utilities Window
            terrain.terrainData.terrainLayers = terrainLayers;

            //Execute the repro steps in code
            TerrainToolboxWindow toolboxWindow = EditorWindow.GetWindow(typeof(TerrainToolboxWindow)) as TerrainToolboxWindow;
            Selection.activeGameObject = m_TerrainGO;
            toolboxWindow.m_TerrainUtilitiesMode.ImportSplatmapsFromTerrain();

            Assert.That(() =>
            {
                toolboxWindow.m_TerrainUtilitiesMode.ExportSplatmapsToTerrain(true);
            }, !Throws.TypeOf<System.IndexOutOfRangeException>());
            toolboxWindow.Close();
        }

        [Test]
        public void TerrainToolboxUtilites_WhenApplySplatmaps_DoesNotDividebyZero()
        {
            // Preparation:
            // Collect data, create needed objects
            Texture2D texture = new Texture2D(512, 512);
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = texture;
            TerrainLayer[] terrainLayers = {layer};

            //Add splatmap to terrain in order to import into the Utilities Window
            m_TerrainComponent.terrainData.terrainLayers = terrainLayers;
            Selection.activeGameObject = m_TerrainGO;

            // Execute the repro steps in code
            TerrainToolboxWindow toolboxWindow = EditorWindow.GetWindow(typeof(TerrainToolboxWindow)) as TerrainToolboxWindow;
            toolboxWindow.m_TerrainUtilitiesMode.ImportSplatmapsFromTerrain(true);
            Selection.activeGameObject = null;
            
            Assert.That(() => 
            {
                toolboxWindow.m_TerrainUtilitiesMode.ExportSplatmapsToTerrain(true);
            }, !Throws.TypeOf<System.DivideByZeroException>());
            toolboxWindow.Close();
        }

        [Test]
        [TestCase(2, 2, 33)]
        [TestCase(2, 2, 65)]
        [TestCase(4, 4, 65)]
        [TestCase(2, 2, 129)]
        [TestCase(8, 8, 129)]
        [TestCase(2, 2, 513)]
        public void TerrainToolboxUtilities_WhenSplitTerrain_HeightmapResolutionIsCorrect(int xSplit, int zSplit, int originalHeightmapRes)
        {
            TerrainToolboxWindow toolboxWindow = EditorWindow.GetWindow(typeof(TerrainToolboxWindow)) as TerrainToolboxWindow;
            Texture2D gradientTexture = CreateGradientTexture();
            int baseLevel = 0;
            int remapLevel = 1;
            int numberOfTiles = 1;
            ToolboxHelper.CopyTextureToTerrainHeight(m_TerrainComponent.terrainData, gradientTexture, Vector2Int.zero, originalHeightmapRes, numberOfTiles, baseLevel, remapLevel);

            Selection.activeGameObject = m_TerrainGO;
            m_TerrainGO.name = "TestTerrain";
            m_TerrainComponent.name = "TestComponent";

            RenderTexture oldRT = RenderTexture.active;
            RenderTexture.active = m_TerrainComponent.terrainData.heightmapTexture;

            // Run the test
            TestSplitTerrainHeightmapResolution(toolboxWindow, originalHeightmapRes, xSplit, zSplit);

            AssetDatabase.Refresh();
            RenderTexture.active = oldRT;
            toolboxWindow.Close();
        }

        void TestSplitTerrainHeightmapResolution(TerrainToolboxWindow toolboxWindow, int heightmapRes, int xSplit, int zSplit)
        {
            // Set up parent object so we can locate the split tiles for cleanup after testing
            int groupingId = 12345;
            var parent = new GameObject().AddComponent<TerrainGroup>();
            parent.GroupID = groupingId;
            m_TerrainComponent.transform.SetParent(parent.transform);

            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.HeightmapResolution = heightmapRes;
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.TileXAxis = xSplit;
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.TileZAxis = zSplit;
            toolboxWindow.m_TerrainUtilitiesMode.SplitTerrain(m_TerrainComponent, groupingId, true);

            // The children should include the original terrain object + the newly created tiles
            int childCount = parent.transform.childCount;
            Assert.AreEqual(xSplit*zSplit + 1, childCount);

            // Check that the original terrain heightmap resolution is unchanged
            Assert.AreEqual(heightmapRes, m_TerrainComponent.terrainData.heightmapResolution);

            // Test and clean up the split tiles (skip the first child as it is the original terrain object)
            for (int i = 1; i < childCount; i++)
            {
                var child = parent.transform.GetChild(i).GetComponent<Terrain>();
                Assert.AreEqual(child.terrainData.heightmapResolution - 1,
                    GetExpectedTileHeightmapResolution(heightmapRes, xSplit));
                string path = Path.Combine("Assets/Terrain", child.transform.name + ".asset");
                FileUtil.DeleteFileOrDirectory(path);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
            }
        }

        int GetExpectedTileHeightmapResolution(int heightmapRes, int xSplit)
        {
            int minHeightmapRes = 32;
            int newHeightmapRes = (heightmapRes - 1) / xSplit;
            return Math.Max(newHeightmapRes, minHeightmapRes);
        }

        [Test]
        [TestCase(1, 1, 2, 2)]
        [TestCase(3, 3, 4, 4)]
        [TestCase(5, 5, 2, 2)]
        public void TerrainToolboxUtilites_WhenSplitTerrain_MissingTrees(int amountOfTreesX, int amountOfTreesZ, int tileXAxis, int tileZAxis)
        {
            //Setup tree prefab (Needs to be persistent)
            GameObject treePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            treePrefab.GetComponent<Renderer>().sharedMaterial.shader = Shader.Find("Nature/Tree Soft Occlusion Bark");
            string localPath = $"Assets/{treePrefab.name}.prefab";
            localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
            PrefabUtility.SaveAsPrefabAsset(treePrefab, localPath);
            treePrefab = AssetDatabase.LoadAssetAtPath(localPath, typeof(GameObject)) as GameObject;

            //Setup terrain object with trees
            TerrainData terrainData =  m_TerrainComponent.terrainData;
            TreePrototype prototype = new TreePrototype();
            prototype.prefab = treePrefab;

            terrainData.treePrototypes = new TreePrototype[]
            {
                prototype
            };
            
            TreeInstance[] treeInstancesArray = new TreeInstance[amountOfTreesX*amountOfTreesZ];
            for (int z = 0; z < amountOfTreesZ; z++)
            {
                for (int x = 0; x < amountOfTreesX; x++)
                {
                    TreeInstance treeInstance = new TreeInstance();
                    treeInstance.prototypeIndex = 0;
                    treeInstance.position = new Vector3(x / (float)amountOfTreesX, 0, z / (float)amountOfTreesZ);
                    treeInstancesArray[(z * amountOfTreesZ) + x] = treeInstance;
                }
            }
            terrainData.treeInstances = treeInstancesArray;

            
            // Set up parent object so we can locate the split tiles for cleanup after testing
            int groupingId = 12345;
            var parent = new GameObject().AddComponent<TerrainGroup>();
            parent.GroupID = groupingId;
            m_TerrainComponent.transform.SetParent(parent.transform);
            
            //Execute the repro steps checking to make sure split terrains have trees
            Selection.activeGameObject = m_TerrainGO;
            TerrainToolboxWindow toolboxWindow = EditorWindow.GetWindow(typeof(TerrainToolboxWindow)) as TerrainToolboxWindow;
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.KeepOldTerrains = true;
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.TileXAxis = tileXAxis;
            toolboxWindow.m_TerrainUtilitiesMode.m_Settings.TileZAxis = tileZAxis;
            toolboxWindow.m_TerrainUtilitiesMode.SplitTerrains(true);
            
            Terrain[] objs = GameObject.FindObjectsOfType<Terrain>();
            Terrain[] splitTerrains = objs.Where(
                obj => obj.terrainData?.treeInstanceCount > 0
                ).ToArray();
            Assert.IsNotEmpty(splitTerrains);

            //Cleanup
            toolboxWindow.Close();
            FileUtil.DeleteFileOrDirectory("Assets/Terrain");
            File.Delete("Assets/Terrain.meta");
            File.Delete(localPath);
            File.Delete(localPath + ".meta");
            UnityEditor.AssetDatabase.Refresh();
        }

        [Test]
        public void TerrainToolboxUtilities_WhenApplySplatmaps_DoesNotModifyColorData()
        {
            //Setup terrain layer data
            TerrainToolboxWindow toolboxWindow = EditorWindow.GetWindow<TerrainToolboxWindow>();
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = CreateGradientTexture();
            byte[] texData = layer.diffuseTexture.GetRawTextureData();
            m_TerrainComponent.terrainData.terrainLayers = new TerrainLayer[] { layer };

            //Reproduce steps
            Selection.activeObject = m_TerrainGO;
            TerrainToolboxUtilities utilities = toolboxWindow.m_TerrainUtilitiesMode;
            utilities.ImportSplatmapsFromTerrain();

            //Manually set splatmap list since the window's OnGUI method isn't called which normally sets the splatmap list
            utilities.m_SplatmapList = new UnityEditorInternal.ReorderableList(utilities.m_Splatmaps, typeof(Texture2D), true, false, true, true);
            utilities.ExportSplatmapsToTerrain();

            Assert.AreEqual(texData, layer.diffuseTexture.GetRawTextureData());
        }

        [SetUp]
        public void Setup()
        {
            m_NumTerrains = Terrain.activeTerrains.Length;
            var terrainData = new TerrainData();
            m_TerrainGO = Terrain.CreateTerrainGameObject(terrainData);
            m_TerrainComponent = m_TerrainGO.GetComponent<Terrain>();
        }

        [TearDown]
        public void Cleanup()
        {
            TerrainGroup group = null;
            var parent = m_TerrainGO.transform.parent;
            if (parent != null)
            {
                group = parent.GetComponent<TerrainGroup>();
            }

            if (group != null)
            {
                var terrains = group.GetComponentsInChildren<Terrain>();
                foreach (var t in terrains)
                {
                    var go = t.gameObject;
                    Object.DestroyImmediate(t.terrainData);
                    Object.DestroyImmediate(t);
                    Object.DestroyImmediate(go);
                }
                
                Object.DestroyImmediate(group.gameObject);
            }
            else
            {
                Object.DestroyImmediate(m_TerrainComponent.terrainData);
                Object.DestroyImmediate(m_TerrainComponent);
                Object.DestroyImmediate(m_TerrainGO);
            }
            
            m_TerrainComponent = null;
            Selection.activeObject = null;
            
            Assert.True(m_NumTerrains == Terrain.activeTerrains.Length, $"Leaked {Terrain.activeTerrains.Length - m_NumTerrains} Terrain objects. Please make sure the test is cleaning up created Terrains.");
        }

        /// <summary>
        /// Create a gradient texture
        /// </summary>
        /// <param name="width">Width of the texture</param>
        /// <param name="height">Height of the texture</param>
        /// <returns></returns>
        Texture2D CreateGradientTexture(int width = 513, int height = 513)
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys =
            {
                new GradientColorKey{color = Color.white, time = 0f},
                new GradientColorKey{color = Color.black, time = 1f},
            };
            GradientAlphaKey[] alphaKeys =
            {
                new GradientAlphaKey{alpha = 1f, time= 0f},
                new GradientAlphaKey{alpha = 0f, time= 1f},
            };

            gradient.SetKeys(colorKeys, alphaKeys);
            var gradTex = new Texture2D(width, height, TextureFormat.R16, false);
            gradTex.filterMode = FilterMode.Bilinear;

            float inv = 1f / (width - 1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var t = x * inv;
                    Color col = gradient.Evaluate(t);
                    gradTex.SetPixel(x, y, col);
                }
            }
            gradTex.Apply();
            return gradTex;
        }
    }
}