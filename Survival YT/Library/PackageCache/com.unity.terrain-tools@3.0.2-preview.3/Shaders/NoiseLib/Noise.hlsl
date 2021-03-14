// #ifndef UNITY_TERRAIN_TOOL_NOISE_INC
// #define UNITY_TERRAIN_TOOL_NOISE_INC

// /**************************************************************************

//     Includes

// **************************************************************************/

// #include "NoiseCommon.hlsl"
// #include "NoiseImpl.hlsl"

// /**************************************************************************

//     Fractal Functions

// **************************************************************************/

// float fbm( float p, FractalParams f )
// {
//     float n = 0;

//     for( float i = 0; i < f.octaves; ++i )
//     {
//         n += f.amplitude * noise_impl( p * f.frequency );
//         f.frequency *= f.lacunarity;
//         f.amplitude *= f.persistence;
//     }

//     return n;
// }

// float fbm( float2 p, FractalParams f )
// {
//     float n = 0;

//     for( float i = 0; i < f.octaves; ++i )
//     {
//         n += f.amplitude * noise_impl( p * f.frequency );
//         f.frequency *= f.lacunarity;
//         f.amplitude *= f.persistence;
//     }

//     return n;
// }

// float fbm( float3 p, FractalParams f )
// {
//     float n = 0;

//     for( float i = 0; i < f.octaves; ++i )
//     {
//         n += f.amplitude * noise_impl( p * f.frequency );
//         f.frequency *= f.lacunarity;
//         f.amplitude *= f.persistence;
//     }

//     return n;
// }

// float fbm( float4 p, FractalParams f )
// {
//     float n = 0;

//     for( float i = 0; i < f.octaves; ++i )
//     {
//         n += f.amplitude * noise_impl( p * f.frequency );
//         f.frequency *= f.lacunarity;
//         f.amplitude *= f.persistence;
//     }

//     return n;
// }

// float fbm( float p )
// {
//     return fbm( p, GetFractalParams() );
// }

// float fbm( float2 p )
// {
//     return fbm( p, GetFractalParams() );
// }

// float fbm( float3 p )
// {
//     return fbm( p, GetFractalParams() );
// }

// float fbm( float4 p )
// {
//     return fbm( p, GetFractalParams() );
// }

// /**************************************************************************

//     Noise Functions
//         - Call these from your shader

// **************************************************************************/

// /**************************************************************************
//     Noise Functions - Fractal, Non-Warped
// **************************************************************************/

// float noise( float1 pos, FractalParams fractal )
// {
//     return fbm( pos, fractal );
// }

// float noise( float2 pos, FractalParams fractal )
// {
//     return fbm( pos, fractal );
// }

// float noise( float3 pos, FractalParams fractal )
// {
//     return fbm( pos, fractal );
// }

// float noise( float4 pos, FractalParams fractal )
// {
//     return fbm( pos, fractal );
// }

// /**************************************************************************
//     Noise Functions - Fractal, Warped
// **************************************************************************/

// float noise( float pos, FractalParams fractal, WarpParams warp )
// {
//     // do warping
//     for ( float i = 0; i < warp.iterations; ++i )
//     {
//         float q = fbm( pos + warp.offset.x );
//         pos = pos + warp.strength * q;
//     }

//     float f = fbm( pos, fractal );

//     return f;
// }

// float noise( float2 pos, FractalParams fractal, WarpParams warp )
// {
//     // do warping
//     for ( float i = 0; i < warp.iterations; ++i )
//     {
//         float2 q = float2( fbm( pos ), fbm( pos + warp.offset.xy ) );
//         pos = pos + warp.strength * q;
//     }
    
//     float f = fbm( pos, fractal );

//     return f;
// }

// float noise( float3 pos, FractalParams fractal, WarpParams warp )
// {
//     float q;
//     // do warping
//     for ( float i = 0; i < warp.iterations; ++i )
//     {
//         q = float3( fbm( pos.xyz ),
//                            fbm( pos.xyz + warp.offset.xyz ),
//                            fbm( pos.xyz + float3( warp.offset.x, warp.offset.y, 0 ) ) );
//         pos = pos + warp.strength * q;
//     }
    
//     float f = fbm( pos, fractal );

//     return f;
// }

// float noise( float4 pos, FractalParams fractal, WarpParams warp )
// {
//     // do warping
//     // for ( float i = 0; i < warp.iterations; ++i )
//     // {
//     //     float4 q = float4( fbm( pos ), fbm( pos + warp.offset.xyzw ) );
//     //     pos = pos + warp.strength * q;
//     // }

//     float f = fbm( pos, fractal );

//     return f;
// }

// /**************************************************************************
//     Noise Functions - Non-Fractal, Non-Warped
// **************************************************************************/

// float noise( float pos )
// {
//     return noise_impl( pos );
// }

// float noise( float2 pos )
// {
//     return noise_impl( pos );
// }

// float noise( float3 pos )
// {
//     return noise_impl( pos );
// }

// float noise( float4 pos )
// {
//     return noise_impl( pos );
// }

// #endif // UNITY_TERRAIN_TOOL_NOISE_INC