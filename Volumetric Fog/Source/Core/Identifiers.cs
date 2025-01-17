using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Avol
{
	public enum LOG_BLUR
	{
		_Off = 0,
		_Weak = 1,
		_Medium = 2,
		_Strong = 3
	}

	public enum SUPER_SAMPLING
	{
		Off = 1,
		_2X = 2,
		_4X = 4
	};

	public enum SPATIAL_FILTERING
	{
		Disabled = 0,
		Weak = 1,
		Avarage = 2,
		Strong = 3
	}

	public enum VOLUME_FILTERING
	{
		Disabled = 0,
		Enabled = 1
	}

	public enum VOLUME_ZONE_MODE
	{
		Disabled = 0,
		CutOut = 1,
		Fill = 2,
		Additive = 3
	}

	public enum NOISE_TYPE
	{
		HashBased = 0/*,
			Simplex     = 1*/
	}

	public enum CLOUDS_FILL_MODE
	{
		WholeScene = 0,
		OnlyVolumes = 1,
		SceneAndVolumes = 2
	}

	public enum TEMPORAL_AA
	{
		Weak = 0,
		Avarage = 1,
		Strong = 2,
		Extreme = 3
	}

	public enum DENSITY_MODE
	{
		Linear = 0,
		Exponential = 1,
		ExponentialSquared = 2
	}

	public enum VOLUME_RESOLUTION
	{
		_96x56x64Cheap = 0,
		_112x64x64Cheap = 1,
		_128x72x64Cheap = 2,
		_128x88x64Cheap = 3,

		_160x96x64Optimal = 4,
		_192x112x64Optimal = 5,
		_160x96x72Optimal = 6,
		_160x96x92Optimal = 7,

		_160x96x128High = 8,
		_192x112x128High = 9,
		_256x144x64High = 10,
		_256x144x96High = 11,

		_320x184x64Cinematic = 12,
		_320x184x92Cinematic = 13,
		_256x144x128Cinematic = 14,
		_320x184x128Cinematic = 15,
	}

	public enum VOLUME_COMPUTATION_RESOLUTION
	{
		Disabled = 0,
	}
}
