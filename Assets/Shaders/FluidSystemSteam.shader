Shader "Unlit/Instanced Steam"
{
	Properties {
		_MainTex ("Particle Texture", 2D) = "white" {}
		_Scale ("Particle Scale", Float) = 1.0
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend OneMinusDstColor One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma target 5.0
			
			#include "UnityCG.cginc"
			#include "ParticleCommon.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};
			
			StructuredBuffer<Particle> _Particles;
			StructuredBuffer<uint> _Alive;
			float _Scale;
			float _Demultiplier;
			
			v2f vert (appdata v, uint instanceID : SV_InstanceID)
			{
				uint idx = _Alive[instanceID];
				Particle p = _Particles[idx];

				float l = 1 - p.life.x / p.life.y;
				
				float s = l * l;
				s *= s;
				s = 1 - 0.8 * s;
				
				v.vertex.xy *= s * _Scale;
				v.vertex.xy += _Demultiplier * p.position;

				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);
				o.color = float4(l, l, l, 1);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv) * i.color;
			}
			ENDCG
		}
	}
}