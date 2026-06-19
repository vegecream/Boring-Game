Shader "RhythmParkour/Rhythm Light Bridge"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.08, 0.28, 0.55, 1)
        _OutlineColor ("Outline Color", Color) = (0.55, 0.95, 1.0, 1)
        _Alpha ("Surface Alpha", Range(0, 1)) = 0.38
        _Emission ("Surface Emission", Range(0, 8)) = 1.35
        _OutlineWorldWidth ("Outline World Width", Range(0.005, 0.4)) = 0.08
        _OutlineIntensity ("Outline Intensity", Range(0, 12)) = 5.0
        _BridgeLength ("Bridge Length", Float) = 28
        _BridgeWidth ("Bridge Width", Float) = 3.2
        _BeatPulse ("Beat Pulse", Range(0, 1)) = 0
        _PulseStrength ("Pulse Strength", Range(0, 2)) = 0.35
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
            Name "RhythmLightBridge"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
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
                float3 positionOS : TEXCOORD1;
                half4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _OutlineColor;
                float _Alpha;
                float _Emission;
                float _OutlineWorldWidth;
                float _OutlineIntensity;
                float _BridgeLength;
                float _BridgeWidth;
                float _BeatPulse;
                float _PulseStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float pulse = _BeatPulse * _PulseStrength;

                float sideDistance = _BridgeWidth * 0.5 - abs(input.positionOS.x);
                float endDistance = min(input.positionOS.z, _BridgeLength - input.positionOS.z);
                float edgeDistance = max(min(sideDistance, endDistance), 0.0);
                float antiAlias = max(fwidth(edgeDistance) * 2.0, 0.001);
                float outline = 1.0 - smoothstep(_OutlineWorldWidth, _OutlineWorldWidth + antiAlias, edgeDistance);

                float panelFalloff = smoothstep(0.0, _OutlineWorldWidth * 5.0, edgeDistance);
                half3 rampColor = lerp(_BaseColor.rgb, input.color.rgb, 0.42);
                half3 surface = rampColor * _Emission;
                half3 color = surface * lerp(0.82, 1.12, panelFalloff);
                color += input.color.rgb * 0.18 * panelFalloff;
                color += surface * pulse * 0.35;
                color += _OutlineColor.rgb * outline * (_OutlineIntensity + pulse * 2.0);

                float alpha = _Alpha * input.color.a;
                alpha += outline * 0.42;
                alpha += pulse * 0.08;

                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
