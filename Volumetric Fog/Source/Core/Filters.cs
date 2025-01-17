using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Avol
{
	public class Filters
	{
		private			VolumetricFog				_VolumetricFog;

		private         Material                    _LogSpaceBlurWeakMaterial;
		private         Material                    _LogSpaceBlurMediumMaterial;
		private         Material                    _LogSpaceBlurStrongMaterial;

		private         RenderTexture               _LogBlurXTexture;
		private         RenderTexture               _LogBlurYTexture;

		private			ComputeShader				_VolumeBlurCompute;
		private			ComputeShader				_TemporalReprojectionCompute;

		private         int                         _VolumeBlurKernel;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="volumetricFog"></param>
		public Filters(VolumetricFog volumetricFog)
		{
			_VolumetricFog					= volumetricFog;

			_LogSpaceBlurWeakMaterial		= new Material(Shader.Find("Avol/LogBlurWeak"));
			_LogSpaceBlurMediumMaterial		= new Material(Shader.Find("Avol/LogBlurMedium"));
			_LogSpaceBlurStrongMaterial		= new Material(Shader.Find("Avol/LogBlurStrong"));

			int shadowResolution = 256 * ((int)QualitySettings.shadowResolution + 1);
			_LogBlurXTexture				= new RenderTexture(shadowResolution, shadowResolution, 1, RenderTextureFormat.RHalf);
			_LogBlurYTexture				= new RenderTexture(shadowResolution, shadowResolution, 1, RenderTextureFormat.RHalf);
			_LogBlurXTexture.filterMode		= FilterMode.Bilinear;
			_LogBlurXTexture.filterMode		= FilterMode.Bilinear;

			_VolumeBlurCompute				= (ComputeShader)Resources.Load("Shaders/Filter");
			_VolumeBlurKernel				= _VolumeBlurCompute.FindKernel("VolumeFilter");
		}




		/// <summary>
		/// Downsamples and transforms shadow data to exponent space.
		/// </summary>
		/*private void _DownSampleShadow(RenderTexture from, RenderTexture to, int resolution)
		{
			_ShadowDownSample.SetVector("_Resolution", new Vector2(resolution, resolution));
			Graphics.Blit(from, to, _ShadowDownSample);
		}*/


		public void VolumeSpatialFilter(int currentFrame, Vector3 volumeResolution,
										RenderTexture source, RenderTexture destination)
		{
			if (_VolumetricFog.VolumeFilter == VOLUME_FILTERING.Enabled)
			{
			
				if (currentFrame % 2 == 0)
				{
					_VolumeBlurCompute.SetTexture(_VolumeBlurKernel, "_Read", source);
					_VolumeBlurCompute.SetTexture(_VolumeBlurKernel, "_Write", destination);
				}
				else
				{
					_VolumeBlurCompute.SetTexture(_VolumeBlurKernel, "_Read", destination);
					_VolumeBlurCompute.SetTexture(_VolumeBlurKernel, "_Write", source);
				}

				_VolumeBlurCompute.SetVector("_VolumeResolution", volumeResolution);

				// dispatch compute
				_VolumeBlurCompute.Dispatch(_VolumeBlurKernel,		(int)volumeResolution.x / 8,
																	(int)volumeResolution.y / 8, 1);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="light"></param>
		public void GenExponentialShadowMap(VolumetricLight light)
		{
			if (_VolumetricFog.LogarithmicFilter != LOG_BLUR._Off)
			{
				Material selectedMaterial = _LogSpaceBlurStrongMaterial;
				switch (_VolumetricFog.LogarithmicFilter)
				{
					case LOG_BLUR._Weak:
						selectedMaterial = _LogSpaceBlurWeakMaterial;
						break;
					case LOG_BLUR._Medium:
						selectedMaterial = _LogSpaceBlurMediumMaterial;
						break;
					case LOG_BLUR._Strong:
						selectedMaterial = _LogSpaceBlurStrongMaterial;
						break;
				}

				int shadowResolution = ((int)QualitySettings.shadowResolution + 1) * 256;
				selectedMaterial.SetVector("_Resolution", new Vector2(shadowResolution, shadowResolution));

				selectedMaterial.SetInt("_Dir", 0);
				Graphics.Blit(light.ShadowMap, _LogBlurXTexture, selectedMaterial);

				selectedMaterial.SetInt("_Dir", 1);
				Graphics.Blit(_LogBlurXTexture, _LogBlurYTexture, selectedMaterial);
			}
		}
		
		public RenderTexture GetExponentialShadowMap()
		{
			return _LogBlurYTexture;
		}
	}
}
