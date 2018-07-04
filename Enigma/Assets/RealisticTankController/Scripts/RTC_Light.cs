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

/// <summary>
/// General lighting system for RTC tanks. Headlight and brake light.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Light/Light")]
public class RTC_Light : MonoBehaviour {

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

	private RTC_TankController tankController;
	private Light _light;
	private Projector projector;

	public LightType lightType;
	public enum LightType{HeadLight, BrakeLight};
	public float inertia = 1f;

	void Start () {
		
		tankController = GetComponentInParent<RTC_TankController>();

		_light = GetComponent<Light>();
		_light.enabled = true;

		_light.renderMode = RTCSettings.useLightsAsVertexLights ? LightRenderMode.ForceVertex : LightRenderMode.ForcePixel;

	}

	void Update () {

		switch(lightType){

		case LightType.HeadLight:
			if(tankController.headLightsOn)
				Lighting(.6f, 50f, 90f);
			else
				Lighting(0f);
			break;

		case LightType.BrakeLight:
			Lighting((!tankController.headLightsOn ? (tankController._brakeInput >= .1f ? 1f : 0f)  : (tankController._brakeInput >= .1f ? 1f : .3f)));
			break;

		}
		
	}

	void Lighting(float input){

		_light.intensity = Mathf.Lerp(_light.intensity, input, Time.deltaTime * inertia * 20f);

	}

	void Lighting(float input, float range, float spotAngle){

		_light.intensity = Mathf.Lerp(_light.intensity, input, Time.deltaTime * inertia * 20f);
		_light.range = range;
		_light.spotAngle = spotAngle;

	}
		
}
