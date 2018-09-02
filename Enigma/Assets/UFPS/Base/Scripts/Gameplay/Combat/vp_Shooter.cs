/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Shooter.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this component can be added to any gameobject, giving it the capability
//					of firing projectiles. it handles firing rate, projectile spawning,
//					muzzle flashes, shell casings and shooting sound. call the 'TryFire'
//					method to fire the shooter (whether this succeeds is determined by
//					firing rate)
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]

public class vp_Shooter : vp_Component
{

	protected CharacterController m_CharacterController = null;

	public GameObject m_ProjectileSpawnPoint = null;
	public GameObject ProjectileSpawnPoint
	{
		get
		{
			return m_ProjectileSpawnPoint;
		}
	}

	
	protected GameObject m_ProjectileDefaultSpawnpoint = null;

	// projectile
	public GameObject ProjectilePrefab = null;			// prefab with a mesh and projectile script
	public float ProjectileScale = 1.0f;				// scale of the projectile decal
	public float ProjectileFiringRate = 0.3f;			// delay between shots fired when fire button is held down
	public float ProjectileSpawnDelay = 0.0f;			// delay between fire button pressed and projectile launched
	public int ProjectileCount = 1;						// amount of projectiles to fire at once
	public float ProjectileSpread = 0.0f;				// accuracy deviation in degrees (0 = spot on)
	public bool ProjectileSourceIsRoot = true;			// whether to report this projectile as being sent from this transform or from its root
	public string FireMessage = "";						// OPTIONAL: if this is set, a regular Unity message will be sent to the root gameobject every time the shooter fires
	protected bool HaveFireMessage = false;				// for avoiding runtime string check
	protected float m_NextAllowedFireTime = 0.0f;		// the next time firing will be allowed after having recently fired a shot

	// muzzle flash
	public Vector3 MuzzleFlashPosition = Vector3.zero;	// position of the muzzle in relation to the parent
	public Vector3 MuzzleFlashScale = Vector3.one;		// scale of the muzzleflash
	public float MuzzleFlashFadeSpeed = 0.075f;			// the amount of muzzle flash alpha to deduct each frame
	public GameObject MuzzleFlashPrefab = null;			// muzzleflash prefab, typically with a mesh and vp_MuzzleFlash script
	public float MuzzleFlashDelay = 0.0f;				// delay between fire button pressed and muzzleflash appearing
	protected GameObject m_MuzzleFlash = null;			// the instantiated muzzle flash. one per weapon that's always there

	public Transform m_MuzzleFlashSpawnPoint = null;	// NEW: this gets populated by any child object with the name 'Muzzle', and will completely override 'MuzzleFlashPosition'

	// shell casing
	public GameObject ShellPrefab = null;				// shell prefab with a mesh and (typically) a vp_Shell script
	public float ShellScale = 1.0f;						// scale of ejected shell casings
	protected Vector3 m_ActualShellScale = Vector3.one;
	public Vector3 ShellEjectDirection = new Vector3(1, 1, 1);	// direction of ejected shell casing
	public Vector3 ShellEjectPosition = new Vector3(1, 0, 1);	// position of ejected shell casing in relation to parent
	public float ShellEjectVelocity = 0.2f;				// velocity of ejected shell casing
	public float ShellEjectDelay = 0.0f;				// time to wait before ejecting shell after firing (for shotguns, grenade launchers etc.)
	public float ShellEjectSpin = 0.0f;					// amount of angular rotation of the shell upon spawn
	protected Rigidbody m_ShellRigidbody;				// current rigidbody being tossed

	public Transform m_ShellEjectSpawnPoint = null;		// NEW: this gets populated by any child object with the name 'Shell', and will completely override 'ShellEjectPosition' and 'ShellEjectDirection'

	// sound
	public AudioClip SoundFire = null;							// sound to play upon firing
	public float SoundFireDelay = 0.0f;							// delay between fire button pressed and fire sound played
	public Vector2 SoundFirePitch = new Vector2(1.0f, 1.0f);	// random pitch range for firing sound

