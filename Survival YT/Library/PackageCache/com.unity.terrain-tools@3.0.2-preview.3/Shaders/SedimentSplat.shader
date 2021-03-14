    Shader "Hidden/TerrainTools/SedimentSplat" {

    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "Packages/com.unity.terrain-tools/Shaders/TerrainTools.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;
			sampler2D _MaskTex;

            float4 _BrushParams;
			#define BRUSH_STRENGTH       (_BrushParams[0])
			#define BRUSH_SPLATSTRENGTH  (_BrushParams[1])

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
            Name "Sediment Splat"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SedimentSplat

            float4 SedimentSplat(v2f i) : SV_Target
            {
				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);

				// out of bounds multiplier
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

				float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
				float alphaMap = tex2D(_MainTex, i.pcUV).r;
				float mask = /*1.0f - */ pow(tex2D(_MaskTex, i.pcUV).r, 1.0f); //TODO - make this user specified
				return saturate(alphaMap + BRUSH_SPLATSTRENGTH * mask * brushStrength);
            }
            ENDCG
        }

		Pass
		{
			Name "Sediment Speed Splat"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment SedimentSpeedSplat

			float4 SedimentSpeedSplat(v2f i) : SV_Target
			{
				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);

				// out of bounds multiplier
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

				float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
				float alphaMap = tex2D(_MainTex, i.pcUV).r;
				float2 vel = tex2D(_MaskTex, i.pcUV).rg;
				float speed = sqrt(vel.x * vel.x + vel.y * vel.y);
				return saturate(alphaMap + BRUSH_SPLATSTRENGTH * speed * brushStrength);
			}
			ENDCG
		}

		Pass
		{
			Name "Sediment Flux Splat"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment SedimentFluxSplat

			float4 SedimentFluxSplat(v2f i) : SV_Target
			{
				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);

				// out of bounds multiplier
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

				float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
				float alphaMap = tex2D(_MainTex, i.pcUV).r;

				float4 flux = tex2D(_MaskTex, i.pcUV);

				float total = 1.0f - 0.01f * (flux.x + flux.y + flux.z + flux.w);
				return saturate(alphaMap + BRUSH_SPLATSTRENGTH * total * brushStrength);

				//float2 vel = 0.001f * pow(tex2D(_MaskTex, i.pcUV).rgba, 4.0f);
				//float speed = sqrt(vel.x * vel.x + vel.y * vel.y);
				//return saturate(alphaMap + BRUSH_SPLATSTRENGTH * speed * brushStrength);
			}
			ENDCG
		}
    }
    Fallback Off
}
