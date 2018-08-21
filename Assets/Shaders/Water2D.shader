// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShooter/Water" 
{
    Properties 
    {
		[MaterialToggle(_TEX_ON)] _DetailTex ("Enable Detail texture", Float) = 0 	//1
		_MainTex ("Detail", 2D) = "white" {}        								//2
		_ToonShade ("Shade", 2D) = "white" {}  										//3
		[MaterialToggle(_VCOLOR_ON)] _VertexColor ("Enable Vertex Color", Float) = 0//4        
		_Brightness ("Brightness 1 = neutral", Float) = 1.0							//5	
        [KeywordEnum(None, Add, Multiply)] _Shadow("Shadow Blending", Float) = 0    //6
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
                #include "HSL.cginc"
                #pragma glsl_no_auto_normalization
                #pragma multi_compile _TEX_OFF _TEX_ON
                #pragma multi_compile _VCOLOR_OFF _VCOLOR_ON
                #pragma multi_compile _SHADOW_NONE, _SHADOW_ADD, _SHADOW_MULTIPLY
                
                #if _TEX_ON
                sampler2D _MainTex;
				half4 _MainTex_ST;
				#endif
                uniform sampler2D Terrain_RT;
				
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
                    float2 uv : TEXCOORD0;
                    #endif
                    half2 uvn : TEXCOORD1;
                    #if _VCOLOR_ON
                    fixed4 color : COLOR;
                    #endif
                    #if _SHADOW_NONE
                    #else
                    float4 screenPos : TEXCOORD2;
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
                    o.uv = TRANSFORM_TEX ( v.texcoord, _MainTex ) + half2(-1, -1) * _Time.x;
                    #endif
                    #if _VCOLOR_ON
                    o.color = v.color;
                    #endif
                    #if _SHADOW_NONE
                    #else
                    o.screenPos = ComputeScreenPos(o.pos);
                    #endif
                    return o;
                }

              	sampler2D _ToonShade;
                fixed _Brightness;
                
                fixed4 frag (v2f i) : COLOR
                {
                    half4 result = tex2D( _ToonShade, i.uvn )*_Brightness;
					#if _TEX_ON
                    result = result * tex2D ( _MainTex, i.uv );
					#endif

                    #if _VCOLOR_ON
                    float3 hsl = rgb2hsl(result.rgb);
                    float3 hslColor = rgb2hsl(i.color.rgb);
                    hsl.r = hslColor.r;
                    hsl.g = hslColor.g;
                    hsl.b *= hslColor.b;
					result.rgb = hsl2rgb(hsl);
                    result.w *= i.color.w;
                    #endif

                    #if _SHADOW_ADD
                    result.rgb = (result + tex2Dproj(Terrain_RT, i.screenPos)).rgb;
                    #endif
                    #if _SHADOW_MULTIPLY
                    result.rgb = (result * tex2Dproj(Terrain_RT, i.screenPos)).rgb;
                    #endif

                    return result;
                }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Diffuse"
}