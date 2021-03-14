#ifndef UNITY_NOISE_VORONOI_INC // [ UNITY_NOISE_VORONOI_INC
#define UNITY_NOISE_VORONOI_INC

/*=========================================================================
    
    VORONOI NOISE

=========================================================================*/

/*=========================================================================
    
    Includes

=========================================================================*/

#include "Packages/com.unity.terrain-tools/Shaders/NoiseLib/NoiseCommon.hlsl"

/*=========================================================================
    
    1D Noise

=========================================================================*/

float get_noise_Voronoi( float p )
{
    float i = floor( p );
    float f = frac( p );

    // get random positions within neighboring cells + offsets

    float n = 1;
    
    for ( float x = -2; x <= 2; ++x )
    {
        float2 r = hash( i + x );
        r = r + x - f;
        float t = r * r; // iq version, cheaper than length

        n = min( n, t );
    }

    return n;
}

/*=========================================================================
    
    2D Noise

=========================================================================*/

float get_noise_Voronoi( float2 p )
{
    float2 i = floor( p );
    float2 f = frac( p );

    // get random positions within neighboring cells + offsets

    float n = 1;

    for ( float x = -2; x <= 2; ++x )
    {
        for ( float y = -2; y <= 2; ++y )
        {
            float2 offset = float2( x, y );

            float2 r = hash( i + offset );
            r = r + offset - f;
            float t = dot( r, r ); // iq version, cheaper than length

            n = min( n, t );
        }
    }

    return n;
}

/*=========================================================================
    
    3D Noise

=========================================================================*/

float get_noise_Voronoi( float3 p )
{
    float3 i = floor( p );
    float3 f = frac( p );

    // get random positions within neighboring cells + offsets

    float n = 1;
    
    for ( float x = -2; x <= 2; x++ )
    {
        for ( float y = -2; y <= 2; y++ )
        {
            for ( float z = -2; z <= 2; z++ )
            {
                float3 offset = float3( x, y, z );
                float3 r = hash( i + offset ) + offset - f;
                float t = dot( r, r ); // iq version, cheaper than length

                n = min( n, t );
            }
        }
    }

    return n;
}

/*=========================================================================
    
    4D Noise

=========================================================================*/

float get_noise_Voronoi( float4 p )
{
    float4 i = floor( p );
    float4 f = frac( p );

    // get random positions within neighboring cells + offsets

    float n = 1;
    
    for ( float x = -2; x <= 2; ++x )
    {
        for ( float y = -2; y <= 2; ++y )
        {
            for ( float z = -2; z <= 2; ++z )
            {
                for ( float w = -2; w <= 2; ++w )
                {
                    float4 offset = float4( x, y, z, w );

                    float4 r = hash( i + offset );
                    r = r + offset - f;
                    float t = dot( r, r ); // iq version, cheaper than length

                    n = min( n, t );
                }
            }
        }
    }

    return n;
}

#endif // ] UNITY_NOISE_VORONOI_INC