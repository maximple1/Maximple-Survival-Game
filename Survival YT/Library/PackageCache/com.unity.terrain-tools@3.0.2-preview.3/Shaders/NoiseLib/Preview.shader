Shader "Hidden/TerrainTools/Noise/Preview"
{
    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader
    {
        ZTest Always Cull OFF ZWrite Off

        HLSLINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height
        float  _Layer;
        
        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        inline float4 remap( float4 n, float o, float p, float a, float b )
        {
            return a.xxxx + (b.xxxx - a.xxxx) * (n - o.xxxx) / (p.xxxx - o.xxxx);
        }

        v2f vert( appdata_t v )
        {
            v2f o;
            
            o.vertex = UnityObjectToClipPos( v.vertex );
            o.uv = v.uv;

            return o;
        }

        ENDHLSL

        Pass
        {
            Name "Noise Preview (2D)"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float4 frag( v2f i ) : SV_Target
            {
                float r = UnpackHeightmap(tex2D(_MainTex, i.uv));

                // r = remap( r.xxxx, 0, 1, -4, 5 );

                float4 color = lerp( float4( 0, abs( r ), abs( r ), 1 ), float4( r.xxx, 1 ), max( sign( r ), 0 ) );

                // color the out of range values
                color = lerp( color, float4( r - .9, 0, 0, 1 ), max( sign( r - 1 ), 0 ) );   // if over 1

                return color;
            }

            ENDHLSL
        }

        Pass
        {
            Name "Noise Preview (3D)"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float4 frag( v2f i ) : SV_Target
            {
                float r = UnpackHeightmap(tex2D(_MainTex, i.uv));

                return lerp( float4( 0, abs( r ), abs( r ), 1 ), float4( r.xxx, 1 ), max( sign( r ), 0 ) );
            }

            ENDHLSL
        }
    }
}