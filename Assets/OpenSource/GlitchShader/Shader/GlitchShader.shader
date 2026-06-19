Shader "GlitchShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ShadowThreshold ("Shadow Threshold", Range(-1,1)) = -0.3
        [Toggle(_)] _ReceiveShadow ("Receive Shadow", Int) = 0
        [Toggle(_PACK_LIGHTDATAS)] _PackLightDatas ("[Debug] Pack Light Datas", Int) = 0
        _ShadowColor ("Shadow Color", Color) = (0.8, 0.8, 0.8, 1)
        _ShadowBoundaryWidth ("Shadow Boundary Width", Range(0, 1)) = 0.1
        _ScanLinePeriod ("Scan Line Thickness", Range(0.001, 5)) = 1
        _ScanLineBrightness ("Scan Line Brightness", Range(0, 1)) = 0.9
        _AlphaCorrection ("Alpha Correction", Float) = 0.1
        _ChromaticAberrationIntensity ("Chromatic Aberration Intensity", Range(0, 1)) = 0.02
        _ChromaticAberrationBaseZShift ("Chromatic Aberration Base Z Shift", Float) = 0.001
        _GlitchSharpness ("Glitch Sharpness", Float) = 100
        _GlitchDisplacementThreshold ("Glitch Displacement Threshold", Range(0, 1)) = 0.5
        _GlitchMaxY ("Glitch Maximum Y Coordinate", Float) = 10
        _GlitchMinY ("Glitch Minimun Y Coordinate", Float) = -10
        _GlitchDisplacement1 ("Glitch Displacement Amount", Float) = 0.5
        _GlitchPeriod1 ("Glitch Period", Float) = 31
        _GlitchDisplacement2 ("Glitch Displacement Amount", Float) = -0.5
        _GlitchPeriod2 ("Glitch Period", Float) = 23
        _AsUnlit ("As Unlit", Range(0,1)) = 1
        _VertexLightStrength ("Vertex Light Strength", Range(0,1)) = 0
        _LightMinLimit ("Light Min Limit", Range(0,1)) = 0.05
        _LightMaxLimit ("Light Max Limit", Range(0,10)) = 1
        _BeforeExposureLimit ("Before Exposure Limit", Float) = 10000
        _MonochromeLighting ("Monochrome lighting", Range(0,1)) = 0
        _LightDirectionOverride ("Light Direction Override", Vector) = (0.001,0.002,0.001,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType"="Unlit"
        }

        Pass
        {
            Name "URPGlitchPreview"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _ScanLinePeriod;
                half _ScanLineBrightness;
                half _AlphaCorrection;
                half _ChromaticAberrationIntensity;
                half _ChromaticAberrationBaseZShift;
                float _GlitchSharpness;
                half _GlitchDisplacementThreshold;
                float _GlitchMaxY;
                float _GlitchMinY;
                float _GlitchDisplacement1;
                float _GlitchPeriod1;
                float _GlitchDisplacement2;
                float _GlitchPeriod2;
            CBUFFER_END

            float BandNoise(float y, float period, float sharpness)
            {
                float band = floor(y * max(period, 0.001) + _Time.y * 12.0);
                float wave = frac(sin(band * 12.9898) * 43758.5453);
                return step(1.0 - saturate(sharpness * 0.01), wave);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float3 worldPos = TransformObjectToWorld(positionOS);

                float yMask = step(_GlitchMinY, worldPos.y) * step(worldPos.y, _GlitchMaxY);
                float glitch1 = BandNoise(worldPos.y + _Time.y, _GlitchPeriod1, _GlitchSharpness);
                float glitch2 = BandNoise(worldPos.y - _Time.y, _GlitchPeriod2, _GlitchSharpness);
                float glitch = saturate(glitch1 + glitch2) * yMask * _GlitchDisplacementThreshold;

                worldPos.x += glitch * (_GlitchDisplacement1 + _GlitchDisplacement2);
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.worldPos = worldPos;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                float scan = frac((input.worldPos.y + _Time.y * 0.35) * max(_ScanLinePeriod, 0.001));
                half scanLine = lerp(1.0h, _ScanLineBrightness, step(0.5, scan));

                half chroma = _ChromaticAberrationIntensity * 0.5h;
                half3 tint = baseSample.rgb + half3(chroma, 0.0h, -chroma);
                half alpha = saturate(baseSample.a + _AlphaCorrection);
                return half4(tint * scanLine, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
