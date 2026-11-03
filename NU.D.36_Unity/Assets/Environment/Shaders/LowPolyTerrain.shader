// Flat-shaded low-poly terrain: vertex colour x (hemisphere ambient + main light N.L), linear fog.
// Fog is computed here from unity_FogParams so it does not depend on fog shader variants being
// included in the build. No textures, no shadows.
Shader "NU36/LowPolyTerrain"
{
    Properties
    {
        _AmbientSky ("Ambient Sky", Color) = (0.60, 0.72, 0.90, 1)
        _AmbientGround ("Ambient Ground", Color) = (0.34, 0.38, 0.26, 1)
        _SunStrength ("Sun Strength", Range(0, 2)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _AmbientSky;
                half4 _AmbientGround;
                half _SunStrength;
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
                half3 color : TEXCOORD1;
                float fog : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                half3 c = input.color.rgb;
            #ifndef UNITY_COLORSPACE_GAMMA
                c = SRGBToLinear(c); // vertex colours are authored in sRGB
            #endif
                output.color = c;
                float dist = distance(_WorldSpaceCameraPos, positionWS);
                output.fog = saturate(dist * unity_FogParams.z + unity_FogParams.w); // 1 = clear, 0 = fully fogged
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half3 ambient = lerp(_AmbientGround.rgb, _AmbientSky.rgb, n.y * 0.5h + 0.5h);
                half3 light = ambient + mainLight.color * (saturate(dot(n, mainLight.direction)) * _SunStrength);
                half3 col = input.color * light;
                col = lerp(unity_FogColor.rgb, col, input.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
