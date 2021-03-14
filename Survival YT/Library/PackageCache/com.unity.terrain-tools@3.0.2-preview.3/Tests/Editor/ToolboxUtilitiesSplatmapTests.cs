using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [TestFixture]
    public class ToolboxUtilitiesSplatmapTests
    {
        [Test]
        public void SplatmapsAreCopiedOnImport()
        {
            var terrainData = new TerrainData();
            terrainData.alphamapResolution = 16;
            terrainData.terrainLayers = new []
            {
                new TerrainLayer(), new TerrainLayer()
            };

            float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight,2];
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                for (int x = 0; x < terrainData.alphamapWidth; x++)
                {

                    map[x, y, 0] = 1;
                }
            }
            map[0, 0, 0] = 0;
            map[0, 0, 1] = 1;
            terrainData.SetAlphamaps(0,0,map);
            
            // create terrain
            var TerrainGO = Terrain.CreateTerrainGameObject(terrainData);
            
            // import splatmaps into terrain toolbox
            var terrainUtilities = new TerrainToolboxUtilities();
            Selection.activeGameObject = TerrainGO;
            terrainUtilities.ImportSplatmapsFromTerrain();
            // perform flip operation (should modify data in the utilities local texture copy)
            terrainUtilities.FlipSplatmap();
            
            // confirm that the value in the terrain splatmap hasn't changed
            terrainData.SyncTexture(TerrainData.AlphamapTextureName);
            var newMap = terrainData.GetAlphamaps(0, 0, terrainData.alphamapResolution, terrainData.alphamapResolution);
            Assert.That(newMap[0,0,0], Is.EqualTo(0));
            Assert.That(newMap[0,0,1], Is.EqualTo(1));
            
        }
    }
}