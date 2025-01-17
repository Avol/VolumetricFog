
#if UNITY_EDITOR


using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Avol
{
	[CustomEditor(typeof(VolumetricFog))]
	[CanEditMultipleObjects]
	[System.Serializable]
	public class FogEditor : Editor
	{
		public Texture2D _InfoIcon;
		public Texture2D _WarningIcon;
		public Texture2D _AllGoodIcon;

		private GUIStyle _InfoStyle;
		private GUIStyle _WarningStyle;
		private GUIStyle _AllGoodStyle;

		private GUIStyle _FoldoutStyle;
		private GUIStyle _FoldoutStyleInner;
		private GUIStyle _HeaderStyle;


		private AnimBool			_PrerequisitiesTab;
		private bool				_ShowPrerequisitiesTab			= false;

		private AnimBool			_GeneralTab;
		private bool				_ShowGeneralTab					= false;

		private AnimBool			_QualityTab;
		private bool				_ShowQualityTab					= false;

		private AnimBool			_FiltersTab;
		private bool				_ShowFiltersTab					= false;

		private AnimBool			_ScatteringTab;
		private bool				_ShowScatteringTab				= false;

		private AnimBool			_DistributionTab;
		private bool				_ShowDistributionTab			= false;

		private AnimBool			_CloudsTab;
		private bool				_ShowCloudsTab					= false;

		private AnimBool			_CloudsDistributionTab;
		private bool				_ShowCloudsDistributionTab		= false;


		private bool _Expanded;
		private float EditorYPos;

		private float _ScrollWidth;
		private float _Height;

		private void OnEnable()
		{
			_PrerequisitiesTab		= new AnimBool(_ShowPrerequisitiesTab);
			_PrerequisitiesTab.valueChanged.AddListener(Repaint);

			_GeneralTab				= new AnimBool(_ShowGeneralTab);
			_GeneralTab.valueChanged.AddListener(Repaint);

			_QualityTab				= new AnimBool(_ShowQualityTab);
			_QualityTab.valueChanged.AddListener(Repaint);

			_FiltersTab				= new AnimBool(_ShowFiltersTab);
			_FiltersTab.valueChanged.AddListener(Repaint);

			_ScatteringTab			= new AnimBool(_ShowScatteringTab);
			_ScatteringTab.valueChanged.AddListener(Repaint);

			_DistributionTab		= new AnimBool(_ShowDistributionTab);
			_DistributionTab.valueChanged.AddListener(Repaint);

			_CloudsTab				= new AnimBool(_ShowCloudsTab);
			_CloudsTab.valueChanged.AddListener(Repaint);

			_CloudsDistributionTab		= new AnimBool(_ShowCloudsDistributionTab);
			_CloudsDistributionTab.valueChanged.AddListener(Repaint);


			_InfoStyle = new GUIStyle();
			_InfoStyle.normal.background = _InfoIcon;

			_WarningStyle = new GUIStyle();
			_WarningStyle.normal.background = _WarningIcon;

			_AllGoodStyle = new GUIStyle();
			_AllGoodStyle.normal.background = _AllGoodIcon;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();


			VolumetricFog scriptObject = (VolumetricFog)target;


			_FoldoutStyle = new GUIStyle(EditorStyles.label);
			_FoldoutStyle.fontSize				= 15;
			_FoldoutStyle.fontStyle				= FontStyle.Normal;
			//_FoldoutStyle.margin				= new RectOffset(-15, 0, 15, 15);
			_FoldoutStyle.padding				= new RectOffset(5,5,5,5);
			_FoldoutStyle.normal.background		= Texture2D.whiteTexture;
			_FoldoutStyle.active.background		= Texture2D.whiteTexture;
			_FoldoutStyle.focused.background	= Texture2D.whiteTexture;
			_FoldoutStyle.hover.background		= Texture2D.whiteTexture;
			_FoldoutStyle.onActive.background	= Texture2D.whiteTexture;
			_FoldoutStyle.onNormal.background	= Texture2D.whiteTexture;
			_FoldoutStyle.onFocused.background	= Texture2D.whiteTexture;
			_FoldoutStyle.onHover.background	= Texture2D.whiteTexture;


	
			_HeaderStyle	= new GUIStyle();
			_HeaderStyle.fontStyle	= FontStyle.BoldAndItalic;
			_HeaderStyle.padding	= new RectOffset(0, 0, 15, 15);
			_HeaderStyle.alignment	= TextAnchor.MiddleLeft;
			_HeaderStyle.clipping	= TextClipping.Clip;




			float contextWidth	= (float)typeof(EditorGUIUtility).GetProperty("contextWidth", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
			float scrollwidth	= contextWidth - Screen.width;
			if (scrollwidth != 0)
				_ScrollWidth = scrollwidth;

		

			float elementWidth					= Screen.width - 15 + _ScrollWidth;
			GUILayoutOption[] elementLayout		= new GUILayoutOption[] { GUILayout.Width(elementWidth - 15) };



			GUILayout.Space(15);
			/// -------------------------------------------------- Quality Tab -------------------------------------------------- ////

			GUILayout.BeginVertical(elementLayout);
		
			_QualityTab.target = GUILayout.Toggle(_QualityTab.target, "■ Quality ", _FoldoutStyle, new GUILayoutOption[] { GUILayout.Width(elementWidth - 15), GUILayout.Height(30) });
			GUILayout.Space(5);
			if (EditorGUILayout.BeginFadeGroup(_QualityTab.faded))
			{
				scriptObject.VolumeResolution			= (VOLUME_RESOLUTION)EditorGUILayout.EnumPopup(new GUIContent("Volume Resolution", "Resolution of all the 3D volumes used to store fog scattering data. Note: this setting heavily affects performance & quality."), scriptObject.VolumeResolution);
				scriptObject.SuperSampling				= (SUPER_SAMPLING)EditorGUILayout.EnumPopup("Super Sampling ", scriptObject.SuperSampling);
				scriptObject.LogarithmicFilter			= (LOG_BLUR)EditorGUILayout.EnumPopup("Logarithmic Filter ", scriptObject.LogarithmicFilter);
				scriptObject.TemporalFilter				= (TEMPORAL_AA)EditorGUILayout.EnumPopup("Temporal Filter ", scriptObject.TemporalFilter);
				scriptObject.VolumeFilter				= (VOLUME_FILTERING)EditorGUILayout.EnumPopup("Spatial Box Filter ", scriptObject.VolumeFilter);

				scriptObject.DebugPerformance			= EditorGUILayout.Toggle("Debug", scriptObject.DebugPerformance);

				GUILayout.Space(5);
			}
			EditorGUILayout.EndFadeGroup();
			GUILayout.EndVertical();

			/// -------------------------------------------------- General Tab -------------------------------------------------- ////

			GUILayout.BeginVertical(elementLayout);
			_GeneralTab.target = GUILayout.Toggle(_GeneralTab.target, "■ General ", _FoldoutStyle, new GUILayoutOption[] { GUILayout.Width(elementWidth - 15), GUILayout.Height(30) });
			GUILayout.Space(5);
			if (EditorGUILayout.BeginFadeGroup(_GeneralTab.faded))
			{
				scriptObject.AnalyticalFog			= EditorGUILayout.Toggle(new GUIContent("Analytical Fog", "Anal fog"), scriptObject.AnalyticalFog);
				scriptObject.RenderDistance			= EditorGUILayout.Slider("Render Distance", scriptObject.RenderDistance, 0, 1);

				scriptObject.VolumetricFogClamp		= EditorGUILayout.Slider("Volumetric Fog Clamp", scriptObject.VolumetricFogClamp, 0, 1);
				scriptObject.AnalyticalFogClamp		= EditorGUILayout.Slider("Analytical Fog Clamp", scriptObject.AnalyticalFogClamp, 0, 1);

				scriptObject.VolumetricFogSkyClamp	= EditorGUILayout.Slider("Volumetric Fog Sky Clamp", scriptObject.VolumetricFogSkyClamp, 0, 1);
				scriptObject.AnalyticalFogSkyClamp	= EditorGUILayout.Slider("Analytical Fog Sky Clamp", scriptObject.AnalyticalFogSkyClamp, 0, 1);

				scriptObject.Shadow					= EditorGUILayout.Slider("Shadow", scriptObject.Shadow, 0, 2);
				scriptObject.ShadowBias				= EditorGUILayout.Slider("Shadow Bias", scriptObject.ShadowBias, -1, 1);

				GUILayout.Space(5);
			}

			EditorGUILayout.EndFadeGroup();
			GUILayout.EndVertical();

			/// -------------------------------------------------- Scattering Tab -------------------------------------------------- ////

			GUILayout.BeginVertical(elementLayout);
			_ScatteringTab.target = GUILayout.Toggle(_ScatteringTab.target, "■ Physical Properties ", _FoldoutStyle, new GUILayoutOption[] { GUILayout.Width(elementWidth-15), GUILayout.Height(30) });
			GUILayout.Space(5);
			if (EditorGUILayout.BeginFadeGroup(_ScatteringTab.faded))
			{
				scriptObject.Absorption				= EditorGUILayout.Slider("Absorption", scriptObject.Absorption, 0, 1);
				scriptObject.Scattering				= EditorGUILayout.Slider("Scattering", scriptObject.Scattering, 0, 1);

				scriptObject.AmbientColor			= EditorGUILayout.ColorField("Ambient Color", scriptObject.AmbientColor);
				scriptObject.AtmosphereColor		= EditorGUILayout.ColorField("Atmosphere Color", scriptObject.AtmosphereColor);
				scriptObject.ShadowColor			= EditorGUILayout.ColorField("Shadow Color", scriptObject.ShadowColor);

				scriptObject.AnisotropyAtmosphere	= EditorGUILayout.Slider("Anisotropy Atmosphere", scriptObject.AnisotropyAtmosphere, -1, 1);
				scriptObject.RadialLobe				= EditorGUILayout.Slider("Radial Lobe", scriptObject.RadialLobe, 0, 1);
				scriptObject.AnisotropySun			= EditorGUILayout.Slider("Anisotropy Sun", scriptObject.AnisotropySun, -1, 1);
				scriptObject.RadialBlend			= EditorGUILayout.Slider("Radial Blend", scriptObject.RadialBlend, -1, 1);

				GUILayout.Space(5);
			}
			
			EditorGUILayout.EndFadeGroup();
			GUILayout.EndVertical();

			/// -------------------------------------------------- Distribution Tab -------------------------------------------------- ////

			GUILayout.BeginVertical(elementLayout);
			_DistributionTab.target = GUILayout.Toggle(_DistributionTab.target, "■ Density Distribution ", _FoldoutStyle, new GUILayoutOption[] { GUILayout.Width(elementWidth-15), GUILayout.Height(30) });
			GUILayout.Space(5);
			if (EditorGUILayout.BeginFadeGroup(_DistributionTab.faded))
			{
				scriptObject.GlobalDensity			= EditorGUILayout.Slider("Global Density", scriptObject.GlobalDensity, 0, 100);
				scriptObject.DensityBottom			= EditorGUILayout.Slider("Density Bottom", scriptObject.DensityBottom, 0, 1);
				scriptObject.DensityTop				= EditorGUILayout.Slider("Density Top", scriptObject.DensityTop, 0, 1);


				scriptObject.HeightBottom			= EditorGUILayout.FloatField("Height Bottom", scriptObject.HeightBottom);
				scriptObject.HeightTop				= EditorGUILayout.FloatField("Height Top", scriptObject.HeightTop);

				//scriptObject.FrontDensity			= EditorGUILayout.FloatField("Height Bottom", scriptObject.HeightBottom);
				//scriptObject.BackDensity			= EditorGUILayout.FloatField("Height Top", scriptObject.HeightTop);

				scriptObject.RampInfluence			= EditorGUILayout.Slider("Ramp Influence", scriptObject.RampInfluence, 0, 1);
				scriptObject.RampStart				= EditorGUILayout.Slider("Ramp Start", scriptObject.RampStart, 0, 0.99f);
				scriptObject.RampEnd				= EditorGUILayout.Slider("Ramp End", scriptObject.RampEnd, 0, 0.99f);

				scriptObject.RampStartDensity		= EditorGUILayout.Slider("Ramp Start Density", scriptObject.RampStartDensity, 0, 1);
				scriptObject.RampEndDensity			= EditorGUILayout.Slider("Ramp End Density", scriptObject.RampEndDensity, 0, 1);

				GUILayout.Space(5);
			}

			EditorGUILayout.EndFadeGroup();
			GUILayout.EndVertical();

			
			/// -------------------------------------------------- Clouds Tab -------------------------------------------------- ////

			GUILayout.BeginVertical(elementLayout);
			_CloudsTab.target = GUILayout.Toggle(_CloudsTab.target, "■ Cloud Renderer ", _FoldoutStyle, new GUILayoutOption[] { GUILayout.Width(elementWidth-15), GUILayout.Height(30) });
			GUILayout.Space(5);
			if (EditorGUILayout.BeginFadeGroup(_CloudsTab.faded))
			{
				scriptObject.Clouds					= EditorGUILayout.Toggle("Clouds", scriptObject.Clouds);
				scriptObject.CloudsOcclusion		= EditorGUILayout.Toggle("Occlusion", scriptObject.CloudsOcclusion);

				scriptObject.OcclusionRayDistance	= EditorGUILayout.Slider("Occlusion Ray Distance", scriptObject.OcclusionRayDistance, 0, 1);

				scriptObject.OcclusionStrength		= EditorGUILayout.Slider("Occlusion Strength", scriptObject.OcclusionStrength, 0, 1);
				scriptObject.OutlineStrength		= EditorGUILayout.Slider("Occlusion Outline Strength", scriptObject.OutlineStrength, 0, 1);
				scriptObject.OutlineRadius			= EditorGUILayout.Slider("Occlusion Outline Radius", scriptObject.OutlineRadius, 0, 1);

				scriptObject.CloudDirectColor		= EditorGUILayout.ColorField("Cloud Color", scriptObject.CloudDirectColor);

				GUILayout.Space(5);
			}

			EditorGUILayout.EndFadeGroup();
			GUILayout.EndVertical();


			/// -------------------------------------------------- Clouds Distribution Tab -------------------------------------------------- ////


			GUILayout.BeginVertical(elementLayout);
			_CloudsDistributionTab.target = GUILayout.Toggle(_CloudsDistributionTab.target, "■ Cloud Distribution ", _FoldoutStyle, new GUILayoutOption[] { GUILayout.Width(elementWidth-15), GUILayout.Height(30) });
			GUILayout.Space(5);
			if (EditorGUILayout.BeginFadeGroup(_CloudsDistributionTab.faded))
			{
				scriptObject.CloudsSize				= EditorGUILayout.Slider("Cloud Size", scriptObject.CloudsSize, 0, 1);

				scriptObject.CloudsFrequency		= EditorGUILayout.Vector3Field("Frequency", scriptObject.CloudsFrequency);

				if (scriptObject.CloudsFrequency.x < 0) scriptObject.CloudsFrequency.x = 0;
				if (scriptObject.CloudsFrequency.y < 0) scriptObject.CloudsFrequency.y = 0;
				if (scriptObject.CloudsFrequency.z < 0) scriptObject.CloudsFrequency.z = 0;

				if (scriptObject.CloudsFrequency.x > 1) scriptObject.CloudsFrequency.x = 1;
				if (scriptObject.CloudsFrequency.y > 1) scriptObject.CloudsFrequency.y = 1;
				if (scriptObject.CloudsFrequency.z > 1) scriptObject.CloudsFrequency.z = 1;


				scriptObject.CloudsDensityScale			= EditorGUILayout.Slider("Density Scale", scriptObject.CloudsDensityScale, 0, 1);
				
				scriptObject.EnableCloudSpacing			= EditorGUILayout.Toggle("Cloud Spacing", scriptObject.EnableCloudSpacing);
				scriptObject.CloudsSpacing				= EditorGUILayout.Slider("Clouds Spacing Size", scriptObject.CloudsSpacing, 0, 1);
				scriptObject.CloudsSpacingFrequency		= EditorGUILayout.Vector3Field("Clouds Spacing Frequency", scriptObject.CloudsSpacingFrequency);

				if (scriptObject.CloudsSpacingFrequency.x < 0) scriptObject.CloudsSpacingFrequency.x = 0;
				if (scriptObject.CloudsSpacingFrequency.y < 0) scriptObject.CloudsSpacingFrequency.y = 0;
				if (scriptObject.CloudsSpacingFrequency.z < 0) scriptObject.CloudsSpacingFrequency.z = 0;

				if (scriptObject.CloudsSpacingFrequency.x > 1) scriptObject.CloudsSpacingFrequency.x = 1;
				if (scriptObject.CloudsSpacingFrequency.y > 1) scriptObject.CloudsSpacingFrequency.y = 1;
				if (scriptObject.CloudsSpacingFrequency.z > 1) scriptObject.CloudsSpacingFrequency.z = 1;


				scriptObject.CloudsHeightBottom			= EditorGUILayout.FloatField("Height Bottom", scriptObject.CloudsHeightBottom);
				scriptObject.CloudsHeightTop			= EditorGUILayout.FloatField("Height Top", scriptObject.CloudsHeightTop);
				scriptObject.CloudsInfluenceBottom		= EditorGUILayout.Slider("Influence Bottom", scriptObject.CloudsInfluenceBottom, 0, 1);
				scriptObject.CloudsInfluenceTop			= EditorGUILayout.Slider("Influence Top", scriptObject.CloudsInfluenceTop, 0, 1);

				scriptObject.CloudsFadeBottomHeight		= EditorGUILayout.FloatField("Fade Bottom Height", scriptObject.CloudsFadeBottomHeight);
				scriptObject.CloudsFadeTopHeight		= EditorGUILayout.FloatField("Fade Top Height", scriptObject.CloudsFadeTopHeight);

				scriptObject.WindVelocity				= EditorGUILayout.Vector3Field("Wind Velocity", scriptObject.WindVelocity);

				scriptObject.DepthAttenuation			= EditorGUILayout.Slider("Depth Attenuation", scriptObject.DepthAttenuation, 0, 1);
			}

			EditorGUILayout.EndFadeGroup();
			GUILayout.EndVertical();

			GUILayout.Space(10);



			GUIStyle footerStyle = new GUIStyle();
			footerStyle.padding = new RectOffset(15,15,15,15);
			footerStyle.normal.background = Texture2D.whiteTexture;

			EditorGUILayout.BeginHorizontal(footerStyle, GUILayout.Width(elementWidth - 15));

				if (GUILayout.Button("Readme", new GUILayoutOption[] { GUILayout.Width(120) }))
				{
					Application.OpenURL("http://www.avol.lt/");
				}

				EditorGUILayout.LabelField("0.98 version. Contact E-mail: avolaso@gmail.com");

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);




			serializedObject.ApplyModifiedProperties();
		}

		public void OnInspectorUpdate()
		{
			this.Repaint();
		}
	}
}

#endif