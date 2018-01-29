// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShooter/TerrainOutline2D"
{
    Properties 
    {

		_MainTex ("Detail", 2D) = "white" {}        								//1
		_OverlayTex ("Overlay", 2D) = "white" {}  									//2
		[MaterialToggle(_COLOR_ON)] _TintColor ("Enable Color Tint", Float) = 0 	//3
		_Color ("Base Color", Color) = (1,1,1,1)									//4
		[MaterialToggle(_VCOLOR_ON)] _VertexColor ("Enable Vertex Color", Float) = 0//5
		_Brightness ("Brightness 1 = neutral", Float) = 1.0							//6
		_OutlineColor ("Outline Color", Color) = (0.5,0.5,0.5,1.0)					//7
		_Outline ("Outline width", Float) = 0.01									//8

    }
 
    SubShader
    {
        Tags { "RenderType"="Opaque" }
		LOD 250 
        Lighting Off
        Fog { Mode Off }
        
        UsePass "PlanetShooter/Terrain2D/BASE"
        	
        Pass
        {
            Cull Back
            ZWrite On
            CGPROGRAM
			#include "UnityCG.cginc"
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma glsl_no_auto_normalization
            #pragma vertex vert
 			#pragma fragment frag
			
            struct appdata_t 
            {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f 
			{
				float4 pos : SV_POSITION;
			};

            fixed _Outline;

            
            v2f vert (appdata_t v) 
            {
                v2f o;
			    o.pos = v.vertex;
				float2 n = normalize(v.normal.xy) *_Outline;
			    o.pos.xyz += float3(n, 0.01);
			    o.pos = UnityObjectToClipPos(o.pos);
			    return o;
            }
            
            fixed4 _OutlineColor;
            
            fixed4 frag(v2f i) :COLOR 
			{
		    	return _OutlineColor;
			}
            
            ENDCG
        }
    }
Fallback "Legacy Shaders/Diffuse"
}