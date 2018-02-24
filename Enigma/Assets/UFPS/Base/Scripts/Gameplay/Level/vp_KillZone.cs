/////////////////////////////////////////////////////////////////////////////////
//
//	vp_KillZone.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a trigger to kill an object on contact. this script will only
//					work on targets with a vp_DamageHandler-derived component and
//					a collider on them. it has logic to address cases where
//					'OnTriggerEnter' behaves erratically
//					
//					IMPORTANT: make sure to add a collider to the killzone
//					gameobject and enable the 'IsTrigger' checkbox!
//
//					TIP: killzones can be used most creatively with other scripts for
//					singleplayer traps and puzzles. use with rigidbodies for rolling
//					boulders. put on the floor for a pool of lava. create a spinning,
//					moving giant circular saw blade with vp_Spin and vp_Bob. or use
//					with vp_Timer and vp_AngleBob to activate / deactivate a devious
//					rotating death-ray! you can even use it with vp_Shooter to fire an
//					insta-kill rigidbody ...
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_KillZone : MonoBehaviour
{

	vp_DamageHandler m_TargetDamageHandler = null;
	vp_Respawner m_TargetRespawner = null;


	/// <summary>
	/// 
	/// </summary>
	void Start()
	{
		gameObject.layer = vp_Layer.Trigger;
	}
	

	/// <summary>
	/// 
	/// </summary>
	void OnTriggerEnter(Collider col)
	{

		// return if this is not a relevant object. TIP: this check can be expanded
		if (col.gameObject.layer == vp_Layer.Debris
			|| col.gameObject.layer == vp_Layer.Pickup)
			return;

		// try to find a damagehandler on the target and abort on fail
		m_TargetDamageHandler = vp_DamageHandler.GetDamageHandlerOfCollider(col);
		if (m_TargetDamageHandler == null)
			return;

		// abort if target is already dead
		// NOTE: this deals with cases of multiple 'OnTriggerEnter' calls on contact
		if (m_TargetDamageHandler.CurrentHealth <= 0)
			return;

		// try to find a respawner on the target to see if it's currently OK to kill it
		m_TargetRespawner = vp_Respawner.GetByCollider(col);
		if (m_TargetRespawner != null)
		{
			// abort if target has respawned within one second before this call.
			// NOTE: this addresses a case where 'OnTriggerEnter' is called when
			// teleporting (respawning) away from the trigger, resulting in the
			// object getting insta-killed on respawn. it will only work if the
			// target gameobject has a vp_Respawner-derived component
			if (Time.time < m_TargetRespawner.LastRespawnTime + 1.0f)
				return;
		}

		m_TargetDamageHandler.Damage(new vp_DamageInfo(m_TargetDamageHandler.CurrentHealth, m_TargetDamageHandler.Transform, vp_DamageInfo.DamageType.KillZone));

	}


}