///////////////////////////////////////////////////////////////////////////////
//
//	vp_LocalPlayer.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this is a wrapper class to greatly simplify scripting the local UFPS player.
//					it provides global quick-access to the local player and its most common UFPS
//					components. also, it exposes a number of local player + input properties and
//					methods for quick and straightforward access in a single place. it's also a
//					good script to study for examples of how to interact with various UFPS systems.
//
//					USAGE:
//						just make sure the scene has a standard UFPS local player setup, and you
//						should be able to use the components, methods and properties of
//						vp_LocalPlayer from within any script in the code base.
//
//					TIPS:
//						1) all properties will get and set values in the 'proper UFPS way'
//							(where applicable). they also return type defaults in case any component
//							dependencies are null, making them very stable. an exception is the
//							component properties themselves. if a particular component is not present
//							on the local player the property will return null..
//						2) for pausing, and quick-access to info on the game session, use 'vp_Gameplay'
//						3) inherit this class to add parameters and methods specific to your own
//							game design.
//						4) in case you want to strip down UFPS and this class gives you compilation
//							errors due to tight coupling, then you can just remove it along with the
//							two references (!) to it in vp_FPPlayerEventHandler. other than that,
//							THIS CLASS IS SAFE TO DELETE.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.SceneManagement;

public static class vp_LocalPlayer
{
	
	private static vp_FPPlayerEventHandler m_FPEventHandler;
	private static vp_FPCamera m_FPCamera;
	private static vp_FPController m_FPController;
	private static vp_FPInput m_FPInput;
	private static vp_FPPlayerDamageHandler m_FPDamageHandler;
	private static vp_FPWeaponHandler m_FPWeaponHandler;
	private static vp_PlayerInventory m_Inventory;
	private static vp_PlayerRespawner m_Respawner;
	private static vp_FPBodyAnimator m_FPBodyAnimator;
	private static vp_RagdollHandler m_RagdollHandler;
	private static vp_PlayerFootFXHandler m_FootFXHandler;

	private static int m_LevelOfLocalPlayer = -99999;

	private static Vector2 m_MouseSensBackup = new Vector2(5.0f, 5.0f);
	private static Texture m_CrosshairBackup = null;
    private static Texture m_InvisibleTexture = null;


	/// <summary>
	/// returns true if UFPS has been able to cache a vp_FPPlayerEventHandler
	/// in the current scene, false if not. NOTE: if this returns false (perhaps
	/// due to loading, or the local player not having spawned yet in multiplayer),
	/// then it is not a good idea to use vp_LocalPlayer calls. if this is the case,
	/// try running 'vp_LocalPlayer' Refresh and check 'Exists' again before using
	/// vp_LocalPlayer calls.
	/// </summary>
	public static bool Exists
	{
		get
		{
			return (m_FPEventHandler != null);
		}
	}


	/// <summary>
	/// if needed, caches the local player event handler in the current scene (as opposed
	/// to a potentially null player from the previous scene) along with all of its most
	/// common UFPS components. NOTE: 'vp_LocalPlayer' assumes that the supported components
	/// are not added or removed from the player at runtime: each player event handler and
	/// additionals components are cached only on startup and level load. NOTE: this method
	/// is called by vp_FPPlayerEventHandler on Awake, and OnLevelLoad, which should hopefully
	/// spare you from ever having to call it
	/// </summary>
	public static void Refresh()
	{

		// only refresh player if null, or if player is set but a new level has been loaded
		if (!((m_FPEventHandler == null) || (m_LevelOfLocalPlayer != vp_Gameplay.CurrentLevel)))
			return;	// if we return here, player seems up-to-date

		// we should most rarely end up here
		m_FPEventHandler = GameObject.FindObjectOfType(typeof(vp_FPPlayerEventHandler)) as vp_FPPlayerEventHandler;
		m_LevelOfLocalPlayer = ((m_FPEventHandler != null) ? vp_Gameplay.CurrentLevel : -99999);
		RefreshPlayerComponents();
	
	}


