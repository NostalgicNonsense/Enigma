//----------------------------------------------
//            Realistic Tank Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

#pragma warning disable 0414

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
#if RTC_REWIRED
using Rewired;
#endif

[RequireComponent (typeof (Rigidbody))]
[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Main/Tank Controller")]
public class RTC_TankController : MonoBehaviour {

	// Getting an Instance of Main Shared RTC Settings.
	#region RTC Settings Instance

	private RTC_Settings RTCSettingsInstance;
	private RTC_Settings RTCSettings {
		get {
			if (RTCSettingsInstance == null) {
				RTCSettingsInstance = RTC_Settings.Instance;
			}
			return RTCSettingsInstance;
		}
	}

	#endregion

	private Rigidbody rigid;	// Rigidbody.
	public RTC_Wheels[] wheels;	// Wheels.

	[System.Serializable]
	public class RTC_Wheels{

		public Transform wheelTransform;
		public RTC_WheelCollider wheelCollider;

	}
		
	public bool canControl = true;		// Enables/Disables controlling the vehicle.
	public bool engineRunning = false;		// Engine is running now?
	private bool engineStarting = false;		// Engine is starting now?
	public bool externalController = false;		// AI / External controller.
	
	// Reversing.
	private bool canGoReverseNow = false;
	private float reverseDelay = 0f;
	private float resetTime = 0f;

	// All Wheel Colliders.
	public List <RTC_WheelCollider> allWheelColliders = new List<RTC_WheelCollider>();

	// Left and right Wheel Colliders of the vehicle.
	public List<RTC_WheelCollider> wheelColliders_L = new List<RTC_WheelCollider>();
	public List<RTC_WheelCollider> wheelColliders_R = new List<RTC_WheelCollider>();
		
	// Left and right useless gear wheels.
	public Transform[] uselessGearTransform_L;
	public Transform[] uselessGearTransform_R;
		
	// Left and right track bones.
	public Transform[] trackBoneTransform_L;
	public Transform[] trackBoneTransform_R;
		
	// Track customization.
	public Renderer leftTrackMesh;
	public Renderer rightTrackMesh;
	public float trackOffset = 0.025f;
	public float trackScrollSpeedMultiplier = 1f;

	// Left and right average wheel RPM.
	public float averageWheelColliderRPM_L = 0f;
	public float averageWheelColliderRPM_R = 0f;

	// Center of mass.
	public Transform COM;

	// Mechanic.
	public AnimationCurve engineTorqueCurve;
	public float engineTorque = 15000.0f;
	public float brakeTorque = 15000.0f; 
	public float minEngineRPM = 600.0f;
	public float maxEngineRPM = 5000.0f;
	public float maxSpeed = 60f;
	public float orgMaxSpeed = 60f;
	public float steerTorque = 5f;
	public float speed = 0f;
	private float engineRPM = 0.0f;
	
	// Inputs.
	public float gasInput = 0f;
	public float brakeInput = 0f;
	public float steerInput = 0f;
	public float fuelInput = 1f;
	public int direction = 1;
	 
	#region Processed Inputs
	// Processed Inputs. Do not feed these values on your own script. Feed above inputs.
	internal float _gasInput{get{

			if(fuelInput <= .25f)
				return 0f;

			return (direction == 1 ? Mathf.Clamp01(gasInput) : Mathf.Clamp01(brakeInput));

		}set{gasInput = value;}}

	internal float _brakeInput{get{

			return (direction == 1 ? Mathf.Clamp01(brakeInput) : Mathf.Clamp01(gasInput));

		}set{brakeInput = value;}}

	#endregion

	// Sound effects.
	private AudioSource engineStartUpAudio;
	private AudioSource engineIdleAudio;
	private AudioSource engineRunningAudio;
	private AudioSource brakeAudio;
		
	// Audio sources.
	public AudioClip engineStartUpAudioClip;
	public AudioClip engineIdleAudioClip;
	public AudioClip engineRunningAudioClip;
	public AudioClip brakeClip;

	// Volume and pitch limits.
	public float minEngineSoundPitch = .5f;
	public float maxEngineSoundPitch = 1.15f;
	public float minEngineSoundVolume = .05f;
	public float maxEngineSoundVolume = .85f;
	public float maxBrakeSoundVolume = .35f;

	public bool headLightsOn = false;		// Headlights bool.

	#if RTC_REWIRED
	private static Player player;
	#endif

	// Event fired when new tank spawned / re-enabled on the scene.
	public delegate void onRTCSpawned(RTC_TankController RTC);
	public static event onRTCSpawned OnRTCSpawned;
		
	void Awake (){

		WheelCollidersInit();		// Getting all wheels and seperate them as left or right wheel.
		SoundsInit();		// Creating new audio sources for engine and brake sounds.

		if (RTCSettings.overrideFixedTimeStep)
			Time.fixedDeltaTime = RTCSettings.fixedTimeStep;

		rigid = GetComponent<Rigidbody>();		// Getting rigidbody of the tank.
		rigid.maxAngularVelocity = RTCSettings.maxAngularVelocity;		// Setting maximum angular velocity of the tank.
		rigid.centerOfMass = new Vector3((COM.localPosition.x) * transform.localScale.x , (COM.localPosition.y) * transform.localScale.y , (COM.localPosition.z) * transform.localScale.z);		// Setting center of mass of the tank.

		// If run engine at awake enabled, it will start the engine at start.
		if(RTCSettings.runEngineAtAwake)
			StartEngine();

		#if RTC_REWIRED
		player = Rewired.ReInput.players.GetPlayer(0);
		#endif

	}

	void Start(){

		// Event fired when new tank spawned / re-enabled on the scene.
		if (OnRTCSpawned != null)
			OnRTCSpawned (this);

	}

	void OnEnable(){

		// Event fired when new tank spawned / re-enabled on the scene.
		if (OnRTCSpawned != null)
			OnRTCSpawned (this);

	}

	void WheelCollidersInit(){

		// Getting all wheel colliders.
		RTC_WheelCollider[] wheelcolliders = GetComponentsInChildren<RTC_WheelCollider>();

		// Adding them to new list.
		foreach(RTC_WheelCollider wc in wheelcolliders){
			allWheelColliders.Add (wc);
		}

		// Seperating all wheel colliders as left or right wheel collider.
		foreach (RTC_WheelCollider wc in allWheelColliders) {
			if (wc.transform.localPosition.x > 0)
				wheelColliders_R.Add (wc);
			else
				wheelColliders_L.Add (wc);
				
		}

	}
		
	void SoundsInit(){

		engineIdleAudio = RTC_CreateAudioSource.NewAudioSource(gameObject, "engineIdleAudio", 5f, 50f, 0f, engineIdleAudioClip, true, true, false);
		engineRunningAudio = RTC_CreateAudioSource.NewAudioSource(gameObject, "engineRunningAudio", 5f, 50f, 0f, engineRunningAudioClip, true, true, false);
		brakeAudio = RTC_CreateAudioSource.NewAudioSource(gameObject, "Brake Sound AudioSource", 5f, 50f, 0, brakeClip, true, true, false);
	
	}

	public void StartEngine (){

		if (!engineRunning)
			StartCoroutine("StartEngineDelayed");

	}

	public void KillEngine (){

		if (engineRunning)
			engineRunning = false;

	}

	private IEnumerator StartEngineDelayed (){
		
		engineRunning = false;
		engineStarting = true;

		if(!engineRunning)
			engineStartUpAudio = RTC_CreateAudioSource.NewAudioSource(gameObject, "Engine Start AudioSource", 5f, 50f, 1f, engineStartUpAudioClip, false, true, true);
		
		yield return new WaitForSeconds(1f);
		engineRunning = true;
		yield return new WaitForSeconds(1f);
		engineStarting = false;
		
	}

	void Update(){

		TrackAnimation ();		// Used to scroll track textures.
		AnimateGears();		// Used to rotate useless gears.
		Sounds ();		// Used to adjust volume and pitch of the audio sources.
		Reset ();		// Used to reset the tank if flipped over.

	}
		
	void FixedUpdate (){
			
		Inputs();
		Engine();

		// If maximum speed is changed on runtime, recreate engine curve.
		if (orgMaxSpeed != maxSpeed)
			EngineCurveInit ();

		float rpm_L = 0f;

		for (int i = 0; i < wheelColliders_L.Count; i++)
			rpm_L += wheelColliders_L [i].wheelRPMToSpeed;

		averageWheelColliderRPM_L = rpm_L / wheelColliders_L.Count;

		float rpm_R = 0f;

		for (int i = 0; i < wheelColliders_R.Count; i++)
			rpm_R += wheelColliders_R [i].wheelRPMToSpeed;

		averageWheelColliderRPM_R = rpm_R / wheelColliders_R.Count;

	}

	void Inputs(){

		// If this tank is not controllable, leave all inputs to 0 and return.
		if(!canControl){
			gasInput = 0;
			brakeInput = 0;
			steerInput = 0;
			return;
		}

		switch (RTCSettings.controllerType) {

		case RTC_Settings.ControllerType.Keyboard:
			
			// Motor input.
			gasInput = Input.GetAxis (RTCSettings.gasInput);
			// Steering input.
			steerInput = Input.GetAxis (RTCSettings.steerInput);
			// Brake input
			brakeInput = -Mathf.Clamp (Input.GetAxis (RTCSettings.gasInput), -1f, 0f);
			// Headlights.

			if (Input.GetKeyDown (RTCSettings.headlightsKB))
				headLightsOn = !headLightsOn;

			if (Input.GetKeyDown (RTCSettings.startEngineKB)) {
				if (!engineRunning)
					StartEngine ();
				else
					KillEngine ();
			}

			break;

		case RTC_Settings.ControllerType.Mobile:

			gasInput = RTC_UIMobileButtons.Instance.GetValues ().controllingVertical;
			steerInput = RTC_UIMobileButtons.Instance.GetValues ().controllingHorizontal;
			brakeInput = Mathf.Clamp01(-RTC_UIMobileButtons.Instance.GetValues ().controllingVertical);
			
			break;

		case RTC_Settings.ControllerType.Custom:

			#if RTC_REWIRED

			gasInput = player.GetAxis (RTCSettings.RW_gasInput);
			steerInput = player.GetAxis (RTCSettings.RW_steerInput);
			brakeInput = -Mathf.Clamp (player.GetAxis (RTCSettings.RW_gasInput), -1f, 0f);

			if (player.GetButtonUp (RTCSettings.RW_headlightsKB))
				headLightsOn = !headLightsOn;

			if (player.GetButtonDown (RTCSettings.RW_startEngineKB)){
				if (!engineRunning)
					StartEngine ();
				else
					KillEngine ();
			}
			
			#endif

			break;

		}

	}
		
	void Engine(){

		speed = rigid.velocity.magnitude * 3.6f;

		if(engineRunning)
			fuelInput = 1;
		else
			fuelInput = 0;

		float wheelRPM = (Mathf.Abs((averageWheelColliderRPM_L) + (averageWheelColliderRPM_R)) / 2f);

		float rpm = Mathf.Clamp((Mathf.Lerp(minEngineRPM, maxEngineRPM, wheelRPM / maxSpeed) + minEngineRPM) * (Mathf.Clamp01(_gasInput + Mathf.Abs(steerInput))), minEngineRPM, maxEngineRPM);

		engineRPM = Mathf.Lerp(engineRPM, (rpm + UnityEngine.Random.Range(-50f, 50f)) * fuelInput, Time.deltaTime * 2f);

		//Reversing Bool.
		if(gasInput < 0  && transform.InverseTransformDirection(rigid.velocity).z < 1 && canGoReverseNow)
			direction = -1;
		else
			direction = 1;

		// Reversing.
		if(brakeInput > .1f && speed < 5)
			reverseDelay += Time.deltaTime;
		else if(brakeInput > 0 && transform.InverseTransformDirection(rigid.velocity).z > 1f)
			reverseDelay = 0f;

		if(reverseDelay >= .5f)
			canGoReverseNow = true;
		else
			canGoReverseNow = false;

		// Applying motor and brake torques to the left wheels.
		for(int i = 0; i < wheelColliders_L.Count; i++){

			wheelColliders_L[i].ApplyMotorTorque(((engineTorque * Mathf.Clamp((_gasInput * direction) + steerInput, -1f, 1f)) * engineTorqueCurve.Evaluate(speed)) * fuelInput);
			wheelColliders_L [i].ApplyBrakeTorque(brakeTorque * (_brakeInput));

		}

		// Applying motor and brake torques to the right wheels.
		for(int i = 0; i < wheelColliders_R.Count; i++){
			
			wheelColliders_R[i].ApplyMotorTorque(((engineTorque * Mathf.Clamp((_gasInput * direction) - steerInput, -1f, 1f)) * engineTorqueCurve.Evaluate(speed)) * fuelInput);
			wheelColliders_R [i].ApplyBrakeTorque(brakeTorque * (_brakeInput));
			
		}

		//Steering.
		if(wheelColliders_L[wheelColliders_L.Count / 2].wheelCollider.isGrounded || wheelColliders_R[wheelColliders_R.Count / 2].wheelCollider.isGrounded){
			if(Mathf.Abs(rigid.angularVelocity.y) < 1f){
				rigid.AddRelativeTorque(((Vector3.up * steerInput) * steerTorque) * fuelInput, ForceMode.Acceleration);
			}
		}
			
	}

	public void Sounds(){
		
		// Engine audio source volume.
		if(engineRunningAudioClip){
			
			engineRunningAudio.volume = Mathf.Lerp (engineRunningAudio.volume, Mathf.Clamp (Mathf.Clamp01(_gasInput + Mathf.Abs(steerInput / 2f)), minEngineSoundVolume, maxEngineSoundVolume), Time.deltaTime * 10f);
			
			if(engineRunning)
				engineRunningAudio.pitch = Mathf.Lerp (engineRunningAudio.pitch, Mathf.Lerp (minEngineSoundPitch, maxEngineSoundPitch, (engineRPM) / (maxEngineRPM)), Time.deltaTime * 10f);
			else
				engineRunningAudio.pitch = Mathf.Lerp (engineRunningAudio.pitch, 0, Time.deltaTime * 10f);
			
		}
		
		if(engineIdleAudioClip){
			
			engineIdleAudio.volume = Mathf.Lerp (engineIdleAudio.volume, Mathf.Clamp ((1 + (_gasInput)), minEngineSoundVolume, 1f), Time.deltaTime * 10f);
			
			if(engineRunning)
				engineIdleAudio.pitch = Mathf.Lerp (engineIdleAudio.pitch, Mathf.Lerp (minEngineSoundPitch, maxEngineSoundPitch, (engineRPM) / (maxEngineRPM)), Time.deltaTime * 10f);
			else
				engineIdleAudio.pitch = Mathf.Lerp (engineIdleAudio.pitch, 0, Time.deltaTime * 10f);
			
		}
			
		brakeAudio.volume = Mathf.Lerp (0f, maxBrakeSoundVolume, Mathf.Clamp01(brakeInput) * Mathf.Lerp(0f, 1f, wheelColliders_L[2].wheelCollider.rpm / 50f));
		
	}
		
	void AnimateGears(){
		
			for(int i = 0; i < uselessGearTransform_R.Length; i++){
				uselessGearTransform_R[i].transform.rotation = wheelColliders_R[Mathf.CeilToInt((wheelColliders_R.Count) / 2)].transform.rotation * Quaternion.Euler(wheelColliders_R[Mathf.CeilToInt((wheelColliders_R.Count) / 2)].wheelRotation, 0f, 0f);
			}
			
			for(int i = 0; i < uselessGearTransform_L.Length; i++){
				uselessGearTransform_L[i].transform.rotation = wheelColliders_L[Mathf.CeilToInt((wheelColliders_L.Count) / 2)].transform.rotation * Quaternion.Euler( wheelColliders_L[Mathf.CeilToInt((wheelColliders_L.Count) / 2)].wheelRotation, 0f, 0f);
			}
			
	}

	void TrackAnimation(){

		// Scrolling Track Texture Offset.
		if(leftTrackMesh && rightTrackMesh){
			
			leftTrackMesh.material.SetTextureOffset("_MainTex", new Vector2((wheelColliders_L[Mathf.CeilToInt((wheelColliders_L.Count) / 2)].wheelRotation / 1000f) * trackScrollSpeedMultiplier, 0));
			leftTrackMesh.material.SetTextureOffset("_BumpMap", new Vector2((wheelColliders_L[Mathf.CeilToInt((wheelColliders_L.Count) / 2)].wheelRotation / 1000f) * trackScrollSpeedMultiplier, 0));
			rightTrackMesh.material.SetTextureOffset("_MainTex", new Vector2((wheelColliders_R[Mathf.CeilToInt((wheelColliders_R.Count) / 2)].wheelRotation / 1000f) * trackScrollSpeedMultiplier, 0));
			rightTrackMesh.material.SetTextureOffset("_BumpMap", new Vector2((wheelColliders_R[Mathf.CeilToInt((wheelColliders_R.Count) / 2)].wheelRotation / 1000f) * trackScrollSpeedMultiplier, 0));

		}

	}

	public void EngineCurveInit (){

		// Creating a curve based on tank's maximum speed. This curve will be used with engine torque.
		engineTorqueCurve = new AnimationCurve(new Keyframe(0, 1));
		engineTorqueCurve.AddKey(new Keyframe(maxSpeed, 0));
		engineTorqueCurve.postWrapMode = WrapMode.Clamp;

		orgMaxSpeed = maxSpeed;

	}

	public void Reset(){

		if (!RTCSettings.autoReset)
			return;

		if(speed < 5 && !rigid.isKinematic){

			if(transform.eulerAngles.z < 300 && transform.eulerAngles.z > 60){
				resetTime += Time.deltaTime;
				if(resetTime > 3){
					transform.rotation = Quaternion.Euler (0f, transform.eulerAngles.y, 0f);
					transform.position = new Vector3(transform.position.x, transform.position.y + 3, transform.position.z);
					resetTime = 0f;
				}
			}

		}

	}

	// Global Enter-Exit System.

	public void SetCanControl(bool state){

		canControl = state;

	}

	public void SetEngine(bool state){

		if (state)
			StartEngine ();
		else
			KillEngine ();

	}
		
	}