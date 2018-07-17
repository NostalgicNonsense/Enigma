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

[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Misc/Track Bone")]
public class RTC_TrackBone : MonoBehaviour {

	private RTC_TankController tankController;
	private RTC_WheelCollider anyWheel;
	private GameObject pivot;

	void Start () {

		tankController = GetComponentInParent<RTC_TankController> ();
		anyWheel = tankController.wheelColliders_L [0];

		pivot = new GameObject (transform.name);
		pivot.transform.SetParent (transform, false);
		pivot.transform.SetParent (transform.parent, true);
		transform.SetParent (pivot.transform, true);
	
	}

	void FixedUpdate () {

		RaycastHit hit;
		Vector3 centerPos = new Vector3 (pivot.transform.position.x, pivot.transform.position.y + .2f, pivot.transform.position.z);

		if (Physics.Raycast (centerPos, -tankController.transform.up, out hit, anyWheel.wheelCollider.suspensionDistance + .2f) && !hit.collider.isTrigger && !hit.transform.IsChildOf(tankController.transform)) {
			
			transform.position = hit.point + (Vector3.up * .05f);

		} else {

			transform.localPosition = new Vector3 (0f, 0f, -anyWheel.wheelCollider.suspensionDistance);

		}
	
	}

}
