using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

namespace Avol
{
	public class VolumetricLight : MonoBehaviour
	{
		[Header("General")]
		[Range(0.0f, 4.0f)]
		public               float                   Intensity          = 0.5f;

		[Tooltip("NOTE: Only affects spot lights.")]
		[Range(0.0f, 1.0f)]
		public				 float				     Softness			= 0.5f;

		[Tooltip("NOTE: Only affects spot lights.")]
		[Range(0.0f, 1.0f)]
		public				 float				     Softness2			= 8;

		public				 bool					 CastShadow			= false;

		[HideInInspector]
		public               Light                   LightComponent;
    
		[HideInInspector]
		public               RenderTexture           ShadowMap; 
    

		/// <summary>
		/// Start this instance.
		/// </summary>
		void Start ()
		{
			if (Camera.main == null)
				Debug.LogError("Camera main is null, create camera and assign MainCamera tag to it.");
			if (Camera.main.GetComponent<VolumetricFog>() == null)
				Debug.LogError("VolumetricFog component is null, assign VolumetricFog script to the main camera.");

			LightComponent  = GetComponent<Light>();
			BufferDepth(); 
		}
		
		/// <summary>
		/// Create command buffer to sample a shadow map
		/// </summary>
		public void BufferDepth()
		{
			if (LightComponent.type == LightType.Directional)
			{
				RenderTargetIdentifier RTI = BuiltinRenderTextureType.CurrentActive;
				int shadowResolution = 256 * ((int)QualitySettings.shadowResolution + 1);
				ShadowMap = new RenderTexture (shadowResolution, shadowResolution, 24);
				ShadowMap.format = RenderTextureFormat.RInt;
				CommandBuffer cb = new CommandBuffer();
				cb.SetShadowSamplingMode(RTI, ShadowSamplingMode.RawDepth);             // Change shadow sampling mode for light's shadowmap.
				cb.Blit(RTI, new RenderTargetIdentifier(ShadowMap));                    // The shadowmap values can now be sampled normally - copy it to a different render texture.
				LightComponent.AddCommandBuffer(LightEvent.AfterShadowMap, cb);         // Execute after the shadowmap has been filled.
			}

			else if (LightComponent.type == LightType.Spot)
			{
				RenderTargetIdentifier RTI = BuiltinRenderTextureType.CurrentActive;
				int shadowResolution = 256 * ((int)LightComponent.shadowResolution + 1);
				ShadowMap = new RenderTexture (shadowResolution, shadowResolution, 24);
				ShadowMap.format = RenderTextureFormat.RHalf;
				CommandBuffer cb = new CommandBuffer();
				cb.SetShadowSamplingMode(RTI, ShadowSamplingMode.RawDepth);             // Change shadow sampling mode for light's shadowmap.
				cb.Blit(RTI, new RenderTargetIdentifier(ShadowMap));                    // The shadowmap values can now be sampled normally - copy it to a different render texture.
				LightComponent.AddCommandBuffer(LightEvent.AfterShadowMap, cb);         // Execute after the shadowmap has been filled.

				if (CastShadow && LightComponent.shadows == LightShadows.None)
					LightComponent.shadows = LightShadows.Hard;
			}
		}
	}
}