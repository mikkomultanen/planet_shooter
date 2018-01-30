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

                
                sampler2D _MainTex;
				half4 _MainTex_ST;
				sampler2D _OverlayTex;
				half4 _OverlayTex_ST;

                struct appdata_base0 
				{
					float4 vertex : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 overlaycoord : TEXCOORD1;
				};
				
                 struct v2f 
                 {
                    float4 pos : SV_POSITION;
                    half2 uv : TEXCOORD0;
                    half2 uvo : TEXCOORD1;
                 };
               
                v2f vert (appdata_base0 v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos ( v.vertex );
                    o.uv = TRANSFORM_TEX ( v.texcoord, _MainTex );
					o.uvo = TRANSFORM_TEX (v.overlaycoord, _OverlayTex );
                    return o;
                }

                fixed _Brightness;
                
                #if _COLOR_ON
                fixed4 _Color;
                #endif
                
                fixed4 frag (v2f i) : COLOR
                {

					fixed4 overlay = tex2D( _OverlayTex, i.uvo );
					#if _COLOR_ON
					fixed4 detail = tex2D ( _MainTex, i.uv )*_Brightness*_Color;
					#else
					fixed4 detail = tex2D ( _MainTex, i.uv )*_Brightness;
					#endif

					return float4(lerp(detail.rgb, overlay.rgb, overlay.a), 1);
                }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Diffuse"
}