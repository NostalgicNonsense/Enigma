/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPMovingPlatform.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	put this script on a moving platform gameobject to make it sync
//					authoritatively over the network in multiplayer.
//
//					this script will restrict moving platform motion to occur on the
//					master client only. on all other clients the object will be remote-
//					controlled by the master.
//
//					NOTES:
//					1) see the 'Moving Platforms' manual chapter for detailed info on
//						how to set up moving platforms in UFPS: http://bit.ly/IS9Xek
//					2) rotating platforms (and waypoints with different angles) are not
//						recommended in multiplayer because latency issues are heavily
//						magnified when it comes to rotation. the 'Custom Rotate' mode
//						is especially strongly advised against
//					3) the platform will automatically assume the 'MovableObject'
//						layer (28). this is enforced by the required 'vp_MovingPlatform'
//						component
//					4)	moving platforms can generate a ton of movement and rotation
//						data that needs syncing across the network. preferably use as
//						few platforms as possible and prefer platforms that stand still
//						by default and only move when activated
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

[RequireComponent(typeof(vp_MovingPlatform))]

public class vp_MPMovingPlatform : vp_MPRigidbody
{

	protected bool m_IsRotatingPlatform = false;	// whether this platform will transmit rotation data

	// cache the required moving platform component
	protected vp_MovingPlatform m_MovingPlatform = null;
	protected vp_MovingPlatform MovingPlatform
	{
		get
		{
			if (m_MovingPlatform == null)
			{
				m_MovingPlatform = GetComponent<vp_MovingPlatform>();
			}
			return m_MovingPlatform;
		}
	}
	

	/// <summary>
	/// 
	/// </summary>
	protected override void Start()
	{

		base.Start();

		// initialize the 'm_IsRotatingPlatform' bool by investigating the
		// settings of the required 'vp_MovingPlatform' component on startup
		if (MovingPlatform.RotationInterpolationMode == vp_MovingPlatform.RotateInterpolationMode.CustomRotate)	// if platform uses the 'CustomRotate' mode ...
			m_IsRotatingPlatform = (MovingPlatform.RotationSpeed != Vector3.zero);								// ... and has values set -> it will rotate!

		else if ((MovingPlatform.PathWaypoints != null)								// if it has a waypoint group object ...
				&& (MovingPlatform.PathWaypoints.transform.childCount > 1))			// ... and has more than one waypoint child object ...
		{
			// ... loop the waypoints to see if they will result in platform rotation
			Vector3 rotation = Vector3.zero;
			bool gotFirstWaypointRotation = false;
			foreach (Transform t in MovingPlatform.PathWaypoints.transform)
			{
				if (!gotFirstWaypointRotation)
				{
					rotation = t.eulerAngles;
					gotFirstWaypointRotation = true;
					continue;
				}
				if (vp_MathUtility.SnapToZero((rotation - t.eulerAngles), 0.001f) != Vector3.zero)
					m_IsRotatingPlatform = true;	// platform does have differently rotated waypoints -> it will rotate!
			}
		}

	}


	/// <summary>
	/// the purpose of this override is to avoid sending unused
	/// rotation data over the network
	/// </summary>
	protected override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{

		if (stream.isWriting)
		{
			stream.SendNext((Vector3)Transform.position);
			if (m_IsRotatingPlatform)
				stream.SendNext((Quaternion)Transform.rotation);
		}
		else
		{
			m_LastPosition = (Vector3)stream.ReceiveNext();
			if (m_IsRotatingPlatform)
				m_LastRotation = (Quaternion)stream.ReceiveNext();
		}

	}
	

	/// <summary>
	/// enables moving platform movement logic on the master client
	/// while disabling it on all other machines. in non-master scenes
	/// the platform will be remote-controlled by the master client.
	/// (called from the base script)
	/// </summary>
	protected override void RefreshMasterControl()
	{

		Rigidbody.isKinematic = true;
		MovingPlatform.enabled = PhotonNetwork.isMasterClient;

	}


	/// <summary>
	/// 
	/// </summary>
	protected void OnTriggerEnter(Collider col)
	{

		if (!vp_Gameplay.IsMultiplayer)
			return;

		if (vp_Gameplay.IsMaster)
			return;

		if (!MovingPlatform.GetPlayer(col))
			return;

		photonView.RPC("RequestAutoStart", PhotonTargets.MasterClient);

	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void RequestAutoStart(PhotonMessageInfo info)
	{

		if (!vp_Gameplay.IsMaster)
			return;

		// must have a rigidbody collider to check for proximity
		if(Collider == null)
			return;

		// find the networkplayer corresponding to the sender id
		vp_MPNetworkPlayer player = vp_MPNetworkPlayer.Get(info.sender.ID);

		// abort if no such player
		if (player == null)
			return;

		// in order to autostart this platform, the player must be close to it
		if(!player.IsCloseTo(Collider))
			return;

		MovingPlatform.TryAutoStart();

	}
	

}