	/// <summary>
	/// re-caches all of the standard UFPS local player components
	/// </summary>
	private static void RefreshPlayerComponents()
	{

		m_FPCamera = null;
		m_FPController = null;
		m_FPInput = null;
		m_FPDamageHandler = null;
		m_FPWeaponHandler = null;
		m_Inventory = null;
		m_Respawner = null;
		m_FPBodyAnimator = null;
		m_RagdollHandler = null;
		m_FootFXHandler = null;

		if (m_FPEventHandler == null)
			return;

		m_FPCamera = m_FPEventHandler.GetComponentInChildren<vp_FPCamera>();
		m_FPController = m_FPEventHandler.GetComponentInChildren<vp_FPController>();
		m_FPInput = m_FPEventHandler.GetComponentInChildren<vp_FPInput>();
		m_FPDamageHandler = m_FPEventHandler.GetComponentInChildren<vp_FPPlayerDamageHandler>();
		m_FPWeaponHandler = m_FPEventHandler.GetComponentInChildren<vp_FPWeaponHandler>();
		m_Inventory = m_FPEventHandler.GetComponentInChildren<vp_PlayerInventory>();
		m_Respawner = m_FPEventHandler.GetComponentInChildren<vp_PlayerRespawner>();
		m_FPBodyAnimator = m_FPEventHandler.GetComponentInChildren<vp_FPBodyAnimator>();
		m_RagdollHandler = m_FPEventHandler.GetComponentInChildren<vp_RagdollHandler>();
		m_FootFXHandler = m_FPEventHandler.GetComponentInChildren<vp_PlayerFootFXHandler>();

	}
	

	/// <summary>
	/// retrieves the UFPS player event handler of the local player in the current level (if any)
	/// </summary>
	public static vp_FPPlayerEventHandler EventHandler
	{
		get
		{
			return m_FPEventHandler;
		}
		set
		{

			m_FPEventHandler = value;

            m_LevelOfLocalPlayer = SceneManager.GetActiveScene().buildIndex;

            RefreshPlayerComponents();

		}
	}


	/// <summary>
	/// retrieves the UFPS camera of the player in the current level (if any)
	/// </summary>
	public static vp_FPCamera Camera
	{
		get
		{
			return m_FPCamera;
		}
	}


	/// <summary>
	/// retrieves the UFPS controller of the player in the current level (if any)
	/// </summary>
	public static vp_FPController Controller
	{
		get
		{
			return m_FPController;
		}
	}


	/// <summary>
	/// retrieves the UFPS input manager of the player in the current level (if any)
	/// </summary>
	public static vp_FPInput InputManager
	{
		get
		{
			return m_FPInput;
		}
	}

	
	/// <summary>
	/// retrieves the UFPS damage handler of the player in the current level (if any)
	/// </summary>
	public static vp_FPPlayerDamageHandler DamageHandler
	{
		get
		{
			return m_FPDamageHandler;
		}
	}
		

	/// <summary>
	/// retrieves the UFPS weapon handler of the player in the current level (if any)
	/// </summary>
	public static vp_FPWeaponHandler WeaponHandler
	{
		get
		{
			return m_FPWeaponHandler;
		}
	}


	/// <summary>
	/// retrieves the UFPS inventory of the player in the current level (if any)
	/// </summary>
	public static vp_PlayerInventory Inventory
	{
		get
		{
			return m_Inventory;
		}
	}


	/// <summary>
	/// retrieves the UFPS respawner of the player in the current level (if any)
	/// </summary>
	public static vp_PlayerRespawner Respawner
	{
		get
		{
			return m_Respawner;
		}
	}


	/// <summary>
	/// retrieves the UFPS body animator of the player in the current level (if any)
	/// </summary>
	public static vp_FPBodyAnimator BodyAnimator
	{
		get
		{
			return m_FPBodyAnimator;
		}
	}
	

	/// <summary>
	/// retrieves the UFPS ragdoll handler of the player in the current level (if any)
	/// </summary>
	public static vp_RagdollHandler RagdollHandler
	{
		get
		{
			return m_RagdollHandler;
		}
	}
	
	
	/// <summary>
	/// retrieves the UFPS foot fx handler of the player in the current level (if any)
	/// </summary>
	public static vp_PlayerFootFXHandler FootFXHandler
	{
		get
		{
			return m_FootFXHandler;
		}
	}


	/// <summary>
	/// gets or sets the world position of the player. NOTE: does not stop the player if moving
	/// </summary>
	public static Vector3 Position
	{
		get
		{
			return m_FPEventHandler.Position.Get();
		}
		set
		{
			m_FPEventHandler.Position.Set(value);
		}
	}


