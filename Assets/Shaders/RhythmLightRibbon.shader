Shader "RhythmParkour/Rhythm Light Ribbon"
{
    Properties
    {
        _CoreColor ("Core Color", Color) = (0.65, 0.95, 1.0, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.45
        _CoreAlpha ("Core Alpha", Range(0, 2)) = 0.85
        _EdgePower ("Edge Fade Power", Range(0.4, 8)) = 2.6
        _CorePower ("Core Power", Range(0.4, 16)) = 5.0
        _FlowFrequency ("Flow Frequency", Range(0, 16)) = 6.0
        _FlowSpeed ("Flow Speed", Float) = 0.35
        _FlowStrength ("Flow Strength", Range(0, 2)) = 0.55
        _BeatPulse ("Beat Pulse", Range(0, 1)) = 0
        _PulseStrength ("Pulse Strength", Range(0, 4)) = 1.15
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType"="Unlit"
        }
        LOD 100

        Pass
        {
            Name "RhythmLightRibbon"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                float _Alpha;
                float _CoreAlpha;
                float _EdgePower;
                float _CorePower;
                float _FlowFrequency;
                float _FlowSpeed;
                float _FlowStrength;
                float _BeatPulse;
                float _PulseStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float across = saturate(input.uv.y);
                float center = 1.0 - abs(across * 2.0 - 1.0);
                float edgeFade = pow(saturate(center), _EdgePower);
                float core = pow(saturate(center), _CorePower);

                float wave = sin((input.uv.x * _FlowFrequency - _Time.y * _FlowSpeed) * TWO_PI) * 0.5 + 0.5;
                float flow = lerp(1.0, smoothstep(0.2, 1.0, wave), _FlowStrength);
                float pulse = 1.0 + _BeatPulse * _PulseStrength;

                half3 color = input.color.rgb * flow * pulse;
                color += _CoreColor.rgb * core * _CoreAlpha * pulse;

                float alpha = _Alpha * input.color.a * edgeFade;
                alpha += core * _CoreAlpha * 0.18;

                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
