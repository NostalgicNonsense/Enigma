//----------------------------------------------
//            Realistic Tank Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI buttons used in options panel. It has an enum for all kind of buttons. 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/UI/Dashboard Button")]
public class RTC_UIDashboardButton : MonoBehaviour {

	public ButtonType _buttonType;
	public enum ButtonType{Fire, Start, Headlights, ChangeCamera, ChangeAmmunation};

	private RTC_TankController[] tankControllers;

	public void OnClicked () {

		tankControllers = GameObject.FindObjectsOfType<RTC_TankController>();

		switch(_buttonType){

		case ButtonType.Fire:

			for(int i = 0; i < tankControllers.Length; i++){

				if(tankControllers[i].canControl && !tankControllers[i].externalController)
					tankControllers[i].GetComponent<RTC_TankGunController>().Fire();

			}
			
			break;

		case ButtonType.Start:

			for(int i = 0; i < tankControllers.Length; i++){

				if (tankControllers [i].canControl && !tankControllers [i].externalController) {
					if(!tankControllers [i].engineRunning)
						tankControllers [i].StartEngine ();
					else
						tankControllers [i].KillEngine ();
				}

			}

			break;

		case ButtonType.Headlights:

			for(int i = 0; i < tankControllers.Length; i++){

				tankControllers[i].headLightsOn = !tankControllers[i].headLightsOn;

			}

			break;

		case ButtonType.ChangeCamera:

			GameObject.FindObjectOfType<RTC_MainCamera> ().ChangeCamera ();

			break;

		case ButtonType.ChangeAmmunation:

			for(int i = 0; i < tankControllers.Length; i++){

				if(tankControllers[i].canControl && !tankControllers[i].externalController)
					tankControllers[i].GetComponent<RTC_TankGunController>().ChangeAmmunation();

			}

			break;

		}

	}

}
