Shader "Unlit/TerrainTest"
{
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Iterations("Iterations", Range(1, 10)) = 3
		_Threshold("Threshold", Range(0, 1)) = 0.5
		_ThresholdAmplitude("Threshold Amplitude", Range(0, 1)) = 0.1
		_Min("Min", Range(0, 1)) = 0.25
		_Max("Max", Range(0, 1)) = 1
		_Seed("Seed", Float) = 1
	}

	SubShader
	{
    	Tags { "RenderType"="Opaque" }
		LOD 250
    	ZWrite On
	   	Cull Back
		Lighting Off
		Fog { Mode Off }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma target 5.0
			
			#include "UnityCG.cginc"
			#include "SimplexNoise3D.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float _Iterations;
			float _Threshold;
			float _ThresholdAmplitude;
			float _Min;
			float _Max;
			float _Seed;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			v2f vert (appdata v, uint instanceID : SV_InstanceID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);
				return o;
			}
			
			float fractalNoise(float2 position) {
				float o = 0;
				float w = 0.5;
				float s = 1;
				for (int i = 0; i < _Iterations; i++) {
					float3 coord = float3(position * s, _Seed);
					float n = abs(snoise(coord));
					n = 1 - n;
					//n *= n;
					//n *= n;
					o += n * w;
					s *= 2.0;
            		w *= 0.5;
				}
				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
				float2 r = 2 * (i.uv / _MainTex_ST.xy) - 1;
				float d = length(r);
				float n = fractalNoise(i.uv);
				float a = smoothstep(_Min, _Min + 0.1, d) * (1 - smoothstep(_Max - 0.1, _Max, d));
				float v = snoise(float3(i.uv, _Seed + 1));
				float b = step(_Threshold + _ThresholdAmplitude * v, n * a);
				
				return float4(b,n,0.5 * v + 0.5,1);
			}
			ENDCG
		}
	}
}