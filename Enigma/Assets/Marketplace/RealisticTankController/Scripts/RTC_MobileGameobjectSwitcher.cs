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

[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Misc/Mobile Gameobject Switcher")]
public class RTC_MobileGameobjectSwitcher : MonoBehaviour {

	public GameObject[] gameobjectsForMobile;
	public GameObject[] gameobjectsForNotMobile;

	void Start () {

		if (Application.isMobilePlatform) {

			for (int i = 0; i < gameobjectsForMobile.Length; i++) {
				gameobjectsForMobile [i].SetActive (true);
			}

			for (int i = 0; i < gameobjectsForNotMobile.Length; i++) {
				gameobjectsForNotMobile [i].SetActive (false);
			}

		} else {

			for (int i = 0; i < gameobjectsForMobile.Length; i++) {
				gameobjectsForMobile [i].SetActive (false);
			}

			for (int i = 0; i < gameobjectsForNotMobile.Length; i++) {
				gameobjectsForNotMobile [i].SetActive (true);
			}

		}
	
	}

}
