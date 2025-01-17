using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Avol
{
	public class VolumetricFog : MonoBehaviour
	{


		// ------------------------------------------- EDITOR PROPERTIES ---------------------------------------------------------- //


		// ========= QUALITY SETTINGS ========= //

		[Header("Quality & Performance")]

		public			SUPER_SAMPLING				SuperSampling					= SUPER_SAMPLING.Off;

		[Tooltip("3D Volume texture used to store fog data resolution.")]
		public			VOLUME_RESOLUTION			VolumeResolution				= VOLUME_RESOLUTION._160x96x92Optimal;

		[Tooltip("Displays FPS and numbers lights / volumes. Will only run in editor.")]
		public          bool                        DebugPerformance                = false;

		// ========= FILTERS ========= //

		[Header("Filters")]

		[Tooltip("Filters shadow map in logarithmic space causing it to have less artifacts and flicker, but at the cost of ray sharpness and performance.")]
		public          LOG_BLUR                    LogarithmicFilter               = LOG_BLUR._Off;

		[Tooltip("Filters fog volume using multiple frame samples. This is always on, because it is neccasery to avoid sharp stepping artifacts.")]
		public          TEMPORAL_AA                 TemporalFilter                  = TEMPORAL_AA.Extreme;

		[Tooltip("Efficiently filters 3D volume to remove artifacts produced by sampling low resolution volume texture at the cost of ray sharpness and performance.")]
		public          VOLUME_FILTERING            VolumeFilter                    = VOLUME_FILTERING.Enabled;


		// ========= GENERAL SETTINGS ========= //

		[Header("General")]

		[Tooltip("Should analytical fog cover rest of the camera render distance, where volumetric fog doesn't reach anymore.  If you want to have higher view distances and still have sharp volumetric rays near camera, alterntively, you can disable this option and use other fog shaders on top that are provided by unity.")]
		public          bool                        AnalyticalFog                   = true;

		[Tooltip("Where volumetric fog should stop being rendered. Smaller the distance, less aliasing occurs.")]
		[Range(0.0f, 1.0f)]
		public          float                       RenderDistance                  = 1.0f;

		[Range(0.0f, 1.0f)]
		[Tooltip("Clamps final volumetric fog contribution to image allowing to show skybox and other objects that are far away from camera.")]
		public          float                       VolumetricFogClamp              = 1.0f;

		[Range(0.0f, 1.0f)]
		[Tooltip("Clamps final analytical fog contribution to image only on sky where depth is 1.")]
		public          float                       AnalyticalFogClamp              = 0.05f;

		[Range(0.0f, 1.0f)]
		[Tooltip("Clamps final fog contribution to image only on sky where depth is 1.")]
		public          float                       VolumetricFogSkyClamp           = 1.0f;

		[Range(0.0f, 1.0f)]
		[Tooltip("Clamps final fog contribution to image only on sky where depth is 1.")]
		public			float						AnalyticalFogSkyClamp			= 1.0f;

		[Range(-1.0f, 1.0f)]
		[Tooltip("Offsets direcetional light shadow depth to create more light leaking(subsurface scattering effect) or to remove it. 0 = default.")]
		public          float                       ShadowBias                      = 0.0f;

		[Range(0.0f, 1.0f)]
		[Tooltip("Makes sun shadows more apparent. NOTE: this is not direct intensity of the shadow, but rather a coefficient for exponential shadow map. Higher values tend to produce more aliasing.")]
		public          float                       Shadow                          = 1.0f;

		// ========= SUN & ATSMOSPHERE SETTINGS ========= //

		[Header("Sun & Atmosphere Settings")]

		[Tooltip("Defines how much light will be absorbed per layer.")]
		[Range(0.0f, 1.0f)]
		public          float                       Absorption                      = 0.5f;

		[Tooltip("Defines how much light will be scattered per layer.")]
		[Range(0.0f, 1.0f)]
		public          float                       Scattering                      = 1.0f;

		[Tooltip("The opposite of sun direction color.")]
		public          Color                       AmbientColor                    = new Color(0, 0.258f, 0.345f);//Color.cyan;

		[Tooltip("The fog color.")]
		public          Color                       AtmosphereColor                 = new Color(1, 0, 0.435f);

		[Tooltip("The shadow color.")]
		public          Color                       ShadowColor                     = Color.black;

		[Tooltip("A anistropic ReyLeigh phase function for the atmosphere color over the fog. Adjusts the anisotropy for sun atmosphere scattering. When 0 = isotropic, then 1 = perfect forward, and -1 = perfect backward scattering.")]
		[Range(-1.0f, 1.0f)]
		public          float                       AnisotropyAtmosphere             = 0.58f;

		[Tooltip("Radial lobe for atmosphere. 0 = uniformal, 1 = ReyLeigh phase function.")]
		[Range(0.0f, 1.0f)]
		public          float                       RadialLobe                       = 0.96f;

		[Tooltip("A anistropic phase function for the Sun color over the fog. Adjusts the anisotropy for sun radial scattering. When 0 = isotropic, then 1 = perfect forward, and -1 = perfect backward scattering.")]
		[Range(-1.0f, 1.0f)]
		public          float                       AnisotropySun                    = 0.585f;

		[Tooltip("Blends atmosphere with sun radial contribution. 0 = full atmosphere contribution, 1 = full sun contribution.")]
		[Range(0.0f, 1.0f)]
		public          float                       RadialBlend                      = 0.4f;



		// ========= FOG DISTRIBUTION SETTINGS ========= //

		[Header("Fog Distribution")]

		[Tooltip("How dense / thick the fog should be. NOTE: too high values will create more artifacts, do not make fog too thick. Too thick fog should also not be desired artisticly as the scene itself won't be visible.")]
		public          float                       GlobalDensity                   = 50.0f;

		[Tooltip("Fog bottom height.")]
		public          float                       HeightBottom                    = -10;

		[Tooltip("Fog top height.")]
		public          float                       HeightTop                       = 500;

		[Tooltip("Density at the bottom of the fog.")]
		[Range(0.0f, 1.0f)]
		public          float                       DensityBottom                   = 1.0f;

		[Tooltip("Density top of the fog.")]
		[Range(0.0f, 1.0f)]
		public          float                       DensityTop                      = 1.0f;


		[Range(0.0f, 1.0f)]
		[Tooltip("How much ramp should influence density.")]
		public          float                       RampInfluence                   = 0.0f;

		[Range(0.0f, 0.99f)]
		[Tooltip("Specifies start distance of the fog density ramp.")]
		public          float                       RampStart                       = 0.0f;

		[Range(0.01f, 1.0f)]
		[Tooltip("Specifies end distance of the fog density ramp.")]
		public          float                       RampEnd                         = 1.0f;

		[Range(0.01f, 1.0f)]
		[Tooltip("Density at the near clip of the camera.")]
		public          float                       RampStartDensity                = 1.0f;

		[Range(0.01f, 1.0f)]
		[Tooltip("Density at the far clip of the camera.")]
		public          float                       RampEndDensity                  = 1.0f;

		// ========= PARTICIPATING MEDIUM SETTINGS ========= //

		[Header("Clouds")]

		[Tooltip("Should fog have cloud like noise.")]
		public          bool                        Clouds                          = false;

		[Tooltip("Occludes noise with directional light. Doubles the total noise sample count.")]
		public          bool                        CloudsOcclusion                 = false;


		public			float						OcclusionRayDistance			= 0.01f;

		[Range(0.0f, 50.0f)]
		[Tooltip("How strong should clouds occlude light.")]
		public          float                       OcclusionStrength               = 10.0f;

		[Range(0.0f, 50.0f)]
		[Tooltip("How strong should clouds be lit by sun.")]
		public          float                       OutlineStrength                 = 1.0f;

		[Range(0.0f, 1.0f)]
		public          float                       OutlineRadius					= 1.0f;

		public			Color						CloudDirectColor				= Color.white;

		[Range(0.01f, 0.99f)]
		[Tooltip("The size of the clouds.")]
		public          float                       CloudsSize                      = 0.01f;

		[Tooltip("Noise spatial frequency.")]
		public          Vector3                     CloudsFrequency                 = Vector3.one;

		[Tooltip("Noise density scale.")]
		[Range(0, 1)]
		public          float                       CloudsDensityScale				= 1.0f;


		public			bool						EnableCloudSpacing				= false;

		public			float						CloudsSpacing					= 0.5f;

		public			Vector3						CloudsSpacingFrequency			= Vector3.one;


		[Tooltip("What height should noise appear at.")]
		public          float                       CloudsHeightBottom              = 20.0f;

		[Tooltip("What height should noise dissapear at.")]
		public          float                       CloudsHeightTop                 = 100.0f;

		[Range(0.0f, 1.0f)]
		[Tooltip("Noise influence over the fog.")]
		public          float                       CloudsInfluenceBottom           = 0.5f;

		[Range(0.0f, 1.0f)]
		[Tooltip("Noise influence over the fog.")]
		public          float                       CloudsInfluenceTop              = 0.5f;

		[Tooltip("Fades the edge between fog without noise & with noise.")]
		public          float                       CloudsFadeBottomHeight          = 20.0f;

		[Tooltip("Fades the edge between fog without noise & with noise.")]
		public          float                       CloudsFadeTopHeight             = 20.0f;

		[Tooltip("The direction of wind that will affect fog noise clouds.")]
		public          Vector3                     WindVelocity                    = Vector3.right;

		//public			float						CloudsVignetting						= 0.5f;
		public			float						DepthAttenuation				= 0.5f;


		// ------------------------------------------- PRIVATE PROPERTIES ---------------------------------------------------------- //


		private         bool                        _Initialized					= false;
		private			FogCore						_Core;
		private			float						_DeltaTime						= 0.0f;


		// ------------------------------------------- MONO EVENTS ----------------------------------------------------------- //

		/// <summary>
		/// Start this instance.
		/// </summary>
		void Start()
		{
			_Core = new FogCore(this);
			_Initialized = true;
		}

		void Update()
		{
			_Core.Update();
		}

		/// <summary>
		/// Raises the render image event.
		/// </summary>
		/// <param name="input">Input.</param>
		/// <param name="output">Output.</param>
		void OnRenderImage(RenderTexture input, RenderTexture output)
		{
			if (this._Initialized)
			{
				_Core.Render(input, output);
			}
		}

		/// <summary>
		/// Release buffers
		/// </summary>
		void OnDisable()
		{
			if (_Initialized)
				_Core.ReleaseAll();
		}

		/// <summary>
		/// Recreate buffers
		/// </summary>
		void OnEnable()
		{
			if (_Initialized)
				_Core.Rebuild();
		}

#if UNITY_EDITOR
		void OnGUI()
		{
			if (DebugPerformance)
			{
				_DeltaTime		+= (Time.unscaledDeltaTime - _DeltaTime) * 0.1f;

				float ms		= _DeltaTime * 1000.0f;
				float fps		= 1.0f / _DeltaTime;

				GUI.Label(new Rect(10, 10, 100, 30), "FPS: " + Mathf.RoundToInt(fps));
				GUI.Label(new Rect(10, 30, 100, 30), string.Format("MS: {0:0.0}", ms));


				int lightCount		= _Core.GetBuffers().Lights.Count;
				int visLightCount	= _Core.GetBuffers().VisibleLights.Count;

				GUI.Label(new Rect(10, 50, 200, 30), "Culled Lights:  " + visLightCount + " / " + lightCount);

				int volumeCount		= _Core.GetBuffers().Lights.Count;
				int visVolumeCount	= _Core.GetBuffers().VisibleLights.Count;

				GUI.Label(new Rect(10, 70, 200, 30), "Culled Volumes:  " + visVolumeCount + " / " + volumeCount);
			}
		}
#endif
	}

}