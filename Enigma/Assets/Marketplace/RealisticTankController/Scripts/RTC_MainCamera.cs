//----------------------------------------------
//            Realistic Tank Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
#if RTC_REWIRED
using Rewired;
#endif

/// <summary>
/// Main RTC Camera controller. Includes 2 different camera modes with many customizable settings. It doesn't use different cameras on your scene like *other* assets. Simply it parents the camera to their positions that's all. No need to be Einstein.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Camera/Main Camera")]
public class RTC_MainCamera : MonoBehaviour{

	// Getting an Instance of Main Shared RTC Settings.
	#region RTC Settings Instance

	private RTC_Settings RTCSettingsInstance;
	private RTC_Settings RTCSettings {
		get {
			if (RTCSettingsInstance == null) {
				RTCSettingsInstance = RTC_Settings.Instance;
			}
			return RTCSettingsInstance;
		}
	}

	#endregion

	// The target we are following transform and rigidbody.
	public Transform currentTank;
	private Rigidbody currentRigid;

	internal Camera cam;	// Camera is not attached to this main gameobject. Our child camera parented to this gameobject. Therefore, we can apply additional position and rotation changes.
	public GameObject pivot;	// Pivot center of the camera. Used for making offsets and collision movements.

	// Camera modes.
	public CameraMode cameraMode;
	public enum CameraMode{ORBIT, FPS}

	public float orbitDistance = 10f;
	public float orbitHeight = 10f;

	internal float targetFieldOfView = 60f;	// Camera will adapt its field of view to this target field of view. All field of views above this line will feed this float value.

	public float gunCameraFOV = 65f;		// Hood field of view.
	public float orbitCameraFOV = 50f;		// Orbit camera field of view.

	internal int cameraSwitchCount = 0;		// Used in switch case for running corresponding camera mode method.
	private RTC_GunCamera gunCam;		// Hood camera. It's a null script. Just used for finding hood camera parented to our vehicle.

	private float speed = 0f;		// Vehicle speed.

	// Orbit X and Y inputs.
	private float orbitX = 0f;
	private float orbitY = 0f;

	// Minimum and maximum Orbit X, Y degrees.
	public float minOrbitY = -20f;
	public float maxOrbitY = 80f;

	//	Orbit X and Y speeds.
	public float orbitXSpeed = 10f;
	public float orbitYSpeed = 10f;

	//	Orbit transform.
	private Vector3 orbitPosition;
	private Quaternion orbitRotation;

	#if RTC_REWIRED
	private static Player player;
	#endif

	void Awake(){

		cam = GetComponentInChildren<Camera>();

		#if RTC_REWIRED
		player = Rewired.ReInput.players.GetPlayer(0);
		#endif

	}

	void GetTarget(){
		
		// Return if we don't have player tank.
		if(!currentTank)
			return;

		ChangeCamera (CameraMode.ORBIT);

		currentRigid = currentTank.GetComponent<Rigidbody>();

		// Getting camera modes from vehicle.
		gunCam = currentTank.GetComponentInChildren<RTC_GunCamera>();

	}

	public void SetTarget(GameObject player){

		currentTank = player.transform;
		GetTarget ();

	}

	void Update(){

		// Early out if we don't have a player.
		if (!currentTank || !currentRigid){
			GetTarget();
			return;
		}

		Inputs ();

		// Speed of the vehicle (smoothed).
		speed = Mathf.Lerp(speed, currentTank.InverseTransformDirection(currentRigid.velocity).z * 3.6f, Time.deltaTime * 3f);

		// Lerping current field of view to target field of view.
		cam.fieldOfView = Mathf.Lerp (cam.fieldOfView, targetFieldOfView, Time.deltaTime * 5f);

	}

	void LateUpdate (){

		// Early out if we don't have a target.
		if (!currentTank || !currentRigid)
			return;

		// Even if we have the player and it's disabled, return.
		if (!currentTank.gameObject.activeSelf)
			return;

		// Run the corresponding method with choosen camera mode.
		switch(cameraMode){

		case CameraMode.ORBIT:
			ORBIT();
			break;

		case CameraMode.FPS:
			break;

		}

	}

	void Inputs(){

		switch (RTCSettings.controllerType) {

		case RTC_Settings.ControllerType.Keyboard:

			orbitX += Input.GetAxis (RTCSettings.mainGunXInput) * (orbitXSpeed * 10f) * Time.deltaTime;
			orbitY -= Input.GetAxis (RTCSettings.mainGunYInput) * (orbitYSpeed * 10f) * Time.deltaTime;

			if(Input.GetKeyDown(RTCSettings.changeCameraKB))
				ChangeCamera();

			break;

		case RTC_Settings.ControllerType.Mobile:

			orbitX += RTC_UIMobileButtons.Instance.GetValues ().aimingHorizontal * (orbitXSpeed * 10f) * Time.deltaTime;
			orbitY -= RTC_UIMobileButtons.Instance.GetValues ().aimingVertical * (orbitYSpeed * 10f) * Time.deltaTime;

			break;

		case RTC_Settings.ControllerType.Custom:

			#if RTC_REWIRED

			orbitX += player.GetAxis (RTCSettings.RW_mainGunXInput) * (orbitXSpeed * 10f) * Time.deltaTime;
			orbitY -= player.GetAxis (RTCSettings.RW_mainGunYInput) * (orbitYSpeed * 10f) * Time.deltaTime;

			if(player.GetButtonDown(RTCSettings.RW_changeCameraKB))
				ChangeCamera();

			#endif

			break;

		}

	}

	// Change camera by increasing camera switch counter.
	public void ChangeCamera(){

		// Increasing camera switch counter at each camera changing.
		cameraSwitchCount ++;

		// We have 7 camera modes at total. If camera switch counter is greater than maximum, set it to 0.
		if (cameraSwitchCount >= 2) {
			cameraSwitchCount = 0;
			cameraMode = 0;
		}

		switch(cameraSwitchCount){

		case 0:
			cameraMode = CameraMode.ORBIT;
			break;

		case 1:
			if (gunCam)
				cameraMode = CameraMode.FPS;
			else
				ChangeCamera ();
			break;

		}

		// Resetting camera when changing camera mode.
		ResetCamera ();

	}

	// Change camera by directly setting it to specific mode.
	public void ChangeCamera(CameraMode mode){

		cameraMode = mode;

		// Resetting camera when changing camera mode.
		ResetCamera ();

	}

	void ORBIT(){

		orbitY = Mathf.Clamp(orbitY, minOrbitY, maxOrbitY);

		orbitRotation = Quaternion.Euler(orbitY, orbitX, 0);
		orbitPosition = orbitRotation * new Vector3(0f, orbitHeight, -orbitDistance) + currentTank.position;

		transform.rotation = orbitRotation;
		transform.position = orbitPosition;

	}

	private void ResetCamera(){

		switch (cameraMode) {

		case CameraMode.ORBIT:
			transform.SetParent (null);

			if(!currentTank.GetComponent<RTC_TankGunController>())
				orbitX = currentTank.eulerAngles.y;
			else
				orbitX = currentTank.GetComponent<RTC_TankGunController>().mainGun.transform.eulerAngles.y;
			
			transform.position = currentTank.position;
			transform.rotation = currentTank.rotation;
			targetFieldOfView = orbitCameraFOV;

			break;

		case CameraMode.FPS:
			transform.SetParent (gunCam.transform, false);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			targetFieldOfView = gunCameraFOV;
			break;

		}

	}

}