#ifndef TERRAIN_TOOLS_INC
#define TERRAIN_TOOLS_INC

#include "TerrainTool.cginc"

/*=================================================================================================
    
    Domain Conversion

=================================================================================================*/

float4 _PcUvVectors; // xy = offset, zw = dimensions in uv space relative to the target heightmap dimensions
float4 _PcPixelRect;

float2 PcUvToHeightmapUv(float2 pcUV)
{
    return _PcUvVectors.xy + pcUV * _PcUvVectors.zw;
}

float2 HeightmapUvToPcUv(float2 heightmapUV)
{
    return (heightmapUV - _PcUvVectors.xy) / _PcUvVectors.zw;
}

/*=================================================================================================
    
    Texel Validity

=================================================================================================*/

sampler2D _PCValidityTex;

float IsPcUvPartOfValidTerrainTileTexel(float2 pcUV)
{
    return sign(max(0, tex2D(_PCValidityTex, pcUV).r));
}

/*=================================================================================================
    
    Filters

=================================================================================================*/

float2 _PrevFilterRange;
float2 _FilterRange;

inline float RemapFilterValue( float4 n, float o, float p, float a, float b )
{
    return a.xxxx + (b.xxxx - a.xxxx) * (n - o.xxxx) / (p.xxxx - o.xxxx);
}

inline float pow_keep_sign( float4 f, float p )
{
    return pow( abs( f ), p ) * sign( f );
}

#endif