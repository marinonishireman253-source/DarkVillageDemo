Shader "Custom/BillboardCharacterLit"
{
    Properties
    {
        [PerRendererData] [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
        _AmbientColor("Ambient Color", Color) = (0.82,0.82,0.86,1)
        _AmbientIntensity("Ambient Intensity", Range(0,2)) = 0.72
        _CharacterMainLightColor("Main Light Color", Color) = (1,1,1,1)
        _CharacterMainLightDirection("Main Light Direction", Vector) = (0.18,-0.96,-0.2,0)
        _CharacterMainLightStrength("Main Light Strength", Range(0,2)) = 0.38
        _AccentColor("Accent Color", Color) = (1,0.9,0.78,1)
        _AccentDirection("Accent Direction", Vector) = (-0.25,-0.84,-0.42,0)
        _AccentStrength("Accent Strength", Range(0,2)) = 0.3
        _LocalLightColor("Local Light Color", Color) = (0,0,0,1)
        _LocalLightPosition("Local Light Position", Vector) = (0,2.5,0,1)
        _LocalLightRange("Local Light Range", Float) = 8
        _LocalLightStrength("Local Light Strength", Range(0,2)) = 0
        _RimColor("Rim Color", Color) = (1,0.96,0.9,1)
        _RimStrength("Rim Strength", Range(0,2)) = 0.22
        _ShadowStrength("Shadow Strength", Range(0,1)) = 0.56
        _MinimumLight("Minimum Light", Range(0,1)) = 0.46
        _NormalBendX("Normal Bend X", Range(0,4)) = 1.2
        _NormalBendY("Normal Bend Y", Range(0,4)) = 1.85
        _AlphaClip("Alpha Clip", Range(0,1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SpriteForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _AmbientColor;
                half4 _CharacterMainLightColor;
                half4 _CharacterMainLightDirection;
                half4 _AccentColor;
                half4 _AccentDirection;
                half4 _LocalLightColor;
                half4 _LocalLightPosition;
                half4 _RimColor;
                half _AmbientIntensity;
                half _CharacterMainLightStrength;
                half _AccentStrength;
                half _LocalLightRange;
                half _LocalLightStrength;
                half _RimStrength;
                half _ShadowStrength;
                half _MinimumLight;
                half _NormalBendX;
                half _NormalBendY;
                half _AlphaClip;
            CBUFFER_END

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 viewDirectionWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.viewDirectionWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.uv = input.uv;
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                clip(baseSample.a - _AlphaClip);

                float2 centeredUv = input.uv * 2.0 - 1.0;
                float3 fauxNormalOS = normalize(float3(centeredUv.x * _NormalBendX, (centeredUv.y - 0.18) * _NormalBendY, 1.0));
                float3 fauxNormalWS = normalize(TransformObjectToWorldDir(fauxNormalOS));
                float3 viewDirectionWS = normalize(input.viewDirectionWS);

                float3 mainLightDirection = normalize(_CharacterMainLightDirection.xyz);
                float mainNdotL = saturate(dot(fauxNormalWS, -mainLightDirection));
                float shadowedMain = lerp(1.0 - _ShadowStrength, 1.0, mainNdotL);

                float3 accentDirection = normalize(_AccentDirection.xyz);
                float accentNdotL = saturate(dot(fauxNormalWS, -accentDirection));

                float3 toLocalLight = _LocalLightPosition.xyz - input.positionWS;
                float localLightDistance = max(length(toLocalLight), 0.0001);
                float3 localLightDirection = toLocalLight / localLightDistance;
                float localAttenuation = saturate(1.0 - localLightDistance / max(_LocalLightRange, 0.0001));
                localAttenuation *= localAttenuation;
                float localNdotL = saturate(dot(fauxNormalWS, localLightDirection));

                float rim = pow(1.0 - saturate(dot(fauxNormalWS, viewDirectionWS)), 3.0) * _RimStrength;

                float3 lighting = _AmbientColor.rgb * _AmbientIntensity;
                lighting += _CharacterMainLightColor.rgb * (_CharacterMainLightStrength * shadowedMain);
                lighting += _AccentColor.rgb * (_AccentStrength * pow(accentNdotL, 1.15));
                lighting += _LocalLightColor.rgb * (_LocalLightStrength * localAttenuation * localNdotL);
                lighting += _RimColor.rgb * rim;
                lighting = max(lighting, _MinimumLight.xxx);

                return half4(baseSample.rgb * lighting, baseSample.a);
            }
            ENDHLSL
        }
    }
}
