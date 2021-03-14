    Shader "Hidden/TerrainTools/SmoothHeight" {

    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "Packages/com.unity.terrain-tools/Shaders/TerrainTools.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;
			sampler2D _FilterTex;

            float2 _BlurDirection;
            float4 _BrushParams;
            #define BRUSH_STRENGTH      (_BrushParams[0])
            #define BRUSH_TARGETHEIGHT  (_BrushParams[1])
			int _KernelSize;

			float4 _SmoothWeights; // centered, min, max, unused

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
			Name "Smooth"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 frag(v2f i) : SV_Target
			{
				float2 pcUV = i.pcUV;
				float2 brushUV = PaintContextUVToBrushUV(pcUV);

				// out of bounds multiplier
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;
				float height = UnpackHeightmap(tex2D(_MainTex, pcUV));
				float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV)) * UnpackHeightmap(tex2D(_FilterTex, pcUV));

				float divisor = 1.0f;
				float h = UnpackHeightmap(tex2D(_MainTex, pcUV));
				float iib = IsPcUvPartOfValidTerrainTileTexel(pcUV);
				int kernelSize = _KernelSize; // todo: subpixel?

				// separate axis guassian blur
			    for(int x = 0; x < kernelSize; ++x)
			    {
			        float2 offset = _MainTex_TexelSize.xy * abs(sign(_BlurDirection)) * (x + 1);
					float weight = (float)(kernelSize - x) / (float)(kernelSize + 1);
					float2 iibKernel = float2(IsPcUvPartOfValidTerrainTileTexel(pcUV + offset), IsPcUvPartOfValidTerrainTileTexel(pcUV - offset));

					h += UnpackHeightmap(tex2D(_MainTex, pcUV + offset)) * weight * iibKernel.x;
					h += UnpackHeightmap(tex2D(_MainTex, pcUV - offset)) * weight * iibKernel.y;
					divisor += weight * (iibKernel.x + iibKernel.y);
			    }
			
				h /= divisor;

				h = dot(float3(h, min(h, height), max(h, height)), _SmoothWeights.xyz);
				return PackHeightmap(lerp(height, h, brushStrength * iib));
			}
			ENDCG
		}
    }
    Fallback Off
}
