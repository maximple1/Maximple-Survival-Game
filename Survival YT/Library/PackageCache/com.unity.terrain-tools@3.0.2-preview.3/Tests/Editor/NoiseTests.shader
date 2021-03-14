Shader "Custom/3DNoiseTests"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Octaves ("Octaves", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #include "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Strata/Perlin.hlsl"
        #include "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Strata/Billow.hlsl"
        #include "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Strata/Value.hlsl"
        #include "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Strata/Voronoi.hlsl"
        #include "Packages/com.unity.terrain-tools/Shaders/NoiseLib/Strata/Ridge.hlsl"

        sampler2D _MainTex;

        struct Input
        {
            float4 screenPos;
            float3 worldPos;
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _Iterations;
        float _Octaves;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        #define NUM_KEYS 8

        #define ERROR_COLOR float3(1, 0, 1)

        float when_gt(float a, float b)
        {
            return max(0, sign( a - b ) );
        }

        struct Gradient
        {
            int type;
            int colorsLength;
            int alphasLength;
            float4 colors[8];
            float2 alphas[8];
        };

        Gradient Unity_Gradient_AD783C23 ()
        {
            Gradient g;
            g.type = 0;
            g.colorsLength = 8;
            g.alphasLength = 2;
            g.colors[0] = float4(0.2470588, 0.07388253, 0.01568627, 0);
            g.colors[1] = float4(1, 0.7142625, 0.3679245, 0.1852903);
            g.colors[2] = float4(0.3174247, 0.1844093, 0.09953334, 0.3911803);
            g.colors[3] = float4(0.9426209, 0.346428, 0.009844661, 0.4676432);
            g.colors[4] = float4(0.2924528, 0.1956704, 0.1213955, 0.5470665);
            g.colors[5] = float4(0.8565952, 0.592296, 0.3008374, 0.6764782);
            g.colors[6] = float4(0.3396226, 0.1743991, 0.08009968, 0.8735332);
            g.colors[7] = float4(1, 1, 1, 1);
            g.alphas[0] = float2(1, 0);
            g.alphas[1] = float2(1, 1);
            g.alphas[2] = float2(0, 0);
            g.alphas[3] = float2(0, 0);
            g.alphas[4] = float2(0, 0);
            g.alphas[5] = float2(0, 0);
            g.alphas[6] = float2(0, 0);
            g.alphas[7] = float2(0, 0);
            return g;
        }

        float3 Unity_SampleGradient_float(Gradient g, float Time)
        {
            float3 color = g.colors[0].rgb;
            
            [unroll]
            for (int c = 1; c < 8; c++)
            {
                float colorPos = saturate((Time - g.colors[c-1].w) / (g.colors[c].w - g.colors[c-1].w)) * step(c, g.colorsLength-1);
                color = lerp(color, g.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), g.type));
            }

            return color;
        }

        float3 get_gradient(float n)
        {
            float keys[NUM_KEYS] =
            {
                0,
                .01,
                .05,
                .1,
                .15,
                .2,
                1,
                1
            };

            float3 colors[NUM_KEYS] =
            {
                float3(109, 62, 32),
                float3(186, 159, 91),
                float3(193, 178, 131),
                float3(170, 104, 82),
                float3(107, 45, 24),
                float3(224, 167, 83),
                float3(176, 178, 131),
                float3(255, 0, 0)
            };

            float3 ret = ERROR_COLOR;

            [unroll]
            for(int i = 0; i < (NUM_KEYS - 1); ++i)
            {
                float startKey = keys[ i ];
                float endKey = keys[ i + 1 ];
                float3 startColor = colors[ i ];
                float3 endColor = colors[ i + 1 ];
                float3 c = lerp( startColor, endColor, saturate( ( n - startKey ) / ( endKey - startKey ) ) );

                float gtStart = when_gt( n, startKey );
                ret = lerp(ret, c, gtStart);
            }

            return ret / 255;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            StrataFractalInput fractalInput = GetStrataFractalInput();

            //// cool warped stuff
            // fractalInput.warpIterations = 0;
            // float heightMask = noise_FbmPerlin( IN.worldPos.xz * .001, fractalInput );
            // heightMask = abs(heightMask);
            // fractalInput.warpIterations = 1;
            // fractalInput.warpStrength = 4;
            // float striation = noise_FbmPerlin( IN.worldPos.xz * heightMask * heightMask * heightMask * 2, fractalInput );
            // striation = abs(striation);

            // fractalInput.warpIterations = 0;
            float3 worldPos = ApplyNoiseTransform( IN.worldPos.xyz );
            // float heightMask = noise_FbmValue( worldPos * .001, fractalInput );
            // fractalInput.warpIterations = 2;
            // fractalInput.warpStrength = 4;
            // float striation = noise_FbmValue( worldPos.y * heightMask * heightMask * heightMask * 2, fractalInput );
            float striation = noise_StrataPerlin( worldPos.y, fractalInput );
            float n = striation;
            // n = heightMask;
            
            // o.Albedo = get_gradient( striation );
            o.Albedo = Unity_SampleGradient_float( Unity_Gradient_AD783C23(), striation );
            // o.Albedo *= saturate(.5 * (noise_NoneValue(IN.screenPos.xy / IN.screenPos.w * 10000) + 1) + .5);
            // o.Albedo = striation;
            // o.Albedo = heightMask;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
