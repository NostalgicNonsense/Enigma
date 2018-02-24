/////////////////////////////////////////////////////////////////////////////////
//
//	vp_WeaponShooter.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a weapon shooter that can be used on AIs and remote players
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;


public class vp_WeaponShooter : vp_Shooter
{

	protected vp_Weapon m_Weapon = null;			// the weapon affected by the shooter

	// projectile
	public float ProjectileTapFiringRate = 0.1f;		// minimum delay between shots fired when fire button is tapped quickly and repeatedly
	protected float m_LastFireTime = 0.0f;
	protected float m_OriginalProjectileSpawnDelay = 0.0f;

	// motion
	public Vector3 MotionPositionRecoil = new Vector3(0, 0, -0.035f);	// positional force applied to weapon upon firing
	public Vector3 MotionRotationRecoil = new Vector3(-10.0f, 0, 0);	// angular force applied to weapon upon firing
	public float MotionRotationRecoilDeadZone = 0.5f;	// 'blind spot' center region for angular z recoil
	public float MotionDryFireRecoil = -0.1f;			// multiplies recoil when the weapon is out of ammo
	public float MotionRecoilDelay = 0.0f;				// delay between fire button pressed and recoil

	// muzzle flash
	public float MuzzleFlashFirstShotMaxDeviation = 180.0f;	// max muzzleflash-to-fire-angle deviation for when the projectile is being fired from idle stance (disabled by default)
	protected bool m_WeaponWasInAttackStateLastFrame = false;			// work variables
	protected float m_MuzzleFlashWeaponAngle = 0.0f;
	protected float m_MuzzleFlashFireAngle = 0.0f;

	// sound
	public AudioClip SoundDryFire = null;				// out of ammo sound

	protected Quaternion m_MuzzlePointRotation = Quaternion.identity;
	protected bool m_3rdPersonFiredThisFrame = false;


