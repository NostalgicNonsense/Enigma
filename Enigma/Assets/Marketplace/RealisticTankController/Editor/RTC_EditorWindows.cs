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

public class RTC_EditorWindows : Editor {

	#region Edit Settings
	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Edit RTC Settings", false, -100)]
	public static void OpenRTCSettings(){
		Selection.activeObject = RTC_Settings.Instance;
	}
	#endregion

	#region Configure
	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Configure Ammunation", false, -70)]
	public static void OpenAmmunationSettings(){
		Selection.activeObject = RTC_Ammunation.Instance;
	}

	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Configure Ground Materials", false, -70)]
	public static void OpenGroundMaterialsSettings(){
		Selection.activeObject = RTC_GroundMaterials.Instance;
	}

	#endregion

	#region Add Cameras
	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/Cameras/Add RTC Camera To Scene", false, -50)]
	public static void CreateRTCCamera(){

		if (GameObject.FindObjectOfType<RTC_MainCamera> ()) {

			EditorUtility.DisplayDialog ("Scene has RTC Camera already!", "Scene has RTC Camera already!", "Ok");
			Selection.activeGameObject = GameObject.FindObjectOfType<RTC_MainCamera> ().gameObject;

		} else {

			GameObject cam = Instantiate (RTC_Settings.Instance.mainCamera.gameObject);
			cam.name = RTC_Settings.Instance.mainCamera.name;
			Selection.activeGameObject = cam.gameObject;

		}

	}

	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/Cameras/Add Gun Camera To Vehicle", false, -50)]
	public static void CreateGunCamera(){

		if(!Selection.activeGameObject.GetComponentInParent<RTC_TankController>()){

			EditorUtility.DisplayDialog("Select a vehicle controlled by Realistic Tank Controller!", "Select a vehicle controlled by Realistic Tank Controller!", "Ok");

		}else{

			if (!Selection.activeGameObject.GetComponentInParent<RTC_TankGunController> ()) {
				EditorUtility.DisplayDialog("Gun Camera needs RTC_TankGunController on your tank!", "Gun Camera needs RTC_TankGunController on your tank!", "Ok");
				return;
			}

			if(Selection.activeGameObject.GetComponentInParent<RTC_TankController>().gameObject.GetComponentInChildren<RTC_GunCamera>()){
				EditorUtility.DisplayDialog("Your Vehicle Has Gun Camera Already!", "Your vehicle has gun camera already!", "Ok");
				Selection.activeGameObject = Selection.activeGameObject.GetComponentInChildren<RTC_GunCamera>().gameObject;
				return;
			}

			GameObject gunCam = (GameObject)Instantiate(RTC_Settings.Instance.gunCamera, Selection.activeGameObject.GetComponentInParent<RTC_TankGunController>().barrel.transform.position, Selection.activeGameObject.GetComponentInParent<RTC_TankGunController>().barrel.transform.rotation);
			gunCam.name = RTC_Settings.Instance.gunCamera.name;
			gunCam.transform.SetParent(Selection.activeGameObject.GetComponentInParent<RTC_TankGunController>().barrel.transform, true);
			Selection.activeGameObject = gunCam;

		}

	}

	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/Cameras/Add Gun Camera To Vehicle", true)]
	public static bool CheckCreateGunCamera() {
		if(Selection.gameObjects.Length > 1 || !Selection.activeTransform)
			return false;
		else
			return true;
	}
	#endregion

