using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Avol
{
	public class Buffers
	{
		public			Vector3						VolumeResolution						{ get; private set; }
		public			float						DepthScaledGlobalDensity				{ get; private set; }

		public			Light						Sun										{ get; private set; }
		public			VolumetricLight				SunVolumetricLight						{ get; private set; }


		// ---------------------------------------------------- CORE -------------------------------------------------------- //

		private		   VolumetricFog				_VolumetricFog							= null;

		public         List<Light>                 Lights									= new List<Light>();
		public         List<FogZone>			   Volumes									= new List<FogZone>();

		public		   List<SPOT_LIGHT_DATA>	   ShadowedSpotLightsData					= new List<SPOT_LIGHT_DATA>();
		public		   List<VolumetricLight>	   ShadowedSpotLights						= new List<VolumetricLight>();


		public		   List<Light>                 VisibleLights							= new List<Light>();
		public         List<FogZone>               VisibleVolumes							= new List<FogZone>();


		// ---------------------------------------------------- Compute Buffers --------------------------------------------------- //

		public			ComputeBuffer				PointLightBuffer						{ get; private set; }
		public			ComputeBuffer				SpotLightBuffer							{ get; private set; }
		public			ComputeBuffer				BoxVolumeBuffer							{ get; private set; }
		public			ComputeBuffer				EllipsoidVolumeBuffer					{ get; private set; }

		public			ComputeBuffer				DepthSteps								{ get; private set; }




		// ---------------------------------------------------- Render Targets --------------------------------------------------- //

		public			RenderTexture				DirLightMatricesTexture					{ get; private set; }
		public			RenderTexture				VolumeNoiseLUT							{ get; private set; }
		//public			RenderTexture				VolumeNoiseOcclusionLUT					{ get; private set; }
		

		public			RenderTexture				PMVolume								{ get; private set; }
		public			RenderTexture				PMVolume2								{ get; private set; }

		public			RenderTexture				PMRadiance								{ get; private set; }
		public			RenderTexture				PMRadiance2								{ get; private set; }

		public			RenderTexture				VolumeShadingMap						{ get; private set; }
		public			RenderTexture				VolumeShadingMap2						{ get; private set; }

		public          RenderTexture[]             RadianceMaps                            = new RenderTexture[8];
		public			RenderTexture				RadianceMap								= null;
		public			RenderTexture				RadianceMap2							= null;



		private         CullingGroup                _CullingGroup;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="volumetricFog"></param>
		public Buffers(VolumetricFog volumetricFog)
		{
			_VolumetricFog = volumetricFog;
			
			// create lists.
			Lights                                 = new List<Light>();
			Volumes                                = new List<FogZone>();
		
			VisibleLights                          = new List<Light>();
			VisibleVolumes                         = new List<FogZone>();

			// HACK: sample dir light shadow projection matrices from engines hidden internals into this texture
			DirLightMatricesTexture = new RenderTexture(4 * QualitySettings.shadowCascades, 1, 1, RenderTextureFormat.ARGBFloat);
			DirLightMatricesTexture.filterMode = FilterMode.Point;

			// initialise buffers
			ComputeVolumeTextures();
			ExtractLights();
			ExtractVolumes();
			_StartCullingGroup();
		}


		/// <summary>
		/// Get volumetric lights
		/// </summary>
		public void ExtractLights()
		{
			Lights.Clear();

			Light[] lights = MonoBehaviour.FindObjectsOfType(typeof(Light)) as Light[];
			for (int i = 0; i < lights.Length; i++)
				if (lights[i].GetComponent<VolumetricLight>() != null)
				{
					if (lights[i].type == LightType.Directional)
					{
						Sun = lights[i];
						SunVolumetricLight = Sun.GetComponent<VolumetricLight>();
					}
					else Lights.Add(lights[i]);
				}

			if (Sun == null)
				UnityEngine.Debug.LogError("No directional light has VolumetricLight.cs script attached to it. For the fog to work, atleast one directional light has to have the script attached.");
		}

		/// <summary>
		/// Extracts fog volumes
		/// </summary>
		public void ExtractVolumes()
		{
			Volumes.Clear();
			FogZone[] fogVolumes = MonoBehaviour.FindObjectsOfType(typeof(FogZone)) as FogZone[];
			for (int i = 0; i < fogVolumes.Length; i++)
				Volumes.Add(fogVolumes[i]);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="resolution"></param>
		/// <param name="format"></param>
		/// <param name="filter"></param>
		/// <param name="wrap"></param>
		/// <returns></returns>
		public RenderTexture _CreateRT(Vector3 resolution, RenderTextureFormat format, FilterMode filter = FilterMode.Bilinear, TextureWrapMode wrap = TextureWrapMode.Clamp)
		{
			RenderTexture rt = new RenderTexture((int)resolution.x, (int)resolution.y, (int)resolution.z);

			rt.filterMode = filter;
			rt.wrapMode = wrap;

			if (resolution.z > 1)
			{
				rt.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
				rt.volumeDepth = (int)resolution.z;
			}

			rt.format = format;
			rt.enableRandomWrite = true;
			rt.antiAliasing = 1;
			rt.Create();

			return rt;
		}


		/// <summary>
		/// Creates lights data buffer
		/// TODO: clean this up.
		/// </summary>
		public void LightBufferGenerate()
		{
			// set all lights.
			List<POINT_LIGHT_DATA>	pointLightData			= new List<POINT_LIGHT_DATA>();
			List<SPOT_LIGHT_DATA>	spotLightData			= new List<SPOT_LIGHT_DATA>();

			List<SPOT_LIGHT_DATA>	shadowedSpotLightData	= new List<SPOT_LIGHT_DATA>();
			List<VolumetricLight>	shadowedSpotLights		= new List<VolumetricLight>();

			for (int i = 0; i < VisibleLights.Count; i++)
			{
				Light light = VisibleLights[i];
				if (light.type == LightType.Point)
				{
					POINT_LIGHT_DATA light_data = new POINT_LIGHT_DATA();
					light_data.Color = new Vector3(light.color.r, light.color.g, light.color.b);
					light_data.Intensity = light.GetComponent<VolumetricLight>().Intensity;
					light_data.Position = light.transform.position;
					light_data.Range = light.range;
					pointLightData.Add(light_data);
				}
				else if (light.type == LightType.Spot)
				{
					SPOT_LIGHT_DATA light_data = new SPOT_LIGHT_DATA();
					VolumetricLight component = light.GetComponent<VolumetricLight>();
					light_data.Color = new Vector3(light.color.r, light.color.g, light.color.b);
					light_data.Intensity = light.GetComponent<VolumetricLight>().Intensity;
					light_data.Position = light.transform.position;
					light_data.Range = light.range;
					light_data.Direction = light.transform.forward;
					light_data.Angle = Mathf.Cos(light.spotAngle * Mathf.Deg2Rad / 2.0f);
					light_data.Attenuation = new Vector2(component.Softness, component.Softness2 * light.range) * 0.5f;

					if (component.CastShadow)
					{
						shadowedSpotLights.Add(component);
						shadowedSpotLightData.Add(light_data);
					}
					else
					{
						spotLightData.Add(light_data);
					}
				}
			}

			if (pointLightData.Count != 0)
			{
				if (PointLightBuffer != null)
					PointLightBuffer.Release();
				PointLightBuffer = new ComputeBuffer(pointLightData.Count, Marshal.SizeOf(typeof(POINT_LIGHT_DATA)));
				PointLightBuffer.SetData(pointLightData.ToArray());
			}

			if (spotLightData.Count != 0)
			{
				if (SpotLightBuffer != null)
					SpotLightBuffer.Release();
				SpotLightBuffer = new ComputeBuffer(spotLightData.Count, Marshal.SizeOf(typeof(SPOT_LIGHT_DATA)));
				SpotLightBuffer.SetData(spotLightData.ToArray());
			}


			ShadowedSpotLightsData = shadowedSpotLightData;
			ShadowedSpotLights = shadowedSpotLights;
		}

		/// <summary>
		/// Creates PM volume buffers.
		/// </summary>
		public void VolumeBufferGenerate()
		{
			// set all lights.
			List<BOX_VOLUME_DATA> boxVolumeData = new List<BOX_VOLUME_DATA>();
			List<ELLIPSOID_VOLUME_DATA> ellipsoidVolumeData = new List<ELLIPSOID_VOLUME_DATA>();

			for (int i = 0; i < VisibleVolumes.Count; i++)
			{
				FogZone volume = VisibleVolumes[i];
				if (volume.VolumeShape == VOLUME_SHAPE.Box)
				{
					BOX_VOLUME_DATA volume_data = new BOX_VOLUME_DATA();
					volume_data.Color = volume.Emission;
					volume_data.Dimensions = volume.Dimensions;
					volume_data.Position = volume.transform.position;
					volume_data.Absorption = volume.Absorption;
					volume_data.Mode = (int)volume.VolumeType;
					volume_data.SoftEdges = volume.SoftEdges;
					boxVolumeData.Add(volume_data);
				}
				else if (volume.VolumeShape == VOLUME_SHAPE.Ellipsoid)
				{
					ELLIPSOID_VOLUME_DATA volume_data = new ELLIPSOID_VOLUME_DATA();
					volume_data.Color = volume.Emission;
					volume_data.Dimensions = volume.Dimensions;
					volume_data.Position = volume.transform.position;
					volume_data.Absorption = volume.Absorption;
					volume_data.Mode = (int)volume.VolumeType;
					volume_data.SoftEdges = volume.SoftEdges;
					ellipsoidVolumeData.Add(volume_data);
				}
			}

			if (boxVolumeData.Count != 0)
			{
				if (BoxVolumeBuffer != null)
					BoxVolumeBuffer.Release();
				BoxVolumeBuffer = new ComputeBuffer(boxVolumeData.Count, Marshal.SizeOf(typeof(BOX_VOLUME_DATA)));
				BoxVolumeBuffer.SetData(boxVolumeData.ToArray());
			}

			if (ellipsoidVolumeData.Count != 0)
			{
				if (EllipsoidVolumeBuffer != null)
					EllipsoidVolumeBuffer.Release();
				EllipsoidVolumeBuffer = new ComputeBuffer(ellipsoidVolumeData.Count, Marshal.SizeOf(typeof(ELLIPSOID_VOLUME_DATA)));
				EllipsoidVolumeBuffer.SetData(ellipsoidVolumeData.ToArray());
			}
		}

		/// <summary>
		/// Generates the depth steps.
		/// </summary>
		public void GenerateDepthSteps()
		{
			float scaledRenderDistance = (QualitySettings.shadowDistance - QualitySettings.shadowNearPlaneOffset)
														/ (Camera.main.farClipPlane - Camera.main.nearClipPlane) * _VolumetricFog.RenderDistance;

			Matrix4x4 PV = (Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix);

			float[] depthSteps = new float[(int)VolumeResolution.z];
			for (int i = 1; i < VolumeResolution.z; i++)
			{
				Vector3 nearPosition = Camera.main.transform.position + Camera.main.transform.forward * Camera.main.nearClipPlane;
				Vector3 samplePosition = Camera.main.transform.forward * ((Camera.main.farClipPlane - Camera.main.nearClipPlane) * scaledRenderDistance);

				Vector3 worldPosition = nearPosition + samplePosition / VolumeResolution.z * i;



				Vector4 n = PV.MultiplyPoint3x4(new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 1.0f)) / (Camera.main.farClipPlane - Camera.main.nearClipPlane);
				depthSteps[i - 1] = n.z;
			}

			DEPTH_STEPS[] steps = new DEPTH_STEPS[(int)VolumeResolution.z];
			for (int i = 0; i < VolumeResolution.z; i++)
				steps[i].Step = depthSteps[i];

			DepthSteps.SetData(steps);
		}

		/// <summary>
		/// recomputes all textures to match specified volume resolution.
		/// </summary>
		public void ComputeVolumeTextures()
		{
			VOLUME_RESOLUTION volumeResolution = _VolumetricFog.VolumeResolution;

			switch (volumeResolution)
			{
				case VOLUME_RESOLUTION._96x56x64Cheap:
					VolumeResolution = new Vector3(96, 56, 64);
					break;
				case VOLUME_RESOLUTION._112x64x64Cheap:
					VolumeResolution = new Vector3(112, 64, 64);
					break;
				case VOLUME_RESOLUTION._128x72x64Cheap:
					VolumeResolution = new Vector3(128, 72, 64);
					break;
				case VOLUME_RESOLUTION._128x88x64Cheap:
					VolumeResolution = new Vector3(128, 88, 64);
					break;
				case VOLUME_RESOLUTION._160x96x64Optimal:
					VolumeResolution = new Vector3(160, 96, 64);
					break;
				case VOLUME_RESOLUTION._192x112x64Optimal:
					VolumeResolution = new Vector3(192, 112, 64);
					break;
				case VOLUME_RESOLUTION._160x96x72Optimal:
					VolumeResolution = new Vector3(160, 96, 72);
					break;
				case VOLUME_RESOLUTION._160x96x92Optimal:
					VolumeResolution = new Vector3(160, 96, 92);
					break;
				case VOLUME_RESOLUTION._160x96x128High:
					VolumeResolution = new Vector3(160, 96, 128);
					break;
				case VOLUME_RESOLUTION._192x112x128High:
					VolumeResolution = new Vector3(192, 112, 128);
					break;
				case VOLUME_RESOLUTION._256x144x64High:
					VolumeResolution = new Vector3(256, 144, 64);
					break;
				case VOLUME_RESOLUTION._256x144x96High:
					VolumeResolution = new Vector3(256, 144, 96);
					break;
				case VOLUME_RESOLUTION._320x184x64Cinematic:
					VolumeResolution = new Vector3(320, 184, 64);
					break;
				case VOLUME_RESOLUTION._320x184x92Cinematic:
					VolumeResolution = new Vector3(320, 184, 92);
					break;
				case VOLUME_RESOLUTION._256x144x128Cinematic:
					VolumeResolution = new Vector3(256, 144, 128);
					break;
				case VOLUME_RESOLUTION._320x184x128Cinematic:
					VolumeResolution = new Vector3(320, 184, 128);
					break;
			}


			// create a bunch of volume textures
			PMVolume				= _CreateRT(VolumeResolution, RenderTextureFormat.ARGBHalf);
			PMVolume2				= _CreateRT(VolumeResolution, RenderTextureFormat.ARGBHalf);

			PMRadiance				= _CreateRT(VolumeResolution, RenderTextureFormat.R8);
			PMRadiance2				= _CreateRT(VolumeResolution, RenderTextureFormat.R8);

			VolumeShadingMap		= _CreateRT(VolumeResolution, RenderTextureFormat.ARGB32);
			VolumeShadingMap2		= _CreateRT(VolumeResolution, RenderTextureFormat.ARGB32); 


			VolumeNoiseLUT				= _CreateRT(new Vector3(76, 76, 76), RenderTextureFormat.R8, FilterMode.Trilinear, TextureWrapMode.Repeat);
			//VolumeNoiseOcclusionLUT		= _CreateRT(new Vector3(76, 76, 76), RenderTextureFormat.ARGB32, FilterMode.Bilinear, TextureWrapMode.Repeat);

			RadianceMap		= _CreateRT(VolumeResolution, RenderTextureFormat.R8);
			RadianceMap2	= _CreateRT(VolumeResolution, RenderTextureFormat.R8);

			// create radiance data volumes for TSSAA.
			for (int i = 0; i < RadianceMaps.Length; i++)
				RadianceMaps[i] = _CreateRT(VolumeResolution, RenderTextureFormat.R8);


			// setup depth buffers
			if (DepthSteps != null)
				DepthSteps.Release();
			DepthSteps = new ComputeBuffer((int)VolumeResolution.z, Marshal.SizeOf(typeof(DEPTH_STEPS)));
			GenerateDepthSteps();

			// generate buffers.
			LightBufferGenerate();
			VolumeBufferGenerate();

			// scale density acording to new depth.
			DepthScaledGlobalDensity = _VolumetricFog.GlobalDensity;// * (VolumeResolution.z / 64.0f);
		}

		/// <summary>
		/// 
		/// </summary>
		public void ReleaseAll()
		{
			DepthSteps.Dispose();
			if (PointLightBuffer != null) PointLightBuffer.Dispose();
			if (SpotLightBuffer != null) SpotLightBuffer.Dispose();
			if (BoxVolumeBuffer != null) BoxVolumeBuffer.Dispose();
			if (EllipsoidVolumeBuffer != null) EllipsoidVolumeBuffer.Dispose();
			_CullingGroup.Dispose();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		private void _CullStateChanged(CullingGroupEvent evt)
		{
			int lCount = Lights.Count;

			if (evt.hasBecomeVisible)
			{
				if (evt.index >= lCount) VisibleVolumes.Add(Volumes[evt.index - lCount]);
				else VisibleLights.Add(Lights[evt.index]);
			}
			else if (evt.hasBecomeInvisible)
			{
				if (evt.index >= lCount) VisibleVolumes.Remove(Volumes[evt.index - lCount]);
				else VisibleLights.Remove(Lights[evt.index]);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void _StartCullingGroup()
		{
			_CullingGroup = new CullingGroup();

			_CullingGroup.targetCamera = Camera.main;

			List<BoundingSphere> boundingSpheres = new List<BoundingSphere>();

			// add lights bounding spheres
			for (int i = 0; i < Lights.Count; i++)
			{
				Light light = Lights[i];
				if (light.type == LightType.Point)
					boundingSpheres.Add(new BoundingSphere(light.transform.position, light.range));
				else if (light.type == LightType.Spot)
					boundingSpheres.Add(new BoundingSphere(light.transform.position, light.range));
			}

			// add volume bounding spheres.
			for (int c = 0; c < Volumes.Count; c++)
			{
				FogZone volume = Volumes[c];
				if (volume.VolumeShape == VOLUME_SHAPE.Box)
					boundingSpheres.Add(new BoundingSphere(volume.transform.position, volume.Dimensions.magnitude));
				else
					boundingSpheres.Add(new BoundingSphere(volume.transform.position, Mathf.Max(Mathf.Max(volume.Dimensions.x, volume.Dimensions.y), volume.Dimensions.z)));
			}

			_CullingGroup.SetBoundingSpheres(boundingSpheres.ToArray());
			_CullingGroup.SetBoundingSphereCount(boundingSpheres.Count);

			_CullingGroup.onStateChanged = _CullStateChanged;
		}

		public string CalculateTextureMemory(RenderTexture rt)
		{
			int pixelCount = rt.width * rt.height * (rt.volumeDepth != 0 ? rt.volumeDepth : 1);
			int bytes	   = 0;

			switch (rt.format)
			{
				case RenderTextureFormat.ARGBHalf:
					bytes = 16 * 4;
					break;
				case RenderTextureFormat.ARGB32:
					bytes = 8 * 4;
					break;
				case RenderTextureFormat.R8:
					bytes = 8;
					break;
				case RenderTextureFormat.RHalf:
					bytes = 16;
					break;
			}

			return "" + Mathf.RoundToInt( (bytes * pixelCount) / (1024.0f * 1024.0f) ) + " MB";
		}
	}
}