	/// <summary>
	/// gets the transform of the solid object that the player is currently
	/// standing on (if any). can be the same as 'Platform'. null if the player
	/// is jumping or falling. TIP: use 'IsGrounded' to simply check for ground
	/// contact
	/// </summary>
	public static Transform Ground
	{
		get
		{
			if(m_FPController == null)
				return null;
			return m_FPController.GroundTransform;
		}

	}

	
	/// <summary>
	/// gets the transform of the platform that the player is currently riding
	/// (if any). might be the same as 'Ground'. returns null if the player is not
	/// standing on a platform. TIP: in UFPS, a platform is defined as a solid object
	/// that has the 'MovingPlatform' (28) layer (and usually also a vp_MovingPlatform
	/// script)
	/// </summary>
	public static Transform Platform
	{
		get
		{
			return m_FPEventHandler.Platform.Get();
		}

	}


	/// <summary>
	/// disconnects the player controller from the moving platform it's currently
	/// riding on (if any). if the platform is moving, this will make the player
	/// slide off of it. TIP: use 'AddForce' right afterwards to fling the player
	/// off the platform in a particluar direction
	/// </summary>
	public static void DismountPlatform()
	{

		m_FPEventHandler.Platform.Set(null);

	}


	/// <summary>
	/// returns the world transform that the player is looking at, within a max range of 2
	/// meters. returns null if looking into empty space, or if the object has no collider.
	/// TIPS: 1) this can be used to highlight objects that the player is looking at. 2)
	/// use 'GetLookTransform' to set a custom max range (may be more useful for VR and
	/// third person)
	/// </summary>
	public static Transform LookTransform
	{
		get
		{
			return GetLookTransform(2);
		}
	}


	/// <summary>
	/// returns the world transform that the player is looking at, or null if looking
	/// into empty space, or if the object has no collider. this will not return transforms
	/// positioned inbetween the camera and the local player (if in third person mode). if
	/// 'layerMask' is not provided, then 'vp_Layer.Mask.ExternalBlockers' will be used.
	/// TIP: this can be used to highlight objects that the player is looking at
	/// </summary>
	public static Transform GetLookTransform(float maxRange, int layerMask = -1)
	{

		if (m_FPCamera == null)
			return null;

		return m_FPCamera.GetLookTransform(maxRange);

	}


	/// <summary>
	/// gets or sets the world rotation of the player. the controller will
	/// rotate around the Y vector. the camera will rotate around its X and
	/// Y vector, subject to pitch and yaw limits. any body animator will
	/// follow suit: the head will rotate along with the camera. the body
	/// and feet will only rotate and move if the new direction diverts from
	/// the previous controller forward direction by more than 90 degrees
	/// </summary>
	public static Vector2 Rotation
	{
		get
		{
			return m_FPEventHandler.Rotation.Get();
		}
		set
		{
			m_FPEventHandler.Rotation.Set(value);
		}
	}


	/// <summary>
	/// returns the direction between the player model's head and the
	/// look point. NOTE: _not_ necessarily the direction between the
	/// camera and the look point (it might be a 3rd person camera)
	/// </summary>
	public static Vector3 BodyHeadLookDirection
	{
		get
		{
			return m_FPEventHandler.HeadLookDirection.Get();
		}
	}


	/// <summary>
	/// returns the direction between the weapon model and the look point.
	/// in 1st person, this is identical to 'BodyHeadLookDirection'. in 3rd
	/// person it is not
	/// </summary>
	public static Vector3 AimDirection
	{
		get
		{
			return m_FPEventHandler.AimDirection.Get();
		}
	}


	/// <summary>
	/// gets or sets the current player world velocity. the velocity returned is
	/// calculated in the same way as that of Unity's CharacterController. NOTE:
	/// setting this value won't have any effect on player position, it will only
	/// affect internal states such as animation speeds (!). to add physics velocity
	/// to the player, use 'AddForce'. to forcibly move the player to a position,
	/// use the 'Position' property. to stop the player, use the 'Stop' method. to
	/// relocate, rotate and stop the player, use one of the 'Teleport' methods
	/// </summary>
	public static Vector3 Velocity
	{
		get
		{
			return m_FPEventHandler.Velocity.Get();
		}
		set
		{
			m_FPEventHandler.Velocity.Set(value);
		}
	}


	/// <summary>
	/// returns the overall world velocity of the player controller
	/// </summary>
	public static float VelocityMagnitude
	{
		get
		{
			return m_FPEventHandler.Velocity.Get().magnitude;
		}
	}
	

	/// <summary>
	/// gets or sets player health. the health is not allowed to go above
	/// MaxHealth, but negative health is allowed (for things like gibbing)
	/// </summary>
	public static float Health
	{
		get
		{
			return m_FPEventHandler.Health.Get();
		}
		set
		{
			m_FPEventHandler.Health.Set(value);
		}
	}


