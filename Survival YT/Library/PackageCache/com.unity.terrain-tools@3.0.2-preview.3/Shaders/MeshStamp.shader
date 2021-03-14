Shader "Hidden/TerrainTools/MeshStamp"
{
    Properties
    {
        _MainTex ( "Texture", any ) = "" {}
    }

    SubShader
    {
        ZTest ALWAYS Cull OFF ZWrite OFF

        Pass // composite
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.unity.terrain-tools/Shaders/TerrainTools.hlsl"

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _FilterTex;
            sampler2D _MeshMaskTex;
            sampler2D _MeshStampTex;

            float4 _BrushParams;
            float _TerrainHeight;

            // _BrushParams macros
            #define BRUSH_STRENGTH      ( _BrushParams[ 0 ] )
            #define BLEND_AMOUNT        ( _BrushParams[ 1 ] )
            #define BRUSH_HEIGHT        ( _BrushParams[ 2 ] )
            #define TOOL_HEIGHT         ( _BrushParams[ 3 ] )

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 pcUV : TEXCOORD0;
            };

            float SmoothMax(float a, float b, float p)
            {
                // calculates a smooth maximum of a and b, using an intersection power p
                // higher powers produce sharper intersections, approaching max()
                return log2(exp2(a * p) + exp2(b * p) - 1.0f) / p;
            }

            v2f vert( appdata_t v )
            {
                v2f o;
                
                o.vertex = UnityObjectToClipPos( v.vertex );
                o.pcUV = v.texcoord;

                return o;
            }

            float4 frag( v2f i ) : SV_Target
            {
                // if (BRUSH_MAXBLENDADD > 0.0f)
                // {
                //     float brushIntersection = saturate(1.0f - BRUSH_MAXBLENDADD);
                //     float brushSmooth = exp2(brushIntersection * 8.0f);
                //     targetHeight = SmoothMax(height, brushHeight, brushSmooth);
                // }
                // else
                // {
                //     targetHeight = max(height, brushHeight);
                // }

                //////////////////////////////////////////////////////////////////////////////

                // get filter value
                float filter = tex2D( _FilterTex, i.pcUV ).r;
                
                // get current height value
                float h = UnpackHeightmap( tex2D( _MainTex, i.pcUV ) );

                float2 brushUV = PaintContextUVToBrushUV( i.pcUV );
                
                // out of bounds multiplier
                float oob = all( saturate( brushUV ) == brushUV ) ? 1 : 0;

                // get brush mask value
                float isMask = oob * max( 0, floor( tex2D( _MeshMaskTex, brushUV ).r ) ); // floor is for filtering
                float stampHeight = tex2D( _MeshStampTex, brushUV ).r;
                float strengthSign = sign( BRUSH_STRENGTH );
                float brushStrength = abs( BRUSH_STRENGTH );
                float toolHeight = TOOL_HEIGHT * abs( strengthSign ) * isMask;
                float brushMask = oob * ( stampHeight - toolHeight ) * brushStrength * filter;
                float brushHeight = BRUSH_HEIGHT * isMask * abs( strengthSign );

                float maxH = max( h, lerp( h, brushMask + brushHeight + toolHeight, isMask ) );
                float addMaxH = max( h, h + brushMask * isMask + toolHeight );
                float finalAdd = lerp( maxH, addMaxH, BLEND_AMOUNT );

                float minH = min( h, lerp( h, brushHeight - ( toolHeight + brushMask ), isMask * abs( strengthSign ) ) );
                float subMinH = min( h, h - ( brushMask * isMask + toolHeight ) );
                float finalSub = lerp( minH, subMinH, BLEND_AMOUNT );

                float isAdd = max( strengthSign, 0 ); // remap -1 and 1 to 0 and 1

                float ret = lerp( finalSub, finalAdd, isAdd );
                // ret = minH;

                return PackHeightmap( clamp( ret, 0, .5 ) );
            }

            ENDHLSL
        }
        
    }
}