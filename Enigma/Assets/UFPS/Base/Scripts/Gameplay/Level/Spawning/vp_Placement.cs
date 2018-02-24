/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Placement.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	represents a transform position and rotation for spawning
//					purposes, and implements static methods for intelligent
//					position adjustment in relation to ground and physics objects
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;


public class vp_Placement
{

	public Vector3 Position = Vector3.zero;
	public Quaternion Rotation = Quaternion.identity;
	

	/// <summary>
	/// performs a sphere check on a placement to look for objects
	/// matching 'vp_Layer.Mask.PhysicsBlockers'. upon finding any,
	/// re-executes itself for up to 'attempts' iterations
	/// </summary>
	public static bool AdjustPosition(vp_Placement p, float physicsRadius, int attempts = 1000)
	{

		attempts--;

		if (attempts > 0)
		{
			// TIP: this can be expanded upon to check for alternative object layers
			if (p.IsObstructed(physicsRadius))
			{

				// adjust the position with a random horizontal distance of up to 1 meter
				Vector3 newPos = Random.insideUnitSphere;
				p.Position.x += newPos.x;
				p.Position.z += newPos.z;
				AdjustPosition(p, physicsRadius, attempts);

			}
		}
		else
		{
			// ran out of attempts! set position to world origin
			Debug.LogWarning("(vp_Placement.AdjustPosition) Failed to find valid placement.");
			return false;
		}

		return true;

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual bool IsObstructed(float physicsRadius = 1.0f)
	{

		if (Physics.CheckSphere(Position, physicsRadius, vp_Layer.Mask.PhysicsBlockers))
			return true;

		return false;

	}


	/// <summary>
	/// tries to snap a placement to the ground (meaning any object above
	/// or below it with an upwards-facing collider). a 'snapDistance' of
	/// '10' means that a unit spawning up to 10 meters above or below a
	/// collider will snap on top of it. NOTE: you may want to reduce the
	/// snap distance inside rooms with floors above it, and increase it
	/// in terrain with steep hills, respectively
	/// </summary>
	public static void SnapToGround(vp_Placement p, float radius, float snapDistance)
	{

		if (snapDistance == 0.0f)
			return;

		RaycastHit hitInfo;
		Physics.SphereCast(new Ray(p.Position + (Vector3.up * snapDistance), Vector3.down),
			radius, out hitInfo, snapDistance * 2.0f, vp_Layer.Mask.ExternalBlockers);
		if (hitInfo.collider != null)
			p.Position.y = hitInfo.point.y + 0.05f;	// spawn 5 centimeters above the surface just to be sure

	}

}