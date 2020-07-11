Shader "Hidden/GlitchShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Amount  ("Amount", Range(0,1)) = 0.0
        _Seed    ("Seed", Float) = 13214
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float rand3(float3 myVector) {
                return frac(sin(dot(myVector, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
            }

            float rand2(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453123);
            }

            float rand(float x)
            {
                return frac(sin(x * 12.9898)*43758.5453123);
            }

            sampler2D _MainTex;
            float _Amount;
            float _Seed;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float2 tile_id = trunc(float2(i.uv.x * 160 /12, i.uv.y * 160/2));                

                if (rand2(tile_id * _Seed) < _Amount * 0.8)
                {
                    col = tex2D(_MainTex, float2(1,1) - i.uv);
                }

                float2 tile_id2 = trunc(float2(i.uv.x * 160 / 8, i.uv.y * 160 / 4));

                if (rand2(tile_id2 * _Seed + 0.01) < _Amount * 0.6)
                {
                    col = tex2D(_MainTex, float2(1 - i.uv.x, i.uv.y));
                }

                float2 tile_id3 = trunc(float2(i.uv.x * 160 / 8, i.uv.y * 160 / 8));

                if (rand2(tile_id3 * _Seed + 0.032) < _Amount * 0.2)
                {
                    col = tex2D(_MainTex, tile_id3 / 160);
                }
                
                // just invert the colors
                col.rgb = col.rgb;
                return col;
            }
            ENDCG
        }
    }
}
