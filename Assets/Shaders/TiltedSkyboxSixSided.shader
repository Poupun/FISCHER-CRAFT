Shader "Skybox/Tilted 6 Sided"
{
    Properties
    {
        _FrontTex ("Front (+Z)", 2D) = "white" {}
        _BackTex  ("Back (-Z)", 2D) = "white" {}
        _LeftTex  ("Left (+X)", 2D) = "white" {}
        _RightTex ("Right (-X)", 2D) = "white" {}
        _UpTex    ("Up (+Y)", 2D) = "white" {}
        _DownTex  ("Down (-Y)", 2D) = "white" {}

        _TiltX ("Tilt X (degrees)", Range(-45,45)) = 0
        _RotationY ("Rotation Y (degrees)", Range(0,360)) = 0
        _Exposure ("Exposure", Range(0, 8)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Front ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _FrontTex, _BackTex, _LeftTex, _RightTex, _UpTex, _DownTex;
            float _TiltX;    // degrees
            float _RotationY; // degrees
            float _Exposure;

            struct appdata { float4 vertex : POSITION; };
            struct v2f {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0; // cube direction from unit cube
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = normalize(v.vertex.xyz);
                return o;
            }

            float3x3 rotX(float a) { float s = sin(a), c = cos(a); return float3x3(1,0,0, 0,c,-s, 0,s,c); }
            float3x3 rotY(float a) { float s = sin(a), c = cos(a); return float3x3(c,0,s, 0,1,0, -s,0,c); }

            float2 CubeUV(float3 d, int face)
            {
                // Map direction to 2D UV per face, range [0,1]
                float2 uv;
                if (face == 0) { // +X (Left)
                    uv = float2(-d.z/d.x, d.y/d.x);
                } else if (face == 1) { // -X (Right)
                    uv = float2(d.z/-d.x, d.y/-d.x);
                } else if (face == 2) { // +Y (Up)
                    uv = float2(d.x/d.y, -d.z/d.y);
                } else if (face == 3) { // -Y (Down)
                    uv = float2(d.x/-d.y, d.z/-d.y);
                } else if (face == 4) { // +Z (Front)
                    uv = float2(d.x/d.z, d.y/d.z);
                } else { // -Z (Back)
                    uv = float2(-d.x/-d.z, d.y/-d.z);
                }
                return uv * 0.5 + 0.5;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Apply tilt and yaw
                float ax = radians(_TiltX);
                float ay = radians(_RotationY);
                float3 dir = normalize(mul(rotY(ay), mul(rotX(ax), normalize(i.dir))));

                // Select dominant axis
                float3 ad = abs(dir);
                int face = 0; // +X
                float m = ad.x;
                if (ad.y > m) { face = 2; m = ad.y; }
                if (ad.z > m) { face = 4; }

                // Adjust for negative faces
                if (face == 0 && dir.x < 0) face = 1; // -X
                if (face == 2 && dir.y < 0) face = 3; // -Y
                if (face == 4 && dir.z < 0) face = 5; // -Z

                float2 uv = CubeUV(dir, face);

                fixed4 col;
                if (face == 0) col = tex2D(_LeftTex, uv);
                else if (face == 1) col = tex2D(_RightTex, uv);
                else if (face == 2) col = tex2D(_UpTex, uv);
                else if (face == 3) col = tex2D(_DownTex, uv);
                else if (face == 4) col = tex2D(_FrontTex, uv);
                else col = tex2D(_BackTex, uv);

                col.rgb *= _Exposure;
                return col;
            }
            ENDHLSL
        }
    }
}
