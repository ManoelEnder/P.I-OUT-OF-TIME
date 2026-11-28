// Passe de tela cheia do visual retro (URP Full Screen Pass Renderer Feature).
// Paleta do jogo: cinzas levemente frios + vermelho preservado (calor/perigo), posterizados
// com pontilhado ordenado (Bayer 4x4) no pixel da imagem de render (o pixel "grande").
Shader "Retro/Palette"
{
    Properties
    {
        _Levels ("Tons por canal", Range(2, 16)) = 8
        _RedThreshold ("Limiar do vermelho (r / max(g,b))", Range(1, 4)) = 1.6
        _ColdTint ("Tom dos cinzas", Color) = (0.92, 0.96, 1.0, 1)
        _Dither ("Forca do pontilhado", Range(0, 1)) = 1
        _Strength ("Intensidade do efeito", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "RetroPalette"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Levels;
            float _RedThreshold;
            float4 _ColdTint;
            float _Dither;
            float _Strength;

            static const float Bayer4[16] =
            {
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            };

            // Quantiza em espaco perceptual (gama): em linear quase todo o laboratorio escuro
            // ficaria abaixo do primeiro degrau e viraria preto.
            float Quantize(float linearValue, float levels, float dither)
            {
                float perceptual = pow(saturate(linearValue), 1.0 / 2.2);
                float stepped = floor(saturate(perceptual + dither / (levels - 1.0)) * (levels - 1.0) + 0.5) / (levels - 1.0);
                return pow(stepped, 2.2);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                float3 color = source.rgb;

                // Posicao em pixels da imagem de render: o padrao acompanha o pixel grande.
                uint2 pixel = (uint2)input.positionCS.xy;
                float dither = ((Bayer4[(pixel.x & 3) + (pixel.y & 3) * 4] + 0.5) / 16.0 - 0.5) * _Dither;

                float levels = max(_Levels, 2.0);
                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                float3 gray = Quantize(luminance, levels, dither) * _ColdTint.rgb;

                // Vermelho de verdade (LEDs, calor, perigo) continua vermelho.
                float redness = color.r / max(max(color.g, color.b), 0.001);
                float isRed = saturate((redness - _RedThreshold) * 2.0) * step(0.05, color.r);
                float redValue = Quantize(color.r, levels, dither);
                float3 red = float3(redValue, redValue * 0.1, redValue * 0.08);

                // Amarelo forte (urina) tambem fica amarelo: r e g altos, b quase zero.
                float yellowness = min(color.r, color.g) / max(color.b, 0.001);
                float isYellow = saturate((yellowness - 2.5) * 1.0) * step(0.03, color.g)
                               * saturate(1.0 - abs(color.r - color.g) / max(color.r, 0.001) * 1.5) * (1.0 - isRed);
                float yellowValue = Quantize(max(color.r, color.g), levels, dither);
                float3 yellow = float3(yellowValue, yellowValue * 0.82, yellowValue * 0.12);

                float3 retro = lerp(lerp(gray, yellow, isYellow), red, isRed);
                return half4(lerp(color, retro, _Strength), source.a);
            }
            ENDHLSL
        }
    }
}
