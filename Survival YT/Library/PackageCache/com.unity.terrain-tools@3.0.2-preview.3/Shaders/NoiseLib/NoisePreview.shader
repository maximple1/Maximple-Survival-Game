Shader "Hidden/TerrainTools/NoiseLib/DefaultPreview"
{
    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader
    {
        ZTest Always Cull OFF ZWrite Off

        HLSLINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height
        
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

        v2f vert( appdata_t v )
        {
            v2f o;
            o.vertex = UnityObjectToClipPos( v.vertex );
            o.uv = v.uv;
            return o;
        }

        ENDHLSL

        Pass // 1
        {
            Name "Noise Preview"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float4 frag( v2f i ) : SV_Target
            {
                float noiseVal = tex2D(_MainTex, i.uv);

                // display negative areas as a separate color
                float4 negativeColor = float4( 0, abs( noiseVal.x ), abs( noiseVal.x ), 1 );
                float4 positiveColor = float4( noiseVal.xxx, 1 );
                float t = max( sign( noiseVal.x ), 0 );

                return lerp( negativeColor, positiveColor, t );
            }

            ENDHLSL
        }
    }
}