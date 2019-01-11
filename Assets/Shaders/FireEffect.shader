Shader "PlanetShooter/Fire Effect" {
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

			uniform sampler2D Firemap_RT;

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

			v2f vert (appdata_t v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.position = v.vertex;
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			float fractalNoise(float2 position) {
				float o = 0;
				float w = 0.5;
				float s = 1;
				for (int i = 0; i < 3; i++) {
					float3 coord = float3(position * s, _Time.w);
					float n = 1 - abs(snoise(coord));
					n *= n;
					o += n * w;
					s *= 2.0;
            		w *= 0.5;
				}
				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
				fixed4 col = tex2Dproj(Firemap_RT, i.screenPos);
				float n = fractalNoise(i.position * 128);
				col.rgb = saturate(2 * n * col.rgb) * 2;
				col.a = saturate(col.a);

				return col;
			}
		ENDCG
	}
}

}