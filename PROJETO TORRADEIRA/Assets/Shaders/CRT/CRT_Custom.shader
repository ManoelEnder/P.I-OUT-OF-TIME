Shader "Custom/CRT_Custom"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "CRT"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS =
                    GetFullScreenTriangleVertexPosition(input.vertexID);

                output.uv =
                    GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(1, 0, 0, 1);
            }

            ENDHLSL
        }
    }
}