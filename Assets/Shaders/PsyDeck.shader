Shader "Trance/PsyDeck"
{
    Properties
    {
        _Tint("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Beat;
            float4 _Tint;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // caleidoscopio: espelha o eixo x
                float2 p = IN.uv * float2(4.0, 16.0);
                p.x = abs(frac(p.x * 0.5) - 0.5) * 2.0;
                float t = _Time.y * 0.30;

                // veias fluidas
                float v1 = sin(p.x * 6.2831 + t * 2.0 + sin(p.y * 6.2831 - t) * 1.5);
                float v2 = sin((p.x + p.y) * 6.2831 * 0.7 - t * 1.4);
                float veins = pow(abs(v1 * v2), 0.35);

                // fluxo em direcao ao jogador
                float flow = pow(0.5 + 0.5 * sin((frac(p.y * 0.5 - _Time.y * 0.55) - 0.5) * 6.2831), 2.0);

                // neon ciclando roxo -> ciano -> magenta
                float3 neon = lerp(float3(0.55, 0.10, 0.95), float3(0.0, 0.80, 1.0),
                                   0.5 + 0.5 * sin(t * 2.0 + p.y * 3.0 + p.x * 2.0));
                neon = lerp(neon, float3(1.0, 0.15, 0.55), 0.25 + 0.25 * sin(t * 1.3 + p.x * 4.0));

                float3 col = float3(0.008, 0.004, 0.025);
                col += neon * veins * (0.45 + _Beat * 1.6) * 1.7;
                col += neon * flow * 0.12;
                return half4(col * _Tint.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
