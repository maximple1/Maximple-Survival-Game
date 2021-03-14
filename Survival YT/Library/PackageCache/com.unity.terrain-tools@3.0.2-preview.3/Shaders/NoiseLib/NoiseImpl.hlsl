#ifndef UNITY_NOISE_IMPL_INCLUDE // [ UNITY_NOISE_IMPL_INCLUDE
#define UNITY_NOISE_IMPL_INCLUDE

/**************************************************************************

    Specific noise implementations

        - Include noise implementation files here.These should have guards that check if the
          keyword for that implementation is actually enabled.
        - Add your custom implementations here. These all declare a function "noise_impl"
          that gets fed into the noise function in Noise.hlsl.
        - To use a particular implementation, either add a shader keyword in the .shader
          or statically define a particular implementation after including Noise.hlsl

**************************************************************************/

#include "Implementation/Ridge.hlsl"
#include "Implementation/Voronoi.hlsl"
#include "Implementation/Billow.hlsl"
#include "Implementation/Perlin.hlsl"
#include "Implementation/Value.hlsl"

#endif // ] UNITY_NOISE_IMPL_INCLUDE