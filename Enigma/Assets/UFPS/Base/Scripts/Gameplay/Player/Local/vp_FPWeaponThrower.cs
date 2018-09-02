/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPWeaponThrower.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	NOTE: this script exists in two versions. this (inherited) version
//					should ONLY be used on a local player (remote players and AI should
//					instead use 'vp_WeaponThrower').
//
//					this script can be placed on the same gameobject as a vp_FPWeapon to
//					make it behave as a throwing weapon. the setup is somewhat complex
//					because we're dealing with an object that is both a WEAPON and its
//					own AMMO.
//
//					MECHANICS
//					the 3d model of the vp_FPWeapon ideally depicts an animated 3d arm holding
//					and throwing the object. when thrown, the arm will go out of view and a
//					projectile version of the thrown object is spawned. if more ammo exists,
//					the hand comes back into view, perceivably holding a new grenade / knife
//					/ rock from the player's backpack.
//
//					CHECKLIST OF REQUIREMENTS
//					- the vp_FPWeapon should have a top level state called 'ReWield'. this state
//						should block all other states, and the text file preset should set
//						'PositionOffset' to	an out-of-view position. this should make the hand smoothly
//						move in-and-out of view. see the example preset 'WeaponGrenade01Rewield.txt'
//					- be sure to also read the checklist in the base script, 'vp_WeaponThrower.cs',
//						since every requirement listed there also applies to this version of the
//						script ...
//	
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_FPWeaponThrower : vp_WeaponThrower
{

	public Vector3 FirePositionOffset = new Vector3(0.35f, 0.0f, 0.0f);		// 1st person projectile spawn offset (default: slightly to right of camera view)
	protected bool m_OriginalLookDownActive = false;

	// timer handles to avoid bad states with overlapping timers
	protected vp_Timer.Handle m_Timer1 = new vp_Timer.Handle();
	protected vp_Timer.Handle m_Timer2 = new vp_Timer.Handle();
	protected vp_Timer.Handle m_Timer3 = new vp_Timer.Handle();
	protected vp_Timer.Handle m_Timer4 = new vp_Timer.Handle();

	// --- properties for performance ---

	protected vp_FPWeapon m_FPWeapon = null;
	public vp_FPWeapon FPWeapon
	{
		get
		{
			if (m_FPWeapon == null)
				m_FPWeapon = (vp_FPWeapon)Transform.GetComponent(typeof(vp_FPWeapon));
			return m_FPWeapon;
		}
	}

	protected vp_FPWeaponShooter m_FPWeaponShooter = null;
	public vp_FPWeaponShooter FPWeaponShooter
	{
		get
		{
			if (m_FPWeaponShooter == null)
				m_FPWeaponShooter = (vp_FPWeaponShooter)Transform.GetComponent(typeof(vp_FPWeaponShooter));
			return m_FPWeaponShooter;
		}
	}

	protected Transform m_FirePosition;
	protected Transform FirePosition
	{
		get
		{
			if (m_FirePosition == null)
			{
				GameObject g = new GameObject("ThrownWeaponFirePosition");
				m_FirePosition = g.transform;
				m_FirePosition.parent = Camera.main.transform;
			}
			return m_FirePosition;
		}
	}

	public Animation WeaponAnimation
	{
		get
		{
			if (m_WeaponAnimation == null)
			{
				if (FPWeapon == null)
					return null;
				if (FPWeapon.WeaponModel == null)
					return null;
				m_WeaponAnimation = FPWeapon.WeaponModel.GetComponent<Animation>();
			}
			return m_WeaponAnimation;
		}
	}
	Animation m_WeaponAnimation = null;


	/// <summary>
	/// 
	/// </summary>
	protected override void Start()
	{

		base.Start();

		m_OriginalLookDownActive = FPWeapon.LookDownActive;

	}

	
	/// <summary>
	/// resets the animation of the vp_FPWeapon's mesh renderer
	/// </summary>
	protected virtual void RewindAnimation()
	{

		if(!Player.IsFirstPerson.Get())
			return;

		if(FPWeapon == null)
			return;

		if(FPWeapon.WeaponModel == null)
			return;

		if (WeaponAnimation == null)
			return;

		if(FPWeaponShooter == null)
			return;

		if(FPWeaponShooter.AnimationFire == null)
			return;

		WeaponAnimation[FPWeaponShooter.AnimationFire.name].time = 0.0f;

		WeaponAnimation.Play();
		WeaponAnimation.Sample();
		WeaponAnimation.Stop();
			
	}
		
	
	/// <summary>
	/// 
	/// </summary>
	protected override void OnStart_Attack()
	{

		base.OnStart_Attack();

		// set spawn position for the projectile
		if (Player.IsFirstPerson.Get())
		{
			// in 1st person, don't necessarily fire the projectile from the
			// center of the camera, but perhaps a little bit to the right
			// where the arm would be
			Shooter.m_ProjectileSpawnPoint = FirePosition.gameObject;
			FirePosition.localPosition = FirePositionOffset;
			FirePosition.localEulerAngles = Vector3.zero;
		}
		else
		{
			// in 3rd person, always spawn the projectile exactly where the weapon is
			Shooter.m_ProjectileSpawnPoint = Weapon.Weapon3rdPersonModel;
		}

		// don't attempt lookdown adjustments while throwing things (it just gets crazy)
		FPWeapon.LookDownActive = false;	// NOTE: this is restored later
	
		// as soon as the fire animation has pulled the arm out of sight, perform
		// some tasks in preparation for the next throw
		vp_Timer.In(Shooter.ProjectileSpawnDelay, delegate()
		{

			// unwield or rewield the weapon depending on inventory state
			if (!HaveAmmoForCurrentWeapon)	// if there are no more units of this type to throw ...
			{

				// ... set the 'ReWield' state to avoid popping of the arm ...
				FPWeapon.SetState("ReWield");
				FPWeapon.Refresh();

				vp_Timer.In(1.0f, delegate()
				{
					if (!Player.SetNextWeapon.Try())	// ... then try to set the next weapon ...
					{
						// ... but if we have no other weapon: instead disarm!
						vp_Timer.In(0.5f, delegate()
						{
							RewindAnimation();
							Player.SetWeapon.Start(0);
						}, m_Timer2);
					}
				});

			}
			else		// if we DO have additional units like this one	...
			{

				if (Player.IsFirstPerson.Get())
				{

					// set the 'ReWield' state to avoid popping of the arm
					FPWeapon.SetState("ReWield");
					FPWeapon.Refresh();

					// rewind animation and restore rewield state in 1 sec
					vp_Timer.In(1.0f, delegate()
					{
						RewindAnimation();
						FPWeapon.Rendering = true;
						FPWeapon.SetState("ReWield", false);
						FPWeapon.Refresh();
					}, m_Timer3);

				}
				else
				{
					// force-stop the attack activity half a second after the throw
					vp_Timer.In(0.5f, delegate()
					{
						Player.Attack.Stop();
					}, m_Timer4);
				}

			}

		}, m_Timer1);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_SetWeapon()
	{

		RewindAnimation();

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected override void OnStop_Attack()
	{

		base.OnStop_Attack();

		// restore original lookdown state
		FPWeapon.LookDownActive = m_OriginalLookDownActive;

	}


}