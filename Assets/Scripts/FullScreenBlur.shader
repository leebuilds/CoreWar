Shader "Hidden/CoreWar/FullScreenBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 0.03)) = 0.014
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
            float _BlurSize;

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
                return SampleBlurred(i.uv, max(_BlurSize, 0.0001));
            }
            ENDCG
        }
    }

    Fallback Off
}
