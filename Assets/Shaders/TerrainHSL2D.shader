// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShooter/TerrainHSL2D" 
{
    Properties 
    {
		_MainTex ("Detail", 2D) = "white" {}        								//1
		_OverlayTex ("Overlay", 2D) = "white" {}  									//2
        _HSLRangeMin ("HSL Affect Range Min", Range(0, 1)) = 0                      //3
        _HSLRangeMax ("HSL Affect Range Max", Range(0, 1)) = 1                      //4
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

                
                sampler2D _MainTex;
				half4 _MainTex_ST;
				sampler2D _OverlayTex;
				half4 _OverlayTex_ST;

                struct appdata_base0 
				{
					float4 vertex : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 overlaycoord : TEXCOORD1;
                    fixed4 color : COLOR;
				};
				
                 struct v2f 
                 {
                    float4 pos : SV_POSITION;
                    half2 uv : TEXCOORD0;
                    half2 uvo : TEXCOORD1;
                    fixed4 color : COLOR;
                 };
               
                v2f vert (appdata_base0 v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos ( v.vertex );
                    o.uv = TRANSFORM_TEX ( v.texcoord, _MainTex );
					o.uvo = TRANSFORM_TEX (v.overlaycoord, _OverlayTex );
                    o.color = v.color;
                    return o;
                }

                float _HSLRangeMin;
                float _HSLRangeMax;
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
					fixed4 overlay = tex2D( _OverlayTex, i.uvo );
                    fixed4 detail = tex2D ( _MainTex, i.uv );
                    float3 detailHsl = rgb2hsl(detail.rgb);
                    float affectMult = step(_HSLRangeMin, detailHsl.r) * step(detailHsl.r, _HSLRangeMax);
                    float3 colorHsl = rgb2hsl(i.color);
                    detailHsl.r = lerp(detailHsl.r, colorHsl.r, affectMult);
                    detailHsl.gb = detailHsl.gb * colorHsl.gb;
                    detail = float4(hsl2rgb(detailHsl), 1);

					return float4(lerp(detail.rgb, overlay.rgb, overlay.a), 1);
                }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Diffuse"
}