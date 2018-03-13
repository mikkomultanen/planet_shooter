Shader "PlanetShooter/Shield2D" {
	Properties 
	{
		_Color("Color", Color) = (1,1,1,1)
		_RimColor("RimColor", Color) = (1, 1, 1, 1)
		_RimPower("RimPower", Range(0.0001, 4)) = 1 
		_RimIntensity("RimIntensity", Float) = 1
		_Speed("Speed", Float) = 1
		_MainTex ("Main Texture", 2D) = "white" {}
	
	}
	SubShader
	{ 
		Tags{"Queue" = "Transparent"}
		
		Pass 
		{
			LOD 200
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
		
			fixed4 _Color, _RimColor;
			float _RimPower, _RimIntensity, _Speed;
			sampler2D _MainTex;			

			struct appdata_t 
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f 
			{
				float4 vertex : SV_POSITION;
				float3 normal : TEXCOORD3;
				float2 texcoord : TEXCOORD0;
			};
			
			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = mul(fixed4(v.normal, 0.0), unity_WorldToObject);
				o.texcoord = v.texcoord + half2(1, 1) * _Speed * _Time;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float3 viewdir = float3(0.0, 0.0, 1.0);
				float ang = 1 - (abs(dot(viewdir, normalize(i.normal))));
				half4 rimCol = _RimColor * pow(ang, _RimPower) * _RimIntensity;

				half4 texColor = tex2D(_MainTex, i.texcoord);

				return rimCol * texColor;
			}
			ENDCG
		}
	}
}