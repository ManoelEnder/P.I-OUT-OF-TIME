// Camada extra das pecas temporais: borda branca brilhante (fresnel) com linhas de glitch subindo.
// Aditiva: soma por cima do material original. Pulso lento so com flashes ligados
// (global _RetroFlashes, definido por FlashSettings: 1 = ligados, 0 = desligados).
Shader "Retro/TemporalRim"
{
    Properties
    {
        _Color ("Cor", Color) = (1, 1, 1, 1)
        _Intensity ("Intensidade", Range(0, 8)) = 2.5
        _RimPower ("Largura da borda", Range(0.5, 8)) = 2.5
        _LineDensity ("Linhas por metro", Range(5, 200)) = 60
        _Speed ("Velocidade das linhas", Range(0, 5)) = 1.5
        _Pulse ("Pulso", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "TemporalRim"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
                float _RimPower;
                float _LineDensity;
                float _Speed;
                float _Pulse;
            CBUFFER_END

            float _RetroFlashes;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float rim = pow(1.0 - saturate(dot(normalize(input.normalWS), view)), _RimPower);

                float lines = step(0.5, frac(input.positionWS.y * _LineDensity - _Time.y * _Speed));
                float pulse = lerp(1.0, 0.6 + 0.4 * sin(_Time.y * 3.0), _Pulse * saturate(_RetroFlashes));

                float glow = (rim * 0.85 + rim * lines * 0.35 + 0.12) * pulse;
                return half4(_Color.rgb * _Intensity * glow, 1.0);
            }
            ENDHLSL
        }
    }
}
