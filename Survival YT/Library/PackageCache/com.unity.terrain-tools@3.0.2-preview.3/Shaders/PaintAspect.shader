	Shader "Hidden/TerrainTools/PaintAspect" {

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
			sampler2D _AspectRemapTex;

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
			Name "Paint Aspect"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment PaintSplatAlphamap

			#define HEIGHT_SAMPLE(__UV__) UnpackHeightmap(tex2D(_Heightmap, __UV__))
			float2 computeNormal(float2 uv, float epsilon)
			{
				float left = uv.x - epsilon;
				float right = uv.x + epsilon;
				float up = uv.y - epsilon;
				float down = uv.y + epsilon;

				float dzdx = ((HEIGHT_SAMPLE(float2(right, down)) + 2.0f * HEIGHT_SAMPLE(float2(right, uv.y)) + HEIGHT_SAMPLE(float2(right, up))) -
							  (HEIGHT_SAMPLE(float2(left, down)) + 2.0f * HEIGHT_SAMPLE(float2(left, uv.y)) + HEIGHT_SAMPLE(float2(left, up)))) / 8.0f;

				float dzdy = ((HEIGHT_SAMPLE(float2(left, up)) + 2.0f * HEIGHT_SAMPLE(float2(uv.x, up)) + HEIGHT_SAMPLE(float2(right, up))) -
							  (HEIGHT_SAMPLE(float2(left, down)) + 2.0f * HEIGHT_SAMPLE(float2(uv.x, down)) + HEIGHT_SAMPLE(float2(right, down)))) / 8.0f;

				return normalize(float2(dzdx, dzdy));
			}

			float4 _AspectValues;
			float GetAspectScale(float2 uv, float h)
			{
				float epsilon = _AspectValues[2] * _MainTex_TexelSize;
				float2 n = computeNormal(uv, epsilon);
				float aspect = saturate(dot(n, _AspectValues.xy));

				return tex2D(_AspectRemapTex, float2(aspect, 0.0f)).r;
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
				float alphaMap = tex2D(_MainTex, i.pcUV).r;
				float h = UnpackHeightmap(tex2D(_Heightmap, i.pcUV));

				float targetAlpha = GetAspectScale(i.pcUV, h);

				return lerp(alphaMap, targetAlpha, brushStrength);
			}

			ENDCG
		}
	}
	Fallback Off
}