//----------------------------------------------
//            Realistic Tank Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RTC_Settings))]
public class RTC_SettingsEditor : Editor {

	RTC_Settings RTCSettingsAsset;

	Color originalGUIColor;
	Vector2 scrollPos;
	PhysicMaterial[] physicMaterials;

	bool foldProjectSettings = false;
	bool foldControllerSettings = false;
	bool foldUISettings = false;
	bool foldWheelPhysics = false;
	bool foldSFX = false;
	bool foldOptimization = false;
	bool foldTagsAndLayers = false;

	public bool EnableReWired
	{
		get
		{
			bool _bool = RTCSettingsAsset.enableReWired;

			return _bool;
		}

		set
		{
			bool _bool = RTCSettingsAsset.enableReWired;

			if(_bool == value)
				return;

			RTCSettingsAsset.enableReWired = value;

			foreach (BuildTargetGroup buildTarget in Enum.GetValues(typeof(BuildTargetGroup))) {
				if(buildTarget != BuildTargetGroup.Unknown)
					SetScriptingSymbol("RTC_REWIRED", buildTarget, value);
			}

		}
	}

	void OnEnable(){

		foldProjectSettings = RTC_Settings.Instance.foldProjectSettings;
		foldControllerSettings = RTC_Settings.Instance.foldControllerSettings;
		foldUISettings = RTC_Settings.Instance.foldUISettings;
		foldWheelPhysics = RTC_Settings.Instance.foldWheelPhysics;
		foldSFX = RTC_Settings.Instance.foldSFX;
		foldOptimization = RTC_Settings.Instance.foldOptimization;
		foldTagsAndLayers = RTC_Settings.Instance.foldTagsAndLayers;

	}

	void OnDestroy(){

		RTC_Settings.Instance.foldProjectSettings = foldProjectSettings;
		RTC_Settings.Instance.foldControllerSettings = foldControllerSettings;
		RTC_Settings.Instance.foldUISettings = foldUISettings;
		RTC_Settings.Instance.foldWheelPhysics = foldWheelPhysics;
		RTC_Settings.Instance.foldSFX = foldSFX;
		RTC_Settings.Instance.foldOptimization = foldOptimization;
		RTC_Settings.Instance.foldTagsAndLayers = foldTagsAndLayers;

	}

	public override void OnInspectorGUI (){

		serializedObject.Update();
		RTCSettingsAsset = (RTC_Settings)target;

		originalGUIColor = GUI.color;
		EditorGUIUtility.labelWidth = 250;
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("RTC Asset Settings Editor Window", EditorStyles.boldLabel);
		GUI.color = new Color(.75f, 1f, .75f);
		EditorGUILayout.LabelField("This editor will keep update necessary .asset files in your project for RTC. Don't change directory of the ''Resources/RTC Assets''.", EditorStyles.helpBox);
		GUI.color = originalGUIColor;
		EditorGUILayout.Space();

		EditorGUI.indentLevel++;

		scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false );

		EditorGUILayout.Space();

		foldProjectSettings = EditorGUILayout.Foldout(foldProjectSettings, "General Settings");

