Shader "CoreWar/VoxelFaceLit"
{
    Properties
    {
        _MainTex ("Grid Texture", 2D) = "white" {}
        _ShadowLevel ("Shadow Level", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Flat fullforwardshadows addshadow noambient
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        half _ShadowLevel;

        struct Input
        {
            float2 uv_MainTex;
        };

        inline half4 LightingFlat(SurfaceOutput s, half3 lightDir, half atten)
        {
            // Two lighting levels only: a surface the sun reaches is fully
            // bright, everything else drops to one flat shadow gray.
            // The small smoothstep window avoids shadow-map edge speckle on
            // faces angled away from the sun.
            half facingSun = smoothstep(0.02, 0.08, dot(s.Normal, lightDir));
            half lit = facingSun * atten;
            return half4(s.Albedo * lerp(_ShadowLevel, 1.0, lit), 1);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
            o.Alpha = 1;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
