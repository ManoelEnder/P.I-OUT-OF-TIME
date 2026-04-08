Shader "Custom/CRT_Curvature"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Strength ("Curvature Strength", Float) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float2 centered = uv * 2.0 - 1.0;
                float dist = dot(centered, centered);

                centered *= 1.0 + _Strength * dist;

                uv = centered * 0.5 + 0.5;

                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return float4(0,0,0,1);

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}