	// event handler property cast as a playereventhandler
	protected vp_PlayerEventHandler m_Player = null;
	vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
			{
				if (EventHandler != null)
					m_Player = (vp_PlayerEventHandler)EventHandler;
			}
			return m_Player;
		}
	}


	public vp_Weapon Weapon
	{
		get
		{
			if(m_Weapon == null)
				m_Weapon = transform.GetComponent<vp_Weapon>();
			return m_Weapon;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Awake()
	{

		// make any null projectilespawnpoint fall back to 3rd person weapon
		// before 'base.Awake' has a chance to make it fall back to gameobject.
		// (though will fallback to gameobject if 3rd person weapon is null)
		if ((m_ProjectileSpawnPoint == null) && (Weapon.Weapon3rdPersonModel != null))
			m_ProjectileSpawnPoint = Weapon.Weapon3rdPersonModel;

		// if firing delegates haven't been set by a derived or external class yet, set them now
		if (GetFireSeed == null)		GetFireSeed = delegate() { return Random.Range(0, 100); };
		if (GetFirePosition == null) GetFirePosition = delegate() { return FirePosition; };
		if (GetFireRotation == null)	GetFireRotation = delegate()	// this is for local 3rd person, and will be overridden for multiplayer remote players (TODO: maybe for AIs too?)
			{
				Quaternion rot = Quaternion.identity;
				if((Player.LookPoint.Get() - FirePosition) != Vector3.zero)
					rot = vp_MathUtility.NaNSafeQuaternion(Quaternion.LookRotation(Player.LookPoint.Get() - FirePosition));
				return rot;
			};

		base.Awake();

		// reset the next allowed fire time
		m_NextAllowedFireTime = Time.time;

		ProjectileSpawnDelay = Mathf.Min(ProjectileSpawnDelay, (ProjectileFiringRate - 0.1f));
		m_OriginalProjectileSpawnDelay = ProjectileSpawnDelay;	// backup value since it may change at runtime

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Start()
	{

		base.Start();

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void LateUpdate()
	{

		if (Player == null)
			return;

		if (Player.IsFirstPerson == null)
			return;

		if ((!Player.IsFirstPerson.Get()) && m_3rdPersonFiredThisFrame)
		{
			m_3rdPersonFiredThisFrame = false;
		}

		m_WeaponWasInAttackStateLastFrame = Weapon.StateManager.IsEnabled("Attack");

		base.LateUpdate();

	}

	/// <summary>
	/// in addition to spawning the projectile in the base class,
	/// plays a fire animation on the weapon and applies recoil
	/// to the weapon spring. also regulates tap fire
	/// </summary>
	protected override void Fire()
	{

		// in UFPS multiplayer add-on, projectiles are spawned by RPC, and
		// local spawn delay must not be applied, so temporarily zero it
		if (vp_Gameplay.IsMultiplayer && !Player.IsLocal.Get())
			ProjectileSpawnDelay = 0.0f;

		m_LastFireTime = Time.time;

		// store firing status for 3rd person
		if (!Player.IsFirstPerson.Get())
			m_3rdPersonFiredThisFrame = true;

		// apply recoil
		if (MotionRecoilDelay == 0.0f)
			ApplyRecoil();
		else
			vp_Timer.In(MotionRecoilDelay, ApplyRecoil);

		base.Fire();

		// keep 'ProjectileSpawnDelay' untouched outside of this scope
		// since other logics may rely on it
		ProjectileSpawnDelay = m_OriginalProjectileSpawnDelay;

	}


	/// <summary>
	/// this method sends a message to the muzzle flash object telling
	/// it to show itself. it also prevents cases where a shot fired
	/// directly from idle stance in 3rd person may wrongly appear to
	/// have been fired into the ground
	/// NOTE: this is currently broken
	/// </summary>
	protected override void ShowMuzzleFlash()
	{

		// if firing the first shot in a salvo, and muzzleflash deviations
		// are set for 3rd person, the muzzleflash mesh will be invisible
		// when the angle between the weapon barrel and the actual firing
		// direction deviates beyond the vertical limit (this frequently
		// happens when a weapon is fired directly from idle mode, or while
		// crouching). the logic will typically make the first shot have a
		// muzzle flash light - but no muzzle flash mesh - which reduces the
		// appearance of firing into the ground when in fact firing ahead.

		if (m_MuzzleFlash == null)
			return;

		if (MuzzleFlashFirstShotMaxDeviation == 180.0f		// this logic is disabled by default ...
			|| Player.IsFirstPerson.Get()					// ... and only for 3rd person ...
			|| m_WeaponWasInAttackStateLastFrame			// ... and only for when firing the first shot of a salvo
			)
		{
			base.ShowMuzzleFlash();							// so no muzzleflash hiding needed here: show it normally
			return;
		}

		// this was a 'first shot', meaning the player may still be in its
		// idle pose. see if the fire direction deviates too much from the
		// muzzleflash direction. NOTE: vertically (pitch) only

		m_MuzzleFlashWeaponAngle = Transform.eulerAngles.x + 90;
		m_MuzzleFlashFireAngle = m_CurrentFireRotation.eulerAngles.x + 90;
		m_MuzzleFlashWeaponAngle = ((m_MuzzleFlashWeaponAngle >= 360) ? (m_MuzzleFlashWeaponAngle - 360) : m_MuzzleFlashWeaponAngle);
		m_MuzzleFlashFireAngle = ((m_MuzzleFlashFireAngle >= 360) ? (m_MuzzleFlashFireAngle - 360) : m_MuzzleFlashFireAngle);

		//Debug.Log(Mathf.Abs(m_MuzzleFlashWeaponAngle - m_MuzzleFlashFireAngle) + " = " + (Mathf.Abs(m_MuzzleFlashWeaponAngle - m_MuzzleFlashFireAngle) > MuzzleFlashFirstShotMaxDeviation));

		if (Mathf.Abs(m_MuzzleFlashWeaponAngle - m_MuzzleFlashFireAngle) > MuzzleFlashFirstShotMaxDeviation)
			m_MuzzleFlash.SendMessage("ShootLightOnly", SendMessageOptions.DontRequireReceiver);	// show muzzle flash mesh + light
		else
			base.ShowMuzzleFlash();				// show muzzle flash light only

	}


	/// <summary>
	/// applies some advanced recoil motions on the weapon when fired
	/// </summary>
	protected virtual void ApplyRecoil()
	{

		// add a positional and angular force to the weapon for one frame
		if (MotionRotationRecoil.z == 0.0f)
			Weapon.AddForce2(MotionPositionRecoil, MotionRotationRecoil);
		else
		{
			// if we have rotation recoil around the z vector, also do dead zone logic
			Weapon.AddForce2(MotionPositionRecoil,
				Vector3.Scale(MotionRotationRecoil, (Vector3.one + Vector3.back)) +	// recoil around x & y
				(((Random.value < 0.5f) ? Vector3.forward : Vector3.back) *	// spin direction (left / right around z)
				Random.Range(MotionRotationRecoil.z * MotionRotationRecoilDeadZone,
												MotionRotationRecoil.z)));		// spin force
		}


	}


	/// <summary>
	/// applies a scaled version of the recoil to the weapon to
	/// signify pulling the trigger with no discharge. then plays
	/// a dryfire sound. TIP: make 'MotionDryFireRecoil' about
	/// -0.1 for a subtle 'out-of-ammo-jerk'
	/// </summary>
	public virtual void DryFire()
	{

		if (Audio != null)
		{
			Audio.pitch = Time.timeScale;
			Audio.PlayOneShot(SoundDryFire);
		}

		DisableFiring();

		m_LastFireTime = Time.time;

		// apply dryfire recoil
		Weapon.AddForce2(MotionPositionRecoil * MotionDryFireRecoil, MotionRotationRecoil * MotionDryFireRecoil);

	}


	/// <summary>
	/// 
	/// </summary>
	public void OnMessage_DryFire()
	{
		DryFire();
	}


	/// <summary>
	/// this callback is triggered when the activity 'Attack' deactivates
	/// </summary>
	protected virtual void OnStop_Attack()
	{

		if (ProjectileFiringRate == 0)
		{
			EnableFiring();
			return;
		}

		DisableFiring(ProjectileTapFiringRate - (Time.time - m_LastFireTime));

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected virtual bool OnAttempt_Fire()
	{

		// weapon can only be fired when firing rate allows it
		if (Time.time < m_NextAllowedFireTime)
			return false;

		// weapon can only be fired if it has ammo (or doesn't require ammo).
		// NOTE: on success this call will remove ammo, so it's done only once
		// everything else checks out
		if (!Player.DepleteAmmo.Try())
		{
			DryFire();
			return false;
		}

		// all good ... fire in the hole!
		Fire();

		return true;

	}


}

