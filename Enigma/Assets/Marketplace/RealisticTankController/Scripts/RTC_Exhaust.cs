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
/// Exhaust based on Particle System. Based on tank controller's throttle situation.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Misc/Exhaust")]
public class RTC_Exhaust : MonoBehaviour {

	private RTC_TankController tankController;

	private ParticleSystem particle;
	private ParticleSystem.EmissionModule emission;
	private ParticleSystem.MinMaxCurve emissionRate;

	public ParticleSystem burstParticle;
	private ParticleSystem.MinMaxCurve burstEmissionRate;

	public float minEmission = 5f;
	public float maxEmission = 50f;

	public float minSize = 2.5f;
	public float maxSize = 5f;

	public float minSpeed = .5f;
	public float maxSpeed = 5f;

	public float burstSeconds = .5f;
	private float burstTimer = 0f;

	void Start () {

		tankController = GetComponentInParent<RTC_TankController>();
		particle = GetComponent<ParticleSystem>();
		emission = particle.emission;

	}

	void Update () {

		if(!tankController || !particle)
			return;

		if(tankController.engineRunning){

			if (burstParticle) {
				if (tankController._gasInput > .1f) {
					if(burstTimer < burstSeconds)
						burstParticle.Play ();
					burstTimer += Time.deltaTime;
				} else {
					burstParticle.Stop ();
					burstTimer = 0f;
				}
			}

			if(tankController.speed < 150){
				if(!emission.enabled)
					emission.enabled = true;
				if(tankController._gasInput > .1f){
					emissionRate.constantMax = maxEmission;
					emission.rate = emissionRate;
					particle.startSpeed = maxSpeed;
					particle.startSize = maxSize;
				}else{
					emissionRate.constantMax = minEmission;
					emission.rate = emissionRate;
					particle.startSpeed = minSpeed;
					particle.startSize = minSize;
				}
			}else{
				if(emission.enabled)
					emission.enabled = false;
			}

		}else{

			if(emission.enabled)
				emission.enabled = false;

		}

	}

}
