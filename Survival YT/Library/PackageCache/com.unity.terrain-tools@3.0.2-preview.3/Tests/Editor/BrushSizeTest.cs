using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.TerrainAPI;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [TestFixture]
    public class BrushSizeTest
    {
        // check enforcing brush sizes 
        [Test]
        public void Test_BrushSizeOutOfRange()
        {
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.baseMapResolution = 1024;
            terrainData.size = new Vector3(20, 600, 20);
            var terrainGO = Terrain.CreateTerrainGameObject(terrainData);

            Selection.activeGameObject = terrainGO;
            var uiInstance = new DefaultBrushUIGroup("SetHeightTool");
            uiInstance.OnEnterToolMode();

            // at 20x20, we shouldn't be able to set this to 500
            // because that would attempt to get too large of a render texture
            var tooLargeBrushSize = 500;
            uiInstance.brushSize = tooLargeBrushSize;
            Assert.That(uiInstance.brushSize, Is.LessThan(tooLargeBrushSize));
            var reasonableBrushSize = 100;

            uiInstance.brushSize = reasonableBrushSize;
            Assert.That(uiInstance.brushSize, Is.EqualTo(reasonableBrushSize));

            GameObject.DestroyImmediate(terrainGO);
        }
    }

    
}
