/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPPlayerCollisionManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script can be used to prevent collision issues between
//					players, that typically occur when player colliders end
//					up inside each other (for any reason) or when one player
//					stands on top of another player.
//
//					in such cases, collision between the two players will be
//					temporarily disabled by this script (detection still works
//					against all other objects including players, bullets and
//					explosions).
//				
//					USAGE:
//						1) assign this script to a single gameobject in the scene
//							(suggestion: the same gameobject as vp_MPMaster).
//						2) 'CheckInterval' in seconds determines how often every
//							player in the scene should be validated against every
//							other player in the scene (minimum: one second).
//						3) 'RestoreInterval' determines how often all disabled
//							collider pairs in the scene should be restored.
//							NOTE: colliders will only be restored if they are
//							'RestoreDistance' meters away from each other.
//
//					NOTE: this is all client side (non-authoritative)
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

public class vp_MPPlayerCollisionManager : Photon.MonoBehaviour
{

	public float CheckInterval = 1.0f;
	public float RestoreInterval = 3.0f;
	public float RestoreDistance = 2.0f;
	public float PlayerHeight = 2.0f;

#if UNITY_EDITOR
	[vp_HelpBox("This script can be used to prevent buggy physics between players, that typically occur when player colliders end up inside each other (for any reason) or when one player stands on top of another player. In such cases, collision between the two players will be temporarily disabled by this script.", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpBox;
#endif

	protected float m_NextAllowedCheckCollisionTime = 0.0f;
	protected float m_NextAllowedRestoreCollisionTime = 0.0f;
	protected List<Collider[]> m_CollidersToRestore = new List<Collider[]>();

	private const float SHOULDER_HEIGHT_MULTIPLIER = 0.75f;


	/// <summary>
	/// 
	/// </summary>
	public virtual void Update()
	{

		TryCheckColliders();

		TryRestoreColliders();

	}


	/// <summary>
	/// checks to see if any of the player colliders are intersecting, and if so
	/// disables collision between the two colliders that are intersecting, and
	/// remembers them for later restoring
	/// </summary>
	public virtual void TryCheckColliders()
	{

		if (Time.time < m_NextAllowedCheckCollisionTime)
			return;

		m_NextAllowedCheckCollisionTime = m_NextAllowedCheckCollisionTime + Mathf.Max(1.0f, CheckInterval);

		if (vp_MPNetworkPlayer.Players.Count < 2)
			return;

		foreach (vp_MPNetworkPlayer p1 in vp_MPNetworkPlayer.Players.Values)
		{
			if (p1 == null)
				continue;
			foreach (vp_MPNetworkPlayer p2 in vp_MPNetworkPlayer.Players.Values)
			{
				if (p2 == null)
					continue;
				if (p1 == p2)
					continue;
				if (Vector3.Distance(p1.Transform.position, p2.Transform.position) > 3)     // most iterations will return here
					continue;
				if (p1.Player == null)
					continue;
				if (p1.Player.Grounded.Get())	// only check grounded players for collision
					continue;
				if (p1.Collider == null)
					continue;
				if (!p1.Collider.enabled)
					continue;
				if (p2.Player == null)
					continue;
				if (p2.Collider == null)
					continue;
				if (!p2.Collider.enabled)
					continue;
				if (!p1.IsCloseTo(p2.Collider))	// see if bounds are intersecting
					continue;
				if (!p2.Player.Grounded.Get()	// if both players are flying inside each other's bounds, this might be due to charactercontroller hiccups
					|| ((p1.Transform.position.y - p2.Transform.position.y) > (PlayerHeight * SHOULDER_HEIGHT_MULTIPLIER)))		// if p1's feet is above p2's shoulders and inside its bounds, p1 is likely walking on top of p2
				{
					// high risk of wacky physics! make these colliders ignore collision with each other for now
					Collider[] c = new Collider[2];
					c[0] = p1.Collider;
					c[1] = p2.Collider;
					Physics.IgnoreCollision(c[0], c[1], true);
					m_CollidersToRestore.Add(c);	// remember to restore collision later
				}

			}
		}

	}


	/// <summary>
	/// restores collision between players whose colliders ignore each other,
	/// if they are more than 3 meters apart
	/// </summary>
	public virtual void TryRestoreColliders()
	{

		if (Time.time < m_NextAllowedRestoreCollisionTime)
			return;

		m_NextAllowedRestoreCollisionTime = m_NextAllowedRestoreCollisionTime + RestoreInterval;

		for (int v = (m_CollidersToRestore.Count - 1); v > -1; v--)
		{
			if ((m_CollidersToRestore[v] == null)
				|| (m_CollidersToRestore[v][0] == null)
				|| (m_CollidersToRestore[v][1] == null))
			{
				m_CollidersToRestore.RemoveAt(v);
				continue;
			}

			// only restore collision if players are far apart and both colliders are enabled
			if ((Vector3.Distance(m_CollidersToRestore[v][0].transform.position, m_CollidersToRestore[v][1].transform.position) > 3.0f)
				&& (m_CollidersToRestore[v][0].enabled)
				&& (m_CollidersToRestore[v][1].enabled))
			{
				Physics.IgnoreCollision(m_CollidersToRestore[v][0], m_CollidersToRestore[v][1], false);
				m_CollidersToRestore.RemoveAt(v);
			}

		}

	}


}

