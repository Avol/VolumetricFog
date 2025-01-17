// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// @ Author - Donatas Kanapickas.
Shader "Avol/LogBlurWeak"
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

			#pragma target 4.0
			
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
			uniform int		  _Dir;
			uniform float2	  _Resolution;

			float log_space(float w0, float d1, float w1, float d2)
			{
				return (d1 + log(w0 + (w1 * exp(d2 - d1))));
			}

			fixed frag (v2f i) : SV_Target
			{ 
				const int samples		= 3;
				const float2 texel		= float2(1.0, 1.0) / _Resolution * 0.5f;
				 
				float v, A, B, C;
				float w					= (1.0 / samples);

				v = 1;

				if (_Dir == 0)
				{
					A = tex2Dlod(_MainTex, float4(i.uv + texel * float2(-1, 0), 0, 0));
					B = tex2Dlod(_MainTex, float4(i.uv + texel * float2(0, 0), 0, 0));
					C = tex2Dlod(_MainTex, float4(i.uv + texel * float2(1, 0), 0, 0));

					v = log_space(0.157731, A, 0.684538, B);
					v = log_space(1.0, v, 0.157731, C);
				}
				else if (_Dir == 1)
				{
					A = tex2Dlod(_MainTex, float4(i.uv + texel * float2(0, -1), 0, 0));
					B = tex2Dlod(_MainTex, float4(i.uv + texel * float2(0, 0), 0, 0));
					C = tex2Dlod(_MainTex, float4(i.uv + texel * float2(0, 1), 0, 0));

					v = log_space(0.157731, A, 0.684538, B);
					v = log_space(1.0, v, 0.157731, C);
				}

				return v;
			}
			ENDCG
		}
	}
}
