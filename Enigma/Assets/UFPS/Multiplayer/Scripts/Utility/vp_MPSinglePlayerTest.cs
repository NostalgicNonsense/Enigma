/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPSinglePlayerTest.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a development utility for quick tests where you don't want
//					to spend time connecting to the Photon Cloud. just put this
//					script on a gameobject in the scene and activate it.
//					if 'SpawnMode' is set to 'Prefab' (and a valid player prefab
//					is provided) it will be spawned in the scene. if 'SpawnMode'
//					is set to 'Scene', the first player object found in the scene
//					will be used.
//
//					NOTE: only intended for testing, not for use in an actual game
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class vp_MPSinglePlayerTest : MonoBehaviour
{

	public GameObject LocalPlayerPrefab = null;
	public SpawnMode m_SpawnMode = SpawnMode.Prefab;

	public enum SpawnMode
	{
		Scene,
		Prefab
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		if (PhotonNetwork.connected)
		{
			Debug.LogWarning("Warning (" + this + ") This script should not be used when connected. Disabling self. (Did you forget to disable a vp_MPSinglePlayerTest object in a multiplayer map?)");
			enabled = false;
			return;
		}

		PhotonNetwork.offlineMode = true;
		vp_Gameplay.IsMultiplayer = false;
		vp_Gameplay.IsMaster = true;

		vp_PlayerEventHandler[] players = FindObjectsOfType<vp_PlayerEventHandler>();
		foreach (vp_PlayerEventHandler player in players)
		{
			vp_Utility.Activate(player.gameObject, false);
		}

		vp_MPMaster[] masters = Component.FindObjectsOfType<vp_MPMaster>() as vp_MPMaster[];
		foreach (vp_MPMaster g in masters)
		{
			if (g.gameObject != gameObject)
				vp_Utility.Activate(g.gameObject, false);
		}

		// disable demo gui via globalevent since we don't want hard references
		// to code in the demo folder
		vp_GlobalEvent.Send("DisableMultiplayerGUI", vp_GlobalEventMode.DONT_REQUIRE_LISTENER);

		vp_SpawnPoint p = vp_SpawnPoint.GetRandomSpawnPoint();

		switch (m_SpawnMode)
		{
			case SpawnMode.Prefab:
				GameObject l = (GameObject)GameObject.Instantiate(LocalPlayerPrefab, p.transform.position, p.transform.rotation);
				l.GetComponent<vp_PlayerEventHandler>().Rotation.Set(p.transform.eulerAngles);
				break;
			case SpawnMode.Scene:
				vp_Utility.Activate(LocalPlayerPrefab, true);
				if (p != null)
				{
					LocalPlayerPrefab.transform.position = p.transform.position;
					LocalPlayerPrefab.GetComponent<vp_PlayerEventHandler>().Rotation.Set(p.transform.eulerAngles);
				}
				break;
		}

	}


}