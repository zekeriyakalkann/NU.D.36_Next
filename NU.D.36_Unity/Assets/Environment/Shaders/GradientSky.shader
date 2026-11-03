// Three-colour gradient sky with a soft sun glow. The horizon colour is meant to equal the scene
// fog colour, so distant terrain fades into the sky without a visible edge.
Shader "NU36/GradientSky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.16, 0.38, 0.78, 1)
        _Horizon ("Horizon", Color) = (0.72, 0.84, 0.95, 1)
        _Ground ("Below Horizon", Color) = (0.72, 0.84, 0.95, 1)
        _Falloff ("Zenith Falloff", Range(0.2, 2)) = 0.6
        _SunGlow ("Sun Glow", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith;
                half4 _Horizon;
                half4 _Ground;
                half _Falloff;
                half _SunGlow;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.dir = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.dir);
                half up = saturate(d.y);
                half down = saturate(-d.y * 6.0);
                half3 col = lerp(_Horizon.rgb, _Zenith.rgb, pow(up, _Falloff));
                col = lerp(col, _Ground.rgb, down);
                half s = saturate(dot(d, _MainLightPosition.xyz));
                col += _MainLightColor.rgb * (pow(s, 600.0) * 1.5 + pow(s, 12.0) * _SunGlow * 0.5) * (1.0 - down);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
