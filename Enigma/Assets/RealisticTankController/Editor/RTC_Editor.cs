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
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RTC_TankController)), CanEditMultipleObjects]
public class RTC_Editor : Editor {
	
	RTC_TankController tankController;

	RTC_TankController.RTC_Wheels[] wheels;

	Transform[] wheelModels_L;
	Transform[] wheelModels_R;

	static bool firstInit = false;
	
	Texture2D wheelIcon;
	Texture2D configIcon;
	Texture2D soundIcon;
	Texture2D trackIcon;
	
	bool WheelSettings;
	bool Configurations;
	bool SoundSettings;
	bool TrackSettings;
	
	Color defBackgroundColor;
	
	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Add Main Controller To Tank", false, -90)]
	static void CreateBehavior(){
		
		if(!Selection.activeGameObject.GetComponentInParent<RTC_TankController>()){

			GameObject pivot = new GameObject (Selection.activeGameObject.name);
			pivot.transform.position = RTC_GetBounds.GetBoundsCenter (Selection.activeGameObject.transform);
			pivot.transform.rotation = Selection.activeGameObject.transform.rotation;
			
			pivot.AddComponent<RTC_TankController>();
			
			pivot.GetComponent<Rigidbody>().mass = 50000f;
			pivot.GetComponent<Rigidbody> ().drag = 0f;
			pivot.GetComponent<Rigidbody>().angularDrag = .5f;
			pivot.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;

			firstInit = true;

			Selection.activeGameObject.transform.SetParent (pivot.transform);
			Selection.activeGameObject = pivot;

			EditorUtility.DisplayDialog("RTC Initialized", "Let's get started by creating new wheels. Open up ''Wheels'' category, and select your wheels.", "Ok");
			
		}else{
			
			EditorUtility.DisplayDialog("Your Gameobject Already Has Realistic Tank Controller", "Your Gameobject Already Has Realistic Tank Controller", "Ok");
			
		}
		
	}
	
	void Awake(){
		
		wheelIcon = Resources.Load("WheelIcon", typeof(Texture2D)) as Texture2D;
		configIcon = Resources.Load("ConfigIcon", typeof(Texture2D)) as Texture2D;
		soundIcon = Resources.Load("SoundIcon", typeof(Texture2D)) as Texture2D;
		trackIcon = Resources.Load("TrackIcon", typeof(Texture2D)) as Texture2D;
		
	}
	
