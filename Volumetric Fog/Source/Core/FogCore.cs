using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Avol
{
	public class FogCore
	{
		// 
		private					VolumetricFog					_VolumetricFog;
		private					Buffers							_Buffers;
		private					Filters							_Filters;

		// shaders references
		private					ComputeShader					_NoiseCompute;
		private					int								_NoiseKernel;

		private					ComputeShader					_SunRadianceCompute;
		private					int								_SunRadianceKernel;

		private					ComputeShader					_PMInjectionCompute;
		private					int								_PMInjectionKernel;
		private					int								_InjectShadowedKernel;

		private					ComputeShader					_InscatterCompute;
		private					int								_InscatterKernel;

		private					Material						_ShadowProjectionMaterial;
		private					Material						_ComposeMaterial;

		// 
		private					int								_CurrentFrame                           = 0;
		private					int								_CurrentTAAFrame                        = 0;

		private					Matrix4x4						_LastProjection							= Matrix4x4.identity;


		private					VOLUME_RESOLUTION				_PVolumeResolution;
		private					Vector3							_PNoiseFrequency;
		private					float							_PDensityScale;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="volumetricFog"></param>
		/// <param name="filters"></param>
		public					FogCore(VolumetricFog volumetricFog)
		{
			_VolumetricFog			= volumetricFog;
			_Filters				= new Filters(volumetricFog);
			_Buffers				= new Buffers(volumetricFog);

			_NoiseCompute			= (ComputeShader)Resources.Load("Shaders/Noise");
			_NoiseKernel			= _NoiseCompute.FindKernel("Noise");

			_SunRadianceCompute		= (ComputeShader)Resources.Load("Shaders/Radiance");
			_SunRadianceKernel		= _SunRadianceCompute.FindKernel("VolumeRadiance");

			_PMInjectionCompute		= (ComputeShader)Resources.Load("Shaders/PMInjection");
			_PMInjectionKernel		= _PMInjectionCompute.FindKernel("InjectParticipatingMedium");
			_InjectShadowedKernel	= _PMInjectionCompute.FindKernel("InjectShadowedLight");

			_InscatterCompute		= (ComputeShader)Resources.Load("Shaders/Inscatter");
			_InscatterKernel		= _InscatterCompute.FindKernel("VolumeInscatter");

			_ShadowProjectionMaterial		= new Material(Shader.Find("Avol/ShadowProjection"));
			_ComposeMaterial				= new Material(Shader.Find("Avol/Compose"));

			_NoisePass();


			_PVolumeResolution		= _VolumetricFog.VolumeResolution;
			_PNoiseFrequency		= _VolumetricFog.CloudsFrequency;
			_PDensityScale			= _VolumetricFog.CloudsDensityScale;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		public		void		Render(RenderTexture input, RenderTexture output)
		{
			// recompute buffers
			_Buffers.LightBufferGenerate();		// TODO: trigger this call only when light data changed.
			_Buffers.VolumeBufferGenerate();    // TODO: trigger this call only when volume data changed.

			// precompute common data.
			float scaledRenderDistance = (QualitySettings.shadowDistance - QualitySettings.shadowNearPlaneOffset)
										/ (Camera.main.farClipPlane - Camera.main.nearClipPlane) * _VolumetricFog.RenderDistance;
			Matrix4x4 cameraPV = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;

			// extract sun shadow projection matrices.
			_ExtractHiddenShadowData();

			// blur directional shadow in log space
			_Filters.GenExponentialShadowMap(_Buffers.SunVolumetricLight);

			// inject sun radiance.
			_InjectRadiancePass(cameraPV, scaledRenderDistance, _Buffers.SunVolumetricLight,
								_Buffers.RadianceMap, _Buffers.VolumeResolution);



			// acquire visible shadowed lights.
			List<VolumetricLight> visibleShadowedLights = new List<VolumetricLight>();
			for (int i = 0; i < _Buffers.ShadowedSpotLights.Count; i++)
			{
				bool visible = _Buffers.VisibleLights.Contains(_Buffers.ShadowedSpotLights[i].LightComponent);
				if (visible)
					visibleShadowedLights.Add(_Buffers.ShadowedSpotLights[i]);
			}


			// participating medium injection pass.
			_PMInjectionPass(scaledRenderDistance, cameraPV, visibleShadowedLights.Count > 0 ? false : true);

			// inject other shadowed lights.
			for (int i = 0; i < visibleShadowedLights.Count; i++)
			{
				_InjectShadowedSpotLight(scaledRenderDistance, cameraPV, visibleShadowedLights[i], i == _Buffers.ShadowedSpotLights.Count - 1 ? true : false);
			}


			// inscatter volume
			_InscatterPass(scaledRenderDistance);

			// filter volume texture.
			_Filters.VolumeSpatialFilter(_CurrentFrame, _Buffers.VolumeResolution,
														_Buffers.VolumeShadingMap,
														_Buffers.VolumeShadingMap2);

			// compose final image.
			_ComposePass(input, output, true);

			// increment frame
			_CurrentFrame++;
			if (_CurrentFrame == int.MaxValue)
				_CurrentFrame = 1;

			// increment TSAA frame.
			_CurrentTAAFrame++;
			int frames = 2 + 2 * (int)_VolumetricFog.TemporalFilter;
			if (_CurrentTAAFrame >= frames)
				_CurrentTAAFrame = 0;


			_LastProjection = cameraPV;
		}

		/// <summary>
		/// 
		/// </summary>
		public		void		Rebuild()
		{
			_Buffers.ComputeVolumeTextures();
		}

		/// <summary>
		/// 
		/// </summary>
		public		void		ReleaseAll()
		{
			_Buffers.ReleaseAll();
		}

		/// <summary>
		/// 
		/// </summary>
		public		void		Update()
		{
			if (_PVolumeResolution != _VolumetricFog.VolumeResolution)
			{
				_Buffers.ComputeVolumeTextures();
				_PVolumeResolution = _VolumetricFog.VolumeResolution;
			}

			if (_PNoiseFrequency != _VolumetricFog.CloudsFrequency)
			{
				_NoisePass();
				_PNoiseFrequency = _VolumetricFog.CloudsFrequency;
			}

			if (_PDensityScale != _VolumetricFog.CloudsDensityScale)
			{
				_NoisePass();
				_PDensityScale = _VolumetricFog.CloudsDensityScale;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public		Buffers		GetBuffers()
		{
			return _Buffers;
		}
		

		// -------------------------------------------------- Precompute ------------------------------------------------------- //

		/// <summary>
		/// Injects fbm noise into a 3D texture LUT
		/// </summary>
		private		void		_NoisePass()
		{
			_NoiseCompute.SetVector("_Resolution", new Vector2(Screen.currentResolution.width, Screen.currentResolution.height));

			_NoiseCompute.SetFloat("_HeightTop", _VolumetricFog.HeightTop);
			_NoiseCompute.SetFloat("_HeightBottom", _VolumetricFog.HeightBottom);
			_NoiseCompute.SetFloat("_DensityBottom", _VolumetricFog.DensityBottom);
			_NoiseCompute.SetFloat("_DensityTop", _VolumetricFog.DensityTop);

			_NoiseCompute.SetFloat("_NearPlane", Camera.main.nearClipPlane);
			_NoiseCompute.SetFloat("_CameraFar", Camera.main.farClipPlane);
			_NoiseCompute.SetBool("_EnableNoise", _VolumetricFog.Clouds);
	
			_NoiseCompute.SetInt("_NoiseOctaves", 5);

			_NoiseCompute.SetFloat("_NoiseHeightBottom", _VolumetricFog.CloudsHeightBottom);
			_NoiseCompute.SetFloat("_NoiseHeightTop", _VolumetricFog.CloudsHeightTop);
	
			_NoiseCompute.SetFloat("_NoiseFadeBottomHeight", _VolumetricFog.CloudsFadeBottomHeight);
			_NoiseCompute.SetFloat("_NoiseFadeTopHeight", _VolumetricFog.CloudsFadeTopHeight);

			_NoiseCompute.SetVector("_NoiseFrequency", _VolumetricFog.CloudsFrequency);


			_NoiseCompute.SetInt("_NoiseType", 0);

			_NoiseCompute.SetFloat("_NoiseDensityScale", _VolumetricFog.CloudsDensityScale);

			_NoiseCompute.SetTexture(_NoiseKernel, "_VolumeMediumMap", _Buffers.VolumeNoiseLUT);
			

			_NoiseCompute.Dispatch(_NoiseKernel, _Buffers.VolumeNoiseLUT.width / 4,
												 _Buffers.VolumeNoiseLUT.height / 4,
												 _Buffers.VolumeNoiseLUT.volumeDepth / 4);
		}

		// -------------------------------------------------- Render Passes ------------------------------------------------------- //

		/// <summary>
		/// 
		/// </summary>
		private		void		_ExtractHiddenShadowData()
		{
			// extract lights shadow view proj matrix
			_ShadowProjectionMaterial.SetInt("_Cascades", QualitySettings.shadowCascades);
			Graphics.Blit(_Buffers.DirLightMatricesTexture, _Buffers.DirLightMatricesTexture, _ShadowProjectionMaterial);

			// extract split sphere data
			//if (QualitySettings.shadowProjection == ShadowProjection.StableFit)
			//    Graphics.Blit(_ShadowSplitSpheresTexture, _ShadowSplitSpheresTexture,  _ShadowSplitSpheresMaterial); 
		}

		/// <summary>
		/// Injects sun light radiance into volume.
		/// </summary>
		/// <param name="cameraPV"></param>
		/// <param name="scaledRenderDistance"></param>
		/// <param name="light"></param>
		/// <param name="radianceTarget"></param>
		/// <param name="volumeResolution"></param>
		/// <param name="currentTSAAframe"></param>
		/// <param name="depthBuffer"></param>
		private		void		_InjectRadiancePass(Matrix4x4 cameraPV, float scaledRenderDistance, VolumetricLight light, RenderTexture radianceTarget,
													Vector3 volumeResolution)
		{
			float cascadeSplitCoeff = (QualitySettings.shadowDistance - QualitySettings.shadowNearPlaneOffset) / (Camera.main.farClipPlane - Camera.main.nearClipPlane);

			switch (QualitySettings.shadowCascades)
			{
				case 4:
					_SunRadianceCompute.SetVector("_LSPN", new Vector4(0, QualitySettings.shadowCascade4Split.x, QualitySettings.shadowCascade4Split.y, QualitySettings.shadowCascade4Split.z) * cascadeSplitCoeff);
					_SunRadianceCompute.SetVector("_LSPF", new Vector4(QualitySettings.shadowCascade4Split.x, QualitySettings.shadowCascade4Split.y, QualitySettings.shadowCascade4Split.z, 1) * cascadeSplitCoeff);
					break;
				case 2:
					_SunRadianceCompute.SetVector("_LSPN", new Vector4(0, QualitySettings.shadowCascade2Split, 0, 0) * cascadeSplitCoeff);
					_SunRadianceCompute.SetVector("_LSPF", new Vector4(QualitySettings.shadowCascade2Split, 1, 0, 0) * cascadeSplitCoeff);
					break;
				default:
					_SunRadianceCompute.SetVector("_LSPN", Vector4.zero);
					_SunRadianceCompute.SetVector("_LSPF", Vector4.zero);
					break;
			}

			_SunRadianceCompute.SetTexture(_SunRadianceKernel, "_DirectionalShadow", _VolumetricFog.LogarithmicFilter != LOG_BLUR._Off ? _Filters.GetExponentialShadowMap() : light.ShadowMap);
			_SunRadianceCompute.SetTexture(_SunRadianceKernel, "_ShadowProjMatrices", _Buffers.DirLightMatricesTexture);

			_SunRadianceCompute.SetTexture(_SunRadianceKernel, "_RadianceMap", radianceTarget);

			_SunRadianceCompute.SetFloat("_Shadow", _VolumetricFog.Shadow * 50.0f);

			int shadowResolution = ((int)QualitySettings.shadowResolution + 1) * 256;
			_SunRadianceCompute.SetVector("_ShadowResolution", new Vector2(shadowResolution, shadowResolution));
			_SunRadianceCompute.SetVector("_VolumeResolution", volumeResolution);

			_SunRadianceCompute.SetFloat("_DepthOffset", _VolumetricFog.ShadowBias);
			_SunRadianceCompute.SetInt("_CascadeCount", QualitySettings.shadowCascades);

			_SunRadianceCompute.SetVector("_CameraPosition", Camera.main.transform.position);
			_SunRadianceCompute.SetVector("_CameraUp", Camera.main.transform.up);
			_SunRadianceCompute.SetVector("_CameraRight", Camera.main.transform.right);
			_SunRadianceCompute.SetVector("_CameraFront", Camera.main.transform.forward);
			_SunRadianceCompute.SetFloat("_CameraAspect", Camera.main.aspect);
			_SunRadianceCompute.SetFloat("_CameraFOV", Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f);
			_SunRadianceCompute.SetMatrix("_VP", cameraPV);

			_SunRadianceCompute.SetFloat("_CameraNear", Camera.main.nearClipPlane);
			_SunRadianceCompute.SetFloat("_CameraFar", Camera.main.farClipPlane);

			_SunRadianceCompute.SetFloat("_RampStart", _VolumetricFog.RampStart);
			_SunRadianceCompute.SetFloat("_RampEnd", _VolumetricFog.RampEnd);
			_SunRadianceCompute.SetFloat("_RampInfluence", _VolumetricFog.RampInfluence);


			_SunRadianceCompute.SetInt("_TSSAAFrame", _CurrentTAAFrame);
			_SunRadianceCompute.SetInt("_TemporalFilter", (int)_VolumetricFog.TemporalFilter);

			_SunRadianceCompute.SetBuffer(_SunRadianceKernel, "_DepthData", _Buffers.DepthSteps);


			_SunRadianceCompute.SetFloat("_RenderDistance", scaledRenderDistance);

			_SunRadianceCompute.Dispatch(_SunRadianceKernel, (int)volumeResolution.x / 8, (int)volumeResolution.y / 8, 1);
		}

		/// <summary>
		/// Injects lights into volume.
		/// </summary>
		/// <param name="scaledRenderDistance"></param>
		/// <param name="cameraPV"></param>
		private		void		_PMInjectionPass(float scaledRenderDistance, Matrix4x4 cameraPV, bool reproject)
		{
			// switch kernels to use different texture formats
			int kernel = _PMInjectionKernel;

			// store buffers
			if (_Buffers.PointLightBuffer != null)
				_PMInjectionCompute.SetBuffer(kernel, "_PointLightData", _Buffers.PointLightBuffer);

			if (_Buffers.SpotLightBuffer != null)
				_PMInjectionCompute.SetBuffer(kernel, "_SpotLightData", _Buffers.SpotLightBuffer);

			if (_Buffers.BoxVolumeBuffer != null)
				_PMInjectionCompute.SetBuffer(kernel, "_BoxVolumeData", _Buffers.BoxVolumeBuffer);

			if (_Buffers.EllipsoidVolumeBuffer != null)
				_PMInjectionCompute.SetBuffer(kernel, "_EllipsoidVolumeData", _Buffers.EllipsoidVolumeBuffer);


			_PMInjectionCompute.SetInt("_BoxVolumeCount", _Buffers.BoxVolumeBuffer == null ? 0 : _Buffers.BoxVolumeBuffer.count);
			_PMInjectionCompute.SetInt("_EllipsoidVolumeCount", _Buffers.EllipsoidVolumeBuffer == null ? 0 : _Buffers.EllipsoidVolumeBuffer.count);

			_PMInjectionCompute.SetInt("_PointLightCount", _Buffers.PointLightBuffer == null ? 0 : _Buffers.PointLightBuffer.count);
			_PMInjectionCompute.SetInt("_SpotLightCount", _Buffers.SpotLightBuffer == null ? 0 : _Buffers.SpotLightBuffer.count);

			 
			_PMInjectionCompute.SetMatrix("_PVHistory", _LastProjection);
			_PMInjectionCompute.SetMatrix("_PV", cameraPV);

			_PMInjectionCompute.SetFloat("_RenderDistance", scaledRenderDistance);

			_PMInjectionCompute.SetVector("_SunDirection", _Buffers.Sun.transform.forward);

			_PMInjectionCompute.SetFloat("_CameraNear", Camera.main.nearClipPlane);
			_PMInjectionCompute.SetFloat("_CameraFar", Camera.main.farClipPlane);

			_PMInjectionCompute.SetVector("_VolumeResolution", _Buffers.VolumeResolution);


			_PMInjectionCompute.SetTexture(kernel, "_PreviousPMVolume", _CurrentFrame % 2 == 0 ? _Buffers.PMVolume2 : _Buffers.PMVolume);
			_PMInjectionCompute.SetTexture(kernel, "_PMVolume", _CurrentFrame % 2 == 0 ? _Buffers.PMVolume : _Buffers.PMVolume2);

			_PMInjectionCompute.SetTexture(kernel, "_PreviousPMRadiance", _CurrentFrame % 2 == 0 ? _Buffers.PMRadiance2 : _Buffers.PMRadiance);
			_PMInjectionCompute.SetTexture(kernel, "_PMRadiance", _CurrentFrame % 2 == 0 ? _Buffers.PMRadiance : _Buffers.PMRadiance2);



			_PMInjectionCompute.SetBool("_Noise", _VolumetricFog.Clouds);
			_PMInjectionCompute.SetFloat("_NoiseSize", _VolumetricFog.CloudsSize);
			_PMInjectionCompute.SetTexture(kernel, "_NoiseLUT", _Buffers.VolumeNoiseLUT);

			_PMInjectionCompute.SetVector("_WindVelocity", _VolumetricFog.WindVelocity * Time.realtimeSinceStartup);

			_PMInjectionCompute.SetTexture(kernel, "_RadianceMap", _Buffers.RadianceMap);

			_PMInjectionCompute.SetVector("_CameraPosition", Camera.main.transform.position);
			_PMInjectionCompute.SetVector("_CameraUp", Camera.main.transform.up);
			_PMInjectionCompute.SetVector("_CameraRight", Camera.main.transform.right);
			_PMInjectionCompute.SetVector("_CameraFront", Camera.main.transform.forward);

			_PMInjectionCompute.SetFloat("_CameraAspect", Camera.main.aspect);
			_PMInjectionCompute.SetFloat("_CameraFOV", Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f);



			_PMInjectionCompute.SetFloat("_NoiseFadeBottomHeight", _VolumetricFog.CloudsFadeBottomHeight);
			_PMInjectionCompute.SetFloat("_NoiseFadeTopHeight", _VolumetricFog.CloudsFadeTopHeight);

			_PMInjectionCompute.SetFloat("_NoiseHeightBottom", _VolumetricFog.CloudsHeightBottom);
			_PMInjectionCompute.SetFloat("_NoiseHeightTop", _VolumetricFog.CloudsHeightTop);
			_PMInjectionCompute.SetFloat("_NoiseInfluenceBottom", _VolumetricFog.CloudsInfluenceBottom);
			_PMInjectionCompute.SetFloat("_NoiseInfluenceTop", _VolumetricFog.CloudsInfluenceTop);

			_PMInjectionCompute.SetBool("_NoiseOcclusion", _VolumetricFog.CloudsOcclusion);

			_PMInjectionCompute.SetFloat("_OcclusionRayDistance", _VolumetricFog.OcclusionRayDistance);
			_PMInjectionCompute.SetFloat("_DirectStrength", _VolumetricFog.OutlineStrength);
			_PMInjectionCompute.SetFloat("_OcclusionStrength", _VolumetricFog.OcclusionStrength);
			_PMInjectionCompute.SetFloat("_OutlineRadius", _VolumetricFog.OutlineRadius * 0.5f);
			_PMInjectionCompute.SetFloat("_CloudSpacing", _VolumetricFog.CloudsSpacing);
			_PMInjectionCompute.SetVector("_CloudSpacingFrequency", _VolumetricFog.CloudsSpacingFrequency);

			_PMInjectionCompute.SetFloat("_RenderDistance", scaledRenderDistance);
			_PMInjectionCompute.SetInt("_SuperSampling", (int)_VolumetricFog.SuperSampling);

			_PMInjectionCompute.SetVector("_WindVelocity", _VolumetricFog.WindVelocity * Time.realtimeSinceStartup);

			_PMInjectionCompute.SetInt("_TSSAAFrame", _CurrentTAAFrame);
			_PMInjectionCompute.SetInt("_TemporalFilter", (int)_VolumetricFog.TemporalFilter);
			_PMInjectionCompute.SetBool("_TemporalReprojection", reproject);
			 
			_PMInjectionCompute.SetFloat("_DepthAttenuation", _VolumetricFog.DepthAttenuation);

			_PMInjectionCompute.SetVector("_CloudDirectColor", _VolumetricFog.CloudDirectColor);
			
			/*
			float	A				= Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad;
			float	nearWidth		= (Mathf.Tan(A) * Camera.main.nearClipPlane);
			float	nearHeight		= (nearWidth / Camera.main.pixelHeight) * Camera.main.pixelWidth;
			float	farWidth		= (Mathf.Tan(A) * Camera.main.farClipPlane);
			float	farHeight		= (farWidth / Camera.main.pixelHeight) * Camera.main.pixelWidth;

			_PMInjectionCompute.SetVector("_CameraDimensions", new Vector4(nearWidth, nearHeight, farWidth, farHeight));
			*/

			_PMInjectionCompute.Dispatch(kernel,	(int)_Buffers.VolumeResolution.x / 4,
													(int)_Buffers.VolumeResolution.y / 4,
													(int)_Buffers.VolumeResolution.z / 4);
		}

		/// <summary>
		/// 
		/// </summary>
		private		void		_InjectShadowedSpotLight(float scaledRenderDistance, Matrix4x4 cameraPV, VolumetricLight VolumetricLight, bool reproject)
		{
			int kernel = _InjectShadowedKernel;


			Camera tempCamera = VolumetricLight.gameObject.AddComponent<Camera>();

			Matrix4x4 lproj = Matrix4x4.Perspective(VolumetricLight.LightComponent.spotAngle, 1, VolumetricLight.LightComponent.shadowNearPlane, VolumetricLight.LightComponent.range);
			Matrix4x4 lview = tempCamera.worldToCameraMatrix;

			_PMInjectionCompute.SetTexture(kernel, "_ShadowMap", VolumetricLight.ShadowMap);
			_PMInjectionCompute.SetMatrix("_ShadowProjection", lproj * lview);
			_PMInjectionCompute.SetFloat("_ShadowStrength", VolumetricLight.LightComponent.shadowStrength);
			Component.Destroy(tempCamera);




			_PMInjectionCompute.SetFloat("_Angle", Mathf.Cos(VolumetricLight.LightComponent.spotAngle * Mathf.Deg2Rad / 2.0f));
			_PMInjectionCompute.SetVector("_Attenuation", new Vector2(VolumetricLight.Softness, VolumetricLight.Softness2 * VolumetricLight.LightComponent.range) * 0.5f);
			_PMInjectionCompute.SetVector("_Color", new Vector3(VolumetricLight.LightComponent.color.r, VolumetricLight.LightComponent.color.g, VolumetricLight.LightComponent.color.b));
			_PMInjectionCompute.SetVector("_Direction", VolumetricLight.LightComponent.transform.forward);
			_PMInjectionCompute.SetFloat("_Intensity", VolumetricLight.Intensity);
			_PMInjectionCompute.SetVector("_Position", VolumetricLight.transform.position);
			_PMInjectionCompute.SetFloat("_Range", VolumetricLight.LightComponent.range);

			int shadowResolution = 256 * ((int)VolumetricLight.LightComponent.shadowResolution + 1);
			_PMInjectionCompute.SetVector("_ShadowResolution", new Vector2(shadowResolution, shadowResolution));




			_PMInjectionCompute.SetMatrix("_PVHistory", _LastProjection);
			_PMInjectionCompute.SetMatrix("_PV", cameraPV);

			_PMInjectionCompute.SetFloat("_RenderDistance", scaledRenderDistance);

			_PMInjectionCompute.SetFloat("_CameraNear", Camera.main.nearClipPlane);
			_PMInjectionCompute.SetFloat("_CameraFar", Camera.main.farClipPlane);

			_PMInjectionCompute.SetVector("_VolumeResolution", _Buffers.VolumeResolution);

			
			_PMInjectionCompute.SetTexture(kernel, "_PreviousPMVolume", _CurrentFrame % 2 == 0 ? _Buffers.PMVolume2 : _Buffers.PMVolume);
			_PMInjectionCompute.SetTexture(kernel, "_PMVolume", _CurrentFrame % 2 == 0 ? _Buffers.PMVolume : _Buffers.PMVolume2);

			_PMInjectionCompute.SetTexture(kernel, "_PreviousPMRadiance", _CurrentFrame % 2 == 0 ? _Buffers.PMRadiance2 : _Buffers.PMRadiance);
			_PMInjectionCompute.SetTexture(kernel, "_PMRadiance", _CurrentFrame % 2 == 0 ? _Buffers.PMRadiance : _Buffers.PMRadiance2);



			_PMInjectionCompute.SetBool("_Noise", _VolumetricFog.Clouds);
			_PMInjectionCompute.SetFloat("_NoiseSize", _VolumetricFog.CloudsSize);

			_PMInjectionCompute.SetVector("_WindVelocity", _VolumetricFog.WindVelocity * Time.realtimeSinceStartup);

			_PMInjectionCompute.SetVector("_CameraPosition", Camera.main.transform.position);
			_PMInjectionCompute.SetVector("_CameraUp", Camera.main.transform.up);
			_PMInjectionCompute.SetVector("_CameraRight", Camera.main.transform.right);
			_PMInjectionCompute.SetVector("_CameraFront", Camera.main.transform.forward);

			_PMInjectionCompute.SetFloat("_CameraAspect", Camera.main.aspect);
			_PMInjectionCompute.SetFloat("_CameraFOV", Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f);



			_PMInjectionCompute.SetFloat("_NoiseFadeBottomHeight", _VolumetricFog.CloudsFadeBottomHeight);
			_PMInjectionCompute.SetFloat("_NoiseFadeTopHeight", _VolumetricFog.CloudsFadeTopHeight);

			_PMInjectionCompute.SetFloat("_NoiseHeightBottom", _VolumetricFog.CloudsHeightBottom);
			_PMInjectionCompute.SetFloat("_NoiseHeightTop", _VolumetricFog.CloudsHeightTop);
			_PMInjectionCompute.SetFloat("_NoiseInfluenceBottom", _VolumetricFog.CloudsInfluenceBottom);
			_PMInjectionCompute.SetFloat("_NoiseInfluenceTop", _VolumetricFog.CloudsInfluenceTop);

			_PMInjectionCompute.SetBool("_NoiseOcclusion", _VolumetricFog.CloudsOcclusion);

			_PMInjectionCompute.SetFloat("_OcclusionRayDistance", _VolumetricFog.OcclusionRayDistance);
			_PMInjectionCompute.SetFloat("_DirectStrength", _VolumetricFog.OutlineStrength);
			_PMInjectionCompute.SetFloat("_OcclusionStrength", _VolumetricFog.OcclusionStrength);
			_PMInjectionCompute.SetFloat("_OutlineRadius", _VolumetricFog.OutlineRadius * 0.5f);


			_PMInjectionCompute.SetBool("_EnableCloudSpacing", _VolumetricFog.EnableCloudSpacing);
			_PMInjectionCompute.SetFloat("_CloudSpacingSize", _VolumetricFog.CloudsSpacing);
			_PMInjectionCompute.SetVector("_CloudSpacingFrequency", _VolumetricFog.CloudsSpacingFrequency);

			_PMInjectionCompute.SetFloat("_RenderDistance", scaledRenderDistance);
			_PMInjectionCompute.SetInt("_SuperSampling", (int)_VolumetricFog.SuperSampling);

			_PMInjectionCompute.SetVector("_WindVelocity", _VolumetricFog.WindVelocity * Time.realtimeSinceStartup);

			_PMInjectionCompute.SetInt("_TSSAAFrame", _CurrentTAAFrame);
			_PMInjectionCompute.SetInt("_TemporalFilter", (int)_VolumetricFog.TemporalFilter);

			_PMInjectionCompute.SetFloat("_DepthAttenuation", _VolumetricFog.DepthAttenuation);
			_PMInjectionCompute.SetBool("_TemporalReprojection", reproject);

			_PMInjectionCompute.Dispatch(kernel, (int)_Buffers.VolumeResolution.x / 4,
												 (int)_Buffers.VolumeResolution.y / 4,
												 (int)_Buffers.VolumeResolution.z / 4);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="scaledRenderDistance"></param>
		private		void		_InscatterPass(float scaledRenderDistance)
		{
			switch (QualitySettings.shadowCascades)
			{
				case 4:
					_InscatterCompute.SetVector("_LSPN", new Vector4(0, QualitySettings.shadowCascade4Split.x, QualitySettings.shadowCascade4Split.y, QualitySettings.shadowCascade4Split.z));
					_InscatterCompute.SetVector("_LSPF", new Vector4(QualitySettings.shadowCascade4Split.x, QualitySettings.shadowCascade4Split.y, QualitySettings.shadowCascade4Split.z, 1));
					break;
				case 2:
					_InscatterCompute.SetVector("_LSPN", new Vector4(0, QualitySettings.shadowCascade2Split, 0, 0));
					_InscatterCompute.SetVector("_LSPF", new Vector4(QualitySettings.shadowCascade2Split, 1, 0, 0));
					break;
				default:
					_InscatterCompute.SetVector("_LSPN", Vector4.zero);
					_InscatterCompute.SetVector("_LSPF", Vector4.zero);
					break;
			}

			_InscatterCompute.SetFloat("_HeightTop", _VolumetricFog.HeightTop);
			_InscatterCompute.SetFloat("_HeightBottom", _VolumetricFog.HeightBottom);

			_InscatterCompute.SetFloat("_DensityBottom", _VolumetricFog.DensityBottom);
			_InscatterCompute.SetFloat("_DensityTop", _VolumetricFog.DensityTop);

			_InscatterCompute.SetFloat("_Density", _VolumetricFog.GlobalDensity);
			_InscatterCompute.SetFloat("_NoiseSize", 1.0f - _VolumetricFog.CloudsSize);
			//InscatterShader.SetVector("_WindVelocity", WindVelocity * Time.realtimeSinceStartup);

			_InscatterCompute.SetVector("_AtmosphereColor", _VolumetricFog.AtmosphereColor);
			_InscatterCompute.SetVector("_ShadowColor", _VolumetricFog.ShadowColor);
			_InscatterCompute.SetVector("_AmbientColor", _VolumetricFog.AmbientColor);

			_InscatterCompute.SetFloat("_CameraNear", Camera.main.nearClipPlane);
			_InscatterCompute.SetFloat("_CameraFar", Camera.main.farClipPlane);
			_InscatterCompute.SetBool("_Noise", _VolumetricFog.Clouds);
			_InscatterCompute.SetFloat("_Shadow", _VolumetricFog.Shadow);


			_InscatterCompute.SetVector("_VolumeResolution", _Buffers.VolumeResolution);


			_InscatterCompute.SetFloat("_Scattering", _VolumetricFog.Scattering);
			_InscatterCompute.SetFloat("_Absorption", _VolumetricFog.Absorption);

			_InscatterCompute.SetFloat("_SunAnistropy", _VolumetricFog.AnisotropySun);
			_InscatterCompute.SetFloat("_AtmosphereAnistropy", _VolumetricFog.AnisotropyAtmosphere);

			_InscatterCompute.SetFloat("_RadialLobe", _VolumetricFog.RadialLobe);
			_InscatterCompute.SetFloat("_RadialBlend", _VolumetricFog.RadialBlend);

			_InscatterCompute.SetVector("_CameraPosition", Camera.main.transform.position);
			_InscatterCompute.SetVector("_CameraUp", Camera.main.transform.up);
			_InscatterCompute.SetVector("_CameraRight", Camera.main.transform.right);
			_InscatterCompute.SetVector("_CameraFront", Camera.main.transform.forward);


			_InscatterCompute.SetFloat("_CameraAspect", Camera.main.aspect);
			_InscatterCompute.SetFloat("_CameraFOV", Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f);

			_InscatterCompute.SetFloat("_RampStart", _VolumetricFog.RampStart);
			_InscatterCompute.SetFloat("_RampEnd", _VolumetricFog.RampEnd);
			_InscatterCompute.SetFloat("_RampStartDensity", _VolumetricFog.RampStartDensity);
			_InscatterCompute.SetFloat("_RampEndDensity", _VolumetricFog.RampEndDensity);
			_InscatterCompute.SetFloat("_RampInfluence", _VolumetricFog.RampInfluence);

			_InscatterCompute.SetFloat("_NoiseInfluence", _VolumetricFog.CloudsInfluenceBottom);

			_InscatterCompute.SetVector("_SunDirection", _Buffers.Sun.transform.forward);
			_InscatterCompute.SetVector("_SunColor", new Vector3(_Buffers.Sun.color.r, _Buffers.Sun.color.g, _Buffers.Sun.color.b));
			_InscatterCompute.SetFloat("_SunIntensity", _Buffers.SunVolumetricLight.Intensity);

			_InscatterCompute.SetFloat("_DensityInfluence", 1);
			_InscatterCompute.SetBool("_NoiseOcclusion", _VolumetricFog.CloudsOcclusion);

			_InscatterCompute.SetTexture(_InscatterKernel, "_SMediaMap", _CurrentFrame % 2 == 0 ? _Buffers.VolumeShadingMap : _Buffers.VolumeShadingMap2);

			_InscatterCompute.SetTexture(_InscatterKernel, "_PMVolume", _CurrentFrame % 2 == 0 ? _Buffers.PMVolume : _Buffers.PMVolume2);
			_InscatterCompute.SetTexture(_InscatterKernel, "_PMRadiance", _CurrentFrame % 2 == 0 ? _Buffers.PMRadiance : _Buffers.PMRadiance2);
			_InscatterCompute.SetFloat("_RenderDistance", scaledRenderDistance);

			_InscatterCompute.SetTexture(_InscatterKernel, "_RadianceMap", _Buffers.RadianceMap);


			_InscatterCompute.SetTexture(_InscatterKernel, "_NoiseLUT", _Buffers.VolumeNoiseLUT);

			_InscatterCompute.SetBuffer(_InscatterKernel, "_DepthData", _Buffers.DepthSteps);

			// dispatch compute
			_InscatterCompute.Dispatch(_InscatterKernel, (int)_Buffers.VolumeResolution.x / 8,
														 (int)_Buffers.VolumeResolution.y / 8, 1);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		/// <param name="merge"></param>
		private		void		_ComposePass(RenderTexture input, RenderTexture output, bool merge)
		{
			_ComposeMaterial.SetInt("_AnalyticalFog", _VolumetricFog.AnalyticalFog ? 1 : 0);
			_ComposeMaterial.SetFloat("_FogDensity", _VolumetricFog.GlobalDensity);
			_ComposeMaterial.SetColor("_AmbientColor", _VolumetricFog.AmbientColor);

			float scaledRenderDistance = (QualitySettings.shadowDistance - QualitySettings.shadowNearPlaneOffset)
											/ (Camera.main.farClipPlane - Camera.main.nearClipPlane) * _VolumetricFog.RenderDistance;

			_ComposeMaterial.SetFloat("_RenderDistance", scaledRenderDistance);
			_ComposeMaterial.SetVector("_SunColor", _Buffers.Sun.color);
			_ComposeMaterial.SetVector("_SunDirection", _Buffers.Sun.transform.forward);
			_ComposeMaterial.SetFloat("_Scattering", _VolumetricFog.Scattering);
			_ComposeMaterial.SetFloat("_Absorption", _VolumetricFog.Absorption);

			_ComposeMaterial.SetFloat("_HeightBottom", _VolumetricFog.HeightBottom);
			_ComposeMaterial.SetFloat("_HeightTop", _VolumetricFog.HeightTop);
			_ComposeMaterial.SetFloat("_DensityBottom", _VolumetricFog.DensityBottom);
			_ComposeMaterial.SetFloat("_DensityTop", _VolumetricFog.DensityTop);

			_ComposeMaterial.SetFloat("_SunIntensity", _Buffers.SunVolumetricLight.Intensity);
			_ComposeMaterial.SetFloat("_SunAnistropy", _VolumetricFog.AnisotropySun);
			_ComposeMaterial.SetFloat("_AtmosphereAnistropy", _VolumetricFog.AnisotropyAtmosphere);
			_ComposeMaterial.SetFloat("_RadialLobe", _VolumetricFog.RadialLobe);
			_ComposeMaterial.SetFloat("_RadialBlend", _VolumetricFog.RadialBlend);
			_ComposeMaterial.SetVector("_AtmosphereColor", _VolumetricFog.AtmosphereColor);

			if (_VolumetricFog.VolumeFilter == VOLUME_FILTERING.Enabled)
				_ComposeMaterial.SetTexture("_PMediaMap0", _CurrentFrame % 2 == 0 ? _Buffers.VolumeShadingMap2 : _Buffers.VolumeShadingMap);
			else
				_ComposeMaterial.SetTexture("_PMediaMap0", _CurrentFrame % 2 == 0 ? _Buffers.VolumeShadingMap : _Buffers.VolumeShadingMap2);

			_ComposeMaterial.SetFloat("_CameraFar", Camera.main.farClipPlane);
			_ComposeMaterial.SetFloat("_CameraNear", Camera.main.nearClipPlane);
			_ComposeMaterial.SetInt("_Noise", _VolumetricFog.Clouds ? 1 : 0);
			_ComposeMaterial.SetInt("_Merge", merge ? 1 : 0);

			_ComposeMaterial.SetVector("_CameraPosition", Camera.main.transform.position);
			_ComposeMaterial.SetVector("_CameraUp", Camera.main.transform.up);
			_ComposeMaterial.SetVector("_CameraRight", Camera.main.transform.right);
			_ComposeMaterial.SetVector("_CameraFront", Camera.main.transform.forward);

			_ComposeMaterial.SetFloat("_CameraAspect", Camera.main.aspect);
			_ComposeMaterial.SetFloat("_CameraFOV", Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f);

			_ComposeMaterial.SetFloat("_FinalClamp", _VolumetricFog.VolumetricFogClamp);
			_ComposeMaterial.SetFloat("_SkyClamp", _VolumetricFog.VolumetricFogSkyClamp);
			_ComposeMaterial.SetFloat("_SkyClampAnalytical", _VolumetricFog.AnalyticalFogSkyClamp);
			_ComposeMaterial.SetFloat("_AnalyticalClamp", _VolumetricFog.AnalyticalFogClamp);


			_ComposeMaterial.SetFloat("_RampStart", _VolumetricFog.RampStart);
			_ComposeMaterial.SetFloat("_RampEnd", _VolumetricFog.RampEnd);
			_ComposeMaterial.SetFloat("_RampInfluence", _VolumetricFog.RampInfluence);

			_ComposeMaterial.SetFloat("_DensityInfluence", 1);

			Graphics.Blit(input, output, _ComposeMaterial);
		}
	}
}
