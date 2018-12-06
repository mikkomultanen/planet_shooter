// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShooter/DistanceFieldDebug" {
    Properties 
    {
		_MainTex ("Detail", 2D) = "white" {}
    }

    SubShader {
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

			sampler2D _MainTex;

            //Fragment Shader
            float4 frag (v2f_img i) : COLOR {
                float3 value = tex2D (_MainTex, i.uv);
                return float4(value.yz, 0.5, (step(0.5, value.x) - step(1, value.x)) * (1 - value.x) * 0.2);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}