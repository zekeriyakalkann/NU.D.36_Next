// Lab 7 night sky (skybox): a dark gradient whose horizon colour equals the fog colour, procedural twinkling stars that fade
// into the haze near the horizon, and the moon with a soft halo in the direction of the moonlight. _Safak (0..1) adds the
// dawn glow along the horizon towards _SafakYonu (driven at run time by the Safak component).
Shader "NU36/GeceGokyuzu"
{
    Properties
    {
        _Tepe ("Zenith", Color) = (0.010, 0.014, 0.030, 1)
        _Ufuk ("Horizon (= fog colour)", Color) = (0.060, 0.072, 0.095, 1)
        _Egri ("Gradient Curve", Range(0.1, 2)) = 0.45
        _YildizYogunlugu ("Star Density", Range(0, 0.2)) = 0.045
        _YildizParlakligi ("Star Brightness", Range(0, 8)) = 2.2
        _AyYonu ("Moon Direction (towards the moon)", Vector) = (-0.3, 0.45, 0.85, 0)
        _AyRengi ("Moon Colour", Color) = (0.95, 0.96, 1.0, 1)
        _AyBoyutu ("Moon Radius (deg)", Range(0.2, 6)) = 1.6
        _AyParlakligi ("Moon Brightness", Range(0, 20)) = 5
        _Hale ("Moon Halo", Range(0, 2)) = 0.6
        _Safak ("Dawn", Range(0, 1)) = 0
        _SafakYonu ("Dawn Direction (towards the rising sun)", Vector) = (1, 0, 0, 0)
        _SafakRengi ("Dawn Glow Colour", Color) = (1.0, 0.55, 0.28, 1)
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
                half4 _Tepe;
                half4 _Ufuk;
                half _Egri;
                half _YildizYogunlugu;
                half _YildizParlakligi;
                float4 _AyYonu;
                half4 _AyRengi;
                half _AyBoyutu;
                half _AyParlakligi;
                half _Hale;
                half _Safak;
                float4 _SafakYonu;
                half4 _SafakRengi;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.dir = input.positionOS.xyz;
                return o;
            }

            float Hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            half3 Yildizlar(float3 d)
            {
                float3 p = d * 220.0;
                float3 hucre = floor(p);
                float h = Hash3(hucre);
                if (h < 1.0 - _YildizYogunlugu)
                    return 0;
                float3 merkez = hucre + 0.25 + 0.5 * float3(Hash3(hucre + 1.7), Hash3(hucre + 4.1), Hash3(hucre + 7.3));
                float r = length(p - merkez);
                float parlak = (h - (1.0 - _YildizYogunlugu)) / max(_YildizYogunlugu, 1e-4);
                float goz = 0.75 + 0.25 * sin(_Time.y * (2.0 + 5.0 * Hash3(hucre + 3.3)) + h * 40.0);
                half3 renk = lerp(half3(0.75, 0.82, 1.0), half3(1.0, 0.9, 0.75), Hash3(hucre + 9.9));
                return renk * smoothstep(0.22, 0.0, r) * parlak * goz * _YildizParlakligi;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.dir);
                float t = saturate(d.y);
                half3 col = lerp(_Ufuk.rgb, _Tepe.rgb, pow(t, _Egri));
                if (d.y < 0)
                    col = _Ufuk.rgb * 0.9;

                col += Yildizlar(d) * smoothstep(0.04, 0.3, d.y);

                float3 ay = normalize(_AyYonu.xyz);
                float c = dot(d, ay);
                float yaricap = radians(_AyBoyutu);
                float disk = smoothstep(cos(yaricap) - 0.00004, cos(yaricap) + 0.00004, c);
                // a little shading across the disc so it does not look flat
                float golge = 0.85 + 0.15 * saturate(dot(normalize(d - ay * c), float3(0.6, 0.3, 0.7)) + 0.5);
                col += _AyRengi.rgb * disk * _AyParlakligi * golge;
                col += _AyRengi.rgb * _Hale * (pow(saturate(c), 900.0) * 0.6 + pow(saturate(c), 60.0) * 0.08);

                // dawn: a warm band low over the eastern horizon, widening as it grows
                float3 sy = normalize(float3(_SafakYonu.x, 0.0, _SafakYonu.z));
                float yan = saturate(dot(normalize(float3(d.x, 0.0, d.z) + 1e-5), sy) * 0.5 + 0.5);
                float bant = exp(-max(d.y, 0.0) * lerp(9.0, 3.5, _Safak));
                col += _SafakRengi.rgb * _Safak * bant * pow(yan, lerp(5.0, 2.0, _Safak)) * 1.6;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
