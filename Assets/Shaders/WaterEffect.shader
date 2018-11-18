Shader "PlanetShooter/Water Effect" {
Properties {
	_Color ("Main color", Color) = (0,0,1,0.5)

	_Cutoff ("Alpha cutoff", Range(0,1)) = 0.2

	_Stroke ("Stroke alpha", Range(0,1)) = 0.3
	_StrokeColor ("Stroke color", Color) = (1,1,1,0.5)
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

			uniform sampler2D Watermap_RT;
			uniform sampler2D Terrain_RT;

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				float4 screenPos : TEXCOORD3;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed _Cutoff;

			half4 _Color;

			fixed _Stroke;
			half4 _StrokeColor;

			v2f vert (appdata_t v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				fixed4 col = tex2Dproj(Watermap_RT, i.screenPos);
				if (col.r < _Cutoff) {
					col.a = 0;
				} else if (col.r < _Stroke) {
					col = _StrokeColor;
				} else {
					col = _Color;
				}

				col.rgb = (col * tex2Dproj(Terrain_RT, i.screenPos)).rgb;

				return col;
			}
		ENDCG
	}
}

}