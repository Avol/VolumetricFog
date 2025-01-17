Shader "Avol/WriteToScreen"
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
			uniform sampler2D	_FogTexture;
			uniform bool		_FlipY;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 input	= tex2D(_MainTex, i.uv);
				fixed4 fog		= tex2D(_FogTexture, i.uv);

				// transmittance outscatter
				input.rgb		*= fog.a;

				return			input + fog;
			}
			ENDCG
		}
	}
}
