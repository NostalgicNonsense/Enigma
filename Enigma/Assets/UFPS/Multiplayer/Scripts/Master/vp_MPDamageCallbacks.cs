/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPDamageCallbacks.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	base class manager that responds to damage, kills and respawns on the
//					current master client by syncing the results to all other clients.
//					without this script 'there can be no death' for non-master clients.
//					override this script to create more complex responses.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_4_OR_NEWER
	using UnityEngine.SceneManagement;
#endif


public class vp_MPDamageCallbacks : Photon.MonoBehaviour
{

	protected static Dictionary<int, vp_DamageHandler> m_DamageHandlersByViewID = new Dictionary<int, vp_DamageHandler>();
	protected static Dictionary<int, vp_Respawner> m_RespawnersByViewID = new Dictionary<int, vp_Respawner>();

	// set this to true to always keep non-player damagehandlers in perfect sync
	// on all machines (not necessarily needed since master will force-kill things
	// that dies in its scene anyway)
	public bool SyncPropHealth = false;


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded += OnLevelLoad;
#endif
		
		vp_GlobalEvent<Transform, Transform, float>.Register("TransmitDamage", TransmitDamage);		// sent by vp_DamageHandler
		vp_GlobalEvent<Transform>.Register("TransmitKill", TransmitKill);							// sent by vp_DamageHandler and vp_PlayerDamageHandler
		vp_GlobalEvent<Transform, vp_Placement>.Register("TransmitRespawn", TransmitRespawn);		// sent by vp_Respawner

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded -= OnLevelLoad;
#endif