	/// <summary>
	/// gets or sets the player's max health. if the value is lower than
	/// CurrentHealth, then that will be capped to the new MaxHealth
	/// </summary>
	public static float MaxHealth
	{
		get
		{
			return m_FPEventHandler.MaxHealth.Get();
		}
		set
		{
			m_FPEventHandler.MaxHealth.Set(value);
		}
	}



	/// <summary>
	/// returns the chronological index of the currently wielded 1st person weapon
	/// gameobject as ordered under the main FPSCamera gameobject
	/// </summary>
	public static int CurrentWeaponIndex
	{
		get
		{
			return m_FPEventHandler.CurrentWeaponIndex.Get();
		}
	}


	/// <summary>
	/// returns the gameobject name of the currently wielded 1st person weapon
	/// gameobject as childed under the main FPSCamera gameobject
	/// </summary>
	public static string CurrentWeaponName
	{
		get
		{
			return m_FPEventHandler.CurrentWeaponName.Get();
		}
	}


	/// <summary>
	/// returns amount of ammo left in the currently wielded weapon
	/// (intended for HUD scripts).
	/// </summary>
	public static int CurrentAmmo
	{
		get
		{
			return m_FPEventHandler.CurrentWeaponAmmoCount.Get();
		}
	}


	/// <summary>
	/// returns max ammo of the currently wielded weapon (intended for
	/// HUD scripts)
	/// </summary>
	public static int CurrentMaxAmmo
	{
		get
		{
			return m_FPEventHandler.CurrentWeaponMaxAmmoCount.Get();
		}
	}


	/// <summary>
	/// returns extra 'backpack' ammo for the currently wielded weapon
	/// (intended for HUD scripts). NOTE: this method will only work with
	/// the UFPS inventory system
	/// </summary>
	public static int SpareAmmoForCurrentWeapon
	{
		get
		{
			if (m_Inventory == null)
				return 0;
			return m_Inventory.GetExtraAmmoForCurrentWeapon();
		}
	}
	
	
	/// <summary>
	/// returns the Texture2D icon of the ItemType of the currently wielded weapon's
	/// ammo type. this is intended for HUD scripts. if using the UFPS inventory system,
	/// this texture is set on the vp_UnitType scriptable object for the ammo
	/// </summary>
	public static Texture2D CurrentAmmoIcon
	{
		get
		{
			return m_FPEventHandler.CurrentAmmoIcon.Get();
		}
	}
	

	/// <summary>
	/// always returns true if the player is in 1st person mode, and false
	/// in 3rd person
	/// </summary>
	public static bool IsFirstPerson
	{
		get
		{
			return m_FPEventHandler.IsFirstPerson.Get();
		}
	}


	/// <summary>
	/// returns true if the local player controller is standing on a solid
	/// object, false if not
	/// </summary>
	public static bool IsGrounded
	{
		get
		{
			return m_FPEventHandler.Grounded.Get();
		}
	}


	/// <summary>
	/// returns true if the local player is the master of an ongoing multiplayer
	/// session. always returns true in singleplayer. NOTE: this is a functionality
	/// of the 'UFPS Photon Multiplayer Starter Kit'. manipulate the value using the
	/// 'vp_Gameplay' class in case you wish to hook it into another network layer
	/// </summary>
	public static bool IsMaster
	{
		get
		{
			return vp_Gameplay.IsMaster;
		}
	}


	/// <summary>
	/// returns the player collider, most likely a Unity CharacterController
	/// </summary>
	public static Collider Collider
	{
		get
		{
			if (m_FPController == null)
				return null;
			return (m_FPController as vp_Controller).Collider;
		}
	}
	

	/// <summary>
	/// if the local player has an animated body hierarchy, returns the transform
	/// of the head bone set on the vp_FPBodyAnimator component. otherwise returns
	/// the transform of the main 1st person camera
	/// </summary>
	public static Transform Head
	{
		get
		{

			if (m_FPBodyAnimator != null)
				return m_FPBodyAnimator.HeadBone.transform;

			if (m_FPCamera != null)
				return m_FPCamera.transform;

			return null;
		}
	}


	/// <summary>
	/// if the player has a foot fx handler, returns the transform of the gameobject
	/// set in its 'Footsteps -> LeftFoot' slot. otherwise returns the transform of
	/// the player controller
	/// </summary>
	public static Transform LeftFoot
	{
		get
		{
			if ((m_FootFXHandler != null) && (m_FootFXHandler.FootLeft != null))
				return m_FootFXHandler.FootLeft.transform;
			if (m_FPController != null)
				return m_FPController.transform;
			return null;
		}
	}


