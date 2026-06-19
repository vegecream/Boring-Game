Shader "RhythmParkour/URP Glitch Silhouette"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.08, 0.16, 0.36, 1)
        _GlitchColor ("Glitch Color", Color) = (0.1, 0.95, 1.0, 1)
        _AccentColor ("Accent Color", Color) = (1.0, 0.16, 0.86, 1)
        _BandDensity ("Band Density", Float) = 34
        _GlitchStrength ("Glitch Strength", Range(0, 1)) = 0.45
        _PulseSpeed ("Pulse Speed", Float) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "GlitchSilhouette"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GlitchColor;
                half4 _AccentColor;
                float _BandDensity;
                float _GlitchStrength;
                float _PulseSpeed;
            CBUFFER_END

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _PulseSpeed;
                float bandCoord = input.positionWS.y * 0.18 + input.positionWS.z * 0.013;
                float band = floor(bandCoord * max(1.0, _BandDensity));
                float noise = Hash(band + floor(t * 12.0));
                float scan = smoothstep(0.46, 0.5, abs(frac(bandCoord * _BandDensity + t) - 0.5));
                float glitch = step(1.0 - _GlitchStrength, noise) * scan;
                float accent = step(0.88, Hash(band * 2.31 + floor(t * 5.0))) * (1.0 - scan);

                half3 color = _BaseColor.rgb;
                color = lerp(color, _GlitchColor.rgb, glitch);
                color = lerp(color, _AccentColor.rgb, accent * 0.55);
                color += _GlitchColor.rgb * (0.08 + glitch * 0.28);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
