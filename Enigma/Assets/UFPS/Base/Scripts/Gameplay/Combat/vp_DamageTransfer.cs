/////////////////////////////////////////////////////////////////////////////////
//
//	vp_DamageTransfer.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	use this script to forward damage sent to one collider to a
//					specific damage handler on another transform. this can be
//					useful in cases where you want a single damage handler to
//					collect incoming damage from multiple colliders, or where
//					a damagehandler gets blocked by an encasing collider
//
//					NOTES:
//						1) if 'TargetObject' is null, the first damagehandler found
//							on the lowest ancestor will be used (if any)
//						2) if 'TargetObject' is set, but has no damagehandler (in itself
//							or in any of its children) the script will attempt to execute
//							the method 'Damage(float)' on 'TargetObject'
//						2) if there is no target object the script will fallback to running
//							the method 'Damage(float)' in all (!) ancestor components that
//							have it present. PLEASE NOTE: in this case more than one damage
//							method might be executed in one frame
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_DamageTransfer : MonoBehaviour
{

	public GameObject TargetObject = null;

	protected vp_DamageHandler m_TargetDamageHandler = null;
	protected Collider m_Collider = null;


	/// <summary>
	/// 
	/// </summary>
	void Start()
	{
		
		// verify collider
		m_Collider = transform.GetComponent<Collider>();
		if (m_Collider == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a collider. Disabling self!");
			this.enabled = false;
			return;
		}

		// find target damage handler
		if (TargetObject != null)
			m_TargetDamageHandler = TargetObject.GetComponentInChildren<vp_DamageHandler>();
		else
		{
			m_TargetDamageHandler = vp_DamageHandler.GetDamageHandlerOfCollider(m_Collider);
			if(m_TargetDamageHandler != null)
				TargetObject = m_TargetDamageHandler.gameObject;
		}

	}


	/// <summary>
	/// forwards damage in UFPS format to a damagehandler on the target object
	/// </summary>
	public virtual void Damage(vp_DamageInfo damageInfo)
	{

		if (!enabled)
			return;

		if (m_TargetDamageHandler != null)
			m_TargetDamageHandler.Damage(damageInfo);
		else
			Damage(damageInfo.Damage);

	}
	

	/// <summary>
	/// forwards damage in float format by executing the method 'Damage(float)'
	/// on the target object
	/// </summary>
	public virtual void Damage(float damage)
	{

		if (!enabled)
			return;

		if (m_TargetDamageHandler != null)
			m_TargetDamageHandler.Damage(damage);
		else if(TargetObject != null)
			TargetObject.SendMessage("Damage", damage, SendMessageOptions.DontRequireReceiver);
		else
			gameObject.SendMessageUpwards("Damage", damage, SendMessageOptions.DontRequireReceiver);

	}


}