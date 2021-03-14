using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
	[Serializable]
	public class TerrainSettings : ScriptableObject
	{
		// Basic settings
		public int GroupingID = 0;
		public bool AutoConnect = true;
		public bool DrawHeightmap = true;
		public bool DrawInstanced = true;
		public float PixelError = 5;
		public float BaseMapDistance = 1000;
		public ShadowCastingMode ShadowCastingMode = ShadowCastingMode.TwoSided;
		public Material MaterialTemplate = null;
#if UNITY_2019_2_OR_NEWER
#else
		public Terrain.MaterialType MaterialType = Terrain.MaterialType.BuiltInStandard;
		public Color LegacySpecular = Color.gray;
		public float LegacyShininess = 0;
#endif
		public ReflectionProbeUsage ReflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

		// Mesh resolution settings
		public float TerrainWidth = 500;
		public float TerrainHeight = 600;
		public float TerrainLength = 500;
		public int DetailResolutaion = 1024;
		public int DetailResolutionPerPatch = 32;

		// Texture resolution settings
		public int BaseTextureResolution = 1024;
		public int AlphaMapResolution = 512;
		public int HeightMapResolution = 513;

		// Tree and detail settings
		public bool DrawTreesAndFoliage = true;
		public bool BakeLightProbesForTrees = true;
		public bool DeringLightProbesForTrees = false;
		public bool PreserveTreePrototypeLayers = false;
		public float DetailObjectDistance = 80;
		public bool CollectDetailPatches = true;
		public float DetailObjectDensity = 1;
		public float TreeDistance = 2000;
		public float TreeBillboardDistance = 50;
		public float TreeCrossFadeLength = 5;
		public int TreeMaximumFullLODCount = 50;

		// Grass wind settings
		public float WavingGrassStrength = 0.5f;
		public float WavingGrassSpeed = 0.5f;
		public float WavingGrassAmount = 0.5f;
		public Color WavingGrassTint = new Color(0.7f, 0.6f, 0.5f, 0.0f);

		// UI
		public bool ShowBasicTerrainSettings = false;
		public bool ShowMeshResolutionSettings = false;
		public bool ShowTextureResolutionSettings = false;
		public bool ShowTreeAndDetailSettings = false;
		public bool ShowGrassWindSettings = false;
		public bool EnableBasicSettings = false;
		public bool EnableMeshResSettings = false;
		public bool EnableTextureResSettings = false;
		public bool EnableTreeSettings = false;
		public bool EnableWindSettings = false;

		public string PresetPath = string.Empty;
		public int PresetMode = 0;

		public void CopyUISettingsFrom(TerrainSettings other)
		{
			if (other == null)
			{
				return;
			}
			
			ShowBasicTerrainSettings = other.ShowBasicTerrainSettings;
			ShowMeshResolutionSettings = other.ShowMeshResolutionSettings;
			ShowTextureResolutionSettings = other.ShowTextureResolutionSettings;
			ShowTreeAndDetailSettings = other.ShowTreeAndDetailSettings;
			ShowGrassWindSettings = other.ShowGrassWindSettings;
			EnableBasicSettings = other.EnableBasicSettings;
			EnableMeshResSettings = other.EnableMeshResSettings;
			EnableTextureResSettings = other.EnableTextureResSettings;
			EnableTreeSettings = other.EnableTreeSettings;
			EnableWindSettings = other.EnableWindSettings;
		}

		public void CopySettingsFrom(TerrainSettings other)
		{
			if (other == null)
			{
				return;
			}
			
			GroupingID = other.GroupingID;
			AutoConnect = other.AutoConnect;
			DrawHeightmap = other.DrawHeightmap;
			DrawInstanced = other.DrawInstanced;
			PixelError = other.PixelError;
			BaseMapDistance = other.BaseMapDistance;
			ShadowCastingMode = other.ShadowCastingMode;
			MaterialTemplate = other.MaterialTemplate;
			ReflectionProbeUsage = other.ReflectionProbeUsage;
#if UNITY_2019_2_OR_NEWER
#else
			MaterialType = other.MaterialType;
			LegacySpecular = other.LegacySpecular;
			LegacyShininess = other.LegacyShininess;
#endif

			// mesh resolution
			TerrainWidth = other.TerrainWidth;
			TerrainHeight = other.TerrainHeight;
			TerrainLength = other.TerrainLength;
			DetailResolutaion = other.DetailResolutaion;
			DetailResolutionPerPatch = other.DetailResolutionPerPatch;

			// texture resolution
			BaseTextureResolution = other.BaseTextureResolution;
			AlphaMapResolution = other.AlphaMapResolution;
			HeightMapResolution = other.HeightMapResolution;

			// tree and details
			DrawTreesAndFoliage = other.DrawTreesAndFoliage;
			BakeLightProbesForTrees = other.BakeLightProbesForTrees;
			DeringLightProbesForTrees = other.DeringLightProbesForTrees;
			PreserveTreePrototypeLayers = other.PreserveTreePrototypeLayers;
			DetailObjectDistance = other.DetailObjectDistance;
			CollectDetailPatches = other.CollectDetailPatches;
			DetailObjectDensity = other.DetailObjectDensity;
			TreeDistance = other.TreeDistance;
			TreeBillboardDistance = other.TreeBillboardDistance;
			TreeCrossFadeLength = other.TreeCrossFadeLength;
			TreeMaximumFullLODCount = other.TreeMaximumFullLODCount;

			// grass wind
			WavingGrassStrength = other.WavingGrassStrength;
			WavingGrassSpeed = other.WavingGrassSpeed;
			WavingGrassAmount = other.WavingGrassAmount;
			WavingGrassTint = other.WavingGrassTint;
		}
		
		public void CopySettingsFrom(Terrain terrain)
		{
			if (terrain == null)
			{
				return;
			}

			// base settings
			GroupingID = terrain.groupingID;
			AutoConnect = terrain.allowAutoConnect;
			DrawHeightmap = terrain.drawHeightmap;
			DrawInstanced = terrain.drawInstanced;
			PixelError = terrain.heightmapPixelError;
			BaseMapDistance = terrain.basemapDistance;
			ShadowCastingMode = terrain.shadowCastingMode;
			MaterialTemplate = terrain.materialTemplate;
			ReflectionProbeUsage = terrain.reflectionProbeUsage;
#if UNITY_2019_2_OR_NEWER
#else
			MaterialType = terrain.materialType;
			LegacySpecular = terrain.legacySpecular;
			LegacyShininess = terrain.legacyShininess;
#endif

			// mesh resolution
			TerrainWidth = terrain.terrainData.size.x;
			TerrainHeight = terrain.terrainData.size.y;
			TerrainLength = terrain.terrainData.size.z;
			DetailResolutaion = terrain.terrainData.detailResolution;
			DetailResolutionPerPatch = terrain.terrainData.detailResolutionPerPatch;

			// texture resolution
			BaseTextureResolution = terrain.terrainData.baseMapResolution;
			AlphaMapResolution = terrain.terrainData.alphamapResolution;
			HeightMapResolution = terrain.terrainData.heightmapResolution;

			// tree and details
			DrawTreesAndFoliage = terrain.drawTreesAndFoliage;
			BakeLightProbesForTrees = terrain.bakeLightProbesForTrees;
			DeringLightProbesForTrees = terrain.deringLightProbesForTrees;
			PreserveTreePrototypeLayers = terrain.preserveTreePrototypeLayers;
			DetailObjectDistance = terrain.detailObjectDistance;
			CollectDetailPatches = terrain.collectDetailPatches;
			DetailObjectDensity = terrain.detailObjectDensity;
			TreeDistance = terrain.treeDistance;
			TreeBillboardDistance = terrain.treeBillboardDistance;
			TreeCrossFadeLength = terrain.treeCrossFadeLength;
			TreeMaximumFullLODCount = terrain.treeMaximumFullLODCount;

			// grass wind
			WavingGrassStrength = terrain.terrainData.wavingGrassStrength;
			WavingGrassSpeed = terrain.terrainData.wavingGrassSpeed;
			WavingGrassAmount = terrain.terrainData.wavingGrassAmount;
			WavingGrassTint = terrain.terrainData.wavingGrassTint;
		}
	}
}
