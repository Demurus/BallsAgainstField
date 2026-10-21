Shader "SDF Physics/Indirect Ball"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.08, 0.65, 1.0, 1.0)
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnityIndirect.cginc"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _AmbientStrength;
            CBUFFER_END

            StructuredBuffer<float4> _BallInstanceData;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                InitIndirectDrawArgs(0);
                const uint instanceIndex = GetIndirectInstanceID(input.instanceID);
                const float4 instance = _BallInstanceData[instanceIndex];

                Varyings output;
                const float3 positionWS = input.positionOS.xyz * instance.w + instance.xyz;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = input.normalOS;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                const Light mainLight = GetMainLight();
                const half lightAmount = saturate(dot(normalize(input.normalWS), mainLight.direction));
                const half3 lighting = _AmbientStrength + lightAmount * mainLight.color;
                return half4(_BaseColor.rgb * lighting, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