	/// <summary>
	/// if the player has a foot fx handler, returns the transform of the gameobject
	/// set in its 'Footsteps -> LeftFoot' slot. otherwise returns the transform of
	/// the player controller
	/// </summary>
	public static Transform RightFoot
	{
		get
		{
			if ((m_FootFXHandler != null) && (m_FootFXHandler.FootRight != null))
				return m_FootFXHandler.FootRight.transform;
			if (m_FPController != null)
				return m_FPController.transform;
			return null;
		}
	}


	/// <summary>
	/// this property allows you to change the death sound of the player at runtime
	/// </summary>
	public static AudioClip DeathSound
	{
		get
		{
			if (m_FPDamageHandler == null)
				return null;
			return m_FPDamageHandler.DeathSound;
		}
		set
		{
			if (m_FPDamageHandler == null)
				return;
			m_FPDamageHandler.DeathSound = value;
		}
	}


	/// <summary>
	/// this property allows you to change the type of ImpactEvent that the
	/// player sends when making footsteps. this makes it easy to change the
	/// footstep effects at runtime for different situations or states, such
	/// as 'Sneaking', 'Sprinting', 'Wet', 'Wounded' etc. NOTE: for info on
	/// how the surface effect system in UFPS works, please see the manual.
	/// </summary>
	public static vp_ImpactEvent FootstepFXImpactEvent
	{
		get
		{
			if (m_FootFXHandler == null)
				return null;
			return m_FootFXHandler.FootstepImpactEvent;
		}
		set
		{
			if (m_FootFXHandler == null)
				return;
			m_FootFXHandler.FootstepImpactEvent = value;
		}
	}


	/// <summary>
	/// this property allows you to change the type of ImpactEvent that the
	/// player sends when falling hard to the ground. this makes it easy to
	/// support different types of fall effects for different situations or
	/// states. NOTE: for info on how the surface effect system in UFPS works,
	/// please see the manual
	/// </summary>
	public static vp_ImpactEvent FallFXImpactEvent
	{
		get
		{
			if (m_FootFXHandler == null)
				return null;
			return m_FootFXHandler.FallImpactEvent;
		}
		set
		{
			if (m_FootFXHandler == null)
				return;
			m_FootFXHandler.FallImpactEvent = value;
		}
	}
		

	/// <summary>
	/// this property allows you to change the type of ImpactEvent that the player
	/// sends when jumping. this makes it easy to support different types of jump
	/// effects for different situations or states. NOTE: for info on how the surface
	/// effect system in UFPS works, please see the manual
	/// </summary>
	public static vp_ImpactEvent JumpFXImpactEvent
	{
		get
		{
			if (m_FootFXHandler == null)
				return null;
			return m_FootFXHandler.JumpImpactEvent;
		}
		set
		{
			if (m_FootFXHandler == null)
				return;
			m_FootFXHandler.JumpImpactEvent = value;
		}
	}


	/// <summary>
	/// applies damage to the player in simple float format, sends a damage
	/// flash message to the HUD and twists the camera briefly
	/// </summary>
	public static void Damage(float damage)
	{

		if (m_FPDamageHandler == null)
			return;

		m_FPDamageHandler.Damage(damage);

	}


	/// <summary>
	/// assembles damage in UFPS format from individual parameters and sends it to
	/// the player, resulting in damage, a HUD damage flash and brief camera twist
	/// NOTE: local player can't damage self with 'vp_DamageInfo.DamageType.Bullet'
	/// </summary>
	public static void Damage(float damage, Transform source, vp_DamageInfo.DamageType type = vp_DamageInfo.DamageType.Unknown)
	{

		if (m_FPDamageHandler == null)
			return;

		if ((source == m_FPDamageHandler.transform) && type == vp_DamageInfo.DamageType.Bullet)
			return;

		vp_DamageInfo damageInfo = new vp_DamageInfo(damage, source, type);

		m_FPDamageHandler.Damage(damageInfo);

	}


	/// <summary>
	/// applies damage to the player in UFPS 'vp_DamageInfo' format, sends a damage
	/// flash message to the HUD and twists the camera briefly
	/// NOTE: local player can't damage self with 'vp_DamageInfo.DamageType.Bullet'
	/// </summary>
	public static void Damage(vp_DamageInfo damageInfo)
	{

		if (m_FPDamageHandler == null)
			return;

		if ((damageInfo.Source == m_FPDamageHandler.transform) && damageInfo.Type == vp_DamageInfo.DamageType.Bullet)
			return;

		m_FPDamageHandler.Damage(damageInfo);

	}


