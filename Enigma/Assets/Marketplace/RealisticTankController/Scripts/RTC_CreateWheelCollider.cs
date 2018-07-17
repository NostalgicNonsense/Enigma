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
using System.Collections.Generic;

// Class was used for creating new WheelColliders on Editor.
public class RTC_CreateWheelCollider : MonoBehaviour {

	// Method was used for creating new WheelColliders on Editor.
	public static WheelCollider CreateWheelCollider (GameObject vehicle, Transform wheel){

		// If we don't have any wheelmodels, throw an error.
		if(!wheel){
			Debug.LogError("You haven't selected your Wheel Model. Please your Wheel Model before creating Wheel Colliders. Script needs to know their sizes and positions, aye?");
			return null;
		}

		// Holding default rotation.
		Quaternion currentRotation = vehicle.transform.rotation;

		// Resetting rotation.
		vehicle.transform.rotation = Quaternion.identity;

		// Creating a new gameobject called Wheel Colliders for all Wheel Colliders, and parenting it to this gameobject.
		GameObject wheelColliders;

		if (!vehicle.transform.Find ("Wheel Colliders")) {
			
			wheelColliders = new GameObject ("Wheel Colliders");
			wheelColliders.transform.SetParent (vehicle.transform, false);
			wheelColliders.transform.localRotation = Quaternion.identity;
			wheelColliders.transform.localPosition = Vector3.zero;
			wheelColliders.transform.localScale = Vector3.one;

		} else {

			wheelColliders = vehicle.transform.Find ("Wheel Colliders").gameObject;

		}

		GameObject wheelcollider = new GameObject(wheel.transform.name); 

		wheelcollider.transform.position = wheel.transform.position;
		wheelcollider.transform.rotation = vehicle.transform.rotation;
		wheelcollider.transform.name = wheel.transform.name;
		wheelcollider.transform.SetParent(wheelColliders.transform);
		wheelcollider.transform.localScale = Vector3.one;
		wheelcollider.AddComponent<WheelCollider>();

		Bounds biggestBound = new Bounds();
		Renderer[] renderers = wheel.GetComponentsInChildren<Renderer>();

		foreach (Renderer render in renderers) {
			if(render.bounds.size.z > biggestBound.size.z)
				biggestBound = render.bounds;
		}

		wheelcollider.GetComponent<WheelCollider>().radius = (biggestBound.extents.y) / vehicle.transform.localScale.y;
		wheelcollider.AddComponent<RTC_WheelCollider>();

		JointSpring spring = wheelcollider.GetComponent<WheelCollider>().suspensionSpring;

		spring.spring = 500000f;
		spring.damper = 10000f;
		spring.targetPosition = .25f;

		wheelcollider.GetComponent<WheelCollider>().suspensionSpring = spring;
		wheelcollider.GetComponent<WheelCollider>().suspensionDistance = .3f;
		wheelcollider.GetComponent<WheelCollider>().forceAppPointDistance = .1f;
		wheelcollider.GetComponent<WheelCollider>().mass = 40f;
		wheelcollider.GetComponent<WheelCollider>().wheelDampingRate = 1f;

		WheelFrictionCurve sidewaysFriction;
		WheelFrictionCurve forwardFriction;

		sidewaysFriction = wheelcollider.GetComponent<WheelCollider>().sidewaysFriction;
		forwardFriction = wheelcollider.GetComponent<WheelCollider>().forwardFriction;

		forwardFriction.extremumSlip = .3f;
		forwardFriction.extremumValue = 1;
		forwardFriction.asymptoteSlip = .8f;
		forwardFriction.asymptoteValue = .6f;
		forwardFriction.stiffness = 1.5f;

		sidewaysFriction.extremumSlip = .3f;
		sidewaysFriction.extremumValue = 1;
		sidewaysFriction.asymptoteSlip = .5f;
		sidewaysFriction.asymptoteValue = .8f;
		sidewaysFriction.stiffness = 1.5f;

		wheelcollider.GetComponent<WheelCollider>().sidewaysFriction = sidewaysFriction;
		wheelcollider.GetComponent<WheelCollider>().forwardFriction = forwardFriction;

		vehicle.transform.rotation = currentRotation;

		return wheelcollider.GetComponent<WheelCollider> ();

	}

}
