using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Avol
{
	public struct POINT_LIGHT_DATA
	{
		public Vector3 Position;
		public float Intensity;
		public Vector3 Color;
		public float Range;
	};

	public struct SPOT_LIGHT_DATA
	{
		public Vector3 Position;
		public float Intensity;
		public Vector3 Color;
		public float Range;
		public Vector3 Direction;
		public float Angle;
		public Vector2 Attenuation;
	};

	public struct BOX_VOLUME_DATA
	{
		public Vector3 Position;
		public Vector3 Dimensions;
		public Color Color;
		public int Mode;
		public float Absorption;
		public float SoftEdges;
	};

	public struct ELLIPSOID_VOLUME_DATA
	{
		public Vector3 Position;
		public Vector3 Dimensions;
		public Color Color;
		public int Mode;
		public float Absorption;
		public float SoftEdges;
	}

	public struct DEPTH_STEPS
	{
		public float Step;
	};

	public enum VOLUME_SHAPE
	{
		Box,
		Ellipsoid
	};

	public enum VOLUME_TYPE
	{
		Opaque,
		CutEverything,
		CutClouds,
		CutDensity,
		FillWithClouds
	};
}