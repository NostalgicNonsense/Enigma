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

[CustomEditor(typeof(RTC_TankGunController)), CanEditMultipleObjects]
public class RTC_GunEditor : Editor {
	
	RTC_TankGunController tankGunController;
	
	Texture2D tankGunIcon;

	bool configurations;
	
	Color defBackgroundColor;

	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Add Gun Controller To Tank", false, -90)]
	static void CreateGunBehavior(){

		if (!Selection.activeGameObject.GetComponentInParent<RTC_TankController> ()) {

			EditorUtility.DisplayDialog ("RTC_TankController Is Missing", "Your Tank Doesn't Have RTC_TankController.", "Ok");

		} else {

			if (Selection.activeGameObject.GetComponentInParent<RTC_TankController> ().gameObject.GetComponent<RTC_TankGunController> ()) {

				EditorUtility.DisplayDialog ("Your Tank Already Has Gun Controller", "Your Tank Already Has Gun Controller", "Ok");

			} else {

				Selection.activeGameObject.GetComponentInParent<RTC_TankController> ().gameObject.AddComponent<RTC_TankGunController> ();
				Selection.activeGameObject = Selection.activeGameObject.GetComponentInParent<RTC_TankController> ().gameObject;

			}

		}
		
	}
	
	void Awake(){
		
		tankGunIcon = Resources.Load("TankGunIcon", typeof(Texture2D)) as Texture2D;
		
	}
	
	public override void OnInspectorGUI () {
		
		serializedObject.Update();
		EditorGUILayout.BeginVertical (GUI.skin.button);
		
		tankGunController = (RTC_TankGunController)target;
		defBackgroundColor = GUI.backgroundColor;

		if(tankGunController.mainGun){
			if (!tankGunController.mainGun.GetComponent<HingeJoint> ())
				CreateJoint ();
		}

		EditorGUILayout.Space();
		
		EditorGUILayout.BeginHorizontal();
		
		if(configurations)
			GUI.backgroundColor = Color.gray;
		else GUI.backgroundColor = defBackgroundColor;
		
		if (GUILayout.Button (tankGunIcon))
			configurations = !configurations;
		
		GUI.backgroundColor = defBackgroundColor;
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();
		
		if(configurations){
			
			EditorGUILayout.Space();
			GUI.color = Color.cyan;
			EditorGUILayout.HelpBox("Tank Gun Configurations", MessageType.None);
			GUI.color = defBackgroundColor;
			EditorGUILayout.Space();

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("canControl"), new GUIContent("Can Be Controllable Now", "Enables/Disables controlling the Main Gun."));
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("mainGun"), new GUIContent("Main Gun", "Main Gun."));
			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("horizontalSensitivity"), new GUIContent("Horizontal Sensitivity", "Horizontal Sensitivity."));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalSensitivity"), new GUIContent("Vertical Sensitivity", "Vertical Sensitivity."));

			EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationTorque"), new GUIContent("Rotation Torque"), false);

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("barrel"), new GUIContent("Barrel of the Main Gun", "Barrel of the Main Gun."));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("barrelOut"), new GUIContent("Barrel Out Transform", "Projectile will be instantiated here."));
			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumAngularVelocity"), new GUIContent("Maximum Angular Velocity"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumRotationLimit"), new GUIContent("Maximum Rotation Limit"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumElevationLimit"), new GUIContent("Minimum Elevation Limit"), false);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumElevationLimit"), new GUIContent("Maximum Elevation Limit"), false);

			EditorGUILayout.BeginHorizontal();
			EditorGUIUtility.fieldWidth = 1f;
			EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedAmmunation"), new GUIContent("Current Selected Ammunation"), false);
			EditorGUILayout.Space();

			if (tankGunController.selectedAmmunation < 0 || tankGunController.selectedAmmunation >= RTC_Ammunation.Instance.ammunations.Length)
				tankGunController.selectedAmmunation = 0;

			GUI.color = Color.cyan;
			EditorGUILayout.HelpBox("Current Projectile: " + RTC_Ammunation.Instance.ammunations[tankGunController.selectedAmmunation].projectile.name, MessageType.None);
			GUI.color = defBackgroundColor;
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			//EditorGUILayout.PropertyField(serializedObject.FindProperty("engineTorqueCurve"), true);

		}

		CheckSystem ();

		EditorGUILayout.EndVertical ();
		
		serializedObject.ApplyModifiedProperties();
		
	}

	void CreateJoint(){

		tankGunController.mainGun.AddComponent<HingeJoint> ();
		tankGunController.mainGun.GetComponent<HingeJoint> ().connectedBody = tankGunController.GetComponent<Rigidbody>();
		tankGunController.mainGun.GetComponent<HingeJoint>().axis = Vector3.up;
		tankGunController.mainGun.GetComponent<HingeJoint>().useSpring = true;

		tankGunController.mainGun.GetComponent<Rigidbody>().mass = 1f;
		tankGunController.mainGun.GetComponent<Rigidbody> ().drag = 0f;
		tankGunController.mainGun.GetComponent<Rigidbody>().angularDrag = 0f;
		tankGunController.mainGun.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;

		JointSpring js = new JointSpring();
		js.damper = 50;
		tankGunController.mainGun.GetComponent<HingeJoint>().spring = js;

	}

	void CheckSystem(){

		if(tankGunController.mainGun == null)
			EditorGUILayout.HelpBox("Missing Main Gun", MessageType.Error);

		if (tankGunController.barrel == null)
			EditorGUILayout.HelpBox ("Missing Barrel", MessageType.Error);

		if(tankGunController.barrelOut == null)
			EditorGUILayout.HelpBox("Missing Barrel Out Transform", MessageType.Error);

		if(RTC_Ammunation.Instance.ammunations != null && RTC_Ammunation.Instance.ammunations.Length == 0)
			EditorGUILayout.HelpBox("No any ammunation in RTC_Ammunation. Create any type of ammunation for firing.", MessageType.Error);

	}
	
}
