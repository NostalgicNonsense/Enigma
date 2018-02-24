/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Weapon.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_Weapon : vp_Component
{

	// 3rd person weapon gameobject
	public GameObject Weapon3rdPersonModel = null;				// NOTE: this is always a GAMEOBJECT from the HIERARCHY (SCENE) and used to represent a vp_Weapon in 3rd person
	public Material Weapon3rdPersonInvisibleMaterial = null;	// used to make 3rd person weapons cast shadows even when they are invisible in the 1st person view. should be
																// set to an invisible, shadow casting material. see the included 'InvisibleShadowCaster' shader & material
	protected Material[] m_VisibleMaterials;					// for caching materials of the weapon on startup, so they can be switched on and off later
	protected Material[] m_InvisibleMaterials;
	protected GameObject m_WeaponModel = null;
	protected static vp_FPBodyAnimator m_SceneFPBodyAnimator = null;
	protected vp_FPBodyAnimator SceneFPBodyAnimator	// if invisible material is null, this is used to fetch an invisible shadowcaster material from the local player, if present
	{
		get
		{
			if (m_SceneFPBodyAnimator == null)
				m_SceneFPBodyAnimator = FindObjectOfType<vp_FPBodyAnimator>();
			return m_SceneFPBodyAnimator;
		}
	}

	// recoil position spring
	public Vector3 PositionOffset = new Vector3(0.15f, -0.15f, -0.15f);
	public float PositionSpring2Stiffness = 0.95f;
	public float PositionSpring2Damping = 0.25f;
	protected vp_Spring m_PositionSpring2 = null;		// spring for secondary forces like recoil (typically with stiffer spring settings)

	// recoil rotation spring
	public Vector3 RotationOffset = Vector3.zero;
	public float RotationSpring2Stiffness = 0.95f;
	public float RotationSpring2Damping = 0.25f;
	protected vp_Spring m_RotationSpring2 = null;		// spring for secondary forces like recoil (typically with stiffer spring settings)

	// weapon switching
	protected bool m_Wielded = true;
	public bool Wielded { get { return (m_Wielded && Rendering); } set { m_Wielded = value; } }

	// misc
	protected vp_Timer.Handle m_Weapon3rdPersonModelWakeUpTimer = new vp_Timer.Handle();
	
	// weapon info
	public int AnimationType = 1;
	public int AnimationGrip = 1;

	public new enum Type
	{
		Custom,
		Firearm,
		Melee,
		Thrown
	}

	public enum Grip
	{
		Custom,
		OneHanded,
		TwoHanded,
		TwoHandedHeavy
	}

#if UNITY_EDITOR
	protected bool m_AllowEditing = false;
	public bool AllowEditTransform
	{
		get
		{
			return m_AllowEditing;
		}
		set
		{
			m_AllowEditing = value;
		}
	}
#endif

	// event handler property cast as a playereventhandler
	protected vp_PlayerEventHandler m_Player = null;
	protected vp_PlayerEventHandler Player
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


	protected Renderer m_Weapon3rdPersonModelRenderer = null;
	public Renderer Weapon3rdPersonModelRenderer
	{
		get
		{
			if ((m_Weapon3rdPersonModelRenderer == null) && (Weapon3rdPersonModel != null))
				m_Weapon3rdPersonModelRenderer = Weapon3rdPersonModel.GetComponent<Renderer>();
			return m_Weapon3rdPersonModelRenderer;
		}
	}


	protected Vector3 m_RotationSpring2DefaultRotation = Vector3.zero;
	public Vector3 RotationSpring2DefaultRotation
	{
		get
		{
			return m_RotationSpring2DefaultRotation;
		}
		set
		{
			m_RotationSpring2DefaultRotation = value;
		}
	}


	/// <summary>
	/// in 'Awake' we do things that need to be run once at the
	/// very beginning. NOTE: as of Unity 4, gameobject hierarchy
	/// can not be altered in 'Awake'
	/// </summary>
	protected override void Awake()
	{
		
		base.Awake();

		RotationOffset = transform.localEulerAngles;
		PositionOffset = transform.position;

		Transform.localEulerAngles = RotationOffset;

		if (transform.parent == null) // TODO: or parent contains a vp_FPCamera
		{
			Debug.LogError("Error (" + this + ") Must not be placed in scene root. Disabling self.");
			vp_Utility.Activate(gameObject, false);
			return;
		}

		// disallow colliders on the weapon or we may get issues with
		// player collision
		if (GetComponent<Collider>() != null)
			GetComponent<Collider>().enabled = false;

#if UNITY_EDITOR
		m_AllowEditing = false;
#endif

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void OnEnable()
	{
		RefreshWeaponModel();
		base.OnEnable();
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void OnDisable()
	{

		RefreshWeaponModel();

		Activate3rdPersonModel(false);

		base.OnDisable();

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

		// setup the weapon springs
		m_PositionSpring2 = new vp_Spring(transform, vp_Spring.UpdateMode.PositionAdditiveSelf, true);
		m_PositionSpring2.MinVelocity = 0.00001f;

		m_RotationSpring2 = new vp_Spring(transform, vp_Spring.UpdateMode.RotationAdditiveGlobal);
		m_RotationSpring2.MinVelocity = 0.00001f;

		// snap the springs so they always start out rested & in the right place
		SnapSprings();
		Refresh();

		CacheRenderers();

		if (Player.IsLocal.Get())
			CacheMaterials();

	}


	/// <summary>
	/// 
	/// </summary>
	public Vector3 Recoil
	{
		get
		{
			return m_RotationSpring2.State;
		}
	}

	

	/// <summary>
	/// 
	/// </summary>
	protected override void FixedUpdate()
	{

		base.FixedUpdate();


		if (Time.timeScale == 0.0f)
			return;

		UpdateSprings();

	}


	/// <summary>
	/// applies positional and angular force to the weapon. the
	/// typical use for this method is applying recoil force.
	/// </summary>
	public virtual void AddForce2(Vector3 positional, Vector3 angular)
	{

		if (m_PositionSpring2 != null)
			m_PositionSpring2.AddForce(positional);

		if (m_RotationSpring2 != null)
			m_RotationSpring2.AddForce(angular);

	}
	

	/// <summary>
	/// 
	/// </summary>
	public virtual void AddForce2(float xPos, float yPos, float zPos, float xRot, float yRot, float zRot)
	{
		AddForce2(new Vector3(xPos, yPos, zPos), new Vector3(xRot, yRot, zRot));
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void UpdateSprings()
	{

		// NOTES:
		//	1) this version of the method should not be run for a 1st person weapon,
		//		only for multiplayer remote players and AI
		//	2) a 3rd person vp_Weapon is just an invisible, locigal entity,
		//		represented by its '3rdPersonWeapon' gameobject reference in the
		//		3d world. still, it's good to fix position and rotation so weapon does
		//		not recoil away into oblivion in 3rd person

		Transform.localPosition = Vector3.up;			// middle of player
		Transform.localRotation = Quaternion.identity;	// aiming head-on

		// update recoil springs for additive position and rotation forces
		m_PositionSpring2.FixedUpdate();	// TODO: only in 1st person
		m_RotationSpring2.FixedUpdate();
		
	}


	/// <summary>
	/// this method is called to reset various weapon settings,
	/// typically after creating or loading a weapon
	/// </summary>
	public override void Refresh()
	{

		if (!Application.isPlaying)
			return;

		if (m_PositionSpring2 != null)
		{
			m_PositionSpring2.Stiffness =
				new Vector3(PositionSpring2Stiffness, PositionSpring2Stiffness, PositionSpring2Stiffness);
			m_PositionSpring2.Damping = Vector3.one -
				new Vector3(PositionSpring2Damping, PositionSpring2Damping, PositionSpring2Damping);
			m_PositionSpring2.RestState = Vector3.zero;
		}

		if (m_RotationSpring2 != null)
		{
			m_RotationSpring2.Stiffness =
				new Vector3(RotationSpring2Stiffness, RotationSpring2Stiffness, RotationSpring2Stiffness);
			m_RotationSpring2.Damping = Vector3.one -
				new Vector3(RotationSpring2Damping, RotationSpring2Damping, RotationSpring2Damping);
			m_RotationSpring2.RestState = m_RotationSpring2DefaultRotation;
		}

	}


	/// <summary>
	/// performs special activation logic for wielding a weapon
	/// properly
	/// </summary>
	public override void Activate()
	{

		base.Activate();

		m_Wielded = true;
		Rendering = true;

	}


	/// <summary>
	/// resets all the springs to their default positions, i.e.
	/// for when loading a new camera or switching a weapon
	/// </summary>
	public virtual void SnapSprings()
	{

		if (m_PositionSpring2 != null)
		{
			m_PositionSpring2.RestState = Vector3.zero;
			m_PositionSpring2.State = Vector3.zero;
			m_PositionSpring2.Stop(true);
		}

		if (m_RotationSpring2 != null)
		{
			m_RotationSpring2.RestState = m_RotationSpring2DefaultRotation;
			m_RotationSpring2.State = m_RotationSpring2DefaultRotation;
			m_RotationSpring2.Stop(true);
		}

	}


	/// <summary>
	/// stops all the springs
	/// </summary>
	public virtual void StopSprings()
	{

		if (m_PositionSpring2 != null)
			m_PositionSpring2.Stop(true);

		if (m_RotationSpring2 != null)
			m_RotationSpring2.Stop(true);

	}



	/// <summary>
	/// transitions the weapon into / out of view depending on
	/// the 'active' parameter, playing wield or unwield sounds
	/// and animations accordingly
	/// </summary>
	public virtual void Wield(bool isWielding = true)
	{


		m_Wielded = isWielding;

		Refresh();
		StateManager.CombineStates();

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void RefreshWeaponModel()
	{

		if (Player == null)
			return;

		// don't touch event handler events if they haven't been initialized yet
		if (Player.IsFirstPerson == null)
			return;

		// toggle 1st person model - but don't disable it or its
		// renderer - since other logics depend on these
		Transform.localScale = (Player.IsFirstPerson.Get() ? Vector3.one : Vector3.zero);

		// toggle 1st / 3rd person weapon model depending on camera mode
		Activate3rdPersonModel(!Player.IsFirstPerson.Get());

		// always disable 3rd person model if it has been unwielded
		if (Player.CurrentWeaponName.Get() != name)
			Activate3rdPersonModel(false);

	}


	/// <summary>
	/// this method enables and (more importantly) disables the 3rd
	/// person weapon model (if any) in a way that doesn't interfere
	/// with other scripts on their Awake
	/// </summary>
	protected virtual void Activate3rdPersonModel(bool active = true)
	{

		// nothing more to do here if we have no 3rd person weapon model
		if (Weapon3rdPersonModel == null)
			return;

		if (active
			|| (Player.IsLocal.Get() && (Player.CurrentWeaponName.Get() == name))	// always render in case of a local player 3rd person dummy weapon, for shadow rendering
			)
		{
			if(Weapon3rdPersonModelRenderer != null)
				Weapon3rdPersonModelRenderer.enabled = true;
			vp_Utility.Activate(Weapon3rdPersonModel, true);
		}
		else
		{
			if (Weapon3rdPersonModelRenderer != null)
				Weapon3rdPersonModelRenderer.enabled = false;

			// allow scripts on the target gameobject some time to wake up properly.
			// this is mainly necessary since the first thing that happens when this
			// script wakes up is the deactivation of 3rd person weapon objects, which
			// might confuse their Awake logic
			vp_Timer.In(0.1f, delegate()
			{
				if (Weapon3rdPersonModel != null)
					vp_Utility.Activate(Weapon3rdPersonModel, false);
			}, m_Weapon3rdPersonModelWakeUpTimer);	// this timer handle is important to properly disable timer on application quit / level load
		}

		if(Player.IsLocal.Get())
			RefreshMaterials(active);

	}


	/// <summary>
	/// for 3d person dummy weapon shadow rendering. caches the materials on the
	/// model on Awake. the materials are sorted into four arrays to be swapped
	/// off and onto the model at runtime depending on the current gameplay situation
	/// </summary>
	protected virtual void CacheMaterials()
	{

		// cache default materials
		if (Weapon3rdPersonModelRenderer == null)
			return;

		m_VisibleMaterials = Weapon3rdPersonModelRenderer.materials;

		// cache invisible material
		if (Weapon3rdPersonInvisibleMaterial == null)
		{
			// if invisible material is not set, take a shot that the first person
			// player has one that will work
			if (SceneFPBodyAnimator != null)
				Weapon3rdPersonInvisibleMaterial = m_SceneFPBodyAnimator.InvisibleMaterial;
		}

		m_InvisibleMaterials = new Material[Weapon3rdPersonModelRenderer.materials.Length];

		for (int v = 0; v < Weapon3rdPersonModelRenderer.materials.Length; v++)
			m_InvisibleMaterials[v] = Weapon3rdPersonInvisibleMaterial;

		if (Weapon3rdPersonInvisibleMaterial == null)
		{
			Debug.LogWarning("Warning (" + this + ") No weapon invisible material has been set. This weapon will cast no 3rd person shadow.");
			return;
		}

		RefreshMaterials();

	}



	/// <summary>
	/// for 3d person dummy weapon shadow rendering. swaps the dummy weapon model's
	/// material array for a regular one, versus an invisible one that only renders
	/// a shadow
	/// </summary>
	public void RefreshMaterials(bool visible = true)
	{

		if (Weapon3rdPersonInvisibleMaterial == null)
			return;

		if (Weapon3rdPersonModelRenderer == null)
			return;

		if (Weapon3rdPersonModelRenderer.materials == null)
			return;

		if (m_VisibleMaterials == null)
			return;

		if (m_InvisibleMaterials == null)
			return;

		if (visible)
			Weapon3rdPersonModelRenderer.materials = m_VisibleMaterials;		// 3rd person: render dummy weapon normally
		else
			Weapon3rdPersonModelRenderer.materials = m_InvisibleMaterials;		// 1st person: render dummy weapon invisible, but casting a shadow

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Dead()
	{
		
		if (Player.IsFirstPerson.Get())
			return;

		Rendering = false;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_Dead()
	{


		if (Player.IsFirstPerson.Get())
			return;

		Rendering = true;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual Vector3 OnValue_AimDirection
	{

		get
		{

			return (Weapon3rdPersonModel.transform.position - Player.LookPoint.Get()).normalized;

		}

	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual bool CanStart_Zoom()
	{

		if (Player.CurrentWeaponType.Get() == (int)vp_Weapon.Type.Melee)
			return false;

		return true;

	}


}


