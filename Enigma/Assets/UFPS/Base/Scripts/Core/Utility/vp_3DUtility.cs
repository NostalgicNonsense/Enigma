/////////////////////////////////////////////////////////////////////////////////
//
//	vp_3DUtility.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	miscellaneous 3D utility functions
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;

public static class vp_3DUtility
{
	

	/// <summary>
	/// Zeroes the y property of a Vector3, for some cases where you want to
	/// make 2D physics calculations.
	/// </summary>
	public static Vector3 HorizontalVector(Vector3 value)
	{

		value.y = 0.0f;
		return value;

	}


	/// <summary>
	/// gets a random bearing in the cardinal directions
	/// </summary>
	public static Vector3 RandomHorizontalDirection()
	{
		return (UnityEngine.Random.rotation * Vector3.up).normalized;
	}


	/// <summary>
	/// Determines whether the object of a certain renderer is visible to a
	/// certain camera and retrieves its screen position.
	/// </summary>
	public static bool OnScreen(Camera camera, Renderer renderer, Vector3 worldPosition, out Vector3 screenPosition)
	{

		screenPosition = Vector2.zero;

		if ((camera == null) || (renderer == null) || !renderer.isVisible)
			return false;

		// calculate the screen space position of the remote object
		screenPosition = camera.WorldToScreenPoint(worldPosition);

		// return false if screen position is behind camera
		if (screenPosition.z < 0.0f)
			return false;

		return true;

	}


	/// <summary>
	/// Performs a linecast from a world position to a target transform,
	/// returning true if the first hit collider is owned by that
	/// transform.
	/// </summary>
	public static bool InLineOfSight(Vector3 from, Transform target, Vector3 targetOffset, int layerMask)
	{

		RaycastHit hitInfo;
		Physics.Linecast(from, target.position + targetOffset, out hitInfo, layerMask);

		if (hitInfo.collider == null || hitInfo.collider.transform.root == target)
			return true;

		return false;

	}
	
	
	/// <summary>
	/// Determines whether the distance between two points is within
	/// a determined range and retrieves the distance.
	/// </summary>
	public static bool WithinRange(Vector3 from, Vector3 to, float range, out float distance)
	{

		distance = Vector3.Distance(from, to);

		if (distance > range)
			return false;

		return true;

	}
	

	/// <summary>
	/// returns the distance between a ray and a point
	/// </summary>
	public static float DistanceToRay(Ray ray, Vector3 point)
	{
		return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
	}


	/// <summary>
	/// returns the angle between a look vector and a target position.
	/// can be used for various aiming logic
	/// </summary>
	public static float LookAtAngle(Vector3 fromPosition, Vector3 fromForward, Vector3 toPosition)
	{

		return (Vector3.Cross(fromForward, (toPosition - fromPosition).normalized).y < 0.0f) ?
				-Vector3.Angle(fromForward, (toPosition - fromPosition).normalized) :
				Vector3.Angle(fromForward, (toPosition - fromPosition).normalized);

	}


	/// <summary>
	/// returns the angle between a look vector and a target position as
	/// seen top-down in the cardinal directions. useful for gui pointers
	/// </summary>
	public static float LookAtAngleHorizontal(Vector3 fromPosition, Vector3 fromForward, Vector3 toPosition)
	{

		return LookAtAngle(
			HorizontalVector(fromPosition),
			HorizontalVector(fromForward),
			HorizontalVector(toPosition));

	}


