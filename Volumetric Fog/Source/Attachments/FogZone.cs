using UnityEngine;

namespace Avol
{

#if UNITY_EDITOR
	using UnityEditor;

	[CustomEditor(typeof(FogZone))]
	public class DrawBezierHandleEditor : Editor
	{
		void OnSceneGUI()
		{
			FogZone fogVolume = (FogZone)target;

			if (fogVolume.VolumeShape == VOLUME_SHAPE.Ellipsoid)
			{
				Vector3 drawPos = fogVolume.transform.position;

				float size = 1.0f;

				Handles.DrawBezier(new Vector3(drawPos.x, drawPos.y + fogVolume.Dimensions.y, drawPos.z), new Vector3(drawPos.x, drawPos.y, drawPos.z + fogVolume.Dimensions.z), fogVolume.transform.TransformPoint(new Vector3(0, fogVolume.Dimensions.y, fogVolume.Dimensions.z / 2)), fogVolume.transform.TransformPoint(new Vector3(0, fogVolume.Dimensions.y / 2, fogVolume.Dimensions.z)), fogVolume.Emission, Texture2D.whiteTexture, size);
				Handles.DrawBezier(new Vector3(drawPos.x, drawPos.y + fogVolume.Dimensions.y, drawPos.z), new Vector3(drawPos.x, drawPos.y, drawPos.z - fogVolume.Dimensions.z), fogVolume.transform.TransformPoint(new Vector3(0, fogVolume.Dimensions.y, -fogVolume.Dimensions.z / 2)), fogVolume.transform.TransformPoint(new Vector3(0, fogVolume.Dimensions.y / 2, -fogVolume.Dimensions.z)), fogVolume.Emission, Texture2D.whiteTexture, size);
				Handles.DrawBezier(new Vector3(drawPos.x, drawPos.y - fogVolume.Dimensions.y, drawPos.z), new Vector3(drawPos.x, drawPos.y, drawPos.z + fogVolume.Dimensions.z), fogVolume.transform.TransformPoint(new Vector3(0, -fogVolume.Dimensions.y, fogVolume.Dimensions.z / 2)), fogVolume.transform.TransformPoint(new Vector3(0, -fogVolume.Dimensions.y / 2, fogVolume.Dimensions.z)), fogVolume.Emission, Texture2D.whiteTexture, size);
				Handles.DrawBezier(new Vector3(drawPos.x, drawPos.y - fogVolume.Dimensions.y, drawPos.z), new Vector3(drawPos.x, drawPos.y, drawPos.z - fogVolume.Dimensions.z), fogVolume.transform.TransformPoint(new Vector3(0, -fogVolume.Dimensions.y, -fogVolume.Dimensions.z / 2)), fogVolume.transform.TransformPoint(new Vector3(0, -fogVolume.Dimensions.y / 2, -fogVolume.Dimensions.z)), fogVolume.Emission, Texture2D.whiteTexture, size);
				//X-Y Ring
				Handles.DrawBezier(new Vector3(drawPos.x + fogVolume.Dimensions.x, drawPos.y, drawPos.z), new Vector3(drawPos.x, drawPos.y + fogVolume.Dimensions.y, drawPos.z), fogVolume.transform.TransformPoint(new Vector3((fogVolume.Dimensions.x), (fogVolume.Dimensions.y / 2), 0)), fogVolume.transform.TransformPoint(new Vector3((fogVolume.Dimensions.x / 2), (fogVolume.Dimensions.y), 0)), fogVolume.Emission, Texture2D.whiteTexture, size);
				Handles.DrawBezier(new Vector3(drawPos.x - fogVolume.Dimensions.x, drawPos.y, drawPos.z), new Vector3(drawPos.x, drawPos.y + fogVolume.Dimensions.y, drawPos.z), fogVolume.transform.TransformPoint(new Vector3(-(fogVolume.Dimensions.x), (fogVolume.Dimensions.y / 2), 0)), fogVolume.transform.TransformPoint(new Vector3(-(fogVolume.Dimensions.x / 2), (fogVolume.Dimensions.y), 0)), fogVolume.Emission, Texture2D.whiteTexture, size);
				Handles.DrawBezier(new Vector3(drawPos.x + fogVolume.Dimensions.x, drawPos.y, drawPos.z), new Vector3(drawPos.x, drawPos.y - fogVolume.Dimensions.y, drawPos.z), fogVolume.transform.TransformPoint(new Vector3((fogVolume.Dimensions.x), -(fogVolume.Dimensions.y / 2), 0)), fogVolume.transform.TransformPoint(new Vector3((fogVolume.Dimensions.x / 2), -(fogVolume.Dimensions.y), 0)), fogVolume.Emission, Texture2D.whiteTexture, size);
				Handles.DrawBezier(new Vector3(drawPos.x - fogVolume.Dimensions.x, drawPos.y, drawPos.z), new Vector3(drawPos.x, drawPos.y - fogVolume.Dimensions.y, drawPos.z), fogVolume.transform.TransformPoint(new Vector3(-(fogVolume.Dimensions.x), -(fogVolume.Dimensions.y / 2), 0)), fogVolume.transform.TransformPoint(new Vector3(-(fogVolume.Dimensions.x / 2), -(fogVolume.Dimensions.y), 0)), fogVolume.Emission, Texture2D.whiteTexture, size);
				//X-Z Ring
				Handles.DrawBezier(new Vector3(drawPos.x + fogVolume.Dimensions.x, drawPos.y, drawPos.z), new Vector3(drawPos.x, drawPos.y, drawPos.z + fogVolume.Dimensions.z), fogVolume.transform.TransformPoint(new Vector3((fogVolume.Dimensions.x), 0, (fogVolume.Dimensions.z / 2))), fogVolume.transform.TransformPoint(new Vector3((fogVolume.Dimensions.x / 2), 0, (fogVolume.Dimensions.z))), fogVolume.Emission, Texture2D.whiteTexture, size);
				Handles.DrawBezier(new Vector3(drawPos.x - fogVolume.Dimensions.x, drawPos.y, drawPos.z), new Vector3(drawPos.x, drawPos.y, drawPos.z + fogVolume.Dimensions.z), fogVolume.transform.TransformPoint(new Vector3(-(fogVolume.Dimensions.x), 0, (fogVolume.Dimensions.z / 2))), fogVolume.transform.TransformPoint(new Vector3(-(fogVolume.Dimensions.x / 2), 0, (fogVolume.Dimensions.z))), fogVolume.Emission, Texture2D.whiteTexture, size);
				Handles.DrawBezier(new Vector3(drawPos.x + fogVolume.Dimensions.x, drawPos.y, drawPos.z), new Vector3(drawPos.x, drawPos.y, drawPos.z - fogVolume.Dimensions.z), fogVolume.transform.TransformPoint(new Vector3((fogVolume.Dimensions.x), 0, -(fogVolume.Dimensions.z / 2))), fogVolume.transform.TransformPoint(new Vector3((fogVolume.Dimensions.x / 2), 0, -(fogVolume.Dimensions.z))), fogVolume.Emission, Texture2D.whiteTexture, size);
				Handles.DrawBezier(new Vector3(drawPos.x - fogVolume.Dimensions.x, drawPos.y, drawPos.z), new Vector3(drawPos.x, drawPos.y, drawPos.z - fogVolume.Dimensions.z), fogVolume.transform.TransformPoint(new Vector3(-(fogVolume.Dimensions.x), 0, -(fogVolume.Dimensions.z / 2))), fogVolume.transform.TransformPoint(new Vector3(-(fogVolume.Dimensions.x / 2), 0, -(fogVolume.Dimensions.z))), fogVolume.Emission, Texture2D.whiteTexture, size);
			}
		}
	}

#endif

	public class FogZone : MonoBehaviour
	{
		public VOLUME_TYPE		VolumeType			= VOLUME_TYPE.Opaque;
		public VOLUME_SHAPE		VolumeShape			= VOLUME_SHAPE.Box;
		public Vector3			Dimensions			= Vector3.one;
		public Color			Emission			= Color.red;

		[Range(0.0f, 1.0f)]
		public float			Absorption			= 0;

		[Range(0.0f, 1.0f)]
		public float			SoftEdges			= 1;

		void OnDrawGizmosSelected()
		{
			Gizmos.color = Emission;
			if (VolumeShape == VOLUME_SHAPE.Box)
				Gizmos.DrawWireCube(transform.position, Dimensions);
		}
	}
}