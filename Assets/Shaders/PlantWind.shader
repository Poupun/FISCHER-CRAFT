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
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST; float _Cutoff; float _WindAmp; float _WindSpeed; float _WindScale; float _WindVertical; float4 _WindDir; float _WindVar;

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; }; 
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float bendW: TEXCOORD1; };

            float hash21(float2 p){ p=frac(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return frac(p.x*p.y); }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;
                float w = IN.color.r; // bend weight 0..1
                // Use base (XZ) for phase; allow per-quad random variation
                float2 dir = normalize(float2(_WindDir.x, _WindDir.y));
                float phase = (pos.x*dir.x + pos.z*dir.y) * (3.0*_WindScale);
                // Add random offset per cross quad using hash of sign combinations
                float rnd = hash21(floor(pos.xz*5.0)) * _WindVar;
                float t = _Time.y * _WindSpeed + rnd * 6.2831;
                float sway = sin(phase + t) * _WindAmp;
                // Apply sway scaled by weight so base (0) stays anchored.
                pos.xz += dir * (sway * w);
                pos.y += (sway * _WindVertical * w);
                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.bendW = w;
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
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
