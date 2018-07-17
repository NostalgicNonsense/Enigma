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

[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Main/Wheel Collider")]
public class RTC_WheelCollider : MonoBehaviour {

	private Rigidbody rigid;
	private RTC_TankController tankController;
	public WheelCollider wheelCollider;
	public Transform wheelModel;

	internal float wheelRotation = 0f;		// Wheel model rotation based on WheelCollider rpm. 
	internal float wheelRPMToSpeed = 0f;		// Wheel RPM to Speed.

	// Getting an Instance of Ground Materials.
	#region RTC_GroundMaterials Instance

	private RTC_GroundMaterials RTCGroundMaterialsInstance;
	private RTC_GroundMaterials RTCGroundMaterials {
		get {
			if (RTCGroundMaterialsInstance == null) {
				RTCGroundMaterialsInstance = RTC_GroundMaterials.Instance;
			}
			return RTCGroundMaterialsInstance;
		}
	}

	#endregion

	// Ground Materials.
	private RTC_GroundMaterials physicsMaterials{get{return RTCGroundMaterials;}}		// Instance of Configurable Ground Materials.
	private RTC_GroundMaterials.GroundMaterialFrictions[] physicsFrictions{get{return RTCGroundMaterials.frictions;}}

	// WheelFriction Curves and Stiffness.
	public WheelFrictionCurve forwardFrictionCurve;
	public WheelFrictionCurve sidewaysFrictionCurve;

	// Original WheelFriction Curves and Stiffness.
	private float orgForwardStiffness = 1f;		// Default forward stiffness.
	private float orgSidewaysStiffness = 1f;		// Default sideways stiffness.

	// List for all particle systems.
	internal List<ParticleSystem> allWheelParticles = new List<ParticleSystem>();
	internal ParticleSystem.EmissionModule emission;

	void Awake () {

		rigid = GetComponentInParent<Rigidbody> ();
		tankController = GetComponentInParent<RTC_TankController> ();
		wheelCollider = GetComponent<WheelCollider> ();
		//wheelCollider.ConfigureVehicleSubsteps (10000f, 1, 1);

		// Increasing WheelCollider mass for avoiding unstable behavior. Only in Unity 5.
		wheelCollider.mass = (rigid.mass / 10f) / tankController.GetComponentsInChildren<RTC_WheelCollider>().Length;

		// Getting friction curves.
		forwardFrictionCurve = wheelCollider.forwardFriction;
		sidewaysFrictionCurve = wheelCollider.sidewaysFriction;

		// Getting default stiffness.
		orgForwardStiffness = forwardFrictionCurve.stiffness;
		orgSidewaysStiffness = sidewaysFrictionCurve.stiffness;

		// Assigning new frictons if one of the behavior preset selected above.
		wheelCollider.forwardFriction = forwardFrictionCurve;
		wheelCollider.sidewaysFriction = sidewaysFrictionCurve;

		// Creating all ground particles, and adding them to list.
		for (int i = 0; i < RTCGroundMaterials.frictions.Length; i++) {

			GameObject ps = (GameObject)Instantiate (RTCGroundMaterials.frictions [i].groundParticles, transform.position, transform.rotation) as GameObject;
			emission = ps.GetComponent<ParticleSystem> ().emission;
			emission.enabled = false;
			ps.transform.SetParent (transform, false);
			ps.transform.localPosition = Vector3.zero;
			ps.transform.localRotation = Quaternion.identity;
			allWheelParticles.Add (ps.GetComponent<ParticleSystem> ());

		}
	
	}

	void Update () {

		WheelAlign ();
	
	}
		
	void FixedUpdate () {

		wheelRPMToSpeed = (((wheelCollider.rpm * wheelCollider.radius) / 2.9f)) * rigid.transform.lossyScale.y;

		Frictions();

	}

	// Aligning wheel model position and rotation.
	public void WheelAlign (){

		// Return if no wheel model selected.
		if(!wheelModel){
			Debug.LogError(transform.name + " wheel of the " + tankController.transform.name + " is missing wheel model. This wheel is disabled");
			enabled = false;
			return;
		}

		// First, we are getting groundhit data.
		RaycastHit hit;
		WheelHit CorrespondingGroundHit;

		// Taking WheelCollider center position.
		Vector3 ColliderCenterPoint = wheelCollider.transform.TransformPoint(wheelCollider.center);
		wheelCollider.GetGroundHit(out CorrespondingGroundHit);

		// Here we are raycasting to downwards.
		if(Physics.Raycast(ColliderCenterPoint, -wheelCollider.transform.up, out hit, (wheelCollider.suspensionDistance + wheelCollider.radius) * transform.localScale.y) && !hit.transform.IsChildOf(tankController.transform) && !hit.collider.isTrigger){
			// Assigning position of the wheel if we have hit.
			wheelModel.transform.position = hit.point + (wheelCollider.transform.up * wheelCollider.radius) * transform.localScale.y;
			float extension = (-wheelCollider.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - wheelCollider.radius) / wheelCollider.suspensionDistance;
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + wheelCollider.transform.up * (CorrespondingGroundHit.force / rigid.mass), extension <= 0.0 ? Color.magenta : Color.white);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - wheelCollider.transform.forward * CorrespondingGroundHit.forwardSlip * 2f, Color.green);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - wheelCollider.transform.right * CorrespondingGroundHit.sidewaysSlip * 2f, Color.red);
		}else{
			// Assigning position of the wheel to default position if we don't have hit.
			wheelModel.transform.position = ColliderCenterPoint - (wheelCollider.transform.up * wheelCollider.suspensionDistance) * transform.localScale.y;
		}

		// X axis rotation of the wheel.
		wheelRotation += wheelCollider.rpm * 6 * Time.deltaTime;

		// Assigning rotation of the wheel (X and Y axises).
		wheelModel.transform.rotation = wheelCollider.transform.rotation * Quaternion.Euler(wheelRotation, wheelCollider.steerAngle, wheelCollider.transform.rotation.z);

	}

	public void ApplyMotorTorque(float torque){

		if (OverTorque ())
			torque = 0f;
			
		wheelCollider.motorTorque = torque;

	}

	public void ApplyBrakeTorque(float brake){

		wheelCollider.brakeTorque = brake;

	}

	// Setting ground frictions to wheel frictions.
	void Frictions(){

		// First, we are getting groundhit data.
		WheelHit GroundHit;
		wheelCollider.GetGroundHit(out GroundHit);

		// Contacted any physic material in Configurable Ground Materials yet?
		bool contacted = false;

		for (int i = 0; i < physicsFrictions.Length; i++) {

			if(GroundHit.point != Vector3.zero && GroundHit.collider.sharedMaterial == physicsFrictions[i].groundMaterial){
				
				contacted = true;

				// Setting wheel stiffness to ground physic material stiffness.
				forwardFrictionCurve.stiffness = physicsFrictions[i].forwardStiffness;
				sidewaysFrictionCurve.stiffness = physicsFrictions[i].sidewaysStiffness;

				// Setting new friction curves to wheels.
				wheelCollider.forwardFriction = forwardFrictionCurve;
				wheelCollider.sidewaysFriction = sidewaysFrictionCurve;

				// Also damp too.
				wheelCollider.wheelDampingRate = Mathf.Lerp(physicsFrictions[i].maximumDamp, physicsFrictions[i].minimumDamp, tankController._gasInput);

				// Set emission to ground physic material smoke.
				emission = allWheelParticles[i].emission;

				// If wheel slip is bigger than ground physic material slip, enable particles. Otherwise, disable particles.
				if (wheelRPMToSpeed > 10f)
					emission.enabled = true;
				else
					emission.enabled = false;

			}

		}

		// If ground pyhsic material is not one of the ground material in Configurable Ground Materials, check if we are on terrain collider...
		if(!contacted && physicsMaterials.useTerrainSplatMapForGroundFrictions){
			
			for (int k = 0; k < physicsMaterials.terrainSplatMapIndex.Length; k++) {
				
				// If current ground is terrain collider...
				if(GroundHit.point != Vector3.zero && GroundHit.collider.sharedMaterial == physicsMaterials.terrainPhysicMaterial){
					
					// Getting current exact position by splatmap of the terrain.
					if(RTC_TerrainSurface.GetTextureMix(transform.position) != null && RTC_TerrainSurface.GetTextureMix(transform.position)[k] > .5f){
						
						contacted = true;

						// Setting wheel stiffness to ground physic material stiffness.
						forwardFrictionCurve.stiffness = physicsFrictions[physicsMaterials.terrainSplatMapIndex[k]].forwardStiffness;
						sidewaysFrictionCurve.stiffness = (physicsFrictions[physicsMaterials.terrainSplatMapIndex[k]].sidewaysStiffness);

						// Setting new friction curves to wheels.
						wheelCollider.forwardFriction = forwardFrictionCurve;
						wheelCollider.sidewaysFriction = sidewaysFrictionCurve;

						// Also damp too.
						wheelCollider.wheelDampingRate = Mathf.Lerp(physicsFrictions[physicsMaterials.terrainSplatMapIndex[k]].maximumDamp, physicsFrictions[physicsMaterials.terrainSplatMapIndex[k]].minimumDamp, tankController._gasInput);

						// Set emission to ground physic material smoke.
						emission = allWheelParticles[physicsMaterials.terrainSplatMapIndex[k]].emission;


						// If wheel slip is bigger than ground physic material slip, enable particles. Otherwise, disable particles.
						if (wheelRPMToSpeed > 10f)
							emission.enabled = true;
						else
							emission.enabled = false;

					}

				}

			}

		}

		// If wheel still not contacted any of ground material in Configurable Ground Materials, set it to original default values.
		if(!contacted){

			// Setting default stiffness to ground physic material stiffness.
			forwardFrictionCurve.stiffness = orgForwardStiffness;
			sidewaysFrictionCurve.stiffness = orgSidewaysStiffness;

			// Setting default friction curves to wheels.
			wheelCollider.forwardFriction = forwardFrictionCurve;
			wheelCollider.sidewaysFriction = sidewaysFrictionCurve;

			// Also default damp too.
			wheelCollider.wheelDampingRate = Mathf.Lerp(physicsFrictions[0].maximumDamp, physicsFrictions[0].minimumDamp, tankController._gasInput);

			// If dontUseAnyParticleEffects bool is not selected in RTC_Settings, set emission to ground physic material smoke to default one.
			emission = allWheelParticles[0].emission;

			// If wheel slip is bigger than ground physic material slip, enable particles. Otherwise, disable particles.

			if (wheelRPMToSpeed > 10f)
				emission.enabled = true;
			else
				emission.enabled = false;

		}

		// Last check if wheel has enabled smoke particles. If slip is smaller than target slip value, disable all of them.

		for (int i = 0; i < allWheelParticles.Count; i++) {

			if (wheelRPMToSpeed > 10f){
				
			} else {
				emission = allWheelParticles [i].emission;
				emission.enabled = false;
			}

		}

	}

//	public void Braking (){
//
//		for(int i = 0; i < allWheelColliders.Count; i++){
//
//			if(brakeInput > .1f && !reversing){
//				ApplyBrakeTorque(allWheelColliders[i], brakeTorque * (brakeInput));
//			}else if(brakeInput > .1f && reversing){
//				ApplyBrakeTorque(allWheelColliders[i], 0f);
//			}else if(gasInput < .1f && Mathf.Abs(steerInput) < .1f){
//				ApplyBrakeTorque(allWheelColliders[i], 10f);
//			}else{
//				ApplyBrakeTorque(allWheelColliders[i], 0f);
//			}
//
//		}
//
//	}

	private bool OverTorque(){

		if(tankController.speed > tankController.maxSpeed || !wheelCollider.isGrounded)
			return true;

		return false;

	}

}
