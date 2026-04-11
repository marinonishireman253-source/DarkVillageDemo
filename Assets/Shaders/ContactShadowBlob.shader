Shader "Custom/ContactShadowBlob"
{
    Properties
    {
        _ShadowColor("Shadow Color", Color) = (0.05,0.05,0.06,0.32)
        _Softness("Softness", Range(0.01, 1)) = 0.42
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Blob"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
                half _Softness;
            CBUFFER_END

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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centeredUv = input.uv * 2.0 - 1.0;
                float ellipseDistance = dot(centeredUv, centeredUv);
                float edgeStart = saturate(1.0 - _Softness);
                float alpha = saturate(1.0 - smoothstep(edgeStart, 1.0, ellipseDistance));
                return half4(_ShadowColor.rgb, _ShadowColor.a * alpha);
            }
            ENDHLSL
        }
    }
}