		if(foldProjectSettings){

			EditorGUILayout.BeginVertical (GUI.skin.box);

			GUILayout.Label("General Settings", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideFixedTimeStep"), new GUIContent("Override FixedTimeStep"));

			if(RTCSettingsAsset.overrideFixedTimeStep)
				EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedTimeStep"), new GUIContent("Fixed Timestep"));
			
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngularVelocity"), new GUIContent("Maximum Angular Velocity"));
			GUI.color = new Color(.75f, 1f, .75f);
			EditorGUILayout.HelpBox("Using above project settings must be same with your other BCG assets.", MessageType.Info);
			GUI.color = originalGUIColor;

			EditorGUILayout.Space();

			EditorGUILayout.EndVertical ();

		}

		EditorGUILayout.Space();

		foldControllerSettings = EditorGUILayout.Foldout(foldControllerSettings, "Controller Settings");

		if(foldControllerSettings){
			
			List<string> controllerTypeStrings =  new List<string>();
			controllerTypeStrings.Add("Keyboard");	controllerTypeStrings.Add("Mobile");	controllerTypeStrings.Add("Custom");
			EditorGUILayout.BeginVertical (GUI.skin.box);

			GUI.color = new Color(.5f, 1f, 1f, 1f);
			GUILayout.Label("Main Controller Type", EditorStyles.boldLabel);
			RTCSettingsAsset.toolbarSelectedIndex = GUILayout.Toolbar(RTCSettingsAsset.toolbarSelectedIndex, controllerTypeStrings.ToArray());
			GUI.color = originalGUIColor;
			EditorGUILayout.Space();


			if(RTCSettingsAsset.toolbarSelectedIndex == 0){

				RTCSettingsAsset.controllerType = RTC_Settings.ControllerType.Keyboard;

				EditorGUILayout.BeginVertical (GUI.skin.box);

				GUILayout.Label("Keyboard Settings", EditorStyles.boldLabel);

				EditorGUILayout.PropertyField(serializedObject.FindProperty("gasInput"), new GUIContent("Gas / Reverse Input Axis"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("steerInput"), new GUIContent("Steering Input Axis"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("mainGunXInput"), new GUIContent("Main Gun X Input Axis"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("mainGunYInput"), new GUIContent("Main Gun Y Input Axis"));
				GUI.color = new Color(.75f, 1f, .75f);
				EditorGUILayout.HelpBox("You can edit above input axises from Edit --> Project Settings --> Input.", MessageType.Info);
				GUI.color = originalGUIColor;
				EditorGUILayout.PropertyField(serializedObject.FindProperty("startEngineKB"), new GUIContent("Start / Stop Engine Key"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("handbrakeKB"), new GUIContent("Handbrake Key"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("headlightsKB"), new GUIContent("Toggle Headlights"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("changeCameraKB"), new GUIContent("Change Camera"));

				EditorGUILayout.PropertyField(serializedObject.FindProperty("fireKB"), new GUIContent("Fire"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("changeAmmunation"), new GUIContent("Change Ammunation"));
				EditorGUILayout.Space();

				EditorGUILayout.EndVertical ();

				EnableReWired = false;

		}
				
		if(RTCSettingsAsset.toolbarSelectedIndex == 1){

			EditorGUILayout.BeginVertical (GUI.skin.box);

			RTCSettingsAsset.controllerType = RTC_Settings.ControllerType.Mobile;

			GUILayout.Label("Mobile Settings", EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("uiType"), new GUIContent("UI Type"));

			GUI.color = new Color(.75f, 1f, .75f);
			EditorGUILayout.HelpBox("All UI/NGUI buttons will feed the vehicles at runtime.", MessageType.Info);
			GUI.color = originalGUIColor;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("UIButtonSensitivity"), new GUIContent("UI Button Sensitivity"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("UIButtonGravity"), new GUIContent("UI Button Gravity"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("gyroSensitivity"), new GUIContent("Gyro Sensitivity"));

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileControllerType"), new GUIContent("Mobile Controller Type"));
			
			GUI.color = new Color(.75f, 1f, .75f);
			EditorGUILayout.HelpBox("You can switch mobile controller type in your game by just calling ''RTC_Settings.Instance.mobileControllerType = RTC_Settings.MobileControllerType.Accelerometer;''.", MessageType.Info);
			GUI.color = originalGUIColor;
			EditorGUILayout.Space();

			EditorGUILayout.EndVertical ();

			EnableReWired = false;

		}

		if(RTCSettingsAsset.toolbarSelectedIndex == 2){

				EditorGUILayout.BeginVertical (GUI.skin.box);

			RTCSettingsAsset.controllerType = RTC_Settings.ControllerType.Custom;

				GUILayout.Label("Custom Input Settings", EditorStyles.boldLabel);

				GUI.color = new Color(.75f, 1f, .75f);
				EditorGUILayout.HelpBox("In this mode, tank controller won't receive these inputs from keyboard or UI buttons. You need to feed these inputs in your own script.", MessageType.Info);
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Tank controller uses these inputs; \n  \n    gasInput = Clamped 0f - 1f.  \n    brakeInput = Clamped 0f - 1f.  \n    steerInput = Clamped -1f - 1f. \n    handbrakeInput = Clamped 0f - 1f.", MessageType.Info);
				EditorGUILayout.Space();
				GUI.color = originalGUIColor;

				EnableReWired = EditorGUILayout.ToggleLeft(new GUIContent("Enable ReWired", "It will enable ReWired support for RTC. Be sure you have imported latest ReWired to your project before enabling this."), EnableReWired);

				EditorGUILayout.Space();

				if (!EnableReWired) {

					GUI.color = new Color(.75f, .75f, 0f);
					EditorGUILayout.HelpBox ("It will enable ReWired support for RTC. Be sure you have imported latest ReWired to your project before enabling this.", MessageType.Warning);
					GUI.color = originalGUIColor;

				} else {

					EditorGUILayout.BeginVertical (GUI.skin.box);

					GUILayout.Label("ReWired Settings", EditorStyles.boldLabel);

					#if RTC_REWIRED
					GUI.color = new Color(.75f, 1f, .75f);
					EditorGUILayout.HelpBox("These input strings must be exactly same with your ReWired Inputs. You can edit them from ''ReWired Input Manager'' on your scene.", MessageType.Info);
					GUI.color = originalGUIColor;
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_gasInput"), new GUIContent("Gas / Reverse Input Axis"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_steerInput"), new GUIContent("Steering Input Axis"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_mainGunXInput"), new GUIContent("Main Gun X Input Axis"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_mainGunYInput"), new GUIContent("Main Gun Y Input Axis"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_startEngineKB"), new GUIContent("Start / Stop Engine Key"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_handbrakeKB"), new GUIContent("Handbrake Key"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_headlightsKB"), new GUIContent("Toggle Headlights"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_changeCameraKB"), new GUIContent("Change Camera"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_enterExitVehicleKB"), new GUIContent("Get In & Get Out Of The Vehicle"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_fireKB"), new GUIContent("Fire"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("RW_changeAmmunation"), new GUIContent("Change Ammunation"));
					#endif

					EditorGUILayout.Space();

					EditorGUILayout.EndVertical ();

				}
				
				GUI.color = originalGUIColor;

				EditorGUILayout.Space();

				EditorGUILayout.EndVertical ();
			
		}

			EditorGUILayout.BeginVertical(GUI.skin.box);

			GUILayout.Label("Main Controller Settings", EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("runEngineAtAwake"), new GUIContent("Engines Are Running At Awake"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("autoReset"), new GUIContent("Auto Reset"));

			EditorGUILayout.Space();

			EditorGUILayout.EndVertical ();

		EditorGUILayout.EndVertical ();

		}

		EditorGUILayout.Space();

		foldUISettings = EditorGUILayout.Foldout(foldUISettings, "UI Settings");

		if(foldUISettings){
			
			EditorGUILayout.BeginVertical (GUI.skin.box);
			GUILayout.Label("UI Dashboard Settings", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("uiType"), new GUIContent("UI Type"));
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical ();

		}

		EditorGUILayout.Space();

		foldWheelPhysics = EditorGUILayout.Foldout(foldWheelPhysics, "Wheel Physics Settings");

		if(foldWheelPhysics){

			if(RTC_GroundMaterials.Instance.frictions != null && RTC_GroundMaterials.Instance.frictions.Length > 0){

					EditorGUILayout.BeginVertical (GUI.skin.box);
					GUILayout.Label("Ground Physic Materials", EditorStyles.boldLabel);

				physicMaterials = new PhysicMaterial[RTC_GroundMaterials.Instance.frictions.Length];
					
					for (int i = 0; i < physicMaterials.Length; i++) {
						physicMaterials[i] = RTC_GroundMaterials.Instance.frictions[i].groundMaterial;
						EditorGUILayout.BeginVertical(GUI.skin.box);
						EditorGUILayout.ObjectField("Ground Physic Materials " + i, physicMaterials[i], typeof(PhysicMaterial), false);
						EditorGUILayout.EndVertical();
					}

					EditorGUILayout.Space();

			}

			GUI.color = new Color(.5f, 1f, 1f, 1f);
			
			if(GUILayout.Button("Configure Ground Physic Materials")){
				Selection.activeObject = Resources.Load("RTC Assets/RTC_GroundMaterials") as RTC_GroundMaterials;
			}

			GUI.color = originalGUIColor;

			EditorGUILayout.Space();

			EditorGUILayout.EndVertical ();

		}

		EditorGUILayout.Space();

		foldOptimization = EditorGUILayout.Foldout(foldOptimization, "Optimization");

		if(foldOptimization){

			EditorGUILayout.BeginVertical(GUI.skin.box);

			GUILayout.Label("Optimization", EditorStyles.boldLabel);

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("useLightsAsVertexLights"), new GUIContent("Use Lights As Vertex Lights On Vehicles"));
			GUI.color = new Color(.75f, 1f, .75f);
			EditorGUILayout.HelpBox("Always use vertex lights for mobile platform. Even only one pixel light will drop your performance dramaticaly!", MessageType.Info);
			GUI.color = originalGUIColor;
			GUI.color = new Color(.75f, 1f, .75f);
			GUI.color = originalGUIColor;
			EditorGUILayout.Space();

			EditorGUILayout.EndVertical();

		}

		foldTagsAndLayers = EditorGUILayout.Foldout(foldTagsAndLayers, "Tags & Layers");

		if (foldTagsAndLayers) {

			EditorGUILayout.BeginVertical (GUI.skin.box);

			GUILayout.Label ("Tags & Layers", EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("setTagsAndLayers"), new GUIContent("Set Tags And Layers Auto"), false);

			if (RTCSettingsAsset.setTagsAndLayers) {

				EditorGUILayout.PropertyField (serializedObject.FindProperty ("RTCLayer"), new GUIContent ("Tank Layer"), false);
				EditorGUILayout.PropertyField (serializedObject.FindProperty ("RTCTag"), new GUIContent ("Tank Tag"), false);
				EditorGUILayout.PropertyField (serializedObject.FindProperty ("tagAllChildrenGameobjects"), new GUIContent ("Tag All Children Gameobjects"), false);
				GUI.color = new Color (.75f, 1f, .75f);
				EditorGUILayout.HelpBox ("Be sure you have that tag and layer in your Tags & Layers", MessageType.Warning);
				EditorGUILayout.HelpBox ("All vehicles powered by Realistic Tank Controller are using this layer. What does this layer do? It was used for masking wheel rays, light masks, and projector masks. Just create a new layer for vehicles from Edit --> Project Settings --> Tags & Layers, and select the layer here.", MessageType.Info);
				GUI.color = originalGUIColor;

			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();

		}

		EditorGUILayout.BeginVertical (GUI.skin.box);

		GUILayout.Label ("Resources", EditorStyles.boldLabel);

		EditorGUILayout.PropertyField(serializedObject.FindProperty("headLight"), new GUIContent("Head Light"), false);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeLight"), new GUIContent("Brake Light"), false);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("exhaustGas"), new GUIContent("Exhaust Gas"), false);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("mainCamera"), new GUIContent("Main Camera"), false);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("gunCamera"), new GUIContent("Gun Camera"), false);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("RTCCanvas"), new GUIContent("RTC UI Canvas"), false);

		EditorGUILayout.Space();

		EditorGUILayout.EndVertical();

		EditorGUILayout.EndScrollView();
		
		EditorGUILayout.Space();

		EditorGUILayout.BeginVertical (GUI.skin.button);

		GUI.color = new Color(.75f, 1f, .75f);

		GUI.color = new Color(1f, .5f, .5f, 1f);
		
		if(GUILayout.Button("Reset To Defaults")){
			ResetToDefaults();
			Debug.Log("Resetted To Defaults!");
		}

		GUI.color = new Color(.5f, 1f, 1f, 1f);
		
		if(GUILayout.Button("Open PDF Documentation")){
			string url = "http://www.bonecrackergames.com/realistic-tank-controller";
			Application.OpenURL(url);
		}

		GUI.color = originalGUIColor;
		
		EditorGUILayout.LabelField("Realistic Tank Controller V2.0\nBoneCrackerGames", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

		EditorGUILayout.LabelField("Created by Buğra Özdoğanlar", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

		EditorGUILayout.EndVertical();

		serializedObject.ApplyModifiedProperties();
		
		if(GUI.changed)
			EditorUtility.SetDirty(RTCSettingsAsset);

	}

	void ResetToDefaults(){

		RTCSettingsAsset.overrideFixedTimeStep = true;
		RTCSettingsAsset.fixedTimeStep = .02f;
		RTCSettingsAsset.maxAngularVelocity = 6;

		// Controller Types.
		RTCSettingsAsset.controllerType = RTC_Settings.ControllerType.Keyboard;
		RTCSettingsAsset.enableReWired = false;
		EnableReWired = false;

		// Keyboard Inputs.
		RTCSettingsAsset.gasInput = "Vertical";
		RTCSettingsAsset.steerInput = "Horizontal";
		RTCSettingsAsset.mainGunXInput = "Mouse X";
		RTCSettingsAsset.mainGunYInput = "Mouse Y";
		RTCSettingsAsset.handbrakeKB = KeyCode.Space;
		RTCSettingsAsset.startEngineKB = KeyCode.I;
		RTCSettingsAsset.headlightsKB = KeyCode.H;
		RTCSettingsAsset.changeCameraKB = KeyCode.C;
		RTCSettingsAsset.fireKB = KeyCode.Mouse0;
		RTCSettingsAsset.changeAmmunation = KeyCode.F;

		#if RTC_REWIRED
		// ReWired Inputs.
		RTCSettingsAsset.RW_gasInput = "Gas";
		RTCSettingsAsset.RW_steerInput = "Steer";
		RTCSettingsAsset.RW_mainGunXInput = "AimX";
		RTCSettingsAsset.RW_mainGunYInput = "AimY";
		RTCSettingsAsset.RW_handbrakeKB = "Handbrake";
		RTCSettingsAsset.RW_startEngineKB = "StartEngine";
		RTCSettingsAsset.RW_headlightsKB = "Headlights";
		RTCSettingsAsset.RW_changeCameraKB = "ChangeCamera";
		RTCSettingsAsset.RW_enterExitVehicleKB = "EnterExitVehicle";
		RTCSettingsAsset.RW_fireKB = "Fire";
		RTCSettingsAsset.RW_changeAmmunation = "ChangeAmmo";
		#endif

		// Main Controller Settings.
		RTCSettingsAsset.runEngineAtAwake = true;
		RTCSettingsAsset.autoReset = true;

		// UI Dashboard Type.
		RTCSettingsAsset.uiType = RTC_Settings.UIType.UI;

		// Mobile controller types.
		RTCSettingsAsset.mobileControllerType = RTC_Settings.MobileControllerType.Buttons;

		// Mobile controller buttons and accelerometer sensitivity.
		RTCSettingsAsset.UIButtonSensitivity = 3f;
		RTCSettingsAsset.UIButtonGravity = 10f;
		RTCSettingsAsset.gyroSensitivity = 2f;

		// Used for using the lights more efficent and realistic.
		RTCSettingsAsset.useLightsAsVertexLights = true;

		// Tags and layers.
		RTCSettingsAsset.setTagsAndLayers = false;
		RTCSettingsAsset.RTCLayer = "Default";
		RTCSettingsAsset.RTCTag = "Untagged";
		RTCSettingsAsset.tagAllChildrenGameobjects = false;

	}

	void SetScriptingSymbols(string symbol, bool isActivate)
	{
		SetScriptingSymbol(symbol, BuildTargetGroup.Android, isActivate);

		#if UNITY_5 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3_OR_NEWER
		SetScriptingSymbol(symbol, BuildTargetGroup.iOS, isActivate);
		#else
		SetScriptingSymbol(symbol, BuildTargetGroup.iPhone, isActivate);
		#endif
	}

	void SetScriptingSymbol(string symbol, BuildTargetGroup target, bool isActivate)
	{
		if(target == BuildTargetGroup.Unknown)
			return;

		var s = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

		s = s.Replace(symbol + ";","");

		s = s.Replace(symbol,"");

		if(isActivate)
			s = symbol + ";" + s;

		PlayerSettings.SetScriptingDefineSymbolsForGroup(target,s);
	}

}
