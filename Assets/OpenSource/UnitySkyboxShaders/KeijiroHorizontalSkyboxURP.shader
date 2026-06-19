// Adapted from Keijiro Takahashi's UnitySkyboxShaders / Horizontal Skybox.
// Source: https://github.com/keijiro/UnitySkyboxShaders
// License: MIT. See LICENSE.txt in this folder.
Shader "RhythmParkour/OpenSource/Keijiro Horizontal Skybox URP"
{
    Properties
    {
        _Color1 ("Top Color", Color) = (0.004, 0.004, 0.025, 1)
        _Color2 ("Horizon Color", Color) = (0.025, 0.14, 0.48, 1)
        _Color3 ("Bottom Color", Color) = (0.025, 0.012, 0.08, 1)
        _Exponent1 ("Top Exponent", Float) = 1.4
        _Exponent2 ("Bottom Exponent", Float) = 1.1
        _Intensity ("Intensity", Float) = 1.0
        _Pulse ("Rhythm Pulse", Range(0, 1)) = 0
        _PulseColor ("Pulse Color", Color) = (0.08, 0.55, 1.0, 1)
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.12
        _CloudColor ("Cloud Color", Color) = (0.45, 0.82, 1.0, 1)
        _CloudStrength ("Cloud Strength", Range(0, 1)) = 0.22
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.58
        _CloudHeight ("Cloud Height", Range(0, 1)) = 0.62
        _CloudThickness ("Cloud Thickness", Range(0.01, 0.6)) = 0.24
        _CloudScale ("Cloud Scale", Float) = 2.8
        _CloudDrift ("Cloud Drift", Float) = 0.015
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Background"
            "Queue"="Background"
            "PreviewType"="Skybox"
        }

        Pass
        {
            Name "KeijiroHorizontalSkyboxURP"

            ZWrite Off
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float _Exponent1;
            float _Exponent2;
            float _Intensity;
            float _Pulse;
            float4 _PulseColor;
            float _PulseStrength;
            float4 _CloudColor;
            float _CloudStrength;
            float _CloudCoverage;
            float _CloudHeight;
            float _CloudThickness;
            float _CloudScale;
            float _CloudDrift;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    value += ValueNoise(p) * amplitude;
                    p = p * 2.03 + 17.31;
                    amplitude *= 0.5;
                }

                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS.xyz);
#if UNITY_REVERSED_Z
                output.positionCS.z = 0.0;
#else
                output.positionCS.z = output.positionCS.w;
#endif
                output.texcoord = input.positionOS.xyz;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.texcoord);
                float p = saturate(direction.y * 0.5 + 0.5) * 2.0 - 1.0;
                float topWeight = 1.0 - pow(saturate(1.0 - p), max(0.0001, _Exponent1));
                float bottomWeight = 1.0 - pow(saturate(1.0 + p), max(0.0001, _Exponent2));
                float horizonWeight = saturate(1.0 - topWeight - bottomWeight);
                float4 baseColor = (_Color1 * topWeight + _Color2 * horizonWeight + _Color3 * bottomWeight) * _Intensity;
                float horizonPulse = horizonWeight * _Pulse * _PulseStrength;
                baseColor.rgb = lerp(baseColor.rgb, _PulseColor.rgb, horizonPulse);

                float2 cloudUv = float2(atan2(direction.x, direction.z) * 0.15915494 + 0.5, direction.y * 0.5 + 0.5);
                cloudUv.x += _Time.y * _CloudDrift;
                float cloudNoise = Fbm(cloudUv * _CloudScale);
                float cloudBand = 1.0 - smoothstep(_CloudThickness, _CloudThickness + 0.18, abs(cloudUv.y - _CloudHeight));
                float cloudShape = smoothstep(_CloudCoverage, 1.0, cloudNoise);
                float cloud = cloudShape * cloudBand * _CloudStrength;
                baseColor.rgb = lerp(baseColor.rgb, _CloudColor.rgb * _Intensity, saturate(cloud));

                return float4(saturate(baseColor.rgb), 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
