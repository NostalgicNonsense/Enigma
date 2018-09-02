/////////////////////////////////////////////////////////////////////////////////
//
//	vp_HealthPowerup.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a simple script for adding health to the player
//					NOTE: this script is an example of overriding the 'vp_Powerup'
//					class with a custom public variable and a 'TryGive' method
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;

public class vp_HealthPowerup : vp_Powerup
{

	public float Health = 1.0f;


	/// <summary>
	/// tries to add 'Health' to the player
	/// </summary>
	protected override bool TryGive(vp_PlayerEventHandler player)
	{

		if (player.Health.Get() < 0.0f)
			return false;

		if (player.Health.Get() >= player.MaxHealth.Get())
			return false;

		// if this is singleplayer or we are a multiplayer master, update health
		if (vp_Gameplay.IsMaster)
			player.Health.Set(Mathf.Min(player.MaxHealth.Get(), (player.Health.Get() + Health)));

		// a multiplayer master transmits the health across the network
		if ((vp_Gameplay.IsMultiplayer) && (vp_Gameplay.IsMaster))
			vp_GlobalEvent<Transform, Transform, float>.Send("TransmitDamage", player.transform.root, transform.root, -Health);

		// returning true here even if we're a multiplayer client (and health hasn't changed locally yet)
		// to prevent a fail sound from playing before master has allowed the health to be added
		return true;

	}

}
