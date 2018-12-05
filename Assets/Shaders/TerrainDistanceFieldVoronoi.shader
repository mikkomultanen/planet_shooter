// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/PlanetShooter/Voronoi" {
    Properties 
    {
		_MainTex ("Detail", 2D) = "white" {}
        _Offset ("Offset", Int) = 0
        _DistanceScale ("Distance Scale", Float) = 1.0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
	   	Cull Back
		Lighting Off
        ZWrite Off
        ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

			sampler2D _MainTex;

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

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

			sampler2D _MainTex;
            uniform float4 _MainTex_TexelSize;
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

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

			sampler2D _MainTex;
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
    }
    FallBack "Diffuse"
}