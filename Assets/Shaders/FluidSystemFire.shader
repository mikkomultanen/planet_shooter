Shader "Unlit/Instanced Fire"
{
	Properties {
		_MainTex ("Particle Texture", 2D) = "white" {}
		_Scale ("Particle Scale", Float) = 1.0
		[HDR]_StartColor ("Start Color", Color) = (1,1,0,1)
		[HDR]_EndColor ("End Color", Color) = (1,0,0,1)
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend SrcAlpha One

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
			float4 _StartColor;
			float4 _EndColor;
			float _Demultiplier;
			
			v2f vert (appdata v, uint instanceID : SV_InstanceID)
			{
				uint idx = _Alive[instanceID];
				Particle p = _Particles[idx];

				float t = p.life.x / p.life.y;
				float l = 1 - t;
				l *= l;
				l *= l;
				float s = 0.2 + 0.8 * smoothstep(0, 0.9, t) * (1 - l);
				
				v.vertex.xy *= s * _Scale;
				v.vertex.xy += _Demultiplier * p.position;

				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);
				o.color = lerp(_StartColor, _EndColor, t);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv) * i.color;
			}
			ENDCG
		}
	}
}