// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShooter/ToonHSL" 
{
    Properties 
    {
		[MaterialToggle(_TEX_ON)] _DetailTex ("Enable Detail texture", Float) = 0 	//1
		_MainTex ("Detail", 2D) = "white" {}        								//2
		_ToonShade ("Shade", 2D) = "white" {}  										//3
		[MaterialToggle(_COLOR_ON)] _TintColor ("Enable Color Tint", Float) = 0 	//4
		_Color ("Base Color", Color) = (1,1,1,1)									//5	
		_Lightness ("Lightness 1 = neutral", Float) = 1.0							//6
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

                
                #if _TEX_ON
                sampler2D _MainTex;
				half4 _MainTex_ST;
				#endif
				
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
                    return o;
                }

              	sampler2D _ToonShade;
                fixed _Lightness;
                
                #if _COLOR_ON
                fixed4 _Color;
                #endif

                float Epsilon = 1e-10;

                float3 rgb2hcv(in float3 RGB)
                {
                    // Based on work by Sam Hocevar and Emil Persson
                    float4 P = lerp(float4(RGB.bg, -1.0, 2.0/3.0), float4(RGB.gb, 0.0, -1.0/3.0), step(RGB.b, RGB.g));
                    float4 Q = lerp(float4(P.xyw, RGB.r), float4(RGB.r, P.yzx), step(P.x, RGB.r));
                    float C = Q.x - min(Q.w, Q.y);
                    float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
                    return float3(H, C, Q.x);
                }

                float3 rgb2hsl(in float3 RGB)
                {
                    float3 HCV = rgb2hcv(RGB);
                    float L = HCV.z - HCV.y * 0.5;
                    float S = HCV.y / (1 - abs(L * 2 - 1) + Epsilon);
                    return float3(HCV.x, S, L);
                }

                float3 hsl2rgb(float3 c)
                {
                    c = float3(frac(c.x), clamp(c.yz, 0.0, 1.0));
                    float3 rgb = clamp(abs(fmod(c.x * 6.0 + float3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0, 0.0, 1.0);
                    return c.z + c.y * (rgb - 0.5) * (1.0 - abs(2.0 * c.z - 1.0));
                }

                fixed4 frag (v2f i) : COLOR
                {
                    fixed4 toonShade = tex2D( _ToonShade, i.uvn );

					#if _TEX_ON
					toonShade = toonShade * tex2D ( _MainTex, i.uv );
					#endif

                    float3 hsl = rgb2hsl(toonShade.rgb);
					#if _COLOR_ON
                    float3 hslColor = rgb2hsl(_Color.rgb);
                    hsl.r = hslColor.r;
                    hsl.g = hslColor.g;
					#endif
                    hsl.b *= _Lightness;
					toonShade.rgb = hsl2rgb(hsl);

					return  toonShade;
                }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Diffuse"
}