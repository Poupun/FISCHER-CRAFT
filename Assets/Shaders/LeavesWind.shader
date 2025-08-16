Shader "Custom/LeavesWind"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.35
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
        Cull Back
        AlphaToMask On
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float _Cutoff; float _WindAmp; float _WindSpeed; float _WindScale; float _WindVertical; float4 _WindDir;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;
                // Wind offset based on world-ish position (object space works per block)
                float t = _Time.y * _WindSpeed;
                float2 dir = normalize(_WindDir.xy);
                float sway = sin( (pos.x * dir.x + pos.z * dir.y) * (4.0 * _WindScale) + t ) * _WindAmp;
                // Sub-variation
                sway += (hash31(pos * 3.17) - 0.5) * _WindAmp * 0.3;
                pos.xz += dir * sway;
                pos.y += sway * _WindVertical;
                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(c.a - _Cutoff);
                return c;
            }
            ENDHLSL
        }
        // Shadow caster pass so leaves contribute to shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST; float _Cutoff; float _WindAmp; float _WindSpeed; float _WindScale; float _WindVertical; float4 _WindDir;
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            float hash31(float3 p){ p=frac(p*0.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z);} 
            Varyings vert(Attributes IN){ Varyings OUT; float3 pos=IN.positionOS.xyz; float t=_Time.y*_WindSpeed; float2 dir=normalize(_WindDir.xy); float sway=sin((pos.x*dir.x+pos.z*dir.y)*(4.0*_WindScale)+t)*_WindAmp; sway += (hash31(pos*3.17)-0.5)*_WindAmp*0.3; pos.xz += dir*sway; pos.y += sway*_WindVertical; OUT.positionCS = TransformObjectToHClip(pos); OUT.uv = TRANSFORM_TEX(IN.uv,_BaseMap); return OUT;} 
            half4 frag(Varyings IN):SV_Target { half4 c = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,IN.uv); clip(c.a - _Cutoff); return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
