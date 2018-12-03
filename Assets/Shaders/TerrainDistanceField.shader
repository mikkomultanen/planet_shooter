// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/PlanetShooter/DistanceField" {
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

            //Fragment Shader
            float frag (v2f_img i) : COLOR {
                return 1;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}