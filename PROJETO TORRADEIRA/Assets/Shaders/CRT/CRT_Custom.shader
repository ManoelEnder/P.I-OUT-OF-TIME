Shader "Custom/CRT_Custom"
{
    Properties
    {
        _Curvature ("Curvature", Range(0, 0.5)) = 0.12
        _Vignette ("Vignette", Range(0, 1)) = 0.25
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.08
        _ScanlineCount ("Scanline Count", Range(100, 1000)) = 500
        _RGBShift ("RGB Shift", Range(0, 0.01)) = 0.001
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1.05
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "CRT"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)

                float _Curvature;
                float _Vignette;
                float _ScanlineStrength;
                float _ScanlineCount;
                float _RGBShift;
                float _Brightness;

            CBUFFER_END


            float2 ApplyCurvature(float2 uv)
            {
                float2 centered = uv - 0.5;

                float2 curved;

                curved.x = centered.x *
                    (1.0 + _Curvature * centered.y * centered.y);

                curved.y = centered.y *
                    (1.0 + _Curvature * centered.x * centered.x);

                return curved + 0.5;
            }


            float CalculateVignette(float2 uv)
            {
                float2 centered = uv - 0.5;

                float distanceFromCenter =
                    dot(centered, centered) * 2.0;

                return saturate(
                    1.0 - distanceFromCenter * _Vignette
                );
            }


            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                float2 curvedUV = ApplyCurvature(uv);


                // Fora da tela curva
                if (curvedUV.x < 0.0 ||
                    curvedUV.x > 1.0 ||
                    curvedUV.y < 0.0 ||
                    curvedUV.y > 1.0)
                {
                    return half4(0, 0, 0, 1);
                }


                // Pequena separação RGB
                float2 rgbOffset = float2(_RGBShift, 0.0);

                half red =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        curvedUV + rgbOffset
                    ).r;

                half green =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        curvedUV
                    ).g;

                half blue =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        curvedUV - rgbOffset
                    ).b;

                half3 color = half3(
                    red,
                    green,
                    blue
                );


                // Scanlines
                float scanline =
                    sin(curvedUV.y * _ScanlineCount);

                scanline =
                    scanline * 0.5 + 0.5;

                scanline =
                    lerp(
                        1.0,
                        scanline,
                        _ScanlineStrength
                    );

                color *= scanline;


                // Vinheta
                color *= CalculateVignette(curvedUV);


                // Brilho
                color *= _Brightness;


                return half4(
                    saturate(color),
                    1.0
                );
            }

            ENDHLSL
        }
    }
}