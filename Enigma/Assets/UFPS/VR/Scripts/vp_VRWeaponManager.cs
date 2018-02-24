/////////////////////////////////////////////////////////////////////////////////
//
//	vp_VRWeaponManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this optional script can be used to adapt UFPS vp_FPWeapons
//					for VR camera use. it is intended only for games without hand
//					tracking (so gamepad or keyboard-&-mouse only). the script
//					keeps the weapon pointing in the camera direction, imposes
//					weapon sway from input and headset rotation, and prevents the
//					weapon from rendering at extreme angles where it would otherwise
//					rotate crazily.
//
//					USAGE:
//						1) make sure you have a vp_VRCameraManager in the scene
//						2) add this component to the same gameobject as the
//							vp_VRCameraManager
//
//					NOTES:
//						- this script forces the shooter to use the camera position for
//							spawning projectiles. also, it disables the camera recoil of
//							the shooter, and lookdown feature of the weapon
//						- the script makes a backup of all the settings on the weapon and
//							shooter that it manipulates, for restoration when disabled.
//							this is to allow for toggling the VR feature set on/off (even at
//							runtime) without any changes to the player hierarchy.
//
//					COMPATIBILITY: this script has been verified to work in Unity 5.3.4f1,
//						and might not work 100% in earlier versions of Unity
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

public class vp_VRWeaponManager : MonoBehaviour
{

	[Range(45, 90)]
	public float RenderPitchLimit = 70.0f;		// the weapon will be made invisible when looking up or down this many degrees, to prevent it from behaving erratically
	[Range(0, 10)]
	public float HeadLookSway = 2.0f;           // how much should the weapon sway behind when looking around using headlook? (generic for all weapons) 
	[Range(0, 1)]
	public float InputSway = 0.25f;				// how much should the weapon sway behind (horizontally) when looking around using gamepad / mouse? (multiplies the FPWeapon's 'RotationLookSway.Y') NOTE: does not work if the VRCameraManager is set to use 'SnapRotate'
	[Range(0, 3)]
	public float ForcedRetraction = 0.0f;		// if zero, does nothing. if more than zero, forces the current weapon's 'RetractionDistance' to this number (same for all weapons). NOTE: if you want to set this individually for each weapon, do so on the weapon components (and state presets, if any), but keep in mind that this will affect the desktop mode as well

	// state
	protected float m_CurrentSwayAmount = 0.0f;
	protected Vector3 RotationLastFrame = Vector3.zero;
	protected float m_NextAllowedSwayTime = 0.0f;
	private const float SWAY_MULTIPLIER = 0.075f;

	// custom VR shooter func
	protected vp_Shooter.FirePositionFunc VRFirePositionFunc;
	protected vp_Shooter.FireRotationFunc VRFireRotationFunc;

	// weapon settings cache
	protected Dictionary<vp_FPWeapon, bool> m_LookDownBackups = new Dictionary<vp_FPWeapon, bool>();
	protected Dictionary<vp_FPWeapon, float> m_RetractionBackups = new Dictionary<vp_FPWeapon, float>();
	protected Dictionary<vp_FPWeaponShooter, Vector2> m_ShooterCameraRecoilBackups = new Dictionary<vp_FPWeaponShooter, Vector2>();
	protected Dictionary<vp_Shooter, vp_Shooter.FirePositionFunc> m_FirePositionFuncBackups = new Dictionary<vp_Shooter, vp_Shooter.FirePositionFunc>();
	protected Dictionary<vp_Shooter, vp_Shooter.FireRotationFunc> m_FireRotationFuncBackups = new Dictionary<vp_Shooter, vp_Shooter.FireRotationFunc>();

	protected vp_VRCameraManager m_VRCameraManager = null;
	protected vp_VRCameraManager VRCamera
	{
		get
		{
			if (m_VRCameraManager == null)
				m_VRCameraManager = transform.GetComponent<vp_VRCameraManager>();
			return m_VRCameraManager;
		}
	}

	protected vp_WeaponHandler m_WeaponHandler = null;
	protected vp_WeaponHandler WeaponHandler
	{
		get
		{
			if (m_WeaponHandler == null)
				m_WeaponHandler = GameObject.FindObjectOfType<vp_WeaponHandler>();
			return m_WeaponHandler;
		}
	}

