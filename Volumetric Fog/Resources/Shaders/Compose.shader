// @ Author - Donatas Kanapickas.
Shader "Avol/Compose"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
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

			#define STEP    0.015625			// 1.0 / 64.0
            #define PI		3.14159265359

			uniform sampler2D	_MainTex;

			uniform sampler3D	_PMediaMap0;
			uniform sampler3D	_PMediaMap1;
			uniform sampler3D	_PMediaMap2;
			uniform sampler3D	_PMediaMap3;
			uniform sampler3D	_PMediaMap4;

			uniform sampler2D	_CameraDepthTexture;


			uniform	int			_Flip;
			uniform int			_Merge;


			uniform float4		_AtmosphereColor;
			uniform float4		_AtmosphereBackColor;
			uniform float4		_AmbientColor;
			uniform float       _SunAnistropy;
			uniform float		_AtmosphereAnistropy;
			uniform float		_RadialLobe;
			uniform float		_RadialBlend;

			uniform float4		_SunColor;
			uniform float3		_SunDirection;
			uniform float		_SunIntensity;
			uniform float		_Scattering;
			uniform float		_Absorption;
			uniform	float		_FogDensity;

			uniform float		_HeightTop;
			uniform	float		_HeightBottom;
			uniform float		_DensityTop;
			uniform float		_DensityBottom;


			uniform bool		_AnalyticalFogEnable;
			uniform bool		_BeerLambert;
			uniform float		_RenderDistance;
			uniform float		_RenderDistanceNear;
			uniform float		_CameraNear;
			uniform float		_CameraFar;
			uniform bool		_Noise;
			uniform bool		_AnalyticalFog;

			uniform				float3		_CameraPosition;
			uniform				float3		_CameraUp;
			uniform				float3		_CameraRight;
			uniform				float3		_CameraFront;
			uniform				float		_CameraAspect;
			uniform				float		_CameraFOV;

			uniform				float		_FinalClamp;
			uniform				float		_SkyClamp;
			uniform				float		_SkyClampAnalytical;
			uniform				float		_AnalyticalClamp;

			uniform				float		_RampStart;
			uniform				float		_RampEnd;
			uniform				float		_RampInfluence;

			uniform				float		_ShadowFadeDistance;

			uniform				float		_DensityInfluence;

			uniform				float       _DensityNear;
			uniform				float       _DensityFar;

			uniform				int			_DensityMode;

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

			// ---------------------------------------------- Helper Functions ----------------------------------------------------------- //


			// semi-random linear function x
			// @ n	- seed
			float Hash(float n) { return frac(sin(n) * 43758.5453123); }


			// ---------------------------------------------- Phase Functions ----------------------------------------------------------- //

			// Henyey-Greenstein phase function
			// @ dotLE	- light to eye dot product
			// @ g		- anistropy parameter
			float HenyeyGreensteinPhase(float dotLE, float g)
			{
				const float g2 = 1.0 - g * g;
				float h = (1.0f + (g * g) - (2 * g * dotLE));
				return (g2 / (4.0f * PI * pow(max(0.0, h), 1.5f)));
			}

			float SchlickPhaseFunction(float dotLE, float k)
			{
				const float k2 = 1.0 - k * k;
				float h = -((k * dotLE) - 1.0f);
				return (k2 / (4.0f * PI * h * h));
			}
			
			float ReyLeighPhaseFunction(float3 ray)
			{
				float fCos = dot(_SunDirection, ray) / length(ray);
				float fCos2 = fCos * fCos;
				return 0.75f + 0.75f * fCos2;
			}

			/*float getMiePhase(float3 ray)
			{
				const static float g = -_AtmosphereAnistropy;

				float fCos      = min(0, dot(_SunDirection, ray) / length(ray));

				float fCos2     = fCos * fCos;
				float g2        = g * g;

				float miePhase  = 1.5f * ((1.0f - g2) / (2.0f + g2)) * (1.0f + fCos2) / pow(abs(1.0f + g2 - 2.0f * g * fCos), 1.5f);

				return miePhase;
			}*/


			float RadialPhaseFunction(float dotLE)
			{
				float factor = (1.0f - abs(dotLE));
				return (factor * (1.0f / (PI * PI)));
			}

			void computeRadialColor(float3 ray, out float3 sun, out float3 ambient)
			{
				float	dotLE		= dot(_SunDirection, -normalize(ray));
				float	sunRadial	= HenyeyGreensteinPhase(dotLE, _SunAnistropy);		// calculate sun phase
				float	phase1		= SchlickPhaseFunction(dotLE, _AtmosphereAnistropy);		// calculate atmosphere phase
				float	phase2		= RadialPhaseFunction(dotLE);

				float	atmoRadial	= lerp(phase2, phase1, _RadialLobe);

				float	blendedPhase = lerp(atmoRadial, sunRadial, _RadialBlend);  // blend phase coeffs
				float3  blendedColor = lerp(atmoRadial * _AtmosphereColor.rgb, sunRadial * _SunColor, _RadialBlend); // blend sun with atmosphere

				sun			= saturate(blendedColor) * _SunIntensity;
				ambient		= _AmbientColor.rgb * ((1 - saturate(blendedPhase)) * 0.5f) * _SunIntensity;			// sun opposite = fog color
			}

			// height attenuation
			// TODO: optimize this.
			// PERFORMANCE: adds 0.05ms delay.
			// @ position - world space position
			float computeHeightAttenuation( float density, float3 worldPosition, float3 ray, float3 nearPos, float3 farPos,
											float heightBottom, float heightTop, float densityTop, float densityBottom )
			{
				float attenuation = 0;
				if (worldPosition.y >= heightBottom && worldPosition.y <= heightTop)
				{
					attenuation = lerp(densityBottom, densityTop, (worldPosition.y - heightBottom) / (heightTop - heightBottom));
				}
					attenuation = 1;

				return attenuation;
				// total ray length.
				/*float total			= length(farPos - nearPos);			
				float attenuation	= 0;

				// if pointing up
				if (ray.y > 0)
				{
					// if inbetween two heights.
					if (nearPos.y < heightTop && nearPos.y > heightBottom)
					{
						// calculate attenuation
						float scalar			= ( heightTop - nearPos.y ) / ray.y;
						float atten				= saturate( scalar / (depth * total) );

						// interpolate density
						float interpolationNear	= ( heightTop - nearPos.y ) / (heightTop - heightBottom);
						float interpolationFar	= ( heightTop - worldPosition.y) / (heightTop - heightBottom);

						// if world position farther than the height top
						if (worldPosition.y > heightTop)
						{
							float farPosition = nearPos.y + scalar * ray.y;
							interpolationFar = (heightTop - farPosition) / (heightTop - heightBottom);
						}
	
						// avarage the density between two points
						float densityNear	= lerp(densityBottom, densityTop, 1.0f - saturate(interpolationNear));
						float densityFar	= lerp(densityBottom, densityTop, 1.0f - saturate(interpolationFar));
						float density		= (densityNear + densityFar) * 0.5f;

						// return height contribution
						attenuation = saturate(atten * density);
					}

					// if below bottom height.
					else if (nearPos.y < heightBottom)
					{
						float scalarTop		= (-nearPos.y + heightTop) / ray.y;
						float scalarBottom	= (-nearPos.y + heightBottom) / ray.y;
						float totalRange	= depth * total;

						// account for height top
						if (totalRange >= scalarBottom)	
						{
							// remove start distance contribution.
							float filledRange = totalRange - scalarBottom;

							// account for height bottom
							if (totalRange >= scalarTop)
								filledRange -= (totalRange - scalarTop);

							// interpolate density
							float heightsDistance		= heightTop - heightBottom;
							float interpolationNear		= (heightBottom - worldPosition.y) / heightsDistance;
							float interpolationFar		= (heightTop - worldPosition.y) / heightsDistance;

							// avarage the density between two points
							float densityNear		= lerp(densityBottom, densityTop, saturate(interpolationNear));
							float densityFar		= lerp(densityBottom, densityTop, 1.0f - saturate(interpolationFar));
							float density			= (densityNear + densityFar) * 0.5f;
		
							// store attenuation
							attenuation = filledRange / totalRange * density;
						}
					}
				}

				// if pointing down
				else if (ray.y <= 0)
				{
					// if inbetween two heights.
					if (nearPos.y < heightTop && nearPos.y > heightBottom)
					{
						// calculate attenuation
						float scalar	= (nearPos.y - heightBottom) / -ray.y;
						float atten		= saturate(scalar / (depth * total));

						// interpolate density
						float interpolationNear		= (nearPos.y - heightBottom) / (heightTop - heightBottom);
						float interpolationFar		= (worldPosition.y - heightBottom) / (heightTop - heightBottom);

						// if world position further than the height top
						if (worldPosition.y < heightBottom)
						{
							float farPosition = nearPos.y + scalar * ray.y;
							interpolationFar = (farPosition - heightBottom) / (heightTop - heightBottom);
						}

						// avarage the density between two points
						float densityNear	= lerp(densityBottom, densityTop, saturate(interpolationNear));
						float densityFar	= lerp(densityBottom, densityTop, saturate(interpolationFar));
						float density		= (densityNear + densityFar) * 0.5f;

						// return height contribution
						attenuation			= saturate(atten * density);
					}

					// if above top height.
					else if (nearPos.y > heightTop)
					{
						float scalarTop			= (nearPos.y - heightTop) / -ray.y;
						float scalarBottom		= (nearPos.y - heightBottom) / -ray.y;
						float totalRange		= depth * total;

						// account for height top
						if (totalRange >= scalarTop)	
						{
							// remove start distance contribution.
							float filledRange = totalRange - scalarTop;

							// account for height bottom
							if (totalRange >= scalarBottom)  
								filledRange -= (totalRange - scalarBottom);

							// interpolate density
							float heightsDistance		= heightTop - heightBottom;
							float interpolationNear		= (worldPosition.y - heightTop) / heightsDistance;
							float interpolationFar		= (worldPosition.y - heightBottom) / heightsDistance;

							// avarage the density between two points
							float densityNear		= lerp(densityBottom, densityTop, 1.0f - saturate(interpolationNear) );
							float densityFar		= lerp(densityBottom, densityTop, saturate(interpolationFar));
							float density			= (densityNear + densityFar) * 0.5f;

							// write attenuation
							attenuation = filledRange / totalRange * density;
						}
					}
				}

				return attenuation;*/
			}


			//
			//
			//
			float computeRampLayerThickness(float depth, float rampStart, float rampEnd)
			{
				if (depth <= rampStart)
					return 0;

				if (depth >= rampEnd)
					return 1;

				float totalLength	= rampEnd - rampStart;
				float pDepth		= depth - rampStart;
				float coeff			= pDepth / totalLength;

				return coeff;
			}

			// TODO: allow customising color over distance - blend with shadow color.
			//
			//
			float3 computeShading(float radiance, float3 sunColor, float3 ambientColor)
			{
				float3 sunRadiance			= radiance * sunColor;
				float3 ambientRadiance		= radiance * ambientColor;

				float3 ambient				= saturate(ambientRadiance);
				float3 sun					= sunRadiance;

				return (ambient + sun);
			}

			// ------------------------------------------------- Vertex Shader --------------------------------------------------- //

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			// ------------------------------------------------- Fragment Shader --------------------------------------------------- //

			fixed4 frag (v2f i) : SV_Target
			{
				float4 input			= tex2D (_MainTex, i.uv);

				// sample camera depth
				float	depth		= tex2Dlod(_CameraDepthTexture, float4(i.uv, 0, 0)).r;
				float	ld			= Linear01Depth(depth);
				float	ldD			= ld / _RenderDistance;

				float	kd = max(0, ld - _RenderDistance) * (1.0f - _RenderDistance);

				// compute analytical contribution
				float4  analyticalFog	= 0;

				if (_AnalyticalFog && ld > _RenderDistance)
				{
					// calculate camera ray
					float2 uv = (i.uv - 0.5f) * _CameraFOV;
					uv.x *= _CameraAspect;
					float3 ray = _CameraUp * uv.y + _CameraRight * uv.x + _CameraFront;

					// calculate near & far & ray.
					float3 	farPos = _CameraPosition + ray * _CameraNear + ray * (_CameraFar - _CameraNear);
					float3	nearPos = _CameraPosition + ray * _CameraNear;
					float3	worldPos = lerp(nearPos, farPos, ld);
					ray = normalize(farPos - nearPos);

					// calculate sun & ambient phase colors based on camera ray.
					float3 sunColor			= 0;
					float3 ambientColor		= 0;
					computeRadialColor(ray, sunColor, ambientColor);
		
					// compute density
					float rampDensity		= computeRampLayerThickness(kd, _RampStart, _RampEnd);
					float density			= lerp(computeRampLayerThickness(kd, 0, 1), rampDensity, _RampInfluence);

					// calc height attenuation & interpolate by influence.
					const float disp = 0.0f; 
					float heightAttenuation = computeHeightAttenuation( density, worldPos, ray,
																		lerp(nearPos, lerp(nearPos, farPos, _RampStart), _RampInfluence),
																		lerp(farPos, lerp(nearPos, farPos, _RampEnd), _RampInfluence),
																		_HeightBottom, _HeightTop, _DensityTop, _DensityBottom );
					//float heightAttenuation = 100;/computeHeightAttenuation( density, worldPos, ray,
																		//lerp(nearPos, lerp(nearPos, farPos, _RampStart), _RampInfluence),
																	//	lerp(farPos, lerp(nearPos, farPos, _RampEnd), _RampInfluence),
																	//	_HeightBottom, _HeightTop, _DensityTop, _DensityBottom );


					const float distMultiplier      = length(ray * ldD);
					//heightAttenuation = 1.0 / exp(-heightAttenuation) * distMultiplier;

					// calc shading
					float3 color		= computeShading(1.0f, sunColor, ambientColor);

					// calculate scattering and absorption
					float3 scattering	= _Scattering * (sqrt(_FogDensity) * density) * 0.5f * color * heightAttenuation;
					float absorption	= _Absorption * (sqrt(_FogDensity) * density) * 0.5f * heightAttenuation;

					// limit visibility after absorption reached 1.
					if (absorption > 1)
					{
						scattering /= absorption;
						absorption = 1;
					}

					// store to this voxel
					analyticalFog = saturate(float4( scattering, absorption));

					//analyticalFog.rgb = min(analyticalFog.rgb, color.rgb);
						
					// clamp analytical fog
					analyticalFog *= _AnalyticalClamp;
					if (ld == 1.0f)
						analyticalFog *= _SkyClampAnalytical;
				}

				// sample volume
				float4 volume = 0;
				float4 volumePosition	= float4(i.uv, saturate(ldD), 0.0);
				volume = tex3Dlod(_PMediaMap0, float4(volumePosition.xyz, 0.0f));

				// 
				volume = saturate(volume);

				// clamp final volumetric contribution
				volume.rgb *= _FinalClamp;
				volume.a += 1.0f - _FinalClamp;

				// clamp final volumetric contribution to sky.
				if (ld == 1.0f)
				{
					volume.rgb *= _SkyClamp;
					volume.a += 1.0f - _SkyClamp;
				}
				
				// transmittance outscatter
				float outscatter = volume.a;
				input.rgb	*= (outscatter - analyticalFog.a);

				// return final value  
				if (_Merge == 1)		return saturate(input) + float4(volume.rgb + analyticalFog.rgb, outscatter);
				else					return float4(volume.rgb + analyticalFog.rgb, outscatter);
			}

			ENDCG
		}
	}
}
