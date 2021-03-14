Shader "Hidden/TerrainTools/MeshUtility"
{
    Properties
    {
        _MainTex ( "Texture", any ) = "" {}
    }

    SubShader
    {
        ZTest LEQUAL Cull OFF ZWrite ON

        HLSLINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;

        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        float4x4 _Matrix_M;
        float4x4 _Matrix_MV;
        float4x4 _Matrix_MVP;

        ENDHLSL

        Pass // render mesh depth to rendertexture
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
                // float4 viewPos : TEXCOORD2;
            };

            v2f vert( appdata_t v )
            {
                v2f o;

                float2 b = float2( 0, 1 );
                
                o.worldPos = mul( _Matrix_M, float4( v.vertex.xyz, 1 ) );        // world space position
                // o.viewPos = mul( _Matrix_MV, float4( v.vertex.xyz, 1 ) );   // view ( camera ) space position
                o.vertex = mul( _Matrix_MVP, float4( v.vertex.xyz, 1 ) );   // clip space position

                return o;
            }

            float4 frag( v2f i ) : SV_Target
            {
                return i.worldPos.y;
                // return PackHeightmap( i.viewPos.z ); 
                // return PackHeightmap( i.vertex.z ); // depth
            }

            ENDHLSL
        }

        Pass // render mask
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert( appdata_t v )
            {
                v2f o;

                o.vertex = mul( _Matrix_MVP, float4( v.vertex.xyz, 1 ) );   // clip space position

                return o;
            }

            float4 frag( v2f i ) : SV_Target
            {
                return 1;
            }

            ENDHLSL
        }
    }
}