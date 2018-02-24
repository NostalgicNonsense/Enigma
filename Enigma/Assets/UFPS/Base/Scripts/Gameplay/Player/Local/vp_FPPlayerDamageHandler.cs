/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPPlayerDamageHandler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a version of the vp_PlayerDamageHandler class extended for use
//					with the local player (vp_FPPlayerEventHandler) via which it
//					talks to the player HUD, weapon handler, controller and camera
//
///////////////////////////////////////////////////////////////////////////////// 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(vp_FPPlayerEventHandler))]

public class vp_FPPlayerDamageHandler : vp_PlayerDamageHandler 
{

	public float CameraShakeFactor = 0.02f;
	protected float m_DamageAngle = 0.0f;
	protected float m_DamageAngleFactor = 1.0f;

	protected vp_FPPlayerEventHandler m_FPPlayer = null;	// should never be referenced directly
	protected vp_FPPlayerEventHandler FPPlayer	// lazy initialization of the event handler field
	{
		get
		{
			if(m_FPPlayer == null)
				m_FPPlayer = transform.GetComponent<vp_FPPlayerEventHandler>();
			return m_FPPlayer;
		}
	}

	protected vp_FPCamera m_FPCamera = null;	// should never be referenced directly
	protected vp_FPCamera FPCamera	// lazy initialization of the fp camera field
	{
		get
		{
			if (m_FPCamera == null)
				m_FPCamera = transform.GetComponentInChildren<vp_FPCamera>();
			return m_FPCamera;
		}
	}

	protected CharacterController m_CharacterController = null;	// should never be referenced directly
	protected CharacterController CharacterController	// lazy initialization of the event handler field
	{
		get
		{
			if (m_CharacterController == null)
				m_CharacterController = transform.root.GetComponentInChildren<CharacterController>();
			return m_CharacterController;
		}
	}


	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected override void OnEnable()
	{

		if (FPPlayer != null)
			FPPlayer.Register(this);

		RefreshColliders();

	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected override void OnDisable()
	{

		if (FPPlayer != null)
			FPPlayer.Unregister(this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		// TODO: this is demo code and should not be present here!
		// fade timescale back to normal if dead during slomo (this needs
		// to be iterated every frame which is why it's in Update)
		// NOTE: remember that slow motion only works for single player
		if (FPPlayer.Dead.Active && Time.timeScale < 1.0f)
			vp_TimeUtility.FadeTimeScale(1.0f, 0.05f);

	}


	/// <summary>
	/// applies damage to the player in simple float format, sends a damage
	/// flash message to the HUD and twists the camera briefly
	/// </summary>
	public override void Damage(float damage)
	{

		if (!enabled)
			return;

		if (!vp_Utility.IsActive(gameObject))
			return;

		base.Damage(damage);

		FPPlayer.HUDDamageFlash.Send(new vp_DamageInfo(damage, null));

		// shake camera to the left or right
		FPPlayer.HeadImpact.Send(Random.value < 0.5f ? (damage * CameraShakeFactor) : -(damage * CameraShakeFactor));

	}


	/// <summary>
	/// applies damage to the player in UFPS format, sends a damage
	/// flash message to the HUD and twists the camera briefly
	/// </summary>
	public override void Damage(vp_DamageInfo damageInfo)
	{

		if (!enabled)
			return;

		if (!vp_Utility.IsActive(gameObject))
			return;

		base.Damage(damageInfo);

		FPPlayer.HUDDamageFlash.Send(damageInfo);

		// shake camera to left or right depending on direction of damage
		if (damageInfo.Source != null)
		{

			m_DamageAngle = vp_3DUtility.LookAtAngleHorizontal(
				FPCamera.Transform.position,
				FPCamera.Transform.forward,
				damageInfo.Source.position);

			// phase out the shake over 30 degrees to the sides to minimize
			// interference when aiming at the attacker. damage from straight
			// ahead will result in zero shake
			m_DamageAngleFactor = ((Mathf.Abs(m_DamageAngle) > 30.0f) ? 1 : (Mathf.Lerp(0, 1, (Mathf.Abs(m_DamageAngle) * 0.033f))));

			FPPlayer.HeadImpact.Send((damageInfo.Damage * CameraShakeFactor * m_DamageAngleFactor) * ((m_DamageAngle < 0.0f) ? 1 : -1));

		}

	}


	/// <summary>
	/// instantiates the player's death effect, clears the current
	/// weapon, activates the 'Dead' activity and prevents gameplay
	/// input
	/// </summary>
	public override void Die()
	{

		base.Die();

		if (!enabled || !vp_Utility.IsActive(gameObject))
			return;

		FPPlayer.InputAllowGameplay.Set(false);

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void RefreshColliders()
	{

		if ((CharacterController != null) && CharacterController.enabled)
		{
			foreach (Collider c in Colliders)
			{
				if (c.enabled)
					Physics.IgnoreCollision(CharacterController, c, true);
			}
		}

	}


	/// <summary>
	/// restores gameplay input and HUD color. this gets called
	/// in response to respawning
	/// </summary>
	protected override void Reset()
	{

		base.Reset();

		if (!Application.isPlaying)
			return;

		FPPlayer.InputAllowGameplay.Set(true);
		FPPlayer.HUDDamageFlash.Send(null);

		RefreshColliders();

	}


	/// <summary>
	/// 
	/// </summary>
	void OnStart_Crouch()
	{
		RefreshColliders();
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_Crouch()
	{
		RefreshColliders();
	}


}