		vp_GlobalEvent<Transform, Transform, float>.Unregister("TransmitDamage", TransmitDamage);
		vp_GlobalEvent<Transform>.Unregister("TransmitKill", TransmitKill);
		vp_GlobalEvent<Transform, vp_Placement>.Unregister("TransmitRespawn", TransmitRespawn);

	}


	/// <summary>
	/// this method is called when any object is damaged locally in the
	/// master scene, transmitting its new health to all remote clients.
	/// it is typically initiated by vp_DamageHandler
	/// </summary>
	protected virtual void TransmitDamage(Transform targetTransform, Transform sourceTransform, float damage)
	{

		// NOTES:
		// 1) players (vp_PlayerDamageHandlers) will have health synced perfectly across
		//		all machines at all times
		// 2) health of plain vp_DamageHandlers (props) is only kept in perfect sync if
		//		'SyncPropHealth' is true. however, when their health reaches zero on the
		//		master and a 'TransmitKill' message occurs the prop in question will
		//		always die immediately on all machines
		// 3) 'sourceTransform' is not used here, but needed for overrides that deal
		//		with more complex gameplay (see example in 'vp_DMDamageCallbacks')
		// 4) 'damage' is assumed to have already been updated in the master scene
		//		damage handler. it is not used here, but overrides can do a lot more with
		//		it (see example in 'vp_DMDamageCallbacks').
		// 5) 'damage' can be both positive and negative. a negative number will add health

		if (!PhotonNetwork.isMasterClient)
			return;

		int viewID = vp_MPMaster.GetViewIDOfTransform(targetTransform);
		if (viewID == 0)
			return;

		vp_DamageHandler d = GetDamageHandlerOfViewID(viewID);
		if(d == null)
			return;

		// abort if target already died (no health to update)
		if (d.CurrentHealth <= 0.0f)	
			return;

		// abort if this is a prop and we're not supposed to sync prop health
		if(!SyncPropHealth && !(d is vp_PlayerDamageHandler))
			return;

		photonView.RPC("ReceiveObjectHealth", PhotonTargets.Others, viewID, (float)d.CurrentHealth);    // NOTE: cast to float required for Anti-Cheat Toolkit support

	}


	/// <summary>
	/// updates the damagehandler of a corresponding photonview id with a
	/// a new health value. sent by the master to keep client damagehandlers
	/// in sync
	/// </summary>
	[PunRPC]
	protected virtual void ReceiveObjectHealth(int viewID, float health, PhotonMessageInfo info)
	{

		//vp_MPDebug.Log("GOT OBJECT HEALTH!");

		if ((info.sender != PhotonNetwork.masterClient) ||
			(info.sender.IsLocal))
			return;

		vp_DamageHandler d = GetDamageHandlerOfViewID(viewID);
		if (d == null)
			return;

		d.CurrentHealth = health;

	}


	/// <summary>
	/// this method responds to a 'TransmitKill' vp_GlobalEvent raised by an
	/// object in the master scene and sends out an RPC to enforce the kill
	/// on remote machines. it is typically sent out by vp_PlayerDamageHandler
	/// or vp_DamageHandler
	/// </summary>
	protected virtual void TransmitKill(Transform targetTransform)
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		// --- killing a PLAYER ---
		vp_MPNetworkPlayer player = vp_MPNetworkPlayer.Get(targetTransform);
		if (player != null)
		{
			player.photonView.RPC("ReceivePlayerKill", PhotonTargets.All);
			return;
		}

		// --- killing an OBJECT ---
		int viewID = vp_MPMaster.GetViewIDOfTransform(targetTransform);
		if (viewID > 0)
		{
			// send RPC with kill command to photonView of this script on all clients
			photonView.RPC("ReceiveObjectKill", PhotonTargets.Others, viewID);
		}

		// TIP: local player could be forced to drop its current weapon as a pickup here
		// however dropping items is not yet supported

	}


	/// <summary>
	/// this method responds to a 'TransmitRespawn' event raised by an object in the
	/// master scene, and sends out an RPC to trigger the respawn on remote machines.
	/// it is typically initiated by vp_DamageHandler.
	/// </summary>
	protected virtual void TransmitRespawn(Transform targetTransform, vp_Placement placement)
	{

		// --- respawning a PLAYER ---
		if (vp_MPNetworkPlayer.Get(targetTransform) != null)
		{
			vp_MPPlayerSpawner.TransmitPlayerRespawn(targetTransform, placement);
			return;
		}

		// --- respawning an OBJECT ---
		int viewID = vp_MPMaster.GetViewIDOfTransform(targetTransform);
		if (viewID > 0)
		{
			photonView.RPC("ReceiveObjectRespawn", PhotonTargets.Others, viewID, placement.Position, placement.Rotation);
		}
		
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	public virtual void ReceiveObjectKill(int viewId, PhotonMessageInfo info)
	{

		if (info.sender != PhotonNetwork.masterClient)
			return;

		vp_DamageHandler d = GetDamageHandlerOfViewID(viewId);
		if ((d == null) || (d is vp_PlayerDamageHandler))
			return;

		// cache respawner before we deactivate the object, or 'GetRespawnerOfViewID'
		// won't be able to find the deactivated object later
		GetRespawnerOfViewID(viewId);

		d.Die();

	}

	
	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	public virtual void ReceiveObjectRespawn(int viewId, Vector3 position, Quaternion rotation, PhotonMessageInfo info)
	{

		if (info.sender != PhotonNetwork.masterClient)
			return;

		vp_Respawner r = GetRespawnerOfViewID(viewId);
		if ((r == null) || (r is vp_PlayerRespawner))
			return;

		// make object temporarily invisible so we don't see it 'pos-lerping'
		// across the map to its respawn position
		if (r.Renderer != null)
			r.Renderer.enabled = false;
	
		r.Respawn();

		// restore visibility in half a sec
		if (r.Renderer != null)
			vp_Timer.In(0.5f, () => { r.Renderer.enabled = true; });

	}


	/// <summary>
	/// caches and returns the damagehandler of the given photonview id.
	/// damagehandlers are stored in a dictionary that resets on level load
	/// </summary>
	public static vp_DamageHandler GetDamageHandlerOfViewID(int id)
	{

		vp_DamageHandler d = null;

		if (!m_DamageHandlersByViewID.TryGetValue(id, out d))
		{
			PhotonView p = PhotonView.Find(id);
			if (p != null)
			{
				d = p.transform.GetComponent<vp_DamageHandler>();
				if (d != null)
					m_DamageHandlersByViewID.Add(id, d);
				return d;
			}
			// NOTE: we do not add null results, since photonviews come and go
		}

		return d;

	}


	/// <summary>
	/// caches and returns the respawner of the given photonview id.
	/// respawners are stored in a dictionary that resets on level load
	/// </summary>
	public static vp_Respawner GetRespawnerOfViewID(int id)
	{

		vp_Respawner d = null;

		if (!m_RespawnersByViewID.TryGetValue(id, out d))
		{

			PhotonView p = PhotonView.Find(id);
			if (p != null)
			{
				d = p.transform.GetComponent<vp_Respawner>();
				if (d != null)
					m_RespawnersByViewID.Add(id, d);
				return d;
			}
			
			// NOTE: we do not add null results, since photonviews come and go
		}

		return d;

	}


	/// <summary>
	/// 
	/// </summary>
#if UNITY_5_4_OR_NEWER
	protected void OnLevelLoad(Scene scene, LoadSceneMode mode)
#else
	protected void OnLevelWasLoaded()
#endif
	{
		m_DamageHandlersByViewID.Clear();
		m_RespawnersByViewID.Clear();
	}


}
