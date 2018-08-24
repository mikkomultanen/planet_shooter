// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/PlanetShooter/Radar" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
		_Threshold("Threshold", float) = 0.01
        _EdgeColor("Edge color", Color) = (0,0,0,1)
        _Radar("Radar", Vector) = (0, 0, 0, 0)
        _AspectRatio("Aspect ratio width/height", float) = 1
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Threshold;
            fixed4 _EdgeColor;
            float4 _Radar;
            float _AspectRatio;
            uniform sampler2D Terrain_RT;

            float GetPixelValue(in float2 uv) {
                return Linear01Depth (tex2D(_CameraDepthTexture, uv).r);
            }

            float sobel (float2 uv) {
                float2 delta = 0.5 * _MainTex_TexelSize.xy;
                
                float4 hr = float4(0, 0, 0, 0);
                float4 vt = float4(0, 0, 0, 0);
                
                hr += GetPixelValue(uv + float2(-1.0, -1.0) * delta) *  1.0;
                hr += GetPixelValue(uv + float2( 0.0, -1.0) * delta) *  0.0;
                hr += GetPixelValue(uv + float2( 1.0, -1.0) * delta) * -1.0;
                hr += GetPixelValue(uv + float2(-1.0,  0.0) * delta) *  2.0;
                hr += GetPixelValue(uv + float2( 0.0,  0.0) * delta) *  0.0;
                hr += GetPixelValue(uv + float2( 1.0,  0.0) * delta) * -2.0;
                hr += GetPixelValue(uv + float2(-1.0,  1.0) * delta) *  1.0;
                hr += GetPixelValue(uv + float2( 0.0,  1.0) * delta) *  0.0;
                hr += GetPixelValue(uv + float2( 1.0,  1.0) * delta) * -1.0;
                
                vt += GetPixelValue(uv + float2(-1.0, -1.0) * delta) *  1.0;
                vt += GetPixelValue(uv + float2( 0.0, -1.0) * delta) *  2.0;
                vt += GetPixelValue(uv + float2( 1.0, -1.0) * delta) *  1.0;
                vt += GetPixelValue(uv + float2(-1.0,  0.0) * delta) *  0.0;
                vt += GetPixelValue(uv + float2( 0.0,  0.0) * delta) *  0.0;
                vt += GetPixelValue(uv + float2( 1.0,  0.0) * delta) *  0.0;
                vt += GetPixelValue(uv + float2(-1.0,  1.0) * delta) * -1.0;
                vt += GetPixelValue(uv + float2( 0.0,  1.0) * delta) * -2.0;
                vt += GetPixelValue(uv + float2( 1.0,  1.0) * delta) * -1.0;
                
                return sqrt(hr * hr + vt * vt);
            }

            //Fragment Shader
            float4 frag (v2f_img i) : COLOR {
                float4 original = tex2D(_MainTex, i.uv);
                float4 shadow = tex2D(Terrain_RT, i.uv);
                float originalBrightness = max(original.r, max(original.g, original.b));
                float brightness = max(shadow.r, max(shadow.g, shadow.b));
                float s = sobel(i.uv);
                float2 dv= i.uv - _Radar.xy;
                dv.x *= _AspectRatio;
                float d = length(dv);

                float radarEdge = smoothstep(_Radar.z - _MainTex_TexelSize.y, _Radar.z, d) * (1 - smoothstep(_Radar.z, _Radar.z + _MainTex_TexelSize.y, d));
                float terrainEdge = smoothstep(_Radar.z - 0.5, _Radar.z, d) * (1 - smoothstep(_Radar.z, _Radar.z + 0.01, d)) * step(_Threshold, s);
                float brightArea = smoothstep(0, 0.1, originalBrightness + brightness);
                float terrainEdgeInShadow = terrainEdge * (1 - brightArea);
                float terrainEdgeInLight = terrainEdge * brightArea;
                return lerp(original, _EdgeColor, _Radar.w * clamp(0.05 * radarEdge + terrainEdgeInShadow + 0.2 * terrainEdgeInLight, 0, 1));

            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}