/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPPushableRigidbody.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	put this script on a rigidbody or gameobject to allow a local
//					vp_FPController in multiplayer to push it around, or stand on
//					it to make it tilt etc. while syncing it authoritatively over
//					the network.
//
//					push detection is done client-side while keeping the master 
//					in charge of push force, proximity, direction, frequency and
//					rigidbody properties (mass, physics material etc.)
//
//					NOTES:
//					1) only use this script if you _really_ need the player to be
//						able to push an object around. for big and heavy objects
//						that the player should not be able to push (but should
//						still be movable by explosions, projectiles or scripting)
//						instead use the base script 'vp_MPRigidBody'
//					2) if you want the player to be able to ride the platform,
//						don't forget to put it in the 'MovableObject' layer (28),
//						otherwise the player will be prone to slide off
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class vp_MPPushableRigidbody : vp_MPRigidbody
{

	public enum PushForceMode
	{
		SameAsController,	// use the mode of the controller (whether 'Simplified' or 'Kinetic')
		Simplified,			// overrides the controller's 'PhysicsPushMode' to be 'Simplified' for this rigidbody
		Kinetic				// overrides the controller's 'PhysicsPushMode' to be 'Kinetic' for this rigidbody
	}

	public PushForceMode PhysicsPushMode = PushForceMode.SameAsController;

	// minimum push interval that the master will accept for this rigidbody
	public float MinPushInterval = 0.05f;
	protected float m_NextAllowedPushTime = 0.0f;

	// register / unregister the 'Push' event
	protected virtual void OnEnable()	{	vp_TargetEvent<Vector3, Vector3>.Register(Rigidbody, "Push", Push);		}
	protected virtual void OnDisable()	{	vp_TargetEvent<Vector3, Vector3>.Unregister(Rigidbody, "Push", Push);	}

		
	/// <summary>
	/// this SendMessage target can be used to ask the master to make
	/// a certain player push the rigidbody in a direction. it is intended for
	/// characters kicking around smaller objects like crates and furniture.
	/// this gets called from vp_FPController by default
	/// </summary>
	protected virtual void Push(Vector3 direction, Vector3 point)
	{

		if (PhotonNetwork.offlineMode)
			return;

		photonView.RPC("RequestPushRigidBody", PhotonTargets.MasterClient, direction, point);

	}


	/// <summary>
	/// this RPC is sent to the master client from a client who wishes
	/// to push the rigidbody. the master decides if the push is allowed
	/// and (if so) pushes the rigidbody. the resulting motion is synced
	/// by this script across all clients
	/// </summary>
	[PunRPC]
	protected virtual void RequestPushRigidBody(Vector3 direction, Vector3 point, PhotonMessageInfo info)
	{

		// abort if this machine does not have authority to push things
		if (!vp_Gameplay.IsMaster)
			return;

		// abort if this rigidbody has very recently been pushed
		if (Time.time < m_NextAllowedPushTime)
			return;
		m_NextAllowedPushTime = Time.time + MinPushInterval;

		// find the networkplayer corresponding to the sender id
		vp_MPNetworkPlayer player = vp_MPNetworkPlayer.Get(info.sender.ID);

		// abort if no such player
		if (player == null)
			return;

		// in order to push this rigidbody the player must be either located
		// inside the bounds of its collider ...
		if (!Collider.bounds.Contains(player.Collider.bounds.center)
			// ... OR must be located within a distance equal to one player height
			// from the 'closest point on bounds' of the requested contact point
			&& (Vector3.Distance(player.Collider.bounds.center, Rigidbody.ClosestPointOnBounds(point)) > player.Player.Height.Get()))
			return;

		// abort if player is trying to pull rather than push
		if (Vector3.Dot(direction.normalized, (player.Transform.position - point)) > 0)
			return;

		// push the rigidbody according to the final direction, push force mode and point of impact
		switch (PhysicsPushMode)
		{
			case PushForceMode.SameAsController:	player.Controller.PushRigidbody(Rigidbody, direction, player.Controller.PhysicsPushMode, point);		break;
			case PushForceMode.Simplified:			player.Controller.PushRigidbody(Rigidbody, direction, vp_Controller.PushForceMode.Simplified, point);	break;
			case PushForceMode.Kinetic:				player.Controller.PushRigidbody(Rigidbody, direction, vp_Controller.PushForceMode.Kinetic, point);		break;
		}
						
	}


}

