Shader "Hidden/CoreWar/SniperScopePost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScopeRadius ("Scope Radius", Range(0.05, 0.9)) = 0.34
        _ScopeBlend ("Scope Blend", Range(0, 1)) = 0
        _VignetteDarkness ("Vignette Darkness", Range(0, 1)) = 0.9
        _BlurSize ("Blur Size", Range(0, 0.03)) = 0.009
        _DarkBandWidth ("Dark Band Width", Range(0.05, 0.6)) = 0.22
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _ScopeRadius;
            float _ScopeBlend;
            float _VignetteDarkness;
            float _BlurSize;
            float _DarkBandWidth;

            fixed4 SampleBlurred(float2 uv, float blurSize)
            {
                float4 color = tex2D(_MainTex, uv) * 0.16;
                color += tex2D(_MainTex, uv + float2(blurSize, 0)) * 0.12;
                color += tex2D(_MainTex, uv + float2(-blurSize, 0)) * 0.12;
                color += tex2D(_MainTex, uv + float2(0, blurSize)) * 0.12;
                color += tex2D(_MainTex, uv + float2(0, -blurSize)) * 0.12;
                color += tex2D(_MainTex, uv + float2(blurSize, blurSize)) * 0.09;
                color += tex2D(_MainTex, uv + float2(-blurSize, blurSize)) * 0.09;
                color += tex2D(_MainTex, uv + float2(blurSize, -blurSize)) * 0.09;
                color += tex2D(_MainTex, uv + float2(-blurSize, -blurSize)) * 0.09;
                return color;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = float2(0.5, 0.5);
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 delta = float2((uv.x - center.x) * aspect, uv.y - center.y);
                float dist = length(delta);

                float4 sharpCol = tex2D(_MainTex, uv);

                // Soft blur ramp — no hard circle edge.
                float blurRamp = smoothstep(_ScopeRadius * 0.55, _ScopeRadius * 1.35, dist);
                float blurAmount = _BlurSize * blurRamp * _ScopeBlend;
                float4 blurredCol = SampleBlurred(uv, max(blurAmount, 0.0001));

                // Darkness peaks just outside the clear zone, fades toward corners.
                float darkBandCenter = _ScopeRadius + (_DarkBandWidth * 0.45);
                float darknessProfile = 1.0 - smoothstep(0.0, _DarkBandWidth, abs(dist - darkBandCenter));
                float darkness = _VignetteDarkness * darknessProfile * blurRamp;

                float4 col = lerp(sharpCol, blurredCol, blurRamp * _ScopeBlend);
                col.rgb = lerp(col.rgb, float3(0, 0, 0), darkness * _ScopeBlend);

                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
