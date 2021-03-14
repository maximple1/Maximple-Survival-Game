Shader "Hidden/LWRP_TerrainVisualization"
{
	Properties
	{
		//Heatmap Data
		[HideInInspector][PerRenderData] _HeatHeightmap("Heat Height map", 2D) = "grey" {}
		[HideInInspector] _HeatmapGradient("Heat Map Gradient", 2D) = "grey" {}
		[HideInInspector] _HeatmapData("Heatmap Data", Vector) = (0,0,0,0) // (Gradient Max, Gradient Min, Sea Level, 0) 

		//Splatmap Data
		[HideInInspector][PerRenderData] _SplatmapTex("Splatmap Texture", 2D) = "grey" {}

		// used in fallback on old cards & base map
		[HideInInspector] _MainTex("BaseMap (RGB)", 2D) = "grey" {}
		[HideInInspector] _Color("Main Color", Color) = (1,1,1,1)

		// TODO: Implement ShaderGUI for the shader and display the checkbox only when instancing is enabled.
		[Toggle(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)] _TERRAIN_INSTANCED_PERPIXEL_NORMAL("Enable Instanced Per-pixel Normal", Float) = 0
	}

	SubShader
	{
		Tags { "Queue" = "Geometry-100" "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "False"}
		
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "LightweightForward" "Renderer" = "LightWeight"}
			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0

			#pragma vertex SplatmapVert
			#pragma fragment SplatmapFragment

			#define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1

			// -------------------------------------
			// Visualization keywords
			#pragma multi_compile _ _HEATMAP
			#pragma multi_compile LOCAL_SPACE WORLD_SPACE //Local space or world space heatmap
			#pragma multi_compile _ _SPLATMAP_PREVIEW

			// -------------------------------------
			// Lightweight Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE


			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			//#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

			#include "LWRP_TerrainVisualizationInput.hlsl"
			#include "LWRP_TerrainVisualizationLitPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly" "Renderer" = "LightWeight"}

			ZWrite On
			ColorMask 0

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			#pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

			#include "LWRP_TerrainVisualizationInput.hlsl"
			#include "LWRP_TerrainVisualizationLitPass.hlsl"
			ENDHLSL
		}

		

		UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
		UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
	}
	Fallback "Hidden/InternalErrorShader"
}
