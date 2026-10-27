Shader "AmberScreen/ModelReview/Grid"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (0.75, 0.75, 0.75, 0.55)
        _CellSize ("Cell Size (m)", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _LineColor;
                float _CellSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float cell = max(_CellSize, 1e-3);
                float2 uv = input.positionWS.xz / cell;
                float2 g = abs(frac(uv - 0.5) - 0.5) / fwidth(uv);
                float line = 1.0 - min(g.x, g.y);
                float a = saturate(line) * _LineColor.a;
                return half4(_LineColor.rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
