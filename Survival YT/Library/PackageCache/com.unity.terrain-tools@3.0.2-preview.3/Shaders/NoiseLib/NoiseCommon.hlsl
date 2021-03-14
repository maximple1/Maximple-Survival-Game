#ifndef NOISE_COMMON_INC
#define NOISE_COMMON_INC

/**************************************************************************

    Properties

**************************************************************************/

// NOISE PROPERTIES

float3      _NoiseTranslation;
float3      _NoiseRotation;
float3      _NoiseScale;
float4x4    _NoiseTransform;

/**************************************************************************

    Util Functions

**************************************************************************/

inline float when_eq( float x, float y )
{
    return ( 1 - sign( abs( x - y ) ) );
}

inline float4 remap( float4 n, float o, float p, float a, float b )
{
    return a.xxxx + (b.xxxx - a.xxxx) * (n - o.xxxx) / (p.xxxx - o.xxxx);
}

/**************************************************************************

    Apply Noise Transform Functions

**************************************************************************/

inline float4x4 TRS( float3 translation, float3 rotation, float3 scale )
{
    float4x4 trs = (float4x4)0;

    // TODO(wyatt): construct TRS matrix on the fly

    return trs;
}

inline float ApplyNoiseTransform( float p )
{
    p *= _NoiseTransform._m00;
    p += _NoiseTransform._m30;

    return p;
}

inline float2 ApplyNoiseTransform( float2 p )
{
    float3 pos = float3(p.x, 0, p.y);

    p = mul(_NoiseTransform, float4(pos, 1)).xz;

    return p;
}

inline float3 ApplyNoiseTransform( float3 p )
{
    p = mul(_NoiseTransform, float4(p.xyz, 1)).xyz;

    return p;
}

/**************************************************************************

    Cubic Interpolation Functions

**************************************************************************/

float2 cubic( float p )
{
    return p * p * ( 3 - 2 * p * p );
}

float2 cubic( float2 p )
{
    return p * p * ( 3 - 2 * p * p );
}

float3 cubic( float3 p )
{
    return p * p * ( 3 - 2 * p * p );
}

float3 cubic( float4 p )
{
    return p * p * ( 3 - 2 * p * p );
}

/**************************************************************************

    Quintic Interpolation Functions

**************************************************************************/

float2 quintic( float p )
{
    return p * p * p * ( p * (6 * p - 15) + 10 );
}

float2 quintic( float2 p )
{
    return p * p * p * ( p * (6 * p - 15) + 10 );
}

float3 quintic( float3 p )
{
    return p * p * p * ( p * (6 * p - 15) + 10 );
}

float4 quintic( float4 p )
{
    return p * p * p * ( p * (6 * p - 15) + 10 );
}

/**************************************************************************

    Hashing Functions

**************************************************************************/

float hash( float p )
{
    float x = p * 98102.5453;

    return -1 + 2 * frac( sin( x ) );
}

float2 hash( float2 p )
{
    float x = dot( p, float2( 165.244, 492.128 ) );
    float y = dot( p, float2( 382.763, 234.567 ) );
    
    return -1 + 2 * frac( sin( float2( x, y ) ) * 98102.5453123 );
}

float3 hash( float3 p )
{
    p = float3( dot( p, float3( 1234.1,  442.66,   94.2 ) ),    // x
                dot( p, float3(  92.12, 639.221,  1.234 ) ),    // y
                dot( p, float3( 98.124,  103.83, 55.928 ) ) );  // z

    return -1 + 2 * frac( sin( p ) * 98102.5453123 );
}

float4 hash( float4 p )
{
    p = float4( dot( p, float4( 1234.1,  442.66,   94.2,  56.98 ) ),    // x
                dot( p, float4(  92.12, 639.221,  1.234, 89.025 ) ),    // y
                dot( p, float4( 98.124,  773.83, 55.928,   4.99 ) ),    // z
                dot( p, float4(  23.46,   105.1, 200.79,   73.5 ) ) );  // w

    return -1 + 2 * frac( sin( p ) * 98102.5453123 );
}

#endif