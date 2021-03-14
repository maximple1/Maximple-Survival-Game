Shader "Hidden/TerrainTools/CloneBrush"
{
    Properties
    {
        _MainTex ("Texture", any) = "" {}
    }

        SubShader
    {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
		    #include "Packages/com.unity.terrain-tools/Shaders/TerrainTools.hlsl"

            float4 _SampleUVScaleOffset;

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;
			sampler2D _FilterTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH      (_BrushParams[0])
            #define BRUSH_TARGETHEIGHT  (_BrushParams[1])
            #define BRUSH_STAMPHEIGHT   (_BrushParams[2])
            #define BRUSH_ROTATION      (_BrushParams[3])

            struct appdata_t {
                float4 vertex : POSITION;
                float2 pcUV : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 pcUV : TEXCOORD0;
                float2 sampleUV: TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pcUV = v.pcUV;
                o.sampleUV = v.pcUV.xy * _SampleUVScaleOffset.xy +_SampleUVScaleOffset.zw;
                return o;
            }

            float SmoothApply(float height, float brushStrength, float targetHeight)
            {
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

        ENDCG


        Pass    // 0 clone stamp tool (alphaMap)
        {
            Name "Clone Stamp Tool Alphamap"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment CloneAlphamap

            sampler2D _CloneTex;

            float4 CloneAlphamap(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV( i.pcUV );
                // out of bounds multiplier
                float oob = all( saturate( brushUV ) == brushUV ) ? 1 : 0;

                // get brush mask value
                float b = oob * UnpackHeightmap( tex2D( _BrushTex, brushUV ) ) * UnpackHeightmap(tex2D(_FilterTex, i.pcUV));

                float currentAlpha = tex2D(_MainTex, i.pcUV).r;
                float sampleAlpha = tex2D(_CloneTex, i.sampleUV).r;

                return saturate(lerp(currentAlpha, sampleAlpha, saturate(BRUSH_STRENGTH * b)));
            }
            ENDCG
        }

        Pass    // 1 clone stamp tool (heightmap)
        {
            Name "Clone Stamp Tool Heightmap"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment CloneHeightmap

            sampler2D _CloneTex;
            float4 _CloneTex_TexelSize;

            #define HeightOffset     ( _BrushParams[ 1 ] )
            #define TerrainMaxHeight ( _BrushParams[ 2 ] )

            float4 CloneHeightmap( v2f i ) : SV_Target
            {
                // get current height value
                float h = UnpackHeightmap( tex2D( _MainTex, i.pcUV ) );

                float2 brushUV = PaintContextUVToBrushUV( i.pcUV );
                // out of bounds multiplier
                float oob = all( saturate( brushUV ) == brushUV ) ? 1 : 0;

                // get brush mask value
                float b = oob * UnpackHeightmap( tex2D( _BrushTex, brushUV ) ) * UnpackHeightmap(tex2D(_FilterTex, i.pcUV));

                // get the height value to clone
                float sampleHeight = UnpackHeightmap( tex2D( _CloneTex, i.sampleUV ) ) + ( HeightOffset / TerrainMaxHeight );

                // blend the current height and the height to clone based on brush strength and mask
                return PackHeightmap( clamp( lerp( h, sampleHeight, saturate(BRUSH_STRENGTH * b)), 0.0f, 0.5f ) );
            }
            ENDCG
        }
    }
    Fallback Off
}
