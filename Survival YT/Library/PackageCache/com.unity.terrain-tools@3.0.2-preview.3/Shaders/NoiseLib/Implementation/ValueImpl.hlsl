#ifndef UNITY_NOISE_VALUE_INC // [ UNITY_NOISE_VALUE_INC
#define UNITY_NOISE_VALUE_INC

/*=========================================================================
    
    VALUE NOISE

=========================================================================*/

/*=========================================================================
    
    Includes

=========================================================================*/

#include "Packages/com.unity.terrain-tools/Shaders/NoiseLib/NoiseCommon.hlsl"

/*=========================================================================
    
    1D Noise

=========================================================================*/

float get_noise_Value( float p )
{
    float i = floor( p );
    float f = frac( p );

    float u = quintic( f );
    
    /*=====================================================================
    
        a(0)         b(1)
          _____________
        
             x-axis

    =====================================================================*/

    float a = hash( i + 0.0 );
    float b = hash( i + 1.0 );

    return remap( lerp( a, b, u ).xxxx, -1, 1, 0, 1 ).x;
}

/*=========================================================================
    
    2D Noise

=========================================================================*/

float get_noise_Value( float2 p )
{
    float2 i = floor( p );
    float2 f = frac( p );
    
    float2 u = quintic( f );

    /*=====================================================================
    
        c(0,1)      d(1,1)
            _____________
           |            |
           |            |
           |            |
           |            |
           |____________|
        
        a(0,0)      b(1,0)

    =====================================================================*/

    float2 a = hash( i + float2( 0.0, 0.0 ) );
    float2 b = hash( i + float2( 1.0, 0.0 ) );
    float2 c = hash( i + float2( 0.0, 1.0 ) );
    float2 d = hash( i + float2( 1.0, 1.0 ) );

    float t1 = lerp( a, b, u.x );   // lerp along bottom edge of cell
    float t2 = lerp( c, d, u.x );   // lerp along top edge of cell

    return remap( lerp( t1, t2, u.y ).xxxx, -1, 1, 0, 1 ).x;
}

/*=========================================================================
    
    3D Noise

=========================================================================*/

float get_noise_Value( float3 p )
{
    float3 i = floor( p );
    float3 f = frac( p );

    float3 u = quintic( f );

    /*=====================================================================
    
                c2(0,1,1)         d2(1,1,1)
                    ______________
                   /|            /|
       c1(0,1,0) /  |d1(1,1,0) /  |
               /____|________/    |
               |    |       |     |
               |    |_ _ _ _|_ _ _|
               |   / aa(0,0,1)   /  b2(1,0,1)
               | /          |  /
               |____________|/
       a1(0,0,0)           b1(1,0,0)

    =====================================================================*/

    float3 a1 = hash( i + float3( 0.0, 0.0, 0.0 ) );
    float3 b1 = hash( i + float3( 1.0, 0.0, 0.0 ) );
    float3 c1 = hash( i + float3( 0.0, 1.0, 0.0 ) );
    float3 d1 = hash( i + float3( 1.0, 1.0, 0.0 ) );

    float3 a2 = hash( i + float3( 0.0, 0.0, 1.0 ) );
    float3 b2 = hash( i + float3( 1.0, 0.0, 1.0 ) );
    float3 c2 = hash( i + float3( 0.0, 1.0, 1.0 ) );
    float3 d2 = hash( i + float3( 1.0, 1.0, 1.0 ) );

    float t1 = lerp( lerp( a1, b1, u.x ), // lerp along bottom edge of cell
                     lerp( c1, d1, u.x ), // lerp along top edge of cell
                     u.y );             // lerp between top and bottom edges
    float t2 = lerp( lerp( a2, b2, u.x ), // lerp along bottom edge of cell
                     lerp( c2, d2, u.x ), // lerp along top edge of cell
                     u.y );             // lerp between top and bottom edges

    return remap( lerp( t1, t2, u.z ).xxxx, -1, 1, 0, 1 ).x;
}

/*=========================================================================
    
    4D Noise

=========================================================================*/

float get_noise_Value( float4 p )
{
    return 0;
}

#endif // ] UNITY_NOISE_VALUE_INC