// Lab 7 light beam (searchlight, landing light, landing-zone beacon): additive, double-sided, no depth write. Brightness =
// colour × vertex alpha (fades along the beam) × a view-angle term (bright in the middle of the cone, soft at its edges),
// faded by the scene's exponential-squared fog so a far beam melts into the haze. The fog factor is computed here from
// unity_FogParams, so no fog shader variants are needed.
Shader "NU36/Huzme"
{
    Properties
    {
        [HDR] _Renk ("Colour", Color) = (1.0, 0.95, 0.8, 1)
        _Yogunluk ("Intensity", Range(0, 4)) = 0.35
        _Kenar ("Edge Softness", Range(0.2, 6)) = 1.6
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Renk;
                half _Yogunluk;
                half _Kenar;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 v = _WorldSpaceCameraPos - input.positionWS;
                float uzaklik = length(v);
                float yuz = abs(dot(normalize(input.normalWS), v / max(uzaklik, 1e-4)));
                float sis = saturate(exp2(-pow(unity_FogParams.x * uzaklik, 2.0)));
                // very close to the camera the cone wall would fill the screen: fade it in over the first 15 m
                float yakin = saturate(uzaklik / 15.0);
                half a = input.color.a * pow(yuz, _Kenar) * _Yogunluk * sis * yakin;
                return half4(_Renk.rgb * a, 1);
            }
            ENDHLSL
        }
    }
}
