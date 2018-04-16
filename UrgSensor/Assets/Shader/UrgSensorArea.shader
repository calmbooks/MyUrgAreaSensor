Shader "Custom/UrgSensorArea" {

	Properties {
		_MainTex ("Albedo (rgba)", 2D) = "white" {}
		_WidthPx ("Width", Int) = 1000
		_HeightPx ("Height", Int) = 1000
        _AREA_WIDTH ("AREA_WIDTH", Int) = 500
        _AREA_HEIGHT ("AREA_HEIGHT", Int) = 500
        _AREA_OFFSET_X ("AREA_OFFSET_X", Int) = 0
        _AREA_OFFSET_Y ("AREA_OFFSET_Y", Int) = 0
    }

    SubShader {
        Tags { "RenderType" = "Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        

        Pass {
            CGPROGRAM

                #include "UnityCG.cginc"

                #pragma vertex vert_img
                #pragma fragment frag

                int _pointCount = 500;
                float _points_x[500];
                float _points_y[500];

                uniform sampler2D _MainTex;                
                uniform int _WidthPx;
                uniform int _HeightPx;
                uniform float _AREA_WIDTH;
                uniform float _AREA_HEIGHT;
                uniform float _AREA_OFFSET_X;
                uniform float _AREA_OFFSET_Y;

                void drawLine( float2 startPos, float2 endPos, float weight, float3 color, float2 uv, inout float3 dest ) { 
                    float lenA = length(uv - startPos);
                    float lenB = length(uv - endPos);
                    float lenC = length(startPos - endPos);
                    float lenD = sqrt(pow(lenA, 2.0) - pow(weight, 2.0)) + sqrt(pow(lenB, 2.0) - pow(weight, 2.0));
                    if( min(lenA, lenB) < weight ) {
                        dest = color;
                    } else if( lenD < lenC ) {
                        dest = color;
                    }
                }

                void drawRect( float x, float y, float w, float h, float2 uv, inout float3 dest ) {
                    drawLine( float2(x,y), float2(x+w,y), 0.001, float3(1.0,1.0,0.0), uv, dest );
                    drawLine( float2(x+w,y), float2(x+w,y+h), 0.001, float3(1.0,1.0,0.0), uv, dest );
                    drawLine( float2(x+w,y+h), float2(x,y+h), 0.001, float3(1.0,1.0,0.0), uv, dest );
                    drawLine( float2(x,y+h), float2(x,y), 0.001, float3(1.0,1.0,0.0), uv, dest );
                }

                void drawCircle( float2 pos, float radius, float3 color, float2 uv, inout float3 dest ) {

                    float l = length(pos - uv);
                    if( l < radius ) {
                        dest = color;
                    }
                }

                #define PI 3.14159

                float4 frag( v2f_img i ) : COLOR {

                    float3 dest = tex2D(_MainTex, i.uv.xy);

                    for (int j = 0; j < _pointCount; j++) {

                        float x = 0.5+(_points_x[j]/-_WidthPx);
                        float y = 0.5+(_points_y[j]/-_HeightPx);

                        if( x <= 1.0 ) {
                            drawCircle( float2(x,y), 0.01, float3(1.0,0.0,0.0), i.uv, dest );
                        }
                    }

                    float w = _AREA_WIDTH / _WidthPx;
                    float h = _AREA_HEIGHT / _HeightPx;
                    float x = 0.5 - ( w / 2) - (_AREA_OFFSET_X / _WidthPx);
                    float y = 0.5 - ( h / 2) - (_AREA_OFFSET_Y / _HeightPx);

                    drawRect(x,y,w,h,i.uv,dest);

                    /*
                    float2 vec = i.uv.xy - float2(0.5, 0.5);

                    float l = length(vec);
                    float r = atan2(vec.y, vec.x) + PI; // 0-2π
                    float t = _Time.y*10;
                    float c = 1-sin(l*70+r+t);

                    float3 rgb = float3(c,c,c);
                    */

                    return float4(dest,1.0);
                }

            ENDCG
        }
    }
}
