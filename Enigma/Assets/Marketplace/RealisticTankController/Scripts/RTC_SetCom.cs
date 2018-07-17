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

public class RTC_SetCom : MonoBehaviour {

	public Vector3 COM;

	void Start () {

		GetComponent<Rigidbody>().centerOfMass = COM;
	
	}

}
