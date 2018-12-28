Shader "PlanetShooter/Steam Effect" {
Properties {
	_Cutoff ("Alpha cutoff", Range(0,1)) = 0.2
	_Stroke ("Stroke alpha", Range(0,1)) = 0.3
}
SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100

	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Back
	Lighting Off
	Fog { Mode Off }

	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "SimplexNoise3D.cginc"

			uniform sampler2D Steammap_RT;
			uniform sampler2D Lightmap_RT;

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				float2 position: TEXCOORD1;
				float4 screenPos : TEXCOORD3;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed _Cutoff;
			fixed _Stroke;

			v2f vert (appdata_t v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.position = v.vertex;
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				fixed v = tex2Dproj(Steammap_RT, i.screenPos);
				fixed4 col;
				float n = snoise(float3(i.position * 128, _Time.z));
				fixed w = max(0.001, _Stroke - _Cutoff);
				col.a = clamp(v * lerp(1, n, 0.2) - _Cutoff, 0, w) / w * (0.9 + 0.1 * n);
				col.rgb = tex2Dproj(Lightmap_RT, i.screenPos).rgb;

				return col;
			}
		ENDCG
	}
}

}