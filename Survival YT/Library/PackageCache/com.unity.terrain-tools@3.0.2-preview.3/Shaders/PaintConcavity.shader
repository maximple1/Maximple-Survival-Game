	Shader "Hidden/TerrainTools/PaintConcavity" {

	Properties{ _MainTex("Texture", any) = "" {} }

	SubShader{
			
		ZTest Always Cull Off ZWrite Off

		CGINCLUDE
			#include "UnityCG.cginc"
			#include "Packages/com.unity.terrain-tools/Shaders/TerrainTools.hlsl"


			sampler2D _MainTex;
			float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

			sampler2D _BrushTex;
			sampler2D _Heightmap;
			sampler2D _ConcavityRemap;

			float4 _BrushParams;
			#define BRUSH_STRENGTH      (_BrushParams[0])
			#define BRUSH_TARGETHEIGHT  (_BrushParams[1])
			
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
			Name "Paint Concavity"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment PaintSplatAlphamap

            #define HEIGHT_SAMPLE(__UV__) UnpackHeightmap(tex2D(_Heightmap, __UV__))
            float3 gradient(float2 uv, float epsilon) {
                float upY = max(0.0f, uv.y - epsilon);
                float downY = min(1.0f, uv.y + epsilon);
                float leftX = max(0.0f, uv.x - epsilon);
                float rightX = min(1.0f, uv.x + epsilon);

                //don't need to divide by 8.0f, since we are normalizing
                float dzdx = ((HEIGHT_SAMPLE(float2(rightX, upY)) + 2.0f * HEIGHT_SAMPLE(float2(rightX, uv.y)) + HEIGHT_SAMPLE(float2(rightX, downY))) -
                    (HEIGHT_SAMPLE(uint2(leftX, upY)) + 2.0f * HEIGHT_SAMPLE(uint2(leftX, uv.y)) + HEIGHT_SAMPLE(float2(leftX, downY)))) / 8.0f;

                float dzdy = ((HEIGHT_SAMPLE(float2(leftX, downY)) + 2 * HEIGHT_SAMPLE(uint2(uv.x, downY)) + HEIGHT_SAMPLE(uint2(rightX, downY))) -
                    (HEIGHT_SAMPLE(float2(leftX, upY)) + 2 * HEIGHT_SAMPLE(float2(uv.x, upY)) + HEIGHT_SAMPLE(float2(rightX, upY)))) / 8.0f;

                float mag = length(float2(dzdx, dzdy));
                return float3(dzdx, dzdy, mag);
            }

            float laplacian(float2 uv, float epsilon) {
                float upY = max(0.0f, uv.y - epsilon);
                float downY = min(1.0f, uv.y + epsilon);
                float leftX = max(0, uv.x - epsilon);
                float rightX = min(1.0f, uv.x + epsilon);

                float3 rightGrad = gradient(float2(rightX, uv.y), epsilon);
                float3 leftGrad = gradient(float2(leftX, uv.y), epsilon);
                float3 downGrad = gradient(float2(uv.x, downY), epsilon);
                float3 upGrad = gradient(float2(uv.x, upY), epsilon);
                
                //TODO: better quality by using above sampling from the gradient function
                float dgdx = rightGrad.x / rightGrad.z - leftGrad.x / leftGrad.z;
                float dgdy = downGrad.y / downGrad.z - upGrad.y / upGrad.z;

                return (dgdx + dgdy) / 2.0f;
            }

			float4 _ConcavityRange;
			float GetConcavityScale(float2 uv, float h)
			{
				float epsilon = _ConcavityRange.z * _MainTex_TexelSize.x;
				float concavity = saturate(_ConcavityRange.w * laplacian(uv, epsilon));

				//remap according to curve
				return tex2D(_ConcavityRemap, float2(concavity, 0.0f)).r;
			}

			float ApplyBrush(float height, float brushStrength)
			{
				float targetHeight = 1.0f;
				if (targetHeight > height)
				{
					height += brushStrength;
					height = height < targetHeight ? height : targetHeight;
				}
				else
				{
					height -= brushStrength;
					height = height > targetHeight ? height : targetHeight;
				}
				return height;
			}

			float4 PaintSplatAlphamap(v2f i) : SV_Target
			{
				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float2 normalUV = PaintContextUVToBrushUV(i.pcUV);

				// out of bounds multiplier
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

				float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV.xy));
				//float3 normals = normalize(tex2D(_NormalTex, normalUV).rgb * 2.0f - 1.0f);
				float alphaMap = tex2D(_MainTex, i.pcUV).r;
				float h = UnpackHeightmap(tex2D(_Heightmap, i.pcUV));

				float targetAlpha = GetConcavityScale(i.pcUV, h);

				return lerp(alphaMap, targetAlpha, brushStrength);
			}

			ENDCG
		}
	}
	Fallback Off
}