Shader "AmberScreen/ModelReview/ClayWire"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        _WireColor ("Wire Color", Color) = (0.15, 0.15, 0.15, 1)
        _WireWidth ("Wire Width (px)", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require geometry
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _WireColor;
                float _WireWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 bary : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = nrm.normalWS;
                o.bary = 0;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle Varyings input[3], inout TriangleStream<Varyings> stream)
            {
                Varyings v0 = input[0]; v0.bary = float3(1, 0, 0);
                Varyings v1 = input[1]; v1.bary = float3(0, 1, 0);
                Varyings v2 = input[2]; v2.bary = float3(0, 0, 1);
                stream.Append(v0);
                stream.Append(v1);
                stream.Append(v2);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float ndl = saturate(dot(n, mainLight.direction));
                float3 ambient = SampleSH(n) * 0.35;
                float3 lit = _BaseColor.rgb * (ambient + mainLight.color * ndl * mainLight.shadowAttenuation);

                float3 d = fwidth(input.bary);
                float3 a = smoothstep(0, d * max(_WireWidth, 0.5), input.bary);
                float edge = 1.0 - min(min(a.x, a.y), a.z);
                float3 col = lerp(lit, _WireColor.rgb, saturate(edge));
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    FallBack Off
}
