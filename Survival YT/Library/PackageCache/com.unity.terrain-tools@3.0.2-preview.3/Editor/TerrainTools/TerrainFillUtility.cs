using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using System.Linq;

namespace UnityEditor.Experimental.TerrainAPI
{
    public static class TerrainFillUtility
    {
        public static Terrain[] GetTerrainsInGroup(Terrain terrain)
        {
            TerrainUtility.TerrainMap terrainMap = TerrainUtility.TerrainMap.CreateFromPlacement(terrain, null, false);
            Terrain[] terrainGroup = terrainMap.m_terrainTiles.Select(o => o.Value).ToArray();

            return terrainGroup;
        }

        public static Terrain[] GetTerrainsInGroup(int groupingID)
        {
            List<Terrain> groupTerrains = new List<Terrain>();

            Terrain[] activeTerrains = Terrain.activeTerrains;

            for(int i = 0; i < activeTerrains.Length; ++i)
            {
                if(activeTerrains[i].groupingID == groupingID)
                {
                    groupTerrains.Add(activeTerrains[i]);
                }
            }

            return groupTerrains.ToArray();
        }

        private static Rect GetTerrainBounds(Terrain terrain)
        {
            return new Rect(0, 0, terrain.terrainData.size.x, terrain.terrainData.size.z);
        }

        public static PaintContext BeginFillHeightmap(Terrain terrain)
        {
            RenderTexture rt = terrain.terrainData.heightmapTexture;
            PaintContext ctx = new PaintContext(terrain,
                                                new RectInt(0, 0, rt.width, rt.height),
                                                rt.width, rt.height);
            ctx.CreateRenderTargets(rt.format);
            ctx.GatherHeightmap();

            return ctx;
        }

        public static void EndFillHeightmap(PaintContext ctx, string editorUndoString)
        {
            TerrainPaintUtility.EndPaintHeightmap(ctx, editorUndoString);
        }

        public static PaintContext BeginFillTexture(Terrain terrain, TerrainLayer inputLayer)
        {
            return TerrainPaintUtility.BeginPaintTexture(terrain, GetTerrainBounds(terrain), inputLayer);
        }

        public static void EndFillTexture(PaintContext ctx, string editorUndoString)
        {
            TerrainPaintUtility.EndPaintTexture(ctx, editorUndoString);
        }

        public static PaintContext CollectNormals(Terrain terrain)
        {
            return TerrainPaintUtility.CollectNormals(terrain, GetTerrainBounds(terrain));
        }
    }
}