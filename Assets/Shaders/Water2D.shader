// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShooter/Water" 
{
    Properties 
    {
		[MaterialToggle(_TEX_ON)] _DetailTex ("Enable Detail texture", Float) = 0 	//1
		_MainTex ("Detail", 2D) = "white" {}        								//2
		_ToonShade ("Shade", 2D) = "white" {}  										//3
		[MaterialToggle(_COLOR_ON)] _TintColor ("Enable Color Tint", Float) = 0 	//4
		_Color ("Base Color", Color) = (1,1,1,1)									//5	
		[MaterialToggle(_VCOLOR_ON)] _VertexColor ("Enable Vertex Color", Float) = 0//6        
		_Brightness ("Brightness 1 = neutral", Float) = 1.0							//7	
    }
   
    Subshader 
    {
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
     	LOD 100
     
     	ZWrite Off
     	Blend SrcAlpha OneMinusSrcAlpha
	 	Cull Back
		Lighting Off
		Fog { Mode Off }
		
        Pass 
        {
            Name "BASE"
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
                #pragma glsl_no_auto_normalization
                #pragma multi_compile _TEX_OFF _TEX_ON
                #pragma multi_compile _COLOR_OFF _COLOR_ON
                #pragma multi_compile _VCOLOR_OFF _VCOLOR_ON
                
                #if _TEX_ON
                sampler2D _MainTex;
				half4 _MainTex_ST;
				#endif
				
                struct appdata_base0 
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 texcoord : TEXCOORD0;
                    #if _VCOLOR_ON
                    fixed4 color : COLOR;
                    #endif
				};
				
                 struct v2f 
                 {
                    float4 pos : SV_POSITION;
                    #if _TEX_ON
                    half2 uv : TEXCOORD0;
                    #endif
                    half2 uvn : TEXCOORD1;
                    #if _VCOLOR_ON
                    fixed4 color : COLOR;
                    #endif
                 };
               
                v2f vert (appdata_base0 v)
                {
					const float PI = 3.14159;
                    v2f o;
					o.pos = v.vertex;
					// v.vertex.x and v.vertex.y must not both be 0
					//if (abs(v.vertex.x) > 0.1 || abs(v.vertex.y) > 0.1) {
					float phase = _Time.y + 40 * atan2(v.vertex.y, v.vertex.x);
					float scale = sin(fmod(phase, 2.0*PI)) * 0.25 * sin(fmod(_Time.w, 2.0*PI));
					o.pos.xy += normalize(v.vertex.xy) * scale;
					//}
                    o.pos = UnityObjectToClipPos ( o.pos );
                    float3 n = mul((float3x3)UNITY_MATRIX_IT_MV, normalize(v.normal));
					normalize(n);
                    n = n * float3(0.5,0.5,0.5) + float3(0.5,0.5,0.5);
                    o.uvn = n.xy;
                    #if _TEX_ON
                    o.uv = TRANSFORM_TEX ( v.texcoord, _MainTex );
                    #endif
                    #if _VCOLOR_ON
                    o.color = v.color;
                    #endif
                    return o;
                }

              	sampler2D _ToonShade;
                fixed _Brightness;
                
                #if _COLOR_ON
                fixed4 _Color;
                #endif
                
                fixed4 frag (v2f i) : COLOR
                {
                    fixed4 result = tex2D( _ToonShade, i.uvn );
					#if _COLOR_ON
					result = result*_Color;
					#endif
                    #if _VCOLOR_ON
                    result = result*i.color;
                    #endif
					#if _TEX_ON
                    result = result*tex2D ( _MainTex, i.uv );
					#endif
                    return result*_Brightness;
                }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Diffuse"
}