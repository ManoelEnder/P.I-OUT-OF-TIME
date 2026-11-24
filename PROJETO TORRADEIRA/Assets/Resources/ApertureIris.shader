// Diafragma de lente em tela cheia (visor da camera), no estilo PSX:
// desenhado em baixa resolucao (_Lines linhas), com pontilhado Bayer 4x4, paleta curta de cinzas,
// costura escura entre as laminas e um fio de luz na borda da abertura.
// _Radius = abertura (apotema do poligono, em meias-alturas da tela); _Rotation gira as laminas.
Shader "OutOfTime/ApertureIris"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Radius ("Abertura", Float) = 0
        _Rotation ("Rotacao", Float) = 0
        _Aspect ("Aspecto", Float) = 1.777
        _Blades ("Laminas", Float) = 6
        _Twist ("Espiral", Float) = 0.9
        _Lines ("Linhas (pixel art)", Float) = 240

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            float _Radius, _Rotation, _Aspect, _Blades, _Twist, _Lines;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            static const float BayerM[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            float Bayer4(float2 p)
            {
                int2 i = int2(fmod(p, 4.0));
                return (BayerM[i.y * 4 + i.x] + 0.5) / 16.0;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const float TAU = 6.2831853;

                // Pixel grande: tudo desenhado numa grade de _Lines linhas.
                float2 res = float2(_Lines * _Aspect, _Lines);
                float2 cell = floor(i.uv * res);
                float2 uv = (cell + 0.5) / res;
                float px = 2.0 / _Lines; // um pixel em meias-alturas

                float2 p = (uv - 0.5) * float2(_Aspect, 1.0) * 2.0;
                float r = length(p);
                float seg = TAU / _Blades;

                float a = atan2(p.y, p.x) + _Rotation;
                float k = fmod(a + 100.0 * seg, seg) - seg * 0.5;
                float m = r * cos(k);

                // Borda dura (pixel art): ou e lamina ou e abertura.
                float edge = m - _Radius;
                float alpha = edge > 0.0 ? 1.0 : 0.0;

                // Laminas em espiral.
                float bladeAngle = a + edge * _Twist;
                float bladePos = fmod(bladeAngle + 100.0 * seg, seg) / seg;
                float index = floor(fmod(bladeAngle + 100.0 * TAU, TAU) / seg);

                // Tom de cada lamina + escurece para fora, quantizado com pontilhado.
                float tone = 0.55 + 0.25 * fmod(index, 2.0) + 0.12 * fmod(index, 3.0);
                tone *= lerp(1.0, 0.45, saturate(edge * 0.9));
                float levels = 4.0;
                float dithered = floor(tone * levels + Bayer4(cell)) / levels;

                // Paleta curta de cinzas frios (PSX).
                float3 c0 = float3(0.02, 0.02, 0.025);
                float3 c1 = float3(0.07, 0.07, 0.08);
                float3 c2 = float3(0.13, 0.13, 0.14);
                float3 c3 = float3(0.21, 0.21, 0.22);
                float3 col = dithered < 0.25 ? c0 : dithered < 0.5 ? c1 : dithered < 0.75 ? c2 : c3;

                // Costura escura entre as laminas (1 pixel).
                float seamWidth = px / max(r, 0.05) / seg * 1.2;
                if (bladePos < seamWidth)
                    col = c0;

                // Fio de luz de 1 pixel na borda da abertura.
                if (edge > 0.0 && edge < px * 1.5)
                    col = float3(0.42, 0.42, 0.43);

                return fixed4(col, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
