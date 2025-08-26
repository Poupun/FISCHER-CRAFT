Shader "Custom/LeavesWind"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _WindAmp ("Wind Amplitude", Range(0,0.2)) = 0.05
        _WindSpeed ("Wind Speed", Range(0,5)) = 1.2
        _WindScale ("Wind Scale", Range(0,2)) = 0.5
        _WindVertical ("Vertical Factor", Range(0,1)) = 0.3
        _WindDir ("Wind Direction", Vector) = (1,0,0,0)
    }
    SubShader
    {
    Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
    Cull Off
        AlphaToMask Off
        ZWrite On
        HLSLINCLUDE
        #pragma target 3.0
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode" = "UniversalForward" }
            Blend Off
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float _Cutoff; float _WindAmp; float _WindSpeed; float _WindScale; float _WindVertical; float4 _WindDir;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 pos = IN.positionOS.xyz;
                float t = _Time.y * _WindSpeed;
                float2 dir = normalize(_WindDir.xy);
                float sway = sin( (pos.x * dir.x + pos.z * dir.y) * (4.0 * _WindScale) + t ) * _WindAmp;
                sway += (hash31(pos * 3.17) - 0.5) * _WindAmp * 0.3;
                pos.xz += dir * sway;
                pos.y += sway * _WindVertical;
                float3 ws = TransformObjectToWorld(pos);
                OUT.positionCS = TransformWorldToHClip(ws);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.positionWS = ws;
                OUT.normalWS = float3(0,1,0);
                #if defined(SHADOWS_SCREEN)
                    OUT.shadowCoord = GetShadowCoord(OUT.positionCS);
                #else
                    OUT.shadowCoord = TransformWorldToShadowCoord(ws);
                #endif
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(c.a - _Cutoff);
                Light mainLight = GetMainLight(IN.shadowCoord);
                half3 L = normalize(mainLight.direction);
                half NdotL = saturate(dot(IN.normalWS, L));
                half lambert = NdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 ambient = SampleSH(IN.normalWS);
                half3 lighting = ambient + mainLight.color.rgb * lambert;
                return half4(c.rgb * lighting, c.a);
            }
            ENDHLSL
        }

        // Provide a ForwardOnly pass like PlantWind to satisfy renderers that request it
        Pass
        {
            Name "ForwardOnly"
            Tags{ "LightMode" = "UniversalForwardOnly" }
            Blend Off
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float _Cutoff; float _WindAmp; float _WindSpeed; float _WindScale; float _WindVertical; float4 _WindDir;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; float3 normalWS:TEXCOORD2; float4 shadowCoord:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float hash31(float3 p){ p=frac(p*0.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z);} 
            Varyings vert(Attributes IN)
            {
                Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 pos = IN.positionOS.xyz;
                float t = _Time.y * _WindSpeed;
                float2 dir = normalize(_WindDir.xy);
                float sway = sin( (pos.x*dir.x + pos.z*dir.y) * (4.0*_WindScale) + t ) * _WindAmp;
                sway += (hash31(pos * 3.17) - 0.5) * _WindAmp * 0.3;
                pos.xz += dir * sway; pos.y += sway * _WindVertical;
                float3 ws = TransformObjectToWorld(pos);
                OUT.positionCS = TransformWorldToHClip(ws);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.positionWS = ws; OUT.normalWS = float3(0,1,0);
                #if defined(SHADOWS_SCREEN)
                    OUT.shadowCoord = GetShadowCoord(OUT.positionCS);
                #else
                    OUT.shadowCoord = TransformWorldToShadowCoord(ws);
                #endif
                return OUT;
            }
            half4 frag(Varyings IN):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,IN.uv);
                clip(c.a - _Cutoff);
                Light mainLight=GetMainLight(IN.shadowCoord);
                half3 L=normalize(mainLight.direction);
                half NdotL=saturate(dot(IN.normalWS,L));
                half lambert=NdotL*mainLight.distanceAttenuation*mainLight.shadowAttenuation;
                half3 ambient=SampleSH(IN.normalWS);
                half3 lighting=ambient + mainLight.color.rgb*lambert;
                return half4(c.rgb*lighting, c.a);
            }
            ENDHLSL
        }
        // Shadow caster pass so leaves contribute to shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma multi_compile_instancing
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; float _Cutoff; float _WindAmp; float _WindSpeed; float _WindScale; float _WindVertical; float4 _WindDir;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float hash31(float3 p){ p=frac(p*0.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z);} 
            Varyings vert(Attributes IN){ Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT); float3 pos=IN.positionOS.xyz; float t=_Time.y*_WindSpeed; float2 dir=normalize(_WindDir.xy); float sway=sin((pos.x*dir.x+pos.z*dir.y)*(4.0*_WindScale)+t)*_WindAmp; sway += (hash31(pos*3.17)-0.5)*_WindAmp*0.3; pos.xz += dir*sway; pos.y += sway*_WindVertical; OUT.positionCS = TransformObjectToHClip(pos); OUT.uv = TRANSFORM_TEX(IN.uv,_BaseMap); return OUT;} 
            half4 frag(Varyings IN):SV_Target { UNITY_SETUP_INSTANCE_ID(IN); half4 c = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,IN.uv); clip(c.a - _Cutoff); return 0; }
            ENDHLSL
        }

        // Fallback unlit so leaves still render with wind under any URP renderer
        Pass
        {
            Name "SRPDefaultUnlit"
            Tags{ "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            Cull Off
            AlphaToMask Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma multi_compile_instancing
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; float _Cutoff; float _WindAmp; float _WindSpeed; float _WindScale; float _WindVertical; float4 _WindDir;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float hash31(float3 p){ p=frac(p*0.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z);} 
            Varyings vert(Attributes IN){ Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT); float3 pos=IN.positionOS.xyz; float t=_Time.y*_WindSpeed; float2 dir=normalize(_WindDir.xy); float sway=sin((pos.x*dir.x+pos.z*dir.y)*(4.0*_WindScale)+t)*_WindAmp; sway += (hash31(pos*3.17)-0.5)*_WindAmp*0.3; pos.xz += dir*sway; pos.y += sway*_WindVertical; OUT.positionCS=TransformObjectToHClip(pos); OUT.uv=TRANSFORM_TEX(IN.uv,_BaseMap); return OUT;}
            half4 frag(Varyings IN):SV_Target{ UNITY_SETUP_INSTANCE_ID(IN); half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,IN.uv); clip(c.a - _Cutoff); return c; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
