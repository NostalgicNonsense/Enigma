/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPWeaponMeleeAttack.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:		a test class devised to see if melee combat would be
//						feasible using springs and without 'real' animations.
//						NOTE: this is still a bit of a prototype script
//						and not the easiest to use
//
//						ITERATION 2: damage logic has now been separated to the
//						vp_WeaponShooter component in order to work correctly in
//						multiplayer. this component now requires a vp_WeaponShooter
//						component (NOTE: _not_ a vp_FPWeaponShooter).
//						the only parameter that should be set on the weapon shooter
//						component should be the 'ProjectilePrefab', which should be
//						a gameobject sporting a vp_Bullet with a short (~1m)
//						range and no decal. the rest of its parameters will be auto
//						synced with the vp_FPWeaponMeleeAttack component at runtime.
//	
//						ITERATION 3: vp_HitscanBullet is obsolete and this script
//						now requires a vp_Bullet (which might be a vp_HitscanBullet)
//						
///////////////////////////////////////////////////////////////////////////////// 


using UnityEngine;
using System.Collections.Generic;

public class vp_FPWeaponMeleeAttack : vp_Component
{

#if (UNITY_EDITOR)
	public bool DrawDebugObjects = false;
#endif

	public string WeaponStatePull = "Pull";			// weapon state for pulling back the weapon pre-slash
	public string WeaponStateSwing = "Swing";		// weapon state for the slash. NOTE: this is not a slash in itself. it just
													// orientates the weapon so that the soft forces can rotate it properly

	// soft forces are used for the slash itself. these are positional and angular
	// spring force impulses that are added over several frames

	// swing
	public float SwingDelay = 0.5f;			// delay until slash begins after weapon has been raised
	public float SwingDuration = 0.5f;		// delay until the weapon swing is stopped
	public float SwingRate = 1.0f;
	protected float m_NextAllowedSwingTime = 0.0f;
	public int SwingSoftForceFrames = 50;				// number of frames over which to apply the forces of each attack
	public Vector3 SwingPositionSoftForce = new Vector3(-0.5f, -0.1f, 0.3f);
	public Vector3 SwingRotationSoftForce = new Vector3(50, -25, 0);

	// impact
	public float ImpactTime = 0.11f;
	public Vector3 ImpactPositionSpringRecoil = new Vector3(0.01f, 0.03f, -0.05f);
	public Vector3 ImpactPositionSpring2Recoil = Vector3.zero;
	public Vector3 ImpactRotationSpringRecoil = Vector3.zero;
	public Vector3 ImpactRotationSpring2Recoil = new Vector3(0.0f, 0.0f, 10.0f);

	// attack
	public bool AttackPickRandomState = true;
	protected int m_AttackCurrent = 0;	// current randomly selected attack

	// sounds
	public List<UnityEngine.Object> SoundSwing = new List<UnityEngine.Object>();	// list of impact sounds to be randomly played
	public Vector2 SoundSwingPitch = new Vector2(0.5f, 1.5f);	// random pitch range for swing sounds

	// timers
	vp_Timer.Handle SwingDelayTimer = new vp_Timer.Handle();
	vp_Timer.Handle ImpactTimer = new vp_Timer.Handle();
	vp_Timer.Handle SwingDurationTimer = new vp_Timer.Handle();
	vp_Timer.Handle ResetTimer = new vp_Timer.Handle();

	// --- properties ---

	vp_FPPlayerEventHandler m_Player = null;
	vp_FPPlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
			{
				if (EventHandler != null)
					m_Player = (vp_FPPlayerEventHandler)EventHandler;
			}
			return m_Player;
		}
	}
	
	vp_FPWeapon m_FPWeapon = null;
	vp_FPWeapon FPWeapon
	{
		get
		{
			if (m_FPWeapon == null)
				m_FPWeapon = Transform.GetComponent<vp_FPWeapon>();
			return m_FPWeapon;
		}
	}

	vp_WeaponShooter m_WeaponShooter = null;
	vp_WeaponShooter WeaponShooter
	{
		get
		{
			if (m_WeaponShooter == null)
			{
				m_WeaponShooter = Transform.GetComponent<vp_WeaponShooter>();
				if (m_WeaponShooter == null)
				{
					Debug.LogWarning("Warning (" + this + ") This component requires a vp_WeaponShooter (adding vp_WeaponShooter automatically).");
					m_WeaponShooter = gameObject.AddComponent<vp_WeaponShooter>();
				}
				else if (m_WeaponShooter is vp_FPWeaponShooter)
				{
					m_WeaponShooter.enabled = false;
					Debug.LogWarning("Warning (" + this + ") This component requires a vp_WeaponShooter. It does _not_ work with a vp_FPWeaponShooter!  (removing vp_FPWeaponShooter and adding vp_WeaponShooter automatically).");
					m_WeaponShooter = gameObject.AddComponent<vp_WeaponShooter>();
				}
			}
			return m_WeaponShooter;
		}
	}

	protected vp_FPCamera m_FPCamera = null;
	public vp_FPCamera FPCamera
	{
		get
		{
			if (m_FPCamera == null)
				m_FPCamera = Root.GetComponentInChildren<vp_FPCamera>();
			return m_FPCamera;
		}
	}

	protected vp_Controller m_FPController = null;
	public vp_Controller FPController
	{
		get
		{
			if (m_FPController == null)
				m_FPController = Root.GetComponentInChildren<vp_Controller>();
			return m_FPController;
		}
	}

	[System.Obsolete("Please use the 'Bullet' parameter instead.")]
	public vp_Bullet HitscanBullet
	{
		get
		{
			return Bullet;
		}
	}
		
	protected vp_Bullet m_Bullet = null;
	public vp_Bullet Bullet
	{
		get
		{
			if (m_Bullet == null && (WeaponShooter != null) && (WeaponShooter.ProjectilePrefab != null))
			{
				m_Bullet = WeaponShooter.ProjectilePrefab.GetComponent<vp_Bullet>();
				if (m_Bullet == null)
					Debug.LogWarning("Warning (" + this + ") ProjectilePrefab of the WeaponShooter has no vp_Bullet-derived component (this melee weapon won't be able to do damage).");
			}
			return m_Bullet;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Awake()
	{

		base.Awake();

		// lock a number of weapon shooter parameters to the corresponding melee
		// attack parameters and check integrity of the shooter setup
		if (WeaponShooter != null)
		{

			WeaponShooter.ProjectileFiringRate = SwingRate;
			WeaponShooter.ProjectileTapFiringRate = SwingRate;
			WeaponShooter.ProjectileSpawnDelay = SwingDelay;
			WeaponShooter.ProjectileScale = 1;
			WeaponShooter.ProjectileCount = 1;
			WeaponShooter.ProjectileSpread = 0;

			if (WeaponShooter.ProjectilePrefab == null)
				Debug.LogWarning("Warning (" + this + ") WeaponShooter for this melee weapon has no 'ProjectilePrefab' (it won't be able to do damage).");

			if (WeaponShooter.Weapon != null)
				WeaponShooter.Weapon.AnimationType = (int)vp_Weapon.Type.Melee;

		}


	}


	/// <summary>
	/// 
	/// </summary>
	protected override void OnEnable()
	{


		RefreshFirePoint();

		base.OnEnable();
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Update()
	{

		base.Update();

		UpdateAttack();

	}


	/// <summary>
	/// simulates a melee swing by carrying out a sequence of
	/// timers, state changes and soft forces on the weapon springs
	/// </summary>
	protected void UpdateAttack()
	{

		if (!Player.Attack.Active)
			return;

		if (Player.SetWeapon.Active)
			return;

		if (FPWeapon == null)
			return;

		if (!FPWeapon.Wielded)
			return;

		if (Time.time < m_NextAllowedSwingTime)
			return;

		m_NextAllowedSwingTime = Time.time + SwingRate;

		// enable random attack states on the melee and weapon components
		if (AttackPickRandomState)
			PickAttack();

		// set 'raise' state (of the chosen attack state) on the weapon component
		FPWeapon.SetState(WeaponStatePull);
		FPWeapon.Refresh();

		// after a short delay, swing the weapon
		vp_Timer.In(SwingDelay, delegate()
		{

			// play a random swing sound
			if (SoundSwing.Count > 0)
			{
				Audio.pitch = Random.Range(SoundSwingPitch.x, SoundSwingPitch.y) * Time.timeScale;
				Audio.clip = (AudioClip)SoundSwing[(int)Random.Range(0, (SoundSwing.Count))];
				if(vp_Utility.IsActive(gameObject))
					Audio.Play();
			}

			// switch to the swing state
			FPWeapon.SetState(WeaponStatePull, false);
			FPWeapon.SetState(WeaponStateSwing);
			FPWeapon.Refresh();

			// apply soft forces of the current attack
			FPWeapon.AddSoftForce(SwingPositionSoftForce, SwingRotationSoftForce, SwingSoftForceFrames);

			// check for target impact after a predetermined duration
			vp_Timer.In(ImpactTime, delegate()
			{

				// perform a sphere cast ray from center of controller, at height
				// of camera and along camera angle
				RaycastHit hit;
				Ray ray = new Ray(new Vector3(FPController.Transform.position.x, FPCamera.Transform.position.y,
												FPController.Transform.position.z), FPCamera.Transform.forward);

				Physics.Raycast(ray, out hit, (Bullet != null ? Bullet.Range : 2), vp_Layer.Mask.BulletBlockers);

				// hit something: perform impact functionality
				if (hit.collider != null)
				{

					if (WeaponShooter != null)
					{
						WeaponShooter.FirePosition = Camera.main.transform.position;
						WeaponShooter.TryFire();
					}
					ApplyRecoil();

				}
				else
				{

					// didn't hit anything: carry on swinging until time is up
					vp_Timer.In(SwingDuration - ImpactTime, delegate()
					{
						FPWeapon.StopSprings();
						Reset();
					}, SwingDurationTimer);

				}

			}, ImpactTimer);

		}, SwingDelayTimer);

	}


	/// <summary>
	/// picks a random component state for the commencing attack
	/// </summary>
	void PickAttack()
	{

		int attack = States.Count - 1;

	reroll:

		attack = UnityEngine.Random.Range(0, States.Count - 1);
		if ((States.Count > 1) && (attack == m_AttackCurrent) && (Random.value < 0.5f))
			goto reroll;

		m_AttackCurrent = attack;

		// set chosen attack state on the melee component
		SetState(States[m_AttackCurrent].Name);

	}


	/// <summary>
	/// adds recoil to the weapon springs in response to hitting
	/// something
	/// </summary>
	void ApplyRecoil()
	{

		FPWeapon.StopSprings();
		FPWeapon.AddForce(ImpactPositionSpringRecoil, ImpactRotationSpringRecoil);
		FPWeapon.AddForce2(ImpactPositionSpring2Recoil, ImpactRotationSpring2Recoil);
		Reset();

	}


	/// <summary>
	/// resets the weapon state to normal
	/// </summary>
	void Reset()
	{

		vp_Timer.In(0.05f, delegate()
		{
			if (FPWeapon != null)
			{
				FPWeapon.SetState(WeaponStatePull, false);
				FPWeapon.SetState(WeaponStateSwing, false);
				FPWeapon.Refresh();
				if (AttackPickRandomState)
					ResetState();
			}
		}, ResetTimer);

	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnMessage_CameraToggle3rdPerson()
	{
		RefreshFirePoint();
	}


	/// <summary>
	/// 
	/// </summary>
	void RefreshFirePoint()
	{

		if (WeaponShooter == null)
			return;
		
		if (Player.IsFirstPerson == null)
			return;

		// --- 1st PERSON ---
		if (Player.IsFirstPerson.Get())
			WeaponShooter.m_ProjectileSpawnPoint = FPCamera.gameObject;

		// --- 3rd PERSON ---
		else
			WeaponShooter.m_ProjectileSpawnPoint = FPController.gameObject;

		if (Player.CurrentWeaponName.Get() != name)
			WeaponShooter.m_ProjectileSpawnPoint = FPCamera.gameObject;

	}
		

}