	/// <summary>
	/// returns the angle between dirA and dirB around axis.
	/// </summary>
	public static float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
	{

		// CREDIT: algorithm from the Unity 'HeadLookController' example
		// project by Rune Skovbo Johansen

		// project A and B onto the plane orthogonal target axis
		dirA = dirA - Vector3.Project(dirA, axis);
		dirB = dirB - Vector3.Project(dirB, axis);

		// find (positive) angle between A and B
		float angle = Vector3.Angle(dirA, dirB);

		// return angle multiplied with 1 or -1
		return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);

	}


	/// <summary>
	/// calculates a correct forward direction for a character bone
	/// in world space, regardless of that bone's inherent 3d space.
	/// 'initialWorldSpaceDifference' can be added to adjust the
	/// rotation in relation to an additional object
	/// </summary>
	public static Quaternion GetBoneLookRotationInWorldSpace(
		Quaternion originalRotation,
		Quaternion parentRotation,
		Vector3 worldLookDir,
		float amount,
		Vector3 referenceLookDir,
		Vector3 referenceUpDir,
		Quaternion relativeWorldSpaceDifference
		)
	{

		// CREDIT: code adapted from the Unity 'HeadLookController'
		// example by Rune Skovbo Johansen

		// INFO: when working with arbitrary character models, the 3d space of
		// their bones will often be completely incompatible with scene world
		// space. attempting to merely flip or exchange vectors will usually
		// do little (or nothing) to get an arm or a spine to point in the right
		// direction. this method does the 3d math needed for getting a bone to
		// point correctly in the desired world space direction

		// desired look direction in neck parent space
		Vector3 lookDirGoal = (Quaternion.Inverse(parentRotation) * worldLookDir.normalized);

		// get the horizontal and vertical rotation angle to look at the target
		lookDirGoal = Quaternion.AngleAxis(AngleAroundAxis(referenceLookDir, lookDirGoal, referenceUpDir), referenceUpDir)
			* Quaternion.AngleAxis(AngleAroundAxis(
			lookDirGoal - Vector3.Project(lookDirGoal, referenceUpDir)
			, lookDirGoal
			, Vector3.Cross(referenceUpDir, lookDirGoal))
			, Vector3.Cross(referenceUpDir, referenceLookDir))
			* referenceLookDir;

		// make look and up perpendicular
		Vector3 upDirGoal = referenceUpDir;
		Vector3.OrthoNormalize(ref lookDirGoal, ref upDirGoal);

		// look and up directions in neck parent space
		Vector3 lookDir = lookDirGoal;
		Vector3 segmentDirUp = upDirGoal;
		Vector3.OrthoNormalize(ref lookDir, ref segmentDirUp);

		// calculate final result divided by amount
		Quaternion dividedRotation = Quaternion.Lerp(
			Quaternion.identity,
			(parentRotation * Quaternion.LookRotation(lookDir, segmentDirUp))
			* Quaternion.Inverse(parentRotation * Quaternion.LookRotation(referenceLookDir, referenceUpDir))
			, amount
			);

		// return look rotation in world space
		return (dividedRotation * originalRotation * relativeWorldSpaceDifference);

	}


	/// <summary>
	/// 
	/// </summary>
	public static GameObject DebugPrimitive(PrimitiveType primitiveType, Vector3 scale, Color color, Vector3 pivotOffset, Transform parent = null)
	{

		GameObject pivot = null;

		Material mat = new Material(Shader.Find("Standard"));
		vp_MaterialUtility.MakeMaterialTransparent(mat);
		mat.color = color;

		GameObject prim = GameObject.CreatePrimitive(primitiveType);
		prim.GetComponent<Collider>().enabled = false;
		prim.GetComponent<Renderer>().material = mat;
		prim.transform.localScale = scale;
		prim.name = "Debug" + prim.name;

		if (pivotOffset != Vector3.zero)
		{
			pivot = new GameObject(prim.name);
			prim.name = prim.name.Replace("Debug", "");
			prim.transform.parent = pivot.transform;
			prim.transform.localPosition = pivotOffset;
		}

		if (parent != null)
		{
			if (pivot == null)
			{
				prim.transform.parent = parent;
				prim.transform.localPosition = Vector3.zero;
			}
			else
			{
				pivot.transform.parent = parent;
				pivot.transform.localPosition = Vector3.zero;
			}
		}

		return ((pivot != null) ? pivot : prim);

	}
	

	/// <summary>
	/// 
	/// </summary>
	public static GameObject DebugPointer(Transform parent = null)
	{
		return vp_3DUtility.DebugPrimitive(PrimitiveType.Sphere, new Vector3(0.01f, 0.01f, 3), new Color(1, 1, 0, 0.75f), Vector3.forward, parent);
	}


	/// <summary>
	/// 
	/// </summary>
	public static GameObject DebugBall(Transform parent = null)
	{
		return vp_3DUtility.DebugPrimitive(PrimitiveType.Sphere, Vector3.one * 0.25f, new Color(1, 0, 0, 0.5f), Vector3.zero, parent);
	}


	/// <summary>
	/// sets rendering mode of 'material' to transparent
	/// </summary>
	[Obsolete("Please use 'vp_MaterialUtility.MakeMaterialTransparent' instead.")]
	public static void MakeMaterialTransparent(Material material)
	{

		material.SetFloat("_Mode", 2);
		material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		material.SetInt("_ZWrite", 0);
		material.DisableKeyword("_ALPHATEST_ON");
		material.EnableKeyword("_ALPHABLEND_ON");
		material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		material.renderQueue = 3000;

	}


}

