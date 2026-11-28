// Tela de tubo CRT para UI: curvatura (barril), cantos escuros, separacao RGB,
// linhas de varredura PARADAS (presas aos pixels da imagem) e vinheta.
Shader "Torradeira/CrtScreen"
{
    Properties
    {
        _MainTex ("Imagem", 2D) = "black" {}
        _Curvature ("Curvatura", Range(0, 0.5)) = 0
        _Lines ("Linhas (altura da imagem)", Float) = 270
        _LineStrength ("Forca das linhas", Range(0, 1)) = 0.12
        _RgbShift ("Separacao RGB (px)", Float) = 0.3
        _Vignette ("Vinheta", Range(0, 2)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Curvature, _Lines, _LineStrength, _RgbShift, _Vignette;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 c = i.uv * 2 - 1;
                c *= 1 + _Curvature * float2(c.y * c.y, c.x * c.x);
                float2 uv = c * 0.5 + 0.5;
                uv = saturate(uv);

                float shift = _RgbShift * _MainTex_TexelSize.x;
                float3 col;
                col.r = tex2D(_MainTex, uv + float2(shift, 0)).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - float2(shift, 0)).b;

                // Linha escura a cada pixel da imagem (nao anda).
                float scan = frac(uv.y * _Lines);
                col *= 1 - _LineStrength * smoothstep(0.55, 1.0, scan);

                // Vinheta e borda arredondada do vidro.
                float2 e = abs(c);
                float edge = pow(pow(e.x, 8) + pow(e.y, 8), 0.125);
                col *= saturate(1 - _Vignette * pow(edge, 3) * 0.6);

                return fixed4(col, i.color.a);
            }
            ENDCG
        }
    }
}
