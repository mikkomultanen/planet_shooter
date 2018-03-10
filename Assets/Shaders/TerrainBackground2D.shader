// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShooter/TerrainBackground2D" 
{
    Properties 
    {
		_MainTex ("Detail", 2D) = "white" {}        								//1
		[MaterialToggle(_COLOR_ON)] _TintColor ("Enable Color Tint", Float) = 0 	//2
		_Color ("Base Color", Color) = (1,1,1,1)									//3
		[MaterialToggle(_VCOLOR_ON)] _VertexColor ("Enable Vertex Color", Float) = 0//4
		_Brightness ("Brightness 1 = neutral", Float) = 1.0							//5
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

                
                sampler2D _MainTex;
				half4 _MainTex_ST;

                struct appdata_base0 
				{
					float4 vertex : POSITION;
					float4 texcoord : TEXCOORD0;
                    #if _VCOLOR_ON
                    fixed4 color : COLOR;
                    #endif
				};
				
                 struct v2f 
                 {
                    float4 pos : SV_POSITION;
                    half2 uv : TEXCOORD0;
                    #if _VCOLOR_ON
                    fixed4 color : COLOR;
                    #endif
                 };
               
                v2f vert (appdata_base0 v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos ( v.vertex );
                    o.uv = TRANSFORM_TEX ( v.texcoord, _MainTex );
                    #if _VCOLOR_ON
                    o.color = v.color;
                    #endif
                    return o;
                }

                fixed _Brightness;
                
                #if _COLOR_ON
                fixed4 _Color;
                #endif
                
                fixed4 frag (v2f i) : COLOR
                {
                    fixed4 result = tex2D ( _MainTex, i.uv )*_Brightness;
					#if _COLOR_ON
					result = result*_Color;
					#endif
                    #if _VCOLOR_ON
                    result = result*i.color;
                    #endif
                    return result;
                }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Diffuse"
}