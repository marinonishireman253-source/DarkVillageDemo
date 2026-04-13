Shader "Custom/WetFloor"
{
    Properties
    {
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.25
        _WaterColor("Water Tint", Color) = (0.15, 0.18, 0.22, 0.35)
        _RippleScale("Ripple Scale", Float) = 8.0
        _RippleSpeed("Ripple Speed", Float) = 0.4
        _RippleStrength("Ripple Distortion", Range(0, 0.1)) = 0.025
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3.0
        _SpecularStrength("Specular Strength", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "WetFloorForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half  _ReflectionStrength;
                half4 _WaterColor;
                half  _RippleScale;
                half  _RippleSpeed;
                half  _RippleStrength;
                half  _FresnelPower;
                half  _SpecularStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
                float3 worldPos    : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                float3 viewDir     : TEXCOORD4;
            };

            // --- Procedural noise (hash-based) ---
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FBM(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * ValueNoise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vni = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = vpi.positionCS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(vpi.positionCS);
                output.worldPos = vpi.positionWS;
                output.worldNormal = vni.normalWS;
                output.viewDir = GetWorldSpaceNormalizeViewDir(vpi.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // --- Procedural ripple distortion ---
                float time = _Time.y * _RippleSpeed;
                float2 worldXZ = input.worldPos.xz * _RippleScale;

                float noise1 = FBM(worldXZ + float2(time * 0.7, time * 0.3), 4);
                float noise2 = FBM(worldXZ * 1.3 + float2(-time * 0.5, time * 0.6), 4);

                float2 distortion = (float2(noise1, noise2) - 0.5) * _RippleStrength;

                // --- SSR: sample opaque texture with distortion and Y-flip ---
                float2 reflUV = screenUV;
                reflUV.y = 1.0 - reflUV.y; // Y-flip for reflection
                reflUV += distortion;

                reflUV = clamp(reflUV, 0.001, 0.999);

                half3 reflColor = SampleSceneColor(reflUV);

                // --- Fresnel factor (view-angle dependent reflection) ---
                float fresnel = pow(1.0 - saturate(dot(input.viewDir, input.worldNormal)), _FresnelPower);

                // --- Specular highlight (simple fake sun/moon) ---
                float3 lightDir = normalize(float3(0.15, 0.9, -0.4));
                float3 halfDir = normalize(input.viewDir + lightDir);
                float spec = pow(saturate(dot(input.worldNormal, halfDir)), 64.0) * _SpecularStrength;

                // --- Compose ---
                float reflAmount = _ReflectionStrength * fresnel;

                half3 finalColor = lerp(_WaterColor.rgb, reflColor, reflAmount);
                finalColor += spec * half3(0.7, 0.75, 0.8);

                // Subtle darkening to simulate wet surface
                finalColor *= lerp(1.0, 0.7, _WaterColor.a * 0.3);

                // Alpha driven by fresnel + base water alpha
                float alpha = _WaterColor.a + reflAmount * 0.5 + spec * 0.15;
                alpha = saturate(alpha);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