	#region Add Lights
	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/Lights/Add Lights To Vehicle/HeadLight", false, -50)]
	public static void CreateHeadLight(){

		if(!Selection.activeGameObject.GetComponentInParent<RTC_TankController>()){

			EditorUtility.DisplayDialog("Select a vehicle controlled by Realistic Tank Controller!", "Select a vehicle controlled by Realistic Tank Controller!", "Ok");

		}else{

			GameObject lightsMain;

			if(!Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform.Find("Lights")){
				lightsMain = new GameObject("Lights");
				lightsMain.transform.SetParent(Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform, false);
			}else{
				lightsMain = Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform.Find("Lights").gameObject;
			}

			GameObject headLight = GameObject.Instantiate (RTC_Settings.Instance.headLight, lightsMain.transform.position, lightsMain.transform.rotation) as GameObject;
			headLight.name = RTC_Settings.Instance.headLight.name;
			headLight.transform.SetParent(lightsMain.transform);
			headLight.transform.localRotation = Quaternion.identity;
			headLight.transform.localPosition = new Vector3(0f, 0f, 2f);
			Selection.activeGameObject = headLight;

		}

	}

	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/Lights/Add Lights To Vehicle/HeadLight", true)]
	public static bool CheckHeadLight() {
		if(Selection.gameObjects.Length > 1 || !Selection.activeTransform)
			return false;
		else
			return true;
	}

	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/Lights/Add Lights To Vehicle/Brake", false, -50)]
	public static void CreateBrakeLight(){

		if(!Selection.activeGameObject.GetComponentInParent<RTC_TankController>()){

			EditorUtility.DisplayDialog("Select a vehicle controlled by Realistic Tank Controller!", "Select a vehicle controlled by Realistic Tank Controller!", "Ok");

		}else{

			GameObject lightsMain;

			if(!Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform.Find("Lights")){
				lightsMain = new GameObject("Lights");
				lightsMain.transform.SetParent(Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform, false);
			}else{
				lightsMain = Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform.Find("Lights").gameObject;
			}

			GameObject brakeLight = GameObject.Instantiate (RTC_Settings.Instance.brakeLight, lightsMain.transform.position, lightsMain.transform.rotation) as GameObject;
			brakeLight.name = RTC_Settings.Instance.brakeLight.name;
			brakeLight.transform.SetParent(lightsMain.transform);
			brakeLight.transform.localRotation = Quaternion.identity;
			brakeLight.transform.localPosition = new Vector3(0f, 0f, -2f);
			Selection.activeGameObject = brakeLight;

		}

	}

	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/Lights/Add Lights To Vehicle/Brake", true)]
	public static bool CheckBrakeLight() {
		if(Selection.gameObjects.Length > 1 || !Selection.activeTransform)
			return false;
		else
			return true;
	}
	#endregion

	#region Add UI
	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/UI/Add RTC Canvas To Scene", false, -50)]
	public static void CreateRTCCanvas(){

		if (GameObject.FindObjectOfType<RTC_UIDashboard> ()) {

			EditorUtility.DisplayDialog ("Scene has RTC Canvas already!", "Scene has RTC Canvas already!", "Ok");
			Selection.activeGameObject = GameObject.FindObjectOfType<RTC_UIDashboard> ().gameObject;

		} else {

			GameObject canvas = Instantiate (RTC_Settings.Instance.RTCCanvas);
			canvas.name = RTC_Settings.Instance.RTCCanvas.name;

		}

	}
	#endregion

	#region Add Exhausts
	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/Misc/Add Exhaust To Vehicle", false, -50)]
	public static void CreateExhaust(){

		if(!Selection.activeGameObject.GetComponentInParent<RTC_TankController>()){

			EditorUtility.DisplayDialog("Select a vehicle controlled by Realistic Tank Controller!", "Select a vehicle controlled by Realistic Tank Controller!", "Ok");

		}else{

			GameObject exhaustsMain;

			if(!Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform.Find("Exhausts")){
				exhaustsMain = new GameObject("Exhausts");
				exhaustsMain.transform.SetParent(Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform, false);
			}else{
				exhaustsMain = Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform.Find("Exhausts").gameObject;
			}

			GameObject exhaust = (GameObject)Instantiate(RTC_Settings.Instance.exhaustGas, Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform.position, Selection.activeGameObject.GetComponentInParent<RTC_TankController>().transform.rotation * Quaternion.Euler(0f, 180f, 0f));
			exhaust.name = RTC_Settings.Instance.exhaustGas.name;
			exhaust.transform.SetParent(exhaustsMain.transform);
			exhaust.transform.localPosition = new Vector3(1f, 0f, -2f);
			Selection.activeGameObject = exhaust;

		}

	}

	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Create/Misc/Add Exhaust To Vehicle", true)]
	public static bool CheckCreateExhaust() {
		if(Selection.gameObjects.Length > 1 || !Selection.activeTransform)
			return false;
		else
			return true;
	}
	#endregion

	#region Help
	[MenuItem("Tools/BoneCracker Games/Realistic Tank Controller/Help", false, 1000000)]
	static void Help(){

		EditorUtility.DisplayDialog("Contact", "Please include your invoice number while sending a contact form.", "Ok");

		string url = "http://www.bonecrackergames.com/contact/";
		Application.OpenURL (url);

	}

	#endregion Help

}