	protected vp_FPPlayerEventHandler m_FPPlayer = null;
	protected vp_FPPlayerEventHandler FPPlayer
	{
		get
		{
			if (m_FPPlayer == null)
				m_FPPlayer = GameObject.FindObjectOfType<vp_FPPlayerEventHandler>();
			return m_FPPlayer;
		}
	}

	protected vp_FPCamera m_FPCamera = null;
	protected vp_FPCamera FPCamera
	{
		get
		{
			if (m_FPCamera == null)
				m_FPCamera = GameObject.FindObjectOfType<vp_FPCamera>();
			return m_FPCamera;
		}
	}

	protected vp_Shooter m_CurrentShooter = null;
	protected vp_Shooter CurrentShooter
	{
		get
		{

			if (FPPlayer == null)
				return null;
			if (FPPlayer.SetWeapon == null)
				return null;
			if (FPPlayer.SetWeapon.Active)
			{
				m_CurrentShooter = null;
			}
			if (WeaponHandler == null)
				return null;
			if (WeaponHandler.CurrentWeapon == null)
				return null;
			if (m_CurrentShooter == null)
				m_CurrentShooter = WeaponHandler.CurrentShooter;
			return m_CurrentShooter;

		}
	}

	protected vp_FPWeapon CurrentFPWeapon
	{
		get
		{
			if (WeaponHandler == null)
				return null;
			if (WeaponHandler.CurrentWeapon == null)
				return null;
			return (WeaponHandler.CurrentWeapon as vp_FPWeapon);
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{
		VRFirePositionFunc = () => { return FPCamera.transform.position; };
		VRFireRotationFunc = () => { return FPCamera.transform.rotation; };
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		if (FPPlayer != null)
			FPPlayer.Register(this);

		m_CurrentShooter = null;
		VRFirePositionFunc = () => { return FPCamera.transform.position; };
		VRFireRotationFunc = () => { return FPCamera.transform.rotation; };

		BackupWeaponSettings();

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		if (FPPlayer != null)
			FPPlayer.Unregister(this);

		RestoreWeaponSettings(CurrentShooter);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		if (FPPlayer == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a vp_FPPlayer component to be present in the scene (disabling self).");
			enabled = false;
			return;
		}

		if (VRCamera == null)
		{
			Debug.LogError("Error (" + this + ") This component must sit on the same transform as a vp_VRCameraManager component (disabling self).");
			enabled = false;
			return;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void LateUpdate()
	{

		UpdateForcedValues();

		UpdateVisibility();

		UpdateSway();

	}


	/// <summary>
	/// disables the camera recoil of the shooter and lookdown feature of the weapon
	/// since these can be nauseating / disorienting
	/// </summary>
	protected virtual void UpdateForcedValues()
	{

		vp_FPWeaponShooter fpShooter = (CurrentShooter as vp_FPWeaponShooter);
		if (fpShooter != null)
			fpShooter.MotionPositionRecoilCameraFactor = fpShooter.MotionRotationRecoilCameraFactor = 0.0f;

		if (CurrentShooter != null)
		{
			CurrentShooter.GetFirePosition = VRFirePositionFunc;
			CurrentShooter.GetFireRotation = VRFireRotationFunc;
		}

		if (CurrentFPWeapon != null)
		{
			CurrentFPWeapon.LookDownActive = false;
			if(CurrentFPWeapon.Renderers[0] != null)
				CurrentFPWeapon.Renderers[0].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			if (ForcedRetraction != 0.0f)
				CurrentFPWeapon.RetractionDistance = ForcedRetraction;
		}

	}


	/// <summary>
	/// makes the weapon invisible when camera is upside down or looking up or
	/// down by more than 'RenderPitchLimit' degrees or if , to prevent weapon
	/// from flipping out
	/// </summary>
	protected virtual void UpdateVisibility()
	{

		if (CurrentFPWeapon == null)
			return;

		// make weapon invisible if camera is upside down or beyond the pitch limit
		bool upsideDown = (Vector3.Dot(Camera.main.transform.up, Vector3.down) > 0);
		if (upsideDown || (FPPlayer.Rotation.Get().x > RenderPitchLimit) || (FPPlayer.Rotation.Get().x < -RenderPitchLimit))
			SetVisible(false);
		else if (!(CurrentFPWeapon.Renderers[0].transform.localScale == Vector3.one))
		{
			// make weapon visible again as soon as allowed
			if (Time.time > m_NextAllowedSwayTime)
				SetVisible();
		}

	}


	/// <summary>
	/// calculates and imposes weapon sway from headset rotation (as opposed to
	/// regular input rotation)
	/// </summary>
	protected virtual void UpdateSway()
	{

		// abort if we can't sway right now
		if (Time.time < m_NextAllowedSwayTime)
		{
			RotationLastFrame = FPCamera.Transform.eulerAngles;
			return;
		}

		// calculate our own lookdown variable
		float lookDown = Mathf.Max(0.0f, ((FPPlayer.Rotation.Get().x - 45) / 45.0f));
		lookDown = 1.0f - Mathf.SmoothStep(0, 1, lookDown);

		// if we have a current weapon and are not rotating by regular input
		// (as opposed to by head tracking)
		if ((CurrentFPWeapon != null) && (FPPlayer.InputSmoothLook.Get() == Vector2.zero))
		{

			// calculate this frame's difference in headset orientation
			Vector3 angleDif = (RotationLastFrame - FPCamera.Transform.eulerAngles);
			angleDif.z = 0;

			// clamp angles
			angleDif.y = angleDif.y < -350.0f ? angleDif.y + 360.0f : angleDif.y;
			angleDif.y = angleDif.y > 350.0f ? angleDif.y - 360.0f : angleDif.y;
			angleDif.x = angleDif.x < -350.0f ? angleDif.x + 360.0f : angleDif.x;
			angleDif.x = angleDif.x > 350.0f ? angleDif.x - 360.0f : angleDif.x;
			if (angleDif.x < 90 && angleDif.x > 70)
				angleDif.x = 70;
			else if (angleDif.x > 90 && angleDif.x < 280)
				angleDif.x = 280;

			// calculate sway
			m_CurrentSwayAmount = Mathf.SmoothStep(m_CurrentSwayAmount, angleDif.magnitude, SWAY_MULTIPLIER) * lookDown;

			m_CurrentSwayAmount += FPPlayer.InputSmoothLook.Get().x * 1000;

			// impose sway from headlook only
			CurrentFPWeapon.AddForce(Vector3.zero, angleDif * Time.deltaTime * m_CurrentSwayAmount * HeadLookSway);


		}
		else if(!VRCamera.SnapRotation.SnapRotate)
		{

			// get sway from gamepad / mouse horizontal input
			m_CurrentSwayAmount = FPPlayer.InputSmoothLook.Get().x;

			// impose sway from input
			if (CurrentFPWeapon != null)
				CurrentFPWeapon.AddForce(Vector3.zero, Vector3.up * Time.deltaTime * m_CurrentSwayAmount * -CurrentFPWeapon.RotationLookSway.y * InputSway);

		}


		// store this frame's rotation for later
		RotationLastFrame = FPCamera.Transform.eulerAngles;

	}


	/// <summary>
	/// makes the weapon visible or invisible by setting its renderer's
	/// scales to zero (this does not interfere with other systems that
	/// may care about the renderers)
	/// </summary>
	protected virtual void SetVisible(bool visible = true)
	{

		if (CurrentFPWeapon == null)
			return;

		for (int v = 0; v < CurrentFPWeapon.Renderers.Count; v++)
		{
			if (CurrentFPWeapon.Renderers[v] != null)
			{
				if (visible)
					CurrentFPWeapon.Renderers[v].transform.localScale = Vector3.one;
				else
					CurrentFPWeapon.Renderers[v].transform.localScale = Vector3.zero;
			}
		}

	}


	/// <summary>
	/// makes a backup of all the settings on the weapon and shooter that this
	/// component manipulates, for later restoration in 'RestoreWeaponSettings'
	/// </summary>
	protected virtual void BackupWeaponSettings()
	{

		if (WeaponHandler == null)
			return;

		foreach (vp_Weapon weapon in WeaponHandler.Weapons)
		{
			if (weapon == null)
				continue;

			// backup weapon lookdown status
			if (weapon is vp_FPWeapon)
			{
				vp_FPWeapon fpWeapon = (weapon as vp_FPWeapon);
				if (!m_LookDownBackups.ContainsKey(fpWeapon))
					m_LookDownBackups.Add(fpWeapon, fpWeapon.LookDownActive);
				if (!m_RetractionBackups.ContainsKey(fpWeapon))
					m_RetractionBackups.Add(fpWeapon, fpWeapon.RetractionDistance);
			}

			// backup shooter fire position
#if UNITY_5_3_OR_NEWER
			vp_Shooter shooter = weapon.GetComponentInChildren<vp_Shooter>(true);	// IMPORTANT: this works in Unity 5.3.4f1, but is not guaranteed to work in earlier versions
#else
			vp_Shooter shooter = weapon.GetComponentInChildren<vp_Shooter>();
#endif
			if (shooter == null)
				continue;
			if ((!(shooter.GetFirePosition == null)) && (!m_FirePositionFuncBackups.ContainsKey(shooter)))
				m_FirePositionFuncBackups.Add(shooter, shooter.GetFirePosition);
			if ((!(shooter.GetFireRotation == null)) && (!m_FireRotationFuncBackups.ContainsKey(shooter)))
				m_FireRotationFuncBackups.Add(shooter, shooter.GetFireRotation);

			// backup 
			if (shooter is vp_FPWeaponShooter)
			{
				vp_FPWeaponShooter ws = (shooter as vp_FPWeaponShooter);
				if (!m_ShooterCameraRecoilBackups.ContainsKey(ws))
					m_ShooterCameraRecoilBackups.Add(ws, new Vector2(ws.MotionPositionRecoilCameraFactor, ws.MotionRotationRecoilCameraFactor));
			}

			Renderer r = weapon.GetComponentInChildren<Renderer>();
			if (r != null)
				r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		}

	}


	/// <summary>
	/// restores all the settings on the weapon and shooter from the backup
	/// made by 'BackupWeaponSettings'. this is to allow for toggling the VR
	/// feature set on and off at runtime
	/// </summary>
	protected virtual void RestoreWeaponSettings(vp_Shooter shooter)
	{

		if (shooter == null)
			return;

		// restore weapon lookdown status
		vp_FPWeapon fpWeapon = shooter.GetComponent<vp_FPWeapon>();
		if (fpWeapon != null)
		{
			bool lookDown;
			if (m_LookDownBackups.TryGetValue(fpWeapon, out lookDown))
				fpWeapon.LookDownActive = lookDown;
			float retraction;
			if (m_RetractionBackups.TryGetValue(fpWeapon, out retraction))
				fpWeapon.RetractionDistance = retraction;
		}

		// restore shooter fire position
		vp_Shooter.FirePositionFunc pf;
		if (m_FirePositionFuncBackups.TryGetValue(shooter, out pf))
			shooter.GetFirePosition = pf;
		vp_Shooter.FireRotationFunc rf;
		if (m_FireRotationFuncBackups.TryGetValue(shooter, out rf))
			shooter.GetFireRotation = rf;

		// restore shooter camera recoils
		if (shooter is vp_FPWeaponShooter)
		{
			vp_FPWeaponShooter ws = (shooter as vp_FPWeaponShooter);
			Vector2 recoils;
			if (m_ShooterCameraRecoilBackups.TryGetValue(ws, out recoils))
			{
				ws.MotionPositionRecoilCameraFactor = recoils.x;
				ws.MotionRotationRecoilCameraFactor = recoils.y;
			}
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_SetWeapon()
	{

		// trigger caching of the new shooter
		m_CurrentShooter = null;

		if (CurrentShooter != null)
		{
			CurrentShooter.GetFirePosition = VRFirePositionFunc;
			CurrentShooter.GetFireRotation = VRFireRotationFunc;
		}

		// make sure the new weapon is visible
		if (CurrentFPWeapon != null)
		{
			if (CurrentFPWeapon.Renderers[0].transform.localScale == Vector3.zero)
				SetVisible();
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_SetWeapon()
	{

		RestoreWeaponSettings(CurrentShooter);

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void OnCameraSnap()
	{

		if (!enabled)
			return;

		if (CurrentFPWeapon != null)
		{
			SetVisible(false);
			CurrentFPWeapon.SnapSprings();
			vp_Timer.In(0, () => { SetVisible(true); });
		}

		m_NextAllowedSwayTime = Time.time + 1.0f;

	}


}

