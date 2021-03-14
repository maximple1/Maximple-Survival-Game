    Shader "Hidden/TerrainTools/SimpleHeightBlend" {

    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "Packages/com.unity.terrain-tools/Shaders/TerrainTools.hlsl"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;
			sampler2D _FilterTex;
			sampler2D _NewHeightTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH      (_BrushParams[0])
            #define BRUSH_FEATURESIZE   (_BrushParams[2])
            #define BRUSH_ROTATION      (_BrushParams[3])

            struct appdata_t {
                float4 vertex : POSITION;
                float2 pcUV : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 pcUV : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pcUV = v.pcUV;
                return o;
            }

        ENDCG


        Pass
        {
            Name "Simple Height Blend"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment ErodeHeight

            float4 ErodeHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = i.pcUV;

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float oldHeight = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				float newHeight = tex2D(_NewHeightTex, heightmapUV).r;
				float brushStrength = oob * BRUSH_STRENGTH * UnpackHeightmap(tex2D(_BrushTex, brushUV)) * UnpackHeightmap(tex2D(_FilterTex, i.pcUV));

				return PackHeightmap(lerp(oldHeight, newHeight, brushStrength));
            }
            ENDCG
        }
    }
    Fallback Off
}
