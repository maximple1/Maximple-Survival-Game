using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Experimental.TerrainAPI
{
	[Serializable]
	public class Layer : ScriptableObject
	{
		public bool IsSelected = false;
		public TerrainLayer AssignedLayer = null;
	}

	public class TerrainToolboxLayer
	{
		// add a list of terrain layers to terrain, and have an option of clear existing ones
		public static void AddLayersToTerrain(TerrainData terrainData, List<TerrainLayer> layers, bool clearExisting)
		{
			if (terrainData == null || layers == null)
				return;

			if (clearExisting)
			{
				terrainData.SetTerrainLayersRegisterUndo(layers.ToArray(), "Add terrain layers");
			}
			else
			{
				// check and remove existing layers
				var filteredLayers = layers.Where(l => !terrainData.terrainLayers.Contains(l)).ToArray();
				int oldLength = terrainData.terrainLayers.Length;
				int newLength = oldLength + filteredLayers.Length;
				var newArray = new TerrainLayer[newLength];
				terrainData.terrainLayers.CopyTo(newArray, 0);
				filteredLayers.CopyTo(newArray, oldLength);
				terrainData.SetTerrainLayersRegisterUndo(newArray, "Add terrain layers");
			}
		}

		// add layer to terrain
		public static void AddLayerToTerrain(TerrainData terrainData, TerrainLayer inputLayer)
		{
			if (inputLayer == null)
				return;

			var layers = terrainData.terrainLayers;
			for (var idx = 0; idx < layers.Length; ++idx)
			{
				if (layers[idx] == inputLayer)
					return;
			}

			int newIndex = layers.Length;
			var newarray = new TerrainLayer[newIndex + 1];
			Array.Copy(layers, 0, newarray, 0, newIndex);
			newarray[newIndex] = inputLayer;
			terrainData.SetTerrainLayersRegisterUndo(newarray, "Add terrain layer");
		}

		public static void CopyTerrainLayers(Terrain fromTerrain, Terrain toTerrain)
		{
			// wipe out existing layers and splatmaps
			RemoveAllLayers(toTerrain.terrainData);

			toTerrain.terrainData.terrainLayers = fromTerrain.terrainData.terrainLayers;
		}

		public static void RemoveAllLayers(TerrainData terrainData)
		{
			terrainData.SetTerrainLayersRegisterUndo(new TerrainLayer[0], "Remove All terrain layer");
		}

		// remove a single layer and clear splatmap
		public static void RemoveLayerFromTerrain(TerrainData terrainData, int index)
		{

			int width = terrainData.alphamapWidth;
			int height = terrainData.alphamapHeight;
			float[,,] alphamap = terrainData.GetAlphamaps(0, 0, width, height);
			int alphaCount = alphamap.GetLength(2);

			int newAlphaCount = alphaCount - 1;
			float[,,] newalphamap = new float[height, width, newAlphaCount];

			// move further alphamaps one index below
			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
					for (int a = 0; a < index; ++a)
						newalphamap[y, x, a] = alphamap[y, x, a];
					for (int a = index + 1; a < alphaCount; ++a)
						newalphamap[y, x, a - 1] = alphamap[y, x, a];
				}
			}

			// normalize weights in new alpha map
			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
					float sum = 0.0F;
					for (int a = 0; a < newAlphaCount; ++a)
						sum += newalphamap[y, x, a];
					if (sum >= 0.01)
					{
						float multiplier = 1.0F / sum;
						for (int a = 0; a < newAlphaCount; ++a)
							newalphamap[y, x, a] *= multiplier;
					}
					else
					{
						// in case all weights sum to pretty much zero (e.g.
						// removing splat that had 100% weight), assign
						// everything to 1st splat texture (just like
						// initial terrain).
						for (int a = 0; a < newAlphaCount; ++a)
							newalphamap[y, x, a] = (a == 0) ? 1.0f : 0.0f;
					}
				}
			}

			// remove splat from terrain prototypes
			var layers = terrainData.terrainLayers;
			var newSplats = new TerrainLayer[layers.Length - 1];
			for (int a = 0; a < index; ++a)
				newSplats[a] = layers[a];
			for (int a = index + 1; a < alphaCount; ++a)
				newSplats[a - 1] = layers[a];
			terrainData.SetTerrainLayersRegisterUndo(newSplats, "Remove terrain layer");

			// set new alphamaps
			terrainData.SetAlphamaps(0, 0, newalphamap);
		}
	}
}