	/// <summary>
	/// immediately kills the player and schedules a respawn. if 'painHUDIntensity'
	/// is positive, uses it to send a HUD damage flash. the required components are:
	/// vp_FPPlayerDamageHandler (for kill), vp_Respawner (for respawn) and vp_PainHUD
	/// (for HUD damage flash)
	/// </summary>
	public static void Die(float painHUDIntensity = 0.0f)
	{

		// can't kill dead players
		if (m_FPEventHandler.Dead.Active)
			return;

		// send the kill to player damage handler and respawner
		m_FPEventHandler.SendMessage("Die");

		// handle pain HUD fx
		if(painHUDIntensity <= 0.0f)
			m_FPEventHandler.HUDDamageFlash.Send(null);	// mute pain HUD fx
		else	// show pain HUD fx
			m_FPEventHandler.HUDDamageFlash.Send(new vp_DamageInfo(painHUDIntensity, null));

	}


	/// <summary>
	/// adds one frame of external force to the player controller, pushing
	/// it in the 'force' direction
	/// </summary>
	public static void AddForce(Vector3 force)
	{

		m_FPEventHandler.ForceImpact.Send(force);

	}


	/// <summary>
	/// makes the camera shake momentarily as if a bomb has gone off nearby.
	/// the default force will be 0.5f unless a 'force' value is provided.
	/// you may provide an AudioClip and AudioSource to also play a sound.
	/// if 'audioSource' is empty, the main camera's audio source will be used
	/// </summary>
	public static void CameraShake(float force = 0.5f, AudioClip audioClip = null, AudioSource audioSource = null)
	{
		
		if (m_FPCamera == null)
			return;

		m_FPEventHandler.CameraBombShake.Send(force);

		if (audioClip == null)
			return;

		if (audioSource == null)
			audioSource = m_FPCamera.Audio;

		if (audioSource == null)
			return;

		audioSource.Stop();
		audioSource.PlayOneShot(audioClip);

	}


	/// <summary>
	/// makes the camera shake momentarily as if a large dinosaur or mech is
	/// approaching. great for bosses! the default force will be 0.5f unless a
	/// 'force' value is provided. you may provide an AudioClip and AudioSource
	/// to also play a sound. if 'audioSource' is empty, the main camera's audio
	/// source will be used
	/// </summary>
	public static void GroundStomp(float force = 0.5f, AudioClip audioClip = null, AudioSource audioSource = null)
	{

		if(m_FPCamera == null)
			return;

		m_FPEventHandler.CameraGroundStomp.Send(force);

		if (audioClip == null)
			return;

		if (audioSource == null)
			audioSource = m_FPCamera.Audio;

		if (audioSource == null)
			return;

		audioSource.Stop();
		audioSource.PlayOneShot(audioClip);

	}


	/// <summary>
	/// forces the player controller to move according to the provided 'inputVector'
	/// in joystick format. that is: a Vector2 where X represents sideways and Y represents
	/// forward / backward motion with values ranging from -1.0 to 1.0. NOTE: this needs to
	/// be called every frame in 'LateUpdate()'
	/// </summary>
	public static void Move(Vector2 inputVector)
	{
		m_FPEventHandler.InputMoveVector.Set(inputVector);
	}


	/// <summary>
	/// stops the controller in one frame, killing all forces acting upon it, and snaps all
	/// camera motion and zoom to a halt. NOTE: if you are calling 'Move' every frame at the
	/// same time, this will not have effect
	/// </summary>
	public static void Stop()
	{

		m_FPEventHandler.Stop.Send();

	}


	/// <summary>
	/// moves the controller to the world 'position' and 'rotation' and stops it.
	/// NOTE: 'rotation.x' is camera pitch, and 'rotation.y' is camera yaw
	/// </summary>
	public static void Teleport(Vector3 position, Vector2 rotation)
	{

		Position = position;
		Rotation = rotation;
		Stop();

	}


	/// <summary>
	/// moves the controller to the world position (x, y, z), sets the camera angle
	/// according to 'pitch' and 'yaw' and stops all controller and camera motion
	/// </summary>
	public static void Teleport(float x, float y, float z, float pitch, float yaw)
	{

		Teleport(new Vector3(x, y, z), new Vector2(pitch, yaw));

	}


