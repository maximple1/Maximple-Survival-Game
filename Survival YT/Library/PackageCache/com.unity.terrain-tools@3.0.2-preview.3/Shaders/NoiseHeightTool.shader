Shader "Hidden/TerrainTools/NoiseHeightTool"
{
    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader
    {
        ZTest Always Cull OFF ZWrite Off

        HLSLINCLUDE

        #include "UnityCG.cginc"
        #include "Packages/com.unity.terrain-tools/Shaders/TerrainTools.hlsl"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

        float2 _WorldHeightRemap;
        
        sampler2D _NoiseTex;
        float4 _NoiseTex_TexelSize;

        sampler2D _BrushTex;
        float4 _BrushParams;            // x = strength, y = , z = , w = brushSize

		sampler2D _FilterTex;

        // _BrushParams macros
        #define BRUSH_STRENGTH      ( _BrushParams[0] )
        #define BRUSH_SIZE          ( _BrushParams[2] )
        #define INV_BRUSH_SIZE      ( _BrushParams[3] )

        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 pcUV : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 pcUV : TEXCOORD0;
        };

        v2f vert( appdata_t v )
        {
            v2f o;
            
            o.vertex = UnityObjectToClipPos( v.vertex );
            o.pcUV = v.pcUV;

            return o;
        }

        ENDHLSL

        Pass
        {
            Name "Apply Noise to Heightmap"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float4 frag( v2f i ) : SV_Target
            {
                // get current height value
                float h = UnpackHeightmap( tex2D( _MainTex, i.pcUV ) );

                float2 brushUV = PaintContextUVToBrushUV( i.pcUV );
                // out of bounds multiplier
                float oob = all( saturate( brushUV ) == brushUV ) ? 1 : 0;

                // get brush mask value
                float b = oob * UnpackHeightmap( tex2D( _BrushTex, brushUV ) ) * UnpackHeightmap(tex2D(_FilterTex, i.pcUV));

                // need to adjust uvs due to "scaling" from brush rotation
                float2 pcUVRescale = float2( length( _PCUVToBrushUVScales.xy ), length( _PCUVToBrushUVScales.zw ) );

                // get noise mask value
                float2 noiseUV = ( i.pcUV - ( .5 ).xx ) * pcUVRescale + ( .5 ).xx + ( .5 ).xx * _NoiseTex_TexelSize.xy;
                float n = UnpackHeightmap( tex2D( _NoiseTex, noiseUV ) );
                
                // TODO(wyatt): remap noise values to match _WorldHeightRemap

                return PackHeightmap( clamp( h + BRUSH_STRENGTH * b * n, 0, 0.5 ) );
            }

            ENDHLSL
        }
    }
}