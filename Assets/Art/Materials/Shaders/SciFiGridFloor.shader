Shader "Vortex/SciFiGridFloor"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.012, 0.015, 0.024, 1.0)
        _MajorGridColor ("Major Grid Color", Color) = (0.0, 0.75, 1.0, 1.0)
        _MinorGridColor ("Minor Grid Color", Color) = (0.0, 0.28, 0.42, 1.0)
        _MajorGridSpacing ("Major Grid Spacing", Float) = 5.0
        _MinorGridSpacing ("Minor Grid Spacing", Float) = 1.0
        _LineWidth ("Line Width", Float) = 0.03
        _VignetteRadius ("Vignette Radius", Float) = 45.0
        _VignetteSoftness ("Vignette Softness", Float) = 18.0
        _CenterPulseFrequency ("Center Pulse Frequency", Float) = 1.5
        _CenterPulseIntensity ("Center Pulse Intensity", Float) = 0.12
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry" 
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float2 uv           : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MajorGridColor;
                float4 _MinorGridColor;
                float _MajorGridSpacing;
                float _MinorGridSpacing;
                float _LineWidth;
                float _VignetteRadius;
                float _VignetteSoftness;
                float _CenterPulseFrequency;
                float _CenterPulseIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = input.uv;
                return output;
            }

            float ComputeGrid(float2 coord, float spacing, float width)
            {
                float2 grid = abs(frac(coord / spacing - 0.5) - 0.5) / (fwidth(coord / spacing) + 0.0001);
                float lineVal = min(grid.x, grid.y);
                return 1.0 - saturate(lineVal - width);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 worldXZ = input.positionWS.xz;

                // Minor Grid lines
                float minorLine = ComputeGrid(worldXZ, _MinorGridSpacing, _LineWidth);
                // Major Grid lines
                float majorLine = ComputeGrid(worldXZ, _MajorGridSpacing, _LineWidth * 1.5);

                // Combine colors
                half3 col = _BaseColor.rgb;
                col = lerp(col, _MinorGridColor.rgb, minorLine * 0.45);
                col = lerp(col, _MajorGridColor.rgb, majorLine * 0.95);

                // Subtle radial center pulse / vortex feel
                float distFromCenter = length(worldXZ);
                float pulse = sin(_Time.y * _CenterPulseFrequency) * 0.5 + 0.5;
                float centerGlow = exp(-distFromCenter * 0.15) * pulse * _CenterPulseIntensity;
                col += _MajorGridColor.rgb * centerGlow;

                // Vignette / fade to dark edges
                float vignette = smoothstep(_VignetteRadius, _VignetteRadius - _VignetteSoftness, distFromCenter);
                col = lerp(_BaseColor.rgb * 0.3, col, saturate(vignette));

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
