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

[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Misc/Fragments")]
public class RTC_Fragments : MonoBehaviour {

	private Rigidbody rigid;

	private bool broken = false;

	void Awake () {

		rigid = GetComponent<Rigidbody> ();
		rigid.isKinematic = true;

	}
	
	// Update is called once per frame
	void FixedUpdate () {
	
		if(!broken)
			Checking();

	}

	void Checking(){

		if (rigid.IsSleeping())
			return;

		RaycastHit hit;
		
		if(Physics.Raycast(transform.position, -transform.forward, out hit)){
			if(hit.rigidbody && hit.rigidbody.isKinematic != true){
				rigid.isKinematic = false;
				broken = true;
			}
		}

	}

	void OnCollisionEnter (Collision collision) {

		if(collision.relativeVelocity.magnitude < 1.5f)
			return;
		
		if(collision.transform.gameObject.layer != LayerMask.NameToLayer("Fragment")){
			rigid.isKinematic = false;
		}

	}


}