	public override void OnInspectorGUI () {
		
		serializedObject.Update();
		EditorGUILayout.BeginVertical (GUI.skin.button);
		
		tankController = (RTC_TankController)target;
		wheels = tankController.wheels;
		defBackgroundColor = GUI.backgroundColor;
		
		if(firstInit)
			SetDefaultSettings();
		
		EditorGUILayout.Space();
		
		EditorGUILayout.BeginHorizontal();
		
		if(WheelSettings)
			GUI.backgroundColor = Color.gray;
		else GUI.backgroundColor = defBackgroundColor;
		
		if (GUILayout.Button (wheelIcon))
			WheelSettings = OpenCategory();
		
		if(Configurations)
			GUI.backgroundColor = Color.gray;
		else GUI.backgroundColor = defBackgroundColor;
		
		if(GUILayout.Button(configIcon))
			Configurations = OpenCategory();
		
		if(SoundSettings)
			GUI.backgroundColor = Color.gray;
		else GUI.backgroundColor = defBackgroundColor;
		
		if(GUILayout.Button(soundIcon))
			SoundSettings = OpenCategory();
		
		if(TrackSettings)
			GUI.backgroundColor = Color.gray;
		else GUI.backgroundColor = defBackgroundColor;
		
		if(GUILayout.Button(trackIcon))
			TrackSettings = OpenCategory();
		
		GUI.backgroundColor = defBackgroundColor;
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();
		
		if(WheelSettings){
			
			EditorGUILayout.Space();
			GUI.color = Color.cyan;
			EditorGUILayout.HelpBox("Wheel Settings", MessageType.None);
			GUI.color = defBackgroundColor;
			EditorGUILayout.Space();

			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100f;

			if (wheels != null) {

				for (int i = 0; i < wheels.Length; i++) {

					EditorGUILayout.BeginVertical (GUI.skin.box);
					EditorGUILayout.BeginHorizontal ();

					if(wheels [i].wheelTransform)
						EditorGUILayout.LabelField (wheels [i].wheelTransform.name, EditorStyles.boldLabel);
					else
						EditorGUILayout.LabelField ("Wheel_" + i.ToString (), EditorStyles.boldLabel);
					
					GUILayout.FlexibleSpace ();

					GUI.color = Color.red;

					if (GUILayout.Button ("X"))
						RemoveWheel (i);

					GUI.color = defBackgroundColor;

					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.Space ();

					EditorGUILayout.BeginHorizontal ();

					wheels [i].wheelTransform = (Transform)EditorGUILayout.ObjectField ("Wheel Model", wheels [i].wheelTransform, typeof(Transform), true);

					if(wheels [i].wheelCollider)
						wheels [i].wheelCollider = (RTC_WheelCollider)EditorGUILayout.ObjectField ("Wheel Collider", wheels [i].wheelCollider, typeof(WheelCollider), true);

					if (wheels [i].wheelCollider == null) {
						GUI.color = Color.cyan;
						if (GUILayout.Button ("Create WheelCollider")) {
							WheelCollider newWheelCollider = RTC_CreateWheelCollider.CreateWheelCollider (tankController.gameObject, wheels [i].wheelTransform);
							wheels [i].wheelCollider = newWheelCollider.gameObject.GetComponent<RTC_WheelCollider> ();
							wheels [i].wheelCollider.wheelModel = wheels [i].wheelTransform;
							Debug.Log ("Created wheelcollider for " + wheels [i].wheelTransform.name);
						}
						GUI.color = defBackgroundColor;
					}
						
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.Space ();

					EditorGUILayout.EndVertical ();

				}

			}

			EditorGUIUtility.labelWidth = defaultLabelWidth;

			GUI.color = Color.green;
			EditorGUILayout.Space ();

			if (GUILayout.Button ("Create New Wheel Slot"))
				AddNewWheel ();

			GUI.color = defBackgroundColor;
			EditorGUILayout.Space ();

			
//			EditorGUILayout.Space();
//			EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelModel_L"), new GUIContent("Wheels Left", "Select all left wheels of your tank."), true);
//			EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelModel_R"), new GUIContent("Wheels Right", "Select all right wheels of your tank."), true);
//			EditorGUILayout.Space();

//			if(GUILayout.Button("Create Wheel Colliders")){
//				
//				WheelCollider[] wheelColliders_L = tankController.gameObject.GetComponentsInChildren<WheelCollider>();
//				
//				if(wheelColliders_L.Length >= 1){
//					foreach(WheelCollider wc in wheelColliders_L)
//						Destroy(wc);
//					tankController.CreatewheelColliders_L();
//				}else{
//					tankController.CreatewheelColliders_L();
//				}
//				
//			}

		}
		
		if(Configurations){
			
			EditorGUILayout.Space();
			GUI.color = Color.cyan;
			EditorGUILayout.HelpBox("Configurations", MessageType.None);
			GUI.color = defBackgroundColor;
			EditorGUILayout.Space();
			
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("canControl"), new GUIContent("Can Be Controllable Now", "Enables/Disables controlling the vehicle."));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRunning"), new GUIContent("Engine Running Now", "Engine is running currently."));
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("COM"), new GUIContent("Center Of Mass (''COM'')", "Center of Mass of the vehicle. Usually, COM is below around front driver seat."));
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("engineTorque"), new GUIContent("Engine Torque"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("steerTorque"), new GUIContent("Steering Torque"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeTorque"), new GUIContent("Brake Torque"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSpeed"), new GUIContent("Maximum Speed"), false);
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("minEngineRPM"), new GUIContent("Minimum Engine RPM"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maxEngineRPM"), new GUIContent("Maximum Engine RPM"), false);
			EditorGUILayout.Space();

		}
		
		if(SoundSettings){
			
			EditorGUILayout.Space();
			GUI.color = Color.cyan;
			EditorGUILayout.HelpBox("Sound Settings", MessageType.None);
			GUI.color = defBackgroundColor;
			
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("engineStartUpAudioClip"), new GUIContent("Starting Engine Sound"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("engineIdleAudioClip"), new GUIContent("Idle Engine Sound"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRunningAudioClip"), new GUIContent("Heavy Engine Sound"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeClip"), new GUIContent("Brake Sound"), false);
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Slider(serializedObject.FindProperty("minEngineSoundPitch"), .25f, 1f);
			EditorGUILayout.Slider(serializedObject.FindProperty("maxEngineSoundPitch"), 1f, 2f);
			EditorGUILayout.Slider(serializedObject.FindProperty("minEngineSoundVolume"), 0f, 1f);
			EditorGUILayout.Slider(serializedObject.FindProperty("maxEngineSoundVolume"), 0f, 1f);
			EditorGUILayout.Slider(serializedObject.FindProperty("maxBrakeSoundVolume"), 0f, 1f);
			EditorGUILayout.Space();
			
		}
		
