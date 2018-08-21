// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShooter/Terrain2D" 
{
    Properties 
    {
		_MainTex ("Detail", 2D) = "white" {}        								//1
		_OverlayTex ("Overlay", 2D) = "white" {}  									//2
		[MaterialToggle(_COLOR_ON)] _TintColor ("Enable Color Tint", Float) = 0 	//3
		_Color ("Base Color", Color) = (1,1,1,1)									//4
		[MaterialToggle(_VCOLOR_ON)] _VertexColor ("Enable Vertex Color", Float) = 0//5
		_Brightness ("Brightness 1 = neutral", Float) = 1.0							//6
        [KeywordEnum(None, Add, Multiply)] _Shadow("Shadow Blending", Float) = 0    //7
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
                #pragma multi_compile _VCOLOR_OFF _VCOLOR_ON
                #pragma multi_compile _SHADOW_NONE, _SHADOW_ADD, _SHADOW_MULTIPLY

                
                sampler2D _MainTex;
				half4 _MainTex_ST;
				sampler2D _OverlayTex;
				half4 _OverlayTex_ST;
                uniform sampler2D Terrain_RT;

                struct appdata_base0 
				{
					float4 vertex : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 overlaycoord : TEXCOORD1;
                    #if _VCOLOR_ON
                    fixed4 color : COLOR;
                    #endif
				};
				
                 struct v2f 
                 {
                    float4 pos : SV_POSITION;
                    half2 uv : TEXCOORD0;
                    half2 uvo : TEXCOORD1;
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
                    v2f o;
                    o.pos = UnityObjectToClipPos ( v.vertex );
                    o.uv = TRANSFORM_TEX ( v.texcoord, _MainTex );
					o.uvo = TRANSFORM_TEX (v.overlaycoord, _OverlayTex );
                    #if _VCOLOR_ON
                    o.color = v.color;
                    #endif
                    #if _SHADOW_NONE
                    #else
                    o.screenPos = ComputeScreenPos(o.pos);
                    #endif
                    return o;
                }

                fixed _Brightness;
                
                #if _COLOR_ON
                fixed4 _Color;
                #endif
                
                fixed4 frag (v2f i) : COLOR
                {

					fixed4 overlay = tex2D( _OverlayTex, i.uvo );
                    fixed4 detail = tex2D ( _MainTex, i.uv )*_Brightness;
					#if _COLOR_ON
					detail = detail*_Color;
					#endif
                    #if _VCOLOR_ON
                    detail = detail*i.color;
                    #endif
					detail = float4(lerp(detail.rgb, overlay.rgb, overlay.a), 1);
                    #if _SHADOW_ADD
                    detail.rgb = (detail + tex2Dproj(Terrain_RT, i.screenPos)).rgb;
                    #endif
                    #if _SHADOW_MULTIPLY
                    detail.rgb = (detail * tex2Dproj(Terrain_RT, i.screenPos)).rgb;
                    #endif
                    return detail;
                }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Diffuse"
}