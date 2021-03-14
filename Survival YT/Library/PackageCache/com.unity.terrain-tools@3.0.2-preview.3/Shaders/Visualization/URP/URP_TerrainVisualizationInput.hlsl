#ifndef TERRAIN_VISUALIZATION_INPUT_INCLUDED
#define TERRAIN_VISUALIZATION_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

//Heatmap
TEXTURE2D(_HeatmapGradient);			SAMPLER(sampler_HeatmapGradient);
TEXTURE2D(_HeatHeightmap);				SAMPLER(sampler_HeatHeightmap);

//Splatmap
TEXTURE2D(_SplatmapTex);				SAMPLER(sampler_SplatmapTex);

CBUFFER_START(Heatmap)
half4 _HeatmapData;
CBUFFER_END

TEXTURE2D(_MainTex);					SAMPLER(sampler_MainTex);
#endif
