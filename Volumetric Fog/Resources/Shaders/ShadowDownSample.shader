Shader "Avol/ShadowDownSample"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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

			#define EXPONENT 6.4
			
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
			
			uniform sampler2D _MainTex;
			uniform float2	  _Resolution;
			uniform float	  _Shadow;

			float log_space(float w0, float d1, float w1, float d2)
			{
				return (d1 + log(w0 + (w1 * exp(d2 - d1))));
			}

			fixed4 frag (v2f i) : SV_Target
			{
				const float2 texel = float2(1.0, 1.0) / _Resolution * 0.5f;

				/*const float2[4] offsets = {	float2(texel.x, texel.y),
											float2(-texel.x, texel.y),
											float2(-texel.x, -texel.y),
											float2(texel.x, -texel.y)	 }*/

				const float samples = 4;
				float v, B, B2; 
				float w = (1.0 / samples);


				float a1 = tex2Dlod(_MainTex, float4(saturate(i.uv/* + offsets[0]*/), 0, 0));
				float a2 = tex2Dlod(_MainTex, float4(saturate(i.uv/* + offsets[1]*/), 0, 0));
				float a3 = tex2Dlod(_MainTex, float4(saturate(i.uv/* + offsets[2]*/), 0, 0));
				float a4 = tex2Dlod(_MainTex, float4(saturate(i.uv/* + offsets[3]*/), 0, 0));


				return float4(0, 0, 0, 0);

				//return max(a1, max(a2, max(a3, a4)));

				/*float v1 = log_space(w, a1, w, a2);
				float v2 = log_space(1.0, v1, w, a3);
				float v3 = log_space(1.0, v2, w, a4);

				return v3;*/

				/*float accumulation = 0;

				accumulation += exp(tex2Dlod(_MainTex, float4(saturate(i.uv + float2(texel.x, texel.y)), 0, 0)) * _Shadow);
				accumulation += exp(tex2Dlod(_MainTex, float4(saturate(i.uv + float2(-texel.x, texel.y)), 0, 0)) * _Shadow);
				accumulation += exp(tex2Dlod(_MainTex, float4(saturate(i.uv + float2(-texel.x, -texel.y)), 0, 0)) * _Shadow);
				accumulation += exp(tex2Dlod(_MainTex, float4(saturate(i.uv + float2(texel.x, -texel.y)), 0, 0)) * _Shadow);

				return dot(accumulation, 1.0 / 16.0);*/
			}
			ENDCG
		}
	}
}
