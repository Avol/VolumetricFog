// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// @ Author - Donatas Kanapickas.
Shader "Avol/ShadowProjection"
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
			
			uniform sampler2D	_MainTex;
			uniform int			_Cascades;

			fixed4 frag (v2f i) : SV_Target
			{
				float4x4 mat1 = unity_WorldToShadow[0];
				float4x4 mat2 = unity_WorldToShadow[1];
				float4x4 mat3 = unity_WorldToShadow[2];
				float4x4 mat4 = unity_WorldToShadow[3];

				const float	step = 1.0f / (4.0f * _Cascades);


				if (_Cascades == 1)
				{
					for (int c = 1; c <= 4; c++)
						if (i.uv.x < step*c)
							return mat1[c - 1];
				}
				else if (_Cascades == 2)
				{
					for (int h = 1; h <= 8;h++)
						if (i.uv.x < step*h) {
							if (h <= 4)	return mat1[h - 1];
							else		return mat2[h - 5];
						}
				}
				else if (_Cascades == 4)
				{
					for (int d = 1; d <= 16; d++)
						if (i.uv.x < step*d) {
							if (d <= 4)			return mat1[d - 1];
							else if (d <= 8)	return mat2[d - 5];
							else if (d <= 12)	return mat3[d - 9];
							else				return mat4[d - 13];
						}
				}

				return 0;
			}

			ENDCG
		}
	}
}
