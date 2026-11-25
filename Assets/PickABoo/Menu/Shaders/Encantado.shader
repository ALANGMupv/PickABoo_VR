// Brillo "encantado" de los objetos del menu: borde de luz (fresnel) aditivo y pulsante.
// Se dibuja como capa extra encima del objeto; no usa luces reales.
Shader "PickABoo/Encantado"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.7, 0.3, 1)
        _Potencia ("Anchura del borde (mayor = mas fino)", Range(0.5, 8)) = 2.5
        _Intensidad ("Intensidad", Range(0, 8)) = 1.5
        _VelocidadPulso ("Velocidad del pulso", Range(0, 6)) = 1.6
        _AmplitudPulso ("Amplitud del pulso", Range(0, 1)) = 0.35
        _Realce ("Realce al apuntar (lo controla el script)", Range(0, 3)) = 0
        _Visibilidad ("Visibilidad (lo controla el script)", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Encantado"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Potencia, _Intensidad, _VelocidadPulso, _AmplitudPulso, _Realce, _Visibilidad;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert (Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.viewWS = GetWorldSpaceViewDir(posWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half ndv = saturate(dot(normalize(i.normalWS), normalize(i.viewWS)));
                half rim = pow(1.0h - ndv, _Potencia);
                half pulso = 1.0h + _AmplitudPulso * sin(_Time.y * _VelocidadPulso * 6.2831h);
                half k = rim * _Intensidad * pulso * (1.0h + _Realce) * _Visibilidad;
                return half4(_Color.rgb * k, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