	/// <summary>
	/// moves the controller to the world 'position' and stops it. does not
	/// affect rotation
	/// </summary>
	public static void Teleport(Vector3 position)
	{

		Position = position;
		Stop();

	}


	/// <summary>
	/// moves the controller to the world position (x, y, z) and stops it.
	/// does not affect rotation
	/// </summary>
	public static void Teleport(float x, float y, float z)
	{

		Teleport(new Vector3(x, y, z));

	}


	/// <summary>
	/// snaps the player to a placement decided by the specified spawnpoint, taking into
	/// account the spawnpoint's radius, ground snap and random rotation settings. if the
	/// player has a vp_Respawner component, its 'ObstructionRadius' setting will be used
	/// for obstruction checking. otherwise the obstruction radius will be 1 meter.
	/// </summary>
	public static void Teleport(vp_SpawnPoint spawnPoint)
	{

		if(spawnPoint == null)
			return;

		float obstructionRadius = ((m_Respawner != null) ? m_Respawner.ObstructionRadius : 1.0f);

		vp_Placement placement = spawnPoint.GetPlacement(obstructionRadius);

		Position = placement.Position;
		Rotation = placement.Rotation.eulerAngles;
		Stop();

	}


	/// <summary>
	/// switches to the 1st person view
	/// </summary>
	public static void GoFirstPerson()
	{

		if (IsFirstPerson)
			return;

		m_FPEventHandler.CameraToggle3rdPerson.Send();

	}


	/// <summary>
	/// switches to the 3rd person view
	/// </summary>
	public static void GoThirdPerson()
	{

		if (!IsFirstPerson)
			return;

		m_FPEventHandler.CameraToggle3rdPerson.Send();

	}


	/// <summary>
	/// toggles between the 1st and 3rd person views
	/// </summary>
	public static void ToggleThirdPerson()
	{

		m_FPEventHandler.CameraToggle3rdPerson.Send();

	}


	/// <summary>
	/// toggles to the next weapon if currently allowed (present in inventory)
	/// otherwise attempts to skip past it (wield the weapon after it)
	/// </summary>
	public static bool SetNextWeapon()
	{

		return m_FPEventHandler.SetNextWeapon.Try();

	}


	/// <summary>
	/// toggles to the previous weapon if currently allowed (present in inventory)
	/// otherwise attempts to skip past it (wield the weapon before it)
	/// </summary>
	public static bool SetPrevWeapon()
	{

		return m_FPEventHandler.SetPrevWeapon.Try();

	}


	/// <summary>
	/// attempts to wield a weapon by the name of its 1st person weapon gameobject
	/// as childed under the main FPSCamera gameobject. will fail if no such gameobject
	/// exists, or if there is an inventory script preventing it
	/// </summary>
	public static bool SetWeaponByName(string gameObjectName)
	{

		return m_FPEventHandler.SetWeaponByName.Try(gameObjectName);

	}


	/// <summary>
	/// attempts to wield a weapon by the chronological index of its 1st person weapon
	/// gameobject as ordered under the main FPSCamera gameobject. will fail if no
	/// such gameobject exists, or if there is an inventory script preventing it
	/// </summary>
	public static bool SetWeaponByIndex(int index)
	{

		return m_FPEventHandler.SetWeapon.TryStart(index);

	}


	/// <summary>
	/// unwields the currently wielded weapon, leaving the player unarmed
	/// </summary>
	public static void UnwieldCurrentWeapon()
	{

		m_FPEventHandler.SetWeapon.TryStart(0);

	}


	/// <summary>
	/// re-enables gameplay input (moving, firing, jumping, reloading, crouching etc.)
	/// </summary>
	public static void EnableGameplayInput()
	{
		m_FPEventHandler.InputAllowGameplay.Set(true);
	}


	/// <summary>
	/// blocks the player from moving and sending gameplay input (moving, firing, jumping,
	/// reloading, crouching etc.) NOTE: this does not affect freelook, mouse cursor or GUI
	/// input. to disallow these, please see the various 'Mouse' and 'FreeLook' methods
	/// </summary>
	public static void DisableGameplayInput()
	{

		Stop();	// stop player or we might get stuck moving
		m_FPEventHandler.InputAllowGameplay.Set(false);

	}


	/// <summary>
	/// toggles 1st person gameplay input on and off (moving, firing, jumping, reloading,
	/// crouching etc.)  NOTE: this does not affect freelook, mouse cursor or GUI
	/// input. to toggle these, instead use 'ToggleMouseCursor' and 'ToggleFreeLook'
	/// </summary>
	public static void ToggleGameplayInput()
	{

		if (m_FPEventHandler.InputAllowGameplay.Get() == false)
			EnableGameplayInput();
		else
			DisableGameplayInput();

	}


