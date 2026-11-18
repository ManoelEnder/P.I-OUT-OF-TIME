// Diafragma de lente em tela cheia para abrir/fechar o visor.
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
        _Feather ("Suavidade", Float) = 0.012

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
            float _Radius, _Rotation, _Aspect, _Blades, _Twist, _Feather;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const float TAU = 6.2831853;
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1.0) * 2.0;
                float r = length(p);
                float seg = TAU / _Blades;

                // Distancia "poligonal": apotema do hexagono que passa pelo ponto.
                float a = atan2(p.y, p.x) + _Rotation;
                float k = fmod(a + 100.0 * seg, seg) - seg * 0.5;
                float m = r * cos(k);

                float alpha = smoothstep(_Radius, _Radius + _Feather, m);

                // Laminas em espiral: cada uma com um cinza levemente diferente.
                float bladeAngle = a + (m - _Radius) * _Twist;
                float index = floor(fmod(bladeAngle + 100.0 * seg, TAU) / seg);
                float shade = 0.035 + 0.025 * fmod(index, 2.0) + 0.012 * fmod(index, 3.0);

                // Fio de luz na borda da abertura e escurecendo para fora.
                float edge = m - _Radius;
                float rim = 1.0 - smoothstep(0.0, 0.035, edge);
                shade = lerp(shade, 0.2, rim * 0.5);
                shade *= lerp(1.0, 0.55, saturate(edge * 0.8));

                return fixed4(shade, shade, shade, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
