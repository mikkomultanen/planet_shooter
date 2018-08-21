// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShooter/Mesh" 
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
        [KeywordEnum(None, Add, Multiply)] _Shadow("Shadow Blending", Float) = 0    //6
    }
   
    Subshader 
    {
    	Tags { "RenderType"="Opaque" }
		LOD 250
    	ZWrite On
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
                #pragma multi_compile _SHADOW_NONE, _SHADOW_ADD, _SHADOW_MULTIPLY

                
                #if _TEX_ON
                sampler2D _MainTex;
				half4 _MainTex_ST;
				#endif
                uniform sampler2D Lightmap_RT;
				
                struct appdata_base0 
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 texcoord : TEXCOORD0;
				};
				
                 struct v2f 
                 {
                    float4 pos : SV_POSITION;
                    #if _TEX_ON
                    half2 uv : TEXCOORD0;
                    #endif
                    half2 uvn : TEXCOORD1;
                    #if _SHADOW_NONE
                    #else
                    float4 screenPos : TEXCOORD2;
                    #endif
                 };
               
                v2f vert (appdata_base0 v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos ( v.vertex );
                    float3 n = normalize(mul(UNITY_MATRIX_IT_MV, v.normal.xyzz).xyz);
                    n = n * float3(0.5,0.5,0.5) + float3(0.5,0.5,0.5);
                    o.uvn = n.xy;
                    #if _TEX_ON
                    o.uv = TRANSFORM_TEX ( v.texcoord, _MainTex );
                    #endif
                    #if _SHADOW_NONE
                    #else
                    o.screenPos = ComputeScreenPos(o.pos);
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
					result *= _Color;
					#endif
					
					#if _TEX_ON
					result *= tex2D ( _MainTex, i.uv );
					#endif

					result *= _Brightness;

                    #if _SHADOW_ADD
                    result.rgb = (result + tex2Dproj(Lightmap_RT, i.screenPos)).rgb;
                    #endif
                    #if _SHADOW_MULTIPLY
                    result.rgb = (result * tex2Dproj(Lightmap_RT, i.screenPos)).rgb;
                    #endif

                    return result;
                }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Diffuse"
}