// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Builtin_TerrainVisualization" {
	Properties{
		//Heatmap Data
		[HideInInspector] _HeatmapGradient("Heat Map Gradient", 2D) = "grey" {}
		[HideInInspector][PerRenderData] _HeatHeightmap("Heat Height map", 2D) = "grey" {}
		[HideInInspector] _HeatmapData("Heatmap Data", Vector) = (0,0,0,0) // (Gradient Max, Gradient Min, Sea Level, 0)

		//Splatmap Data
		[HideInInspector][PerRenderData] _SplatmapTex("Splatmap Texture", 2D) = "grey" {}

		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert vertex:SplatmapVert addshadow fullforwardshadows
		#pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
		#pragma multi_compile _ _HEATMAP
		#pragma multi_compile_local LOCAL_SPACE WORLD_SPACE //Local space or world space heatmap
		#pragma multi_compile _ _SPLATMAP_PREVIEW
		#define TERRAIN_BASE_PASS
		#include "BLT_TerrainVisualizationCommon.cginc"
			
		sampler2D _MainTex;
		sampler2D _SplatmapTex;
		sampler2D _HeatmapGradient;
		sampler2D _HeatHeightmap;
		float4 _HeatmapData;
	
		fixed4 _Color;

		void surf(Input IN, inout SurfaceOutput o) 
		{
#ifdef _HEATMAP
	#ifdef LOCAL_SPACE
			half height = tex2D(_HeatHeightmap, IN.tc.xy).r * 2;
	#else
			half height = ((IN.vertex.y - _HeatmapData.z) - _HeatmapData.x) / (_HeatmapData.y - _HeatmapData.x);
	#endif
			o.Albedo = tex2D(_HeatmapGradient, height.xx);
#elif _SPLATMAP_PREVIEW
		o.Albedo = tex2D(_SplatmapTex, IN.tc.xy);
#else
		o.Albedo = tex2D(_MainTex, IN.tc.xy);
#endif
		}
			ENDCG

		UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
		UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
	}
	Fallback "Legacy Shaders/VertexLit"
}