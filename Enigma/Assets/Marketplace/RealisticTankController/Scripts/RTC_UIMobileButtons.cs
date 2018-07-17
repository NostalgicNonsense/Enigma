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
using System.Collections.Generic;

[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/UI/Mobile Buttons Handler")]
public class RTC_UIMobileButtons : MonoBehaviour {

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

	#region singleton
	public static RTC_UIMobileButtons instance;
	public static RTC_UIMobileButtons Instance{
		get{
			if(instance == null)
				instance = FindObjectOfType<RTC_UIMobileButtons>();
			return instance;
		}
	}
	#endregion

	public List<RTC_TankController> tankControllers = new List<RTC_TankController> ();
	private RTC_MainCamera mainCamera;

	public GameObject mobileControllers;

	public RTC_UIJoystickController controllingJoystick;
	public RTC_UIJoystickController aimingJoystick;
	public RTC_UIDragController aimingDrag;

	[SerializeField]
	public class MobileInput {

		public float controllingVertical = 0f;
		public float controllingHorizontal = 0f;
		public float aimingVertical = 0f;
		public float aimingHorizontal = 0f;

	}

	MobileInput mobileInput = new MobileInput();

	// Use this for initialization
	void Start () {

		if (RTCSettings.controllerType != RTC_Settings.ControllerType.Mobile)
			mobileControllers.SetActive (false);
		else
			mobileControllers.SetActive (true);
	
	}

	void OnEnable(){

		RTC_TankController.OnRTCSpawned += RTC_TankController_OnRTCSpawned;

	}

	void Update(){

		if (RTCSettings.controllerType == RTC_Settings.ControllerType.Mobile) {

			switch (RTCSettings.mobileControllerType) {

			case RTC_Settings.MobileControllerType.Buttons:

				if(controllingJoystick.axesToUse != RTC_UIJoystickController.AxisOption.Both)
					controllingJoystick.axesToUse = RTC_UIJoystickController.AxisOption.Both;

				break;

			case RTC_Settings.MobileControllerType.Accelerometer:

				if(controllingJoystick.axesToUse != RTC_UIJoystickController.AxisOption.OnlyVertical)
					controllingJoystick.axesToUse = RTC_UIJoystickController.AxisOption.OnlyVertical;

				break;

			}

		}

	}

	void RTC_TankController_OnRTCSpawned (RTC_TankController RTC)	{

		if (!tankControllers.Contains (RTC))
			tankControllers.Add (RTC);
		
	}

	public MobileInput GetValues () {

		switch (RTCSettings.mobileControllerType) {

		case RTC_Settings.MobileControllerType.Buttons:

			mobileInput.controllingHorizontal = GetHorizontalInput(controllingJoystick);

			break;

		case RTC_Settings.MobileControllerType.Accelerometer:

			mobileInput.controllingHorizontal = Input.acceleration.x * RTCSettings.gyroSensitivity;

			break;

		}

		mobileInput.controllingVertical = GetVerticalInput(controllingJoystick);
		mobileInput.aimingVertical = GetVerticalInput(aimingJoystick) + GetVerticalInput(aimingDrag);
		mobileInput.aimingHorizontal = GetHorizontalInput(aimingJoystick) + GetHorizontalInput(aimingDrag);

		return mobileInput;
	
	}

	float GetVerticalInput(RTC_UIJoystickController joystick){

		if(joystick == null)
			return 0f;

		return(joystick.verticalInput);

	}

	float GetHorizontalInput(RTC_UIJoystickController joystick){

		if(joystick == null)
			return 0f;

		return(joystick.horizontal);

	}

	float GetVerticalInput(RTC_UIDragController drag){

		if(drag == null)
			return 0f;

		return(drag.verticalInput);

	}

	float GetHorizontalInput(RTC_UIDragController drag){

		if(drag == null)
			return 0f;

		return(drag.horizontal);

	}

	void OnDisable(){

		RTC_TankController.OnRTCSpawned -= RTC_TankController_OnRTCSpawned;

	}

}