	public GameObject MuzzleFlash
	{
		get
		{

			// instantiate muzzleflash
			if ((m_MuzzleFlash == null) && (MuzzleFlashPrefab != null) && (ProjectileSpawnPoint != null))
			{
				m_MuzzleFlash = (GameObject)vp_Utility.Instantiate(MuzzleFlashPrefab,
																ProjectileSpawnPoint.transform.position,
																ProjectileSpawnPoint.transform.rotation);
				m_MuzzleFlash.name = transform.name + "MuzzleFlash";
				m_MuzzleFlash.transform.parent = ProjectileSpawnPoint.transform;
			}
			return m_MuzzleFlash;
		}
	}


	public delegate void NetworkFunc();
	public NetworkFunc m_SendFireEventToNetworkFunc = null;	// null in singleplayer & remote players, set if local & multiplayer
	
	public delegate Vector3 FirePositionFunc();
	public FirePositionFunc GetFirePosition = null;
	public delegate Quaternion FireRotationFunc();
	public FireRotationFunc GetFireRotation = null;
	public delegate int FireSeedFunc();
	public FireSeedFunc GetFireSeed = null;

	// work variables for the current shot being fired
	protected Vector3 m_CurrentFirePosition = Vector3.zero;				// spawn position
	protected Quaternion m_CurrentFireRotation = Quaternion.identity;	// spawn rotation
	protected int m_CurrentFireSeed;									// unique number used to generate a random spread for every projectile

	public Vector3 FirePosition = Vector3.zero;


	/// <summary>
	/// in 'Awake' we do things that need to be run once at the
	/// very beginning. NOTE: as of Unity 4, gameobject hierarchy
	/// can not be altered in 'Awake'
	/// </summary>
	protected override void Awake()
	{
		
		base.Awake();

		if (m_ProjectileSpawnPoint == null)
			m_ProjectileSpawnPoint = gameObject;	// NOTE: may also be set by derived classes

		m_ProjectileDefaultSpawnpoint = m_ProjectileSpawnPoint;

		// if firing delegates haven't been set by a derived or external class yet, set them now
		if (GetFirePosition == null)	GetFirePosition = delegate()	{	return FirePosition; };
		if (GetFireRotation == null)	GetFireRotation = delegate()	{	return m_ProjectileSpawnPoint.transform.rotation;	};
		if (GetFireSeed == null)		GetFireSeed = delegate()		{	return Random.Range(0, 100);	};

		m_CharacterController = m_ProjectileSpawnPoint.transform.root.GetComponentInChildren<CharacterController>();

		// reset the next allowed fire time
		m_NextAllowedFireTime = Time.time;
		ProjectileSpawnDelay = Mathf.Min(ProjectileSpawnDelay, (ProjectileFiringRate - 0.1f));
		if(ShellPrefab != null)
			m_ActualShellScale = ShellPrefab.transform.localScale * ShellScale;

		HaveFireMessage = !string.IsNullOrEmpty(FireMessage);

		// SNIPPET: if you see problems with pooled bullets in multiplayer, you
		// can uncomment this to make the vp_PoolManager instance ignore this
		// shooter's bullet prefabs in multiplayer
		//if (vp_Gameplay.IsMultiplayer && (vp_PoolManager.Instance != null))
		//	vp_PoolManager.Instance.AddIgnoredPrefab(ProjectilePrefab);

	}


