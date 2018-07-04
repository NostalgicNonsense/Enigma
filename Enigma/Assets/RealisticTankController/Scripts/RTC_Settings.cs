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

/// <summary>
/// Stored all general shared RTC settings here.
/// </summary>
[System.Serializable]
public class RTC_Settings : ScriptableObject {
	
	#region singleton
	public static RTC_Settings instance;
	public static RTC_Settings Instance{	get{if(instance == null) instance = Resources.Load("RTC Assets/RTC_Settings") as RTC_Settings; return instance;}}
	#endregion

	public int toolbarSelectedIndex;

	public bool overrideFixedTimeStep = true;
	[Range(.005f, .06f)]public float fixedTimeStep = .02f;
	[Range(.5f, 20f)]public float maxAngularVelocity = 6;

	// Controller Types.
	public ControllerType controllerType;
	public enum ControllerType{Keyboard, Mobile, Custom}
	public bool enableReWired = false;

	// Keyboard Inputs.
	public string gasInput = "Vertical";
	public string steerInput = "Horizontal";
	public string mainGunXInput = "Mouse X";
	public string mainGunYInput = "Mouse Y";
	public KeyCode handbrakeKB = KeyCode.Space;
	public KeyCode startEngineKB = KeyCode.I;
	public KeyCode headlightsKB = KeyCode.H;
	public KeyCode changeCameraKB = KeyCode.C;
	public KeyCode fireKB = KeyCode.Mouse0;
	public KeyCode changeAmmunation = KeyCode.F;

	#if RTC_REWIRED
	// ReWired Inputs.
	public string RW_gasInput = "Gas";
	public string RW_steerInput = "Steer";
	public string RW_mainGunXInput = "AimX";
	public string RW_mainGunYInput = "AimY";
	public string RW_handbrakeKB = "Handbrake";
	public string RW_startEngineKB = "StartEngine";
	public string RW_headlightsKB = "Headlights";
	public string RW_changeCameraKB = "ChangeCamera";
	public string RW_enterExitVehicleKB = "EnterExitVehicle";
	public string RW_fireKB = "Fire";
	public string RW_changeAmmunation = "ChangeAmmo";
	#endif

	// Main Controller Settings.
	public bool runEngineAtAwake = true;
	public bool autoReset = true;

	// UI Dashboard Type.
	public UIType uiType;
	public enum UIType{UI, None}

	// Mobile controller types.
	public MobileControllerType mobileControllerType;
	public enum MobileControllerType {Buttons, Accelerometer}

	// Mobile controller buttons and accelerometer sensitivity.
	public float UIButtonSensitivity = 3f;
	public float UIButtonGravity = 10f;
	public float gyroSensitivity = 2f;

	// Used for using the lights more efficent and realistic.
	public bool useLightsAsVertexLights = true;

	// Tags and layers.
	public bool setTagsAndLayers = false;
	public string RTCLayer = "Default";
	public string RTCTag = "Untagged";
	public bool tagAllChildrenGameobjects = false;

	// Resources.
	public GameObject mainCamera;
	public GameObject gunCamera;
	public GameObject exhaustGas;
	public GameObject headLight;
	public GameObject brakeLight;
	public GameObject RTCCanvas;

	// Used for folding sections of RTC Settings.
	public bool foldProjectSettings = false;
	public bool foldControllerSettings = false;
	public bool foldUISettings = false;
	public bool foldWheelPhysics = false;
	public bool foldSFX = false;
	public bool foldOptimization = false;
	public bool foldTagsAndLayers = false;

}
