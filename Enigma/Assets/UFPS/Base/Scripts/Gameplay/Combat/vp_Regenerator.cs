/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Regenerator.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script will make object health start to regenerate a
//					certain time after taking damage. by default, health will
//					start to regenerate 5 seconds after taking a hit, and will
//					increase by 10% per second if damagehandler max health is 10
//
//					NOTE: the script must sit on the same transform as a
//					vp_DamageHandler (or derived class)
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;

public class vp_Regenerator : MonoBehaviour
{

	public float DelayAfterDamage = 5.0f;	// delay in seconds until regeneration can start after taking damage
	public float TickRate = 1.0f;			// interval in seconds between damage additions (keep rate low for multiplayer)
	public float TickAmount = 0.1f;			// amount of health to add for every tick (TIP: MaxHealth is 1.0 by default)

	protected float m_NextAllowedTickTime = 0.0f;

	protected vp_DamageHandler m_DamageHandler = null;
	public vp_DamageHandler DamageHandler
	{
		get
		{
			if (!m_TriedGetDamageHandler && (m_DamageHandler == null))
			{
				m_DamageHandler = transform.root.GetComponentInChildren<vp_DamageHandler>();
				m_TriedGetDamageHandler = true;
			}
			return m_DamageHandler;
		}
	}
	protected bool m_TriedGetDamageHandler = false;


	/// <summary>
	/// 
	/// </summary>
	protected void Update()
	{

		if (DamageHandler == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a DamageHandler. Disabling self.");
			this.enabled = false;
			return;
		}

		// only regenerate health within the 0-max range

		if (DamageHandler.CurrentHealth >= DamageHandler.MaxHealth)
			return;

		if (DamageHandler.CurrentHealth <= 0.0f)
			return;

		// impose delay since last damage
		if (Mathf.Abs(Mathf.Abs(Time.time) - Mathf.Abs(DamageHandler.LastDamageTime)) < 0.1f)
		{
			m_NextAllowedTickTime = (Time.time + DelayAfterDamage);
			return;
		}

		// tick
		if (Time.time < m_NextAllowedTickTime)
			return;
		m_NextAllowedTickTime = Time.time + TickRate;

		// if this is singleplayer or we are a multiplayer master, update health
		if (vp_Gameplay.IsMaster)
			DamageHandler.CurrentHealth += Mathf.Max(0, TickAmount);

		// a multiplayer master transmits the health across the network
		if ((vp_Gameplay.IsMultiplayer) && (vp_Gameplay.IsMaster))
			vp_GlobalEvent<Transform, Transform, float>.Send("TransmitDamage", transform.root, transform.root, -Mathf.Max(0, TickAmount));

	}


}