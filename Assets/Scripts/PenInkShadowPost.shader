Shader "Hidden/CoreWar/PenInkShadowPost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InkColor ("Ink Color", Color) = (0.34,0.34,0.36,1)
        _PaperTint ("Paper Tint", Color) = (0.985,0.985,0.985,1)
        _ShadowThreshold ("Shadow Threshold", Range(0.2, 0.9)) = 0.28
        _HatchScale ("Hatch Scale", Range(8,80)) = 20
        _PaperBlend ("Paper Blend", Range(0,1)) = 0.03
        _CenterDarkness ("Center Darkness", Range(0,2)) = 0.78
        _CircularFalloff ("Circular Falloff", Range(0.5,6)) = 3.2
        _TopSurfaceThreshold ("Top Surface Threshold", Range(0.3,1)) = 0.82
        _VoxelSize ("Voxel Size", Float) = 1
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
            sampler2D _CameraDepthTexture;
            sampler2D _CameraDepthNormalsTexture;
            float4 _InkColor;
            float4 _PaperTint;
            float _ShadowThreshold;
            float _HatchScale;
            float _PaperBlend;
            float _CenterDarkness;
            float _CircularFalloff;
            float _TopSurfaceThreshold;
            float _VoxelSize;
            float4x4 _InverseViewProjection;

            float Luma(float3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            float3 ReconstructWorldPos(float2 uv, float rawDepth)
            {
                float4 clip = float4(uv * 2.0 - 1.0, rawDepth * 2.0 - 1.0, 1.0);
                float4 world = mul(_InverseViewProjection, clip);
                return world.xyz / max(world.w, 0.0001);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float3 src = tex2D(_MainTex, uv).rgb;
                float lum = Luma(src);
                float shadow = saturate((_ShadowThreshold - lum) / max(_ShadowThreshold, 0.0001));

                float4 depthNormals = tex2D(_CameraDepthNormalsTexture, uv);
                float depth01;
                float3 viewNormal;
                DecodeDepthNormal(depthNormals, depth01, viewNormal);

                float3 worldNormal = normalize(mul((float3x3)unity_CameraToWorld, viewNormal));
                float topMask = smoothstep(_TopSurfaceThreshold, 1.0, worldNormal.y);

                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float3 worldPos = ReconstructWorldPos(uv, rawDepth);

                float2 cellUv = frac(worldPos.xz / max(_VoxelSize, 0.0001));
                float2 toCenter = (cellUv - 0.5) * 2.0;
                float radial = saturate(1.0 - length(toCenter));
                float circularShadow = pow(radial, _CircularFalloff) * _CenterDarkness;

                float hatchA = step(0.52, frac((worldPos.x + worldPos.z) * _HatchScale));
                float hatchB = step(0.52, frac((worldPos.x - worldPos.z) * _HatchScale));
                float hatch = lerp(0.65, 1.0, (hatchA + hatchB) * 0.5);

                float inkMask = saturate(shadow * topMask * circularShadow * hatch);
                float3 paper = lerp(src, _PaperTint.rgb, _PaperBlend);
                float3 finalColor = lerp(paper, _InkColor.rgb, inkMask);

                return float4(finalColor, 1);
            }
            ENDCG
        }
    }
}
