/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPPlatformSwitch.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	put this script on a vp_PlatformSwitch gameobject to allow non-
//					master clients to toggle switches using the interact button
//					in multiplayer (default: F)
//
//					NOTE: this script is not required for simple on-collision switches.
//					for these you will be fine using a regular 'vp_PlatformSwitch'
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

[RequireComponent(typeof(vp_PlatformSwitch))]
[RequireComponent(typeof(PhotonView))]

public class vp_MPPlatformSwitch : Photon.MonoBehaviour
{

	// cache the interactable component
	protected vp_Interactable m_Interactable = null;
	protected vp_Interactable Interactable
	{
		get
		{
			if (m_Interactable == null)
			{
				m_Interactable = GetComponent<vp_Interactable>();
			}
			return m_Interactable;
		}
	}

	protected Collider m_Collider = null;
	protected Collider Collider
	{
		get
		{
			if (m_Collider == null)
				m_Collider = transform.GetComponent<Collider>();
			return m_Collider;
		}
	}


	/// <summary>
	/// this gets called by the vp_PlatformSwitch component on the
	/// same gameobject every time a non-master client enables a
	/// switch in a non-master scene using their 'Interact' key
	/// (default: F) 
	/// </summary>
	public void ClientTryInteract()
	{

		if (!vp_Gameplay.IsMultiplayer)
			return;

		if (vp_Gameplay.IsMaster)
			return;

		// send a message to the master client requesting to interact
		// with this switch
		photonView.RPC("RequestInteraction", PhotonTargets.MasterClient);

	}


	/// <summary>
	/// this is executed on the master, and gets called by a client
	/// who wishes to enable a switch in the master scene
	/// </summary>
	[PunRPC]
	void RequestInteraction(PhotonMessageInfo info)
	{

		if (!vp_Gameplay.IsMaster)
			return;

		if (info.sender.IsMasterClient)
			return;

		// find the networkplayer corresponding to the sender id
		vp_MPNetworkPlayer player = vp_MPNetworkPlayer.Get(info.sender.ID);

		// abort if no such player
		if (player == null)
			return;

		// in order to trigger an interaction, the player must be close to the interactable
		if (!player.IsCloseTo(Collider))
			return;

		if (player.Player == null)
			return;

		Interactable.TryInteract(player.Player);

	}
	

}







