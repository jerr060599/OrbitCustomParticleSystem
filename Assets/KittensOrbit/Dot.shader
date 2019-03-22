Shader "Unlit/Dot"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
			Cull Off

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

			float4 _Color;
			float _Scale;

			struct kitten {
				float3 pos;
				float3 vel;
			};

			StructuredBuffer<kitten> data;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
				v2f o;
				kitten cat = data[instanceID];
				o.vertex = mul(UNITY_MATRIX_V, float4(cat.pos, 1));
				o.vertex += v.vertex * _Scale;
				o.vertex = mul(UNITY_MATRIX_P, o.vertex);

				o.uv = v.uv;
				return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float2 p = i.uv - 0.5;
				if (p.x * p.x + p.y * p.y > 0.25) {
					discard;
					return float4(0, 0, 0, 1);
				}
				return  _Color;
            }
            ENDCG
        }
    }
}
