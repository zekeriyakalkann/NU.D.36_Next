// Lab 7 particles (smoke, fire, sparks, dust): texture × particle (vertex) colour × tint, double-sided, no depth write.
// _SrcBlend/_DstBlend choose alpha blending (smoke: SrcAlpha / OneMinusSrcAlpha) or additive (fire: SrcAlpha / One).
// Fog (exponential squared, from unity_FogParams): alpha-blended particles fade to the fog colour, additive ones fade out.
Shader "NU36/Parcacik"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Renk ("Tint", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Toggle] _Eklemeli ("Additive (fog fades it out)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend [_SrcBlend] [_DstBlend]
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Renk;
                half _Eklemeli;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float sis : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(p);
                o.color = input.color;
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                float d = distance(_WorldSpaceCameraPos, p);
                o.sis = saturate(exp2(-pow(unity_FogParams.x * d, 2.0)));   // 1 = clear
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color * _Renk;
                if (_Eklemeli > 0.5)
                    c.rgb *= input.sis;
                else
                    c.rgb = lerp(unity_FogColor.rgb, c.rgb, input.sis);
                return c;
            }
            ENDHLSL
        }
    }
}
