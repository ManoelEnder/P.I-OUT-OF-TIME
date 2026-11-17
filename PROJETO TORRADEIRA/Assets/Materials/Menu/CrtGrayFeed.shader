// Tela de CRT em preto e branco: como a primeira foto digital (1975), so tons de cinza.
Shader "OutOfTime/CrtGrayFeed"
{
    Properties
    {
        _BaseMap ("Imagem", 2D) = "white" {}
        _Brightness ("Brilho", Float) = 1.3
        _Contrast ("Contraste", Float) = 1.25
        _Levels ("Niveis de cinza", Float) = 16
        _Scanline ("Scanline", Range(0, 1)) = 0.25
        _Lines ("Linhas", Float) = 100
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Brightness;
                float _Contrast;
                float _Levels;
                float _Scanline;
                float _Lines;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half gray = dot(color, half3(0.299, 0.587, 0.114));
                gray = saturate((gray - 0.5) * _Contrast + 0.5);
                gray = floor(gray * _Levels) / _Levels;

                half scan = 1.0 - _Scanline * step(0.5, frac(input.uv.y * _Lines));
                return half4(gray.xxx * scan * _Brightness, 1.0);
            }
            ENDHLSL
        }
    }
}
