Shader "Custom/PlantWind"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _WindAmp ("Wind Amplitude", Range(0,0.25)) = 0.07
        _WindSpeed ("Wind Speed", Range(0,5)) = 1.4
        _WindScale ("Wind Scale", Range(0,3)) = 0.8
        _WindVertical ("Vertical Factor", Range(0,1)) = 0.2
        _WindVar ("Variation", Range(0,1)) = 0.5
        _WindDir ("Wind Direction", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 150
        Cull Off
        AlphaToMask On
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode" = "UniversalForward" }
            Blend Off

            HLSLPROGRAM
            #pragma target 3.0
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
            float _Cutoff; 
            float _WindAmp; 
            float _WindSpeed; 
            float _WindScale; 
            float _WindVertical; 
            float4 _WindDir; 
            float _WindVar;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; float3 normalWS:TEXCOORD2; float4 shadowCoord:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };

            float hash21(float2 p){ p=frac(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return frac(p.x*p.y); }

            Varyings vert(Attributes IN)
            {
                Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 pos = IN.positionOS.xyz;
                float w = IN.color.r; // bend weight 0..1
                float2 dir = normalize(float2(_WindDir.x, _WindDir.y));
                float phase = (pos.x*dir.x + pos.z*dir.y) * (3.0*_WindScale);
                float rnd = hash21(floor(pos.xz*5.0)) * _WindVar;
                float t = _Time.y * _WindSpeed + rnd * 6.2831;
                float sway = sin(phase + t) * _WindAmp;
                pos.xz += dir * (sway * w);
                pos.y += (sway * _WindVertical * w);
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
                // Simple lit with main light + shadows and ambient
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

        // Some URP configurations look for 'UniversalForwardOnly'. Provide the same pass.
        Pass
        {
            Name "ForwardOnly"
            Tags{ "LightMode" = "UniversalForwardOnly" }
            Blend Off

            HLSLPROGRAM
            #pragma target 3.0
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
            float _Cutoff; 
            float _WindAmp; 
            float _WindSpeed; 
            float _WindScale; 
            float _WindVertical; 
            float4 _WindDir; 
            float _WindVar;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; float3 normalWS:TEXCOORD2; float4 shadowCoord:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float hash21(float2 p){ p=frac(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return frac(p.x*p.y); }
            Varyings vert(Attributes IN)
            {
                Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT); float3 pos = IN.positionOS.xyz; float w = IN.color.r; float2 dir = normalize(float2(_WindDir.x, _WindDir.y));
                float phase = (pos.x*dir.x + pos.z*dir.y) * (3.0*_WindScale);
                float rnd = hash21(floor(pos.xz*5.0)) * _WindVar;
                float t = _Time.y * _WindSpeed + rnd * 6.2831;
                float sway = sin(phase + t) * _WindAmp;
                pos.xz += dir * (sway * w); pos.y += (sway * _WindVertical * w);
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
            half4 frag(Varyings IN) : SV_Target
            { UNITY_SETUP_INSTANCE_ID(IN); half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv); clip(c.a - _Cutoff);
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

        // Shadow caster so animated plants contribute to shadows when desired
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex sc_vert
            #pragma fragment sc_frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma multi_compile_instancing
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; float _Cutoff; float _WindAmp; float _WindSpeed; float _WindScale; float _WindVertical; float4 _WindDir; float _WindVar;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float hash21(float2 p){ p=frac(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return frac(p.x*p.y); }
            Varyings sc_vert(Attributes IN)
            { Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT); float3 pos = IN.positionOS.xyz; float w = IN.color.r; float2 dir = normalize(float2(_WindDir.x,_WindDir.y));
              float phase = (pos.x*dir.x + pos.z*dir.y) * (3.0*_WindScale); float rnd = hash21(floor(pos.xz*5.0)) * _WindVar; float t = _Time.y*_WindSpeed + rnd*6.2831; float sway = sin(phase + t) * _WindAmp; pos.xz += dir*(sway*w); pos.y += (sway*_WindVertical*w);
              OUT.positionCS = TransformObjectToHClip(pos); OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap); return OUT; }
            half4 sc_frag(Varyings IN):SV_Target { UNITY_SETUP_INSTANCE_ID(IN); half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv); clip(c.a - _Cutoff); return 0; }
            ENDHLSL
        }

                // Fallback for any URP renderer (e.g., 2D Renderer) so we never go magenta
                Pass
                {
                        Name "SRPDefaultUnlit"
                        Tags{ "LightMode" = "SRPDefaultUnlit" }
                        Blend Off
                        ZWrite On
                        Cull Off
                        HLSLPROGRAM
                        #pragma target 3.0
                        #pragma vertex vert
                        #pragma fragment frag
                        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                        #pragma multi_compile_instancing

                        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
                        CBUFFER_START(UnityPerMaterial)
                        float4 _BaseMap_ST; float _Cutoff; float _WindAmp; float _WindSpeed; float _WindScale; float _WindVertical; float4 _WindDir; float _WindVar;
                        CBUFFER_END
                        struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
                        struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
                        float hash21(float2 p){ p=frac(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return frac(p.x*p.y); }
                        Varyings vert(Attributes IN)
                        { Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT); float3 pos=IN.positionOS.xyz; float w=IN.color.r; float2 dir=normalize(float2(_WindDir.x,_WindDir.y));
                            float phase=(pos.x*dir.x+pos.z*dir.y)*(3.0*_WindScale); float rnd=hash21(floor(pos.xz*5.0))*_WindVar; float t=_Time.y*_WindSpeed + rnd*6.2831; float sway=sin(phase+t)*_WindAmp;
                            pos.xz += dir*(sway*w); pos.y += (sway*_WindVertical*w);
                            OUT.positionCS = TransformObjectToHClip(pos); OUT.uv = TRANSFORM_TEX(IN.uv,_BaseMap); return OUT; }
                        half4 frag(Varyings IN):SV_Target{ UNITY_SETUP_INSTANCE_ID(IN); half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,IN.uv); clip(c.a - _Cutoff); return c; }
                        ENDHLSL
                }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
