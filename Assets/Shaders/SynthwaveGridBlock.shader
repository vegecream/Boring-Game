Shader "RhythmParkour/Synthwave Grid Block"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.03, 0.02, 0.08, 1)
        _GridColor ("Grid Color", Color) = (0.0, 0.9, 1.0, 1)
        _EdgeColor ("Edge Color", Color) = (1.0, 0.1, 0.9, 1)
        _GridSpacing ("Grid Spacing", Float) = 0.5
        _LineThickness ("Line Thickness", Range(0.001, 0.2)) = 0.035
        _GridIntensity ("Grid Intensity", Range(0, 8)) = 2.0
        _EdgeThickness ("Edge Thickness", Range(0.001, 0.2)) = 0.008
        _EdgeIntensity ("Edge Intensity", Range(0, 12)) = 2.4
        _RimIntensity ("Rim Intensity", Range(0, 8)) = 0.55
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.8
        _Pulse ("Beat Pulse", Range(0, 1)) = 0
        _PulseStrength ("Pulse Strength", Range(0, 5)) = 1.2
        _ScrollSpeedX ("Grid Scroll Speed X", Float) = 0.08
        _ScrollSpeedY ("Grid Scroll Speed Y", Float) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType"="Unlit"
        }
        LOD 100

        Pass
        {
            Name "SynthwaveGridBlock"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GridColor;
                half4 _EdgeColor;
                float _GridSpacing;
                float _LineThickness;
                float _GridIntensity;
                float _EdgeThickness;
                float _EdgeIntensity;
                float _RimIntensity;
                float _RimPower;
                float _Pulse;
                float _PulseStrength;
                float _ScrollSpeedX;
                float _ScrollSpeedY;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.worldPos);
                o.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;
                o.viewDir = GetWorldSpaceViewDir(o.worldPos);
                return o;
            }

            float GridLine(float value, float spacing, float thickness)
            {
                spacing = max(spacing, 0.0001);
                float grid = abs(frac(value / spacing - 0.5) - 0.5);
                float derivative = max(fwidth(value / spacing), 0.0001);
                return 1.0 - smoothstep(thickness, thickness + derivative, grid);
            }

            float FaceBorder(float2 uv, float thickness)
            {
                float2 distToEdge = min(uv, 1.0 - uv);
                float edgeDistance = min(distToEdge.x, distToEdge.y);
                float derivative = max(fwidth(edgeDistance), 0.0001);
                return 1.0 - smoothstep(thickness, thickness + derivative, edgeDistance);
            }

            float2 GetFaceGridUv(float3 worldPos, float3 worldNormal)
            {
                float3 axis = abs(normalize(worldNormal));

                if (axis.y > axis.x && axis.y > axis.z)
                {
                    return worldPos.xz;
                }

                if (axis.x > axis.z)
                {
                    return worldPos.yz;
                }

                return worldPos.xy;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float pulse = 1.0 + _Pulse * _PulseStrength;

                float2 gridUv = GetFaceGridUv(i.worldPos, normal);
                gridUv += _Time.y * float2(_ScrollSpeedX, _ScrollSpeedY);

                float grid = max(
                    GridLine(gridUv.x, _GridSpacing, _LineThickness),
                    GridLine(gridUv.y, _GridSpacing, _LineThickness)
                );

                float border = FaceBorder(i.uv, _EdgeThickness);
                float rim = pow(1.0 - saturate(dot(normal, viewDir)), _RimPower);

                half3 color = _BaseColor.rgb;
                color += _GridColor.rgb * grid * _GridIntensity * pulse;
                color += _EdgeColor.rgb * border * _EdgeIntensity * pulse;
                color += _EdgeColor.rgb * rim * _RimIntensity * pulse;

                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