		if(TrackSettings){
			
			EditorGUILayout.Space();
			GUI.color = Color.cyan;
			EditorGUILayout.HelpBox("Track Settings", MessageType.None);
			GUI.color = defBackgroundColor;
			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("leftTrackMesh"), new GUIContent("Track Left", "Select left track of your tank."), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("rightTrackMesh"), new GUIContent("Track Right", "Select right track of your tank."), false);
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("trackOffset"), new GUIContent("Track Height Offset", "Height offset of your tracks."), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("trackScrollSpeedMultiplier"), new GUIContent("Track Scroll Speed", "Track scroll speed multiplier."), false);
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("trackBoneTransform_L"), new GUIContent("Track Bones Left", "Select all left bones of your track."), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("trackBoneTransform_R"), new GUIContent("Track Bones Right", "Select all right bones of your track."), true);
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("uselessGearTransform_L"), new GUIContent("Useless Gears Left", "Select all left useless gears of your tank."), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("uselessGearTransform_R"), new GUIContent("Useless Gears Right", "Select all right useless gears of your tank."), true);
			EditorGUILayout.Space();
//			EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelColliders_L"), new GUIContent("Left Wheel Colliders", "Select all left wheel colliders of your tank."), true);
//			EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelColliders_R"), new GUIContent("Right Wheel Colliders", "Select all right wheel colliders of your tank."), true);


		}

		CheckSystem ();

		EditorGUILayout.EndVertical ();

		serializedObject.ApplyModifiedProperties();
		
		if(GUI.changed && !EditorApplication.isPlaying){
			
			if(RTC_Settings.Instance.setTagsAndLayers)
				SetLayerMask();
			
			tankController.EngineCurveInit();

		}
		
	}
	
	bool OpenCategory(){

		WheelSettings = false;
		Configurations = false;
		SoundSettings = false;
		TrackSettings = false;

		return true;

	}

	void AddNewWheel(){

		List<RTC_TankController.RTC_Wheels> currentWheels = new List<RTC_TankController.RTC_Wheels>();

		if(wheels != null)
			currentWheels.AddRange(wheels);

		currentWheels.Add (null);

		tankController.wheels = currentWheels.ToArray ();

	}

	void RemoveWheel(int index){

		List<RTC_TankController.RTC_Wheels> currentWheels = new List<RTC_TankController.RTC_Wheels>();

		if(wheels != null)
			currentWheels.AddRange(wheels);

		if(currentWheels [index].wheelCollider != null)
			DestroyImmediate (currentWheels [index].wheelCollider.gameObject);

		currentWheels.RemoveAt(index);

		tankController.wheels = currentWheels.ToArray ();

	}

	void CheckSystem(){

//		if(tankController.FrontLeftWheelCollider == null || carScript.FrontRightWheelCollider == null || carScript.RearLeftWheelCollider == null || carScript.RearRightWheelCollider == null)
//			EditorGUILayout.HelpBox("Wheel Colliders = NOT OK", MessageType.Error);

//		if(carScript.FrontLeftWheelTransform == null || carScript.FrontRightWheelTransform == null || carScript.RearLeftWheelTransform == null || carScript.RearRightWheelTransform == null)
//			EditorGUILayout.HelpBox("Wheel Models = NOT OK", MessageType.Error);

		if (wheels != null) {

			if(wheels.Length < 4)
				EditorGUILayout.HelpBox("Wheels = 4 wheels needed at least", MessageType.Error);
			
			for (int i = 0; i < wheels.Length; i++) {

				if(wheels[i].wheelTransform == null || wheels[i].wheelCollider == null)
					EditorGUILayout.HelpBox("Wheel number " + i.ToString() + " = NOT OK", MessageType.Error);

			}

		}

		if(tankController.COM == null)
			EditorGUILayout.HelpBox("COM = NOT OK", MessageType.Error);

		Collider[] cols = tankController.gameObject.GetComponentsInChildren<Collider>();
		int totalCountsOfWheelColliders = tankController.GetComponentsInChildren<WheelCollider>().Length;

		if(cols.Length - totalCountsOfWheelColliders <= 0)
			EditorGUILayout.HelpBox("Your vehicle MUST have any type of body Collider.", MessageType.Error);

		if(tankController.COM){

			if(Mathf.Approximately(tankController.COM.transform.localPosition.y, 0f))
				EditorGUILayout.HelpBox("You haven't changed COM position of the vehicle yet. Keep in that your mind, COM is most extremely important for realistic behavior.", MessageType.Warning);

		}else{

			EditorGUILayout.HelpBox("You haven't created COM of the vehicle yet.", MessageType.Error);

		}

		if (tankController.trackBoneTransform_L != null) {

			for (int i = 0; i < tankController.trackBoneTransform_L.Length; i++) {

				if (tankController.trackBoneTransform_L[i] != null && tankController.trackBoneTransform_L[i].GetComponent<RTC_TrackBone> () == null)
					tankController.trackBoneTransform_L[i].gameObject.AddComponent<RTC_TrackBone> ();

			}

		}

		if (tankController.trackBoneTransform_R != null) {

			for (int i = 0; i < tankController.trackBoneTransform_R.Length; i++) {

				if (tankController.trackBoneTransform_R[i] != null && tankController.trackBoneTransform_R[i].GetComponent<RTC_TrackBone> () == null)
					tankController.trackBoneTransform_R[i].gameObject.AddComponent<RTC_TrackBone> ();

			}

		}

		if (tankController.uselessGearTransform_L != null) {

			for (int i = 0; i < tankController.uselessGearTransform_L.Length; i++) {

				if (tankController.uselessGearTransform_L[i] == null)
					EditorGUILayout.HelpBox("Missing Useless Gear number " + i.ToString() + " (Left)", MessageType.Error);

			}

		}

		if (tankController.uselessGearTransform_R != null) {

			for (int i = 0; i < tankController.uselessGearTransform_R.Length; i++) {

				if (tankController.uselessGearTransform_R[i] == null)
					EditorGUILayout.HelpBox("Missing Useless Gear number " + i.ToString() + " (Right)", MessageType.Error);

			}

		}

		if (tankController.trackBoneTransform_L != null) {

			for (int i = 0; i < tankController.trackBoneTransform_L.Length; i++) {

				if (tankController.trackBoneTransform_L[i] == null)
					EditorGUILayout.HelpBox("Missing Track Bone number " + i.ToString() + " (Left)", MessageType.Error);

			}

		}

		if (tankController.trackBoneTransform_R != null) {

			for (int i = 0; i < tankController.trackBoneTransform_R.Length; i++) {

				if (tankController.trackBoneTransform_R[i] == null)
					EditorGUILayout.HelpBox("Missing Track Bone number " + i.ToString() + " (Right)", MessageType.Error);

			}

		}

	}

	void SetLayerMask(){

		if (string.IsNullOrEmpty (RTC_Settings.Instance.RTCLayer)) {
			Debug.LogError ("RTC Layer is missing in RTC Settings. Go to Tools --> BoneCracker Games --> RTC --> Edit Settings, and set the layer of RTC.");
			return;
		}

		if (string.IsNullOrEmpty (RTC_Settings.Instance.RTCTag)) {
			Debug.LogError ("RTC Tag is missing in RTC Settings. Go to Tools --> BoneCracker Games --> RTC --> Edit Settings, and set the tag of RTC.");
			return;
		}

		Transform[] allTransforms = tankController.GetComponentsInChildren<Transform>();

		foreach (Transform t in allTransforms) {

			int layerInt = LayerMask.NameToLayer (RTC_Settings.Instance.RTCLayer);

			if (layerInt >= 0 && layerInt <= 31) {

				t.gameObject.layer = LayerMask.NameToLayer (RTC_Settings.Instance.RTCLayer);

				if (!tankController.externalController) {
					if (RTC_Settings.Instance.tagAllChildrenGameobjects)
						t.gameObject.transform.tag = RTC_Settings.Instance.RTCTag;
					else
						tankController.transform.gameObject.tag = RTC_Settings.Instance.RTCTag;
				} else {
					t.gameObject.transform.tag = "Untagged";
				}

			} else {

				Debug.LogError ("RTC Layer selected in RTC Settings doesn't exist on your Tags & Layers. Go to Edit --> Project Settings --> Tags & Layers, and create a new layer named ''" + RTC_Settings.Instance.RTCLayer + "''.");
				Debug.LogError ("From now on, ''Setting Tags and Layers'' disabled in RTC Settings! You can enable this when you created this layer.");
				foreach (Transform tr in allTransforms)
					tr.gameObject.layer = LayerMask.NameToLayer ("Default");
				RTC_Settings.Instance.setTagsAndLayers = false;
				return;

			}

		}

	}
	
	void SetDefaultSettings(){

		GameObject COM = new GameObject("COM");
		COM.transform.parent = tankController.transform;
		COM.transform.localPosition = Vector3.zero;
		COM.transform.localScale = Vector3.one;
		COM.transform.rotation = tankController.transform.rotation;
		tankController.COM = COM.transform;

		firstInit = false;
		
	}
	
}
