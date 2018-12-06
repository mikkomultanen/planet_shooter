// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/PlanetShooter/Voronoi" {
    Properties 
    {
		_MainTex ("Detail", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    half4 Sample (float2 uv) {
        return tex2D(_MainTex, uv);
    }

    half4 SampleBox (float2 uv, float delta) {
        float4 o = _MainTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
        half4 s =
            Sample(uv + o.xy) + Sample(uv + o.zy) +
            Sample(uv + o.xw) + Sample(uv + o.zw);
        return s * 0.25f;
    }

    ENDCG

    SubShader {
        Tags { "RenderType"="Opaque" }
	   	Cull Back
		Lighting Off
        ZWrite Off
        ZTest Always

        Pass { // 0 init position
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            //Fragment Shader
            half2 frag (v2f_img i) : COLOR {
                fixed value = tex2D (_MainTex, i.uv);
                if (value == 0) {
                    return half2(0,0);
                } else {
                    return i.uv;
                }
            }
            ENDCG
        }

        Pass { // 1 JFA step
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            int _Offset;

            //Fragment Shader
            half2 frag (v2f_img i) : COLOR {
                float stepwidth = _MainTex_TexelSize.xy * _Offset;
    
                float bestDistance = 9999.0;
                half2 bestCoord = half2(0, 0);
                
                for (int y = -1; y <= 1; ++y) {
                    for (int x = -1; x <= 1; ++x) {
                        float2 sampleCoord = i.uv + float2(x,y) * stepwidth;
                        
                        half2 seedCoord = tex2D (_MainTex, sampleCoord);
                        if (seedCoord.x > 0 && seedCoord.y > 0)
                        {
                            float2 v = seedCoord - i.uv;
                            float distsq = dot(v, v);
                            if (distsq < bestDistance) {
                                bestDistance = distsq;
                                bestCoord = seedCoord;
                            }
                        }
                    }
                }

                return bestCoord;
            }
            ENDCG
        }

        Pass { // 2 calculate distance
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            float _DistanceScale;

            //Fragment Shader
            fixed frag (v2f_img i) : COLOR {
                float2 bestCoord = tex2D (_MainTex, i.uv);
                if (bestCoord.x > 0 && bestCoord.y > 0) {
                    return length(bestCoord - i.uv) * _DistanceScale;
                } else {
                    return 1;
                }
            }
            ENDCG
        }

        Pass { // 3 box filter
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            float _BoxOffset;

            //Fragment Shader
            half4 frag (v2f_img i) : COLOR {
                return SampleBox(i.uv, _BoxOffset);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}