// Contorno simple para resaltar objetos (URP, compatible con Quest / Single Pass Instanced).
// Dibuja el objeto un poco "inflado" por detrás, en un color plano: se ve como un borde.
// Lo usa el script Highlighter. Crea un material con este shader y asígnalo en HighlightSettings.
Shader "Custom/HighlightOutline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.8, 0.2, 1)
        _Width ("Grosor (m)", Float) = 0.004
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+10" "RenderType" = "Transparent" }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                positionWS += normalWS * _Width;   // grosor en metros, no depende de la escala del objeto
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