	/// <summary>
	/// in 'Start' we do things that need to be run once at the
	/// beginning, but potentially depend on all other scripts
	/// first having run their 'Awake' calls.
	/// NOTE: don't do anything here that depends on activity
	/// in other 'Start' calls
	/// </summary>
	protected override void Start()
	{

		base.Start();
		
		// audio defaults
		Audio.playOnAwake = false;
		Audio.dopplerLevel = 0.0f;

		RefreshDefaultState();

		Refresh();
		
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void LateUpdate()
	{

		// save a 'FirePosition' to ensure a stable firing position. in depth:
		// the position of ProjectileSpawnPoint must not be fetched at arbitrary
		// points in the update cycle (during which the body system manipulates
		// the position of the camera). using a snapshot of the position during
		// the entire update loop will ensure that having e.g. a camera as a
		// projectile spawnpoint won't return 'unstable' positions 
		FirePosition = m_ProjectileSpawnPoint.transform.position; 

	}


	/// <summary>
	/// calls the fire method if the firing rate of this shooter
	/// allows it. override this method to add further rules
	/// </summary>
	public virtual bool CanFire()
	{

		// return if we can't fire yet
		if (Time.time < m_NextAllowedFireTime)
			return false;

		return true;

	}
	

	/// <summary>
	/// calls the fire method if the firing rate of this shooter
	/// allows it. override this method to add further rules.
	/// NOTE: the vp_WeaponShooter does not use this method. it
	/// fires via the player event handler's 'Fire' vp_Attempt.
	/// </summary>
	public virtual bool TryFire()
	{

		// return if we can't fire yet
		if (Time.time < m_NextAllowedFireTime)
			return false;

		Fire();

		return true;

	}


	/// <summary>
	/// spawns projectiles, shell cases, and the muzzleflash.
	/// plays a firing sound and updates the firing rate. NOTE:
	/// don't call this every frame, instead call it via 'TryFire'
	/// to subject it to firing rate.
	/// </summary>
	protected virtual void Fire()
	{
		// update firing rate
		m_NextAllowedFireTime = Time.time + ProjectileFiringRate;

		 //play fire sound
		if (SoundFireDelay == 0.0f)
		    PlayFireSound();
		else
		    vp_Timer.In(SoundFireDelay, PlayFireSound);

		// spawn projectiles
		if (ProjectileSpawnDelay == 0.0f)
			SpawnProjectiles();
		else
			vp_Timer.In(ProjectileSpawnDelay, delegate() { SpawnProjectiles(); });

		// spawn shell casing
		if (ShellEjectDelay == 0.0f)
			EjectShell();
		else
			vp_Timer.In(ShellEjectDelay, EjectShell);

		// show muzzle flash
		if (MuzzleFlashDelay == 0.0f)
			ShowMuzzleFlash();
		else
			vp_Timer.In(MuzzleFlashDelay, ShowMuzzleFlash);

		// send fire message
		// NOTE: this doesn't do anything by default and is just provided as a
		// means of triggering your own method in a script on the root gameobject
		// when the shooter fires
		if(HaveFireMessage)
			gameObject.transform.root.gameObject.SendMessage(FireMessage, SendMessageOptions.DontRequireReceiver);

	}


	/// <summary>
	/// plays the fire sound
	/// </summary>
	protected virtual void PlayFireSound()
	{

		if (Audio == null)
			return;

		Audio.pitch = Random.Range(SoundFirePitch.x, SoundFirePitch.y) * Time.timeScale;
		Audio.clip = SoundFire;
		Audio.Play();
		// LORE: we must use 'Play' rather than 'PlayOneShot' for the
		// AudioSource to be regarded as 'isPlaying' which is needed
		// for 'vp_Component:DeactivateWhenSilent'
	
	}


	/// <summary>
	/// spawns one or more projectiles in a customizable conical
	/// pattern. NOTE: this does not send the projectiles flying.
	/// the spawned gameobjects need to have their own movement
	/// logic
	/// </summary>
	protected virtual void SpawnProjectiles()
	{

		if (ProjectilePrefab == null)
			return;

		// will only trigger on local player in multiplayer
		if (m_SendFireEventToNetworkFunc != null)
			m_SendFireEventToNetworkFunc.Invoke();

		m_CurrentFirePosition = GetFirePosition();
		m_CurrentFireRotation = GetFireRotation();
		m_CurrentFireSeed = GetFireSeed();

		// when firing a single projectile per discharge (pistols, machineguns)
		// this loop will only run once. if firing several projectiles per
		// round (shotguns) the loop will iterate several times. the fire seed
		// is the same for every iteration, but is multiplied with the number
		// of iterations to get a unique, deterministic seed for each projectile.
		for (int v = 0; v < ProjectileCount; v++)
		{
		
			GameObject p = null;

			p = (GameObject)vp_Utility.Instantiate(ProjectilePrefab, m_CurrentFirePosition, m_CurrentFireRotation);

			// TIP: uncomment this to debug-draw bullet paths and points of impact
			//DrawProjectileDebugInfo(v);

			p.SendMessage("SetSource", (ProjectileSourceIsRoot ? Root : Transform), SendMessageOptions.DontRequireReceiver);
			p.transform.localScale = new Vector3(ProjectileScale, ProjectileScale, ProjectileScale);	// preset defined scale

			SetSpread(m_CurrentFireSeed * (v + 1), p.transform);

		}
			
	}


	/// <summary>
	/// applies conical twist to the target transform according
	/// to a certain seed and this shooter's 'ProjectileSpread'
	/// </summary>
	public void SetSpread(int seed, Transform target)
	{

		vp_MathUtility.SetSeed(seed);

		//vp_MasterClient.DebugMsg = "Firing shot from '" + photonView.viewID + "' with seed: " + Random.seed + ".";
		target.Rotate(0, 0, Random.Range(0, 360));									// first, rotate up to 360 degrees around z for circular spread
		target.Rotate(0, Random.Range(-ProjectileSpread, ProjectileSpread), 0);		// then rotate around y with user defined deviation
		
	}
	

	/// <summary>
	/// sends a message to the muzzle flash object telling it
	/// to show itself
	/// </summary>
	protected virtual void ShowMuzzleFlash()
	{

		if (MuzzleFlash == null)
			return;
		
		if (m_MuzzleFlashSpawnPoint != null && ProjectileSpawnPoint != null)
		{
			MuzzleFlash.transform.position = m_MuzzleFlashSpawnPoint.transform.position;
			MuzzleFlash.transform.rotation = m_MuzzleFlashSpawnPoint.transform.rotation;
		}

		MuzzleFlash.SendMessage("Shoot", SendMessageOptions.DontRequireReceiver);
		
	}


	/// <summary>
	/// spawns the 'ShellPrefab' gameobject and gives it a velocity
	/// </summary>
	protected virtual void EjectShell()
	{

		if (this == null)
			return;

		if (ShellPrefab == null)
			return;

		// spawn the shell
		GameObject s = null;
		s = (GameObject)vp_Utility.Instantiate(ShellPrefab,
			((m_ShellEjectSpawnPoint == null)
			? FirePosition + m_ProjectileSpawnPoint.transform.TransformDirection(ShellEjectPosition)	// we have no shell eject object: use old logic
			: m_ShellEjectSpawnPoint.transform.position)												// we have a shell eject object: use new logic
			, m_ProjectileSpawnPoint.transform.rotation);

		s.transform.localScale = m_ActualShellScale;
		vp_Layer.Set(s.gameObject, vp_Layer.Debris);

		// send it flying
		m_ShellRigidbody = s.GetComponent<Rigidbody>();
		if (m_ShellRigidbody == null)
			return;
			
		Vector3 force = ((m_ShellEjectSpawnPoint == null) ?
		transform.TransformDirection(ShellEjectDirection).normalized * ShellEjectVelocity	// we have a shell eject object: use new logic
		: m_ShellEjectSpawnPoint.transform.forward.normalized * ShellEjectVelocity);		// we have no shell eject object: use old logic

		// toss the shell
		m_ShellRigidbody.AddForce(force, ForceMode.Impulse);
		
		// make the shell inherit the current speed of the controller (if any)
		if (m_CharacterController)	// TODO: should use a velocity calculated from operator transform
		{

			Vector3 velocityForce = (m_CharacterController.velocity);
			m_ShellRigidbody.AddForce(velocityForce, ForceMode.VelocityChange);

		}

		// add random spin if user-defined
		if (ShellEjectSpin > 0.0f)
		{
			if (Random.value > 0.5f)
				m_ShellRigidbody.AddRelativeTorque(-Random.rotation.eulerAngles * ShellEjectSpin);
			else
				m_ShellRigidbody.AddRelativeTorque(Random.rotation.eulerAngles * ShellEjectSpin);
		}

	}


	/// <summary>
	/// this method prevents the shooter from firing for 'seconds',
	/// useful e.g. while switching weapons. if no value is given,
	/// shooting will be disabled practically forever
	/// </summary>
	public virtual void DisableFiring(float seconds = 10000000)
	{

		m_NextAllowedFireTime = Time.time + seconds;

	}


	/// <summary>
	/// makes the shooter immediately ready for firing
	/// </summary>
	public virtual void EnableFiring()
	{

		m_NextAllowedFireTime = Time.time;

	}
	

	/// <summary>
	/// this method is called to reset various shooter settings,
	/// typically after creating or loading a shooter
	/// </summary>
	public override void Refresh()
	{

		if (!Application.isPlaying)
			return;

		// update muzzle flash position, scale and fadespeed from preset
		if (MuzzleFlash != null)
		{

			if (m_MuzzleFlashSpawnPoint == null)
			{
				if (ProjectileSpawnPoint == m_ProjectileDefaultSpawnpoint)
					m_MuzzleFlashSpawnPoint = vp_Utility.GetTransformByNameInChildren(ProjectileSpawnPoint.transform, "muzzle");
				else
					m_MuzzleFlashSpawnPoint = vp_Utility.GetTransformByNameInChildren(Transform, "muzzle");
			}

			if (m_MuzzleFlashSpawnPoint != null)
			{
				if (ProjectileSpawnPoint == m_ProjectileDefaultSpawnpoint)
				{
					// 3RD PERSON
					//m_MuzzleFlash.transform.parent = transform.root;	// SNIPPET: use this instead to prevent muzzleflash from being affected by recoil
					m_MuzzleFlash.transform.parent = ProjectileSpawnPoint.transform.parent.parent.parent;	// TODO: ouch, not very nice
				}
				else
				{
					// 1ST PERSON
					m_MuzzleFlash.transform.parent = ProjectileSpawnPoint.transform;
				}
			}
			else
			{
				m_MuzzleFlash.transform.parent = ProjectileSpawnPoint.transform;
				MuzzleFlash.transform.localPosition = MuzzleFlashPosition;
				MuzzleFlash.transform.rotation = ProjectileSpawnPoint.transform.rotation;
			}

			MuzzleFlash.transform.localScale = MuzzleFlashScale;
			MuzzleFlash.SendMessage("SetFadeSpeed", MuzzleFlashFadeSpeed, SendMessageOptions.DontRequireReceiver);
			
		}

		if (ShellPrefab != null)
		{
			if ((m_ShellEjectSpawnPoint == null) && (ProjectileSpawnPoint != null))
			{
				if (ProjectileSpawnPoint == m_ProjectileDefaultSpawnpoint)
				{
					// 3RD PERSON
					m_ShellEjectSpawnPoint = vp_Utility.GetTransformByNameInChildren(ProjectileSpawnPoint.transform, "shell");

				}
				else
				{
					// 1ST PERSON
					m_ShellEjectSpawnPoint = vp_Utility.GetTransformByNameInChildren(Transform, "shell");
				}

			}

		}

	}





	/// <summary>
	/// performs special logic for activating a shooter properly
	/// </summary>
	public override void Activate()
	{

		base.Activate();

		if (MuzzleFlash != null)
			vp_Utility.Activate(MuzzleFlash);

	}


	/// <summary>
	/// performs special logic for deactivating a shooter properly
	/// </summary>
	public override void Deactivate()
	{

		base.Deactivate();

		if (MuzzleFlash != null)
			vp_Utility.Activate(MuzzleFlash, false);

	}


	/// <summary>
	/// draws bullet path debug lines and points of impact. 'index' is the
	/// zero-based index of the projectile as part of a single discharge.
	/// (for example: pellet #3 in a shotgun salvo of 7 pellets)
	/// </summary>
	protected void DrawProjectileDebugInfo(int projectileIndex)
	{

		GameObject debugArrow = vp_3DUtility.DebugPointer();
		debugArrow.transform.rotation = GetFireRotation();
		debugArrow.transform.position = GetFirePosition();
		//Debug.Log("seed: (" + m_CurrentFireSeed + ") * index (" + (projectileIndex + 1) + ") = " + (m_CurrentFireSeed * (projectileIndex + 1)));
		GameObject debugBall = vp_3DUtility.DebugBall();

		RaycastHit hit;
		if (Physics.Linecast(
			debugArrow.transform.position,
			(debugArrow.transform.position + (debugArrow.transform.forward * 1000)),		// max aim range: 1000 meters
			out hit,
			vp_Layer.Mask.ExternalBlockers) && !hit.collider.isTrigger &&	// only aim at non-local player solids
			(Root.InverseTransformPoint(hit.point).z > 0.0f)	// don't aim at stuff between camera & local player
			)
		{
			debugBall.transform.position = hit.point;
		}

	}


}

