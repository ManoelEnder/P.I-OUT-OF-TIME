Shader "Custom/Pixelation"
{
    Properties
    {
        _PixelSize ("Pixel Size", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelSize;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float2 size = max(_PixelSize, 1.0);

                float2 screenSize =
                    _ScreenParams.xy;

                float2 pixelCount =
                    screenSize / size;

                uv =
                    floor(uv * pixelCount) /
                    pixelCount;

                return
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        uv
                    );
            }

            ENDHLSL
        }
    }
}