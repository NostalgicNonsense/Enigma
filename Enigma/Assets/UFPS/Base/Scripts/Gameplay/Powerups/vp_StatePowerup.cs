/////////////////////////////////////////////////////////////////////////////////
//
//	vp_StatePowerup.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a simple script to set a time-limited state. by default sets
//					the state 'MegaSpeed' on the player for  a few seconds.
//					any state can be set or disabled in this way! you could do
//					anything from increasing jump force to enabling a 'drunk'
//					camera state. see the code comments below for more info.
//
//					NOTE: for this script to work in multiplayer, make sure that
//					the remote player objects have the required states on them
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;

public class vp_StatePowerup : vp_Powerup
{

	public string State = "MegaSpeed";
	public float Duration = 0.0f;

	// timer handle to manage multiple timers
	protected vp_Timer.Handle m_Timer = new vp_Timer.Handle();


	/// <summary>
	///
	/// </summary>
	protected override void Update()
	{

		// handle rotation and bob, if enabled
		UpdateMotion();

		// remove powerup if depleted and silent
		if (m_Depleted)
		{

			if (!m_Audio.isPlaying)
				Remove();

		}
		
	}


	/// <summary>
	/// tries to enable 'State' on the player
	/// </summary>
	protected override bool TryGive(vp_PlayerEventHandler player)
	{

		// prevent the player from picking up the item again until any
		// currently running speed timer has run its course
		if (m_Timer.Active)
			return false;

		if (string.IsNullOrEmpty(State))
			return false;

		// --- proper way of doing speed ---

		// for something like this we use the State Manager and vp_Timer!
		// in the powerup demo folder you will find a controller preset
		// named 'ControllerMegaSpeed.txt' which boosts player acceleration
		// and increases its push force on rigidbodies.
		// in the demo scene this has been added as a state named 'MegaSpeed'
		// to the controller component

		player.SetState(State);

		// restore state after 'Duration' seconds. if that's not set, uses
		// the powerup's 'RespawnDuration'
		vp_Timer.In(((Duration <= 0.0f) ? RespawnDuration : Duration), delegate()
		{
			player.SetState(State, false);
		}, m_Timer);

		// NOTE: binding the 'm_Timer' handle above makes sure this timer
		// is canceled and restarted if it's already running. if you allow
		// players to pick up multiple powerups, this will prevent a depleted
		// powerup from disabling the state if the player has enabled a new one
		// while the previous one is active (i.e. the timer will be restarted)

		// --- buggy way of doing speed ---

		// the below would also be a way of adding speed, but it would get messed up
		// if player pressed or released the 'Run' modifier key. speed would multiply
		// in case of several powerups and we would have to store the original controller
		// acceleration value in a 'Start' method. messy and error-prone. use states.

		//Player.Controller.MotorAcceleration *= 4.0f;
		//vp_Timer.In(Value, delegate()
		//{
		//    Player.Controller.MotorAcceleration *= 0.25f;	// ... or a stored original speed
		//});

		return true;

	}


}
