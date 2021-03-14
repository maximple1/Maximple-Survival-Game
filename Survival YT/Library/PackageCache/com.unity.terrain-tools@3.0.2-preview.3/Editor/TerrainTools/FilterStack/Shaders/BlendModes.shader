Shader "Hidden/TerrainTools/BlendModes"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
    }

    SubShader
    {
        ZTest Always Cull Off ZWrite Off

        HLSLINCLUDE

        #include "UnityCG.cginc"
        #include "Packages/com.unity.terrain-tools/Shaders/TerrainTools.hlsl"

        sampler2D _MainTex;
		sampler2D _BlendTex;
        float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height
        float4 _BlendTex_TexelSize;

		float4 _BlendParams;
		#define MAIN_TEX_ROT			_BlendParams[2]

        struct appdata_s
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f_s
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        v2f_s vert( appdata_s v )
        {
            v2f_s o;

            o.vertex = UnityObjectToClipPos( v.vertex );
            o.uv = v.texcoord;

            return o;
        }

		inline float3 RotateUVs(float2 sourceUV, float rotAngle)
		{
			float4 rotAxes;
			rotAxes.x = cos(rotAngle);
			rotAxes.y = sin(rotAngle);
			rotAxes.w = rotAxes.x;
			rotAxes.z = -rotAxes.y;

			float2 tempUV = sourceUV - float2(0.5, 0.5);
			float3 retVal;

			// We fix some flaws by setting zero-value to out of range UVs, so what we do here
			// is test if we're out of range and store the mask in the third component.
			retVal.xy = float2(dot(rotAxes.xy, tempUV), dot(rotAxes.zw, tempUV)) + float2(0.5, 0.5);
			tempUV = clamp(retVal.xy, float2(0.0, 0.0), float2(1.0, 1.0));
			retVal.z = ((tempUV.x == retVal.x) && (tempUV.y == retVal.y)) ? 1.0 : 0.0;
			return retVal;
		}

        ENDHLSL

        Pass // 0 - Multiply
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            float4 frag( v2f_s i ) : SV_Target
            {
				float2 pcUVRescale = 1.0f / float2(length(_PCUVToBrushUVScales.xy), length(_PCUVToBrushUVScales.zw));
				float2 scaledUV = (i.uv - (.5).xx) * pcUVRescale + (.5).xx + (.5).xx * _MainTex_TexelSize.xy;
				float4 a = tex2D(_MainTex, scaledUV);

				float3 blendTexUVs = RotateUVs(i.uv, MAIN_TEX_ROT);
				float4 b = tex2D(_BlendTex, blendTexUVs.z * blendTexUVs); 
                
				a = a * b;

                return float4( a.rgb, 1 );
            }

            ENDHLSL
        }

        Pass // 0 - Multiply
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            float4 frag( v2f_s i ) : SV_Target
            {
                float2 mainUV = i.uv;
				float4 a = tex2D(_MainTex, mainUV);
				float4 b = tex2D(_BlendTex, mainUV); 
                
				a = a * b;

                return float4( a.rgb, 1 );
            }

            ENDHLSL
        }
    }
}