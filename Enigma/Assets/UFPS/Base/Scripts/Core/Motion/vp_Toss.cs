/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Toss.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script can be used to toss an object along an arc and have
//					it bounce and come to rest at a specific point on the ground,
//					regardless of whether it has a rigidbody or not. this is useful
//					in multiplayer where deterministic target positions is highly
//					desireable.
//
//					USAGE:
//						1. add this component to any object
//						2. run one of the 'Toss' methods to initate the toss
//						3. the component will disable itself when the toss is complete
//
//					NOTES:
//					- vp_Bob and vp_Spin will interfere with vp_Toss, so these
//						scripts are disabled in OnEnable and smoothly re-enabled
//						in OnDisable
//					- if there is a vp_RigidBodyFX component on the object, it
//						will attempt to play effects when the procedurally animated
//						bounce happens (regardless of whether there's a rigidbody)
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_Toss : MonoBehaviour
{

	public float VerticalVelocity = 0.05f;
	public float Gravity = 0.0055f;

	protected Vector3 m_TargetPosition;
	protected Quaternion m_TargetRotation;
	protected float m_Duration;
	protected float m_StartTime;
	protected Vector3 m_Direction;
	protected float m_Distance;
	protected float m_YPos = 0.0f;

	// state
	protected bool m_ReenableBobWhenDone = true;
	protected bool m_ReenableSpinWhenDone = true;
	protected bool m_BeingTossed = false;

	private const float BOB_GROUND_OFFSET = 0.5f;


	protected Transform m_Transform;
	protected Transform Transform
	{
		get
		{
			if (m_Transform == null)
				m_Transform = transform;
			return m_Transform;
		}
	}

	protected vp_RigidbodyFX m_RigidbodyFX;
	protected vp_RigidbodyFX RigidbodyFX
	{
		get
		{
			if (m_RigidbodyFX == null)
				m_RigidbodyFX = Transform.GetComponentInChildren<vp_RigidbodyFX>();
			return m_RigidbodyFX;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		// temporarily disable bob and spin components because they will
		// interfere with the toss

		vp_Bob bob = GetComponentInChildren<vp_Bob>();
		if (bob != null)
			bob.enabled = false;

		vp_Spin spin = GetComponentInChildren<vp_Spin>();
		if (spin != null)
			spin.enabled = false;

		Gravity = 0.0055f * Random.Range(0.5f, 1.0f);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		// reenable bob and spin components gradually to prevent popping motions

		if (m_ReenableBobWhenDone)
		{
			vp_Bob bob = GetComponentInChildren<vp_Bob>();
			if (bob != null)
			{
				// re-enable bob in a way that the object floats smoothly
				// off the ground and starts bobbing
				bob.SmoothGroundOffset = true;
				bob.GroundOffset = BOB_GROUND_OFFSET;
				bob.InitialPosition = Transform.position;
				bob.enabled = true;
			}
		}

		if (m_ReenableSpinWhenDone)
		{
			vp_Spin spin = GetComponentInChildren<vp_Spin>();
			if (spin != null)
			{
				// re-enable spin in a way that the object smoothly starts spinning
				spin.Accelerate = true;
				spin.enabled = true;
			}
		}

		m_BeingTossed = false;

	}


	/// <summary>
	/// lobs object from a position in a direction over a preset distance,
	/// stopping on collision. returns the resulting position
	/// </summary>
	public virtual Vector3 Toss(Vector3 startPos, float targetYaw, Vector3 direction, float distance, bool reenableBobWhenDone = false, bool reenableSpinWhenDone = false)
	{

		m_Direction = direction;
		m_Distance = distance;
		m_TargetPosition = GetTargetPosition();

		TossInternal(startPos, targetYaw, direction, reenableBobWhenDone, reenableSpinWhenDone);

		return m_TargetPosition;

	}


	/// <summary>
	/// lobs object from a position to a predefined destination with no collision
	/// </summary>
	public virtual void Toss(Vector3 startPos, Vector3 targetPos, float targetYaw, bool reenableBobWhenDone = false, bool reenableSpinWhenDone = false)
	{


		Vector3 direction = (targetPos - startPos).normalized;

		m_TargetPosition = targetPos;

		TossInternal(startPos, targetYaw, direction, reenableBobWhenDone, reenableSpinWhenDone);

	}


	/// <summary>
	/// sets up a range of variables in a way that's common between the two
	/// 'Toss' methods, and starts the toss
	/// </summary>
	protected virtual void TossInternal(Vector3 startPos, float targetYaw, Vector3 direction, bool reenableBobWhenDone, bool reenableSpinWhenDone)
	{

		Transform.position = startPos;											// move to start point
		Transform.LookAt(startPos + direction);									// point in drop direction
		Transform.eulerAngles = new Vector3(0, Transform.eulerAngles.y, 0);
		m_YPos = Transform.position.y;											// store y position for applying gravity separately
		m_TargetRotation = Quaternion.Euler(Vector3.up * targetYaw);			// prepare a half-circle turn
		m_Duration = Vector3.Distance(Transform.position, m_TargetPosition);    // 1m / sec
		m_StartTime = Time.time;												// store start time for interpolations
		m_ReenableBobWhenDone = reenableBobWhenDone;
		m_ReenableSpinWhenDone = reenableSpinWhenDone;

		enabled = true;

		m_BeingTossed = true;

	}


	/// <summary>
	/// moves the object to its position and applies simple gravity and bounce.
	/// disables the vp_Toss component on arrival
	/// </summary>
	protected virtual void FixedUpdate()
	{

		if (!m_BeingTossed)
			return;

		// horizontal motion
			Transform.position =
			vp_MathUtility.NaNSafeVector3(Vector3.Lerp(
			Transform.position,
			m_TargetPosition,
			Mathf.Lerp(0, 1, (Time.time - m_StartTime) / m_Duration)
			));

		// rotation
		Transform.rotation =
			vp_MathUtility.NaNSafeQuaternion(
			Quaternion.Lerp(Transform.rotation, m_TargetRotation, (Time.time - m_StartTime) / (m_Duration * 3.0f)), Transform.rotation);

		// gravity
		m_YPos += VerticalVelocity;
		VerticalVelocity -= Gravity;

		// bounce
		if (m_YPos <= m_TargetPosition.y)
		{
			m_YPos = m_TargetPosition.y;
			VerticalVelocity = -VerticalVelocity * 0.5f;
			TryPlayBounceFX();
		}

		// vertical motion
		Transform.position = new Vector3(Transform.position.x, m_YPos, Transform.position.z);

		// arrival
		if (Transform.position == m_TargetPosition)
			this.enabled = false;
		
	}


	/// <summary>
	/// plays surface system effects (sounds, particles) upon bouncing.
	/// NOTE: requires a vp_RigidbodyFX component, although not a Rigidbody
	/// </summary>
	protected virtual bool TryPlayBounceFX()
	{

		if (RigidbodyFX == null)
			return false;

		// raycast to get a raycasthit, which is required for the SurfaceManager
			RaycastHit hit;
		if (!Physics.Raycast(
			new Ray(Transform.position + Vector3.up, Vector3.down),    // raycast to the collision point from a 2dm (default) distance
			out hit,
			2.0f,
			vp_Layer.Mask.BulletBlockers))
			return false;

		return RigidbodyFX.TryPlayFX(hit);

	}


	/// <summary>
	/// decides on a good, clear target position to toss the object to
	/// </summary>
	protected virtual Vector3 GetTargetPosition()
	{
		Vector3 floorPoint = GetPositionBelow(Transform.position);
		
		RaycastHit hit;
		if (Physics.SphereCast(floorPoint + (Vector3.up * 0.15f), 0.1f, m_Direction, out hit, m_Distance, vp_Layer.Mask.BulletBlockers))
		{
			// hit a wall - get position directly below
			return GetPositionBelow(Vector3.Lerp(floorPoint, hit.point, 0.5f));
		}
		
		// hit the ground
		return GetPositionBelow(Transform.position + (m_Direction.normalized * m_Distance));// + new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 2.5f));

	}


	/// <summary>
	/// gets a position on the ground near 'position', clear of other solid
	/// objects, platforms, pickups and remote players
	/// </summary>
	protected virtual Vector3 GetPositionBelow(Vector3 position)
	{
		RaycastHit hit;
		if (Physics.Raycast(position, Vector3.down, out hit, 300, vp_Layer.Mask.ExternalBlockers))
		{
			// make items fall away from platforms, pickups and remote players
				// NOTE: there are several challenges when childing objects to platforms, such as
				// non-uniform stretching, intersection with moving platforms, issues
				// with multiplayer position sync etc. so let's not go there
			if ((hit.collider.gameObject.layer == vp_Layer.MovingPlatform)
				|| (hit.collider.gameObject.layer == vp_Layer.Pickup)
				|| (hit.collider.gameObject.layer == vp_Layer.RemotePlayer)
				)
			{
				position += transform.forward.normalized;	// step until we're no longer over the platform
				return GetPositionBelow(position);
			}

			return hit.point + (Vector3.up * 0.1f); // 10 cm above ground
		}

		return position + (Vector3.down * 300);
	}

}