	/// <summary>
	/// re-enables camera freelook
	/// </summary>
	public static void EnableFreeLook()
	{

		if (m_FPInput == null)
			return;

		if (m_FPInput.MouseLookSensitivity != Vector2.zero)
			return;

		m_FPInput.MouseLookSensitivity = m_MouseSensBackup;

	}


	/// <summary>
	/// disables camera freelook
	/// </summary>
	public static void DisableFreeLook()
	{

		if (m_FPInput == null)
			return;

		if (m_FPInput.MouseLookSensitivity == Vector2.zero)
			return;

		m_MouseSensBackup = m_FPInput.MouseLookSensitivity;
		m_FPInput.MouseLookSensitivity = Vector2.zero;

	}


	/// <summary>
	/// toggles camera freelook on and off
	/// </summary>
	public static void ToggleFreeLook()
	{

		if (m_FPInput.MouseLookSensitivity == Vector2.zero)
			EnableFreeLook();
		else
			DisableFreeLook();

	}

	
	/// <summary>
	/// restores the crosshair texture in case it has been previously hidden
	/// </summary>
	public static void ShowCrosshair()
	{

		if (CrosshairTexture != m_InvisibleTexture)
			return;

		if (m_CrosshairBackup == null)
			return;

		CrosshairTexture = m_CrosshairBackup;

	}


	/// <summary>
	/// backs up the current crosshair texture and replaces it with an invisible one
	/// </summary>
	public static void HideCrosshair()
	{

		if (CrosshairTexture == m_InvisibleTexture)
			return;

		m_CrosshairBackup = CrosshairTexture;
		Debug.Log(CrosshairTexture);

		CrosshairTexture = m_InvisibleTexture;

	}
	

	/// <summary>
	/// toggles crosshair visibility on / off
	/// </summary>
	public static void ToggleCrosshair()
	{

		if (CrosshairTexture == m_InvisibleTexture)
			ShowCrosshair();
		else
			HideCrosshair();

	}


	/// <summary>
	/// this property allows you to change the crosshair texture at runtime.
	/// TIP: use 'LookTransform' to detect what object the player is looking at and
	/// change the crosshair to an appropriate one
	/// </summary>
	public static Texture CrosshairTexture
	{

		get
		{
			return m_FPEventHandler.Crosshair.Get();
		}
		set
		{
			m_FPEventHandler.Crosshair.Set(value);
			if(value != m_InvisibleTexture)
				m_CrosshairBackup = value;
		}

	}


	/// <summary>
	/// disables freelook and firing, and enables the mouse cursor all over
	/// the screen, allowing GUI mouse input such as clicking buttons
	/// </summary>
	public static void ShowMouseCursor()
	{

		if (m_FPInput == null)
			return;

		m_FPInput.MouseCursorForced = true;
		m_FPInput.MouseCursorBlocksMouseLook = true;

	}


	/// <summary>
	/// enables the mouse cursor all over the screen and disables firing,
	/// while still allowing freelook and body rotation
	/// </summary>
	public static void ShowMouseCursorAndAllowMouseLook()
	{

		if (m_FPInput == null)
			return;

		m_FPInput.MouseCursorForced = true;
		m_FPInput.MouseCursorBlocksMouseLook = false;

	}



	/// <summary>
	/// hides the mouse cursor, disabling GUI mouse input. PLEASE NOTE:
	/// hiding the mouse cursor cleanly will only work in fullscreen mode.
	/// in the editor, and in windowed mode, the game window must
	/// receive a click to hide the mouse cursor
	/// </summary>
	public static void HideMouseCursor()
	{

		if (m_FPInput == null)
			return;

		m_FPInput.MouseCursorForced = false;
		m_FPInput.MouseCursorBlocksMouseLook = false;

	}


	/// <summary>
	/// toggles mouse cursor and freelook on and off. PLEASE NOTE: hiding
	/// the mouse cursor cleanly will only work in fullscreen mode.
	/// in the editor, and in windowed mode, the game window must receive
	/// a click to hide the mouse cursor
	/// </summary>
	public static void ToggleMouseCursor()
	{

		if (m_FPInput == null)
			return;

		if (m_FPInput.MouseCursorForced)
			HideMouseCursor();
		else
			ShowMouseCursor();

	}


}