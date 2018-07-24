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

[RequireComponent (typeof (Rigidbody))]
[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Ammunation/Projectile")]
public class RTC_Projectile : MonoBehaviour {

	private Rigidbody rigid;

	public GameObject explosionPrefab;
	public float explosionForce = 300000f;
	public float explosionRadius = 5f;
	public int lifeTimeOfTheBullet = 5;
	private float lifeTime;

	void Start(){

		rigid = GetComponent<Rigidbody>();
		rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		rigid.interpolation = RigidbodyInterpolation.Interpolate;

	}

	void OnEnable(){
		
		for (int i = 0; i < RTC_Ammunation.Instance.ammunations.Length; i++) {
			
			if (gameObject.name == RTC_Ammunation.Instance.ammunations [i].projectile.name + "(Clone)") {
				explosionPrefab = RTC_Ammunation.Instance.ammunations [i].explosionPrefab;
				explosionForce = RTC_Ammunation.Instance.ammunations [i].explosionForce;
				explosionRadius = RTC_Ammunation.Instance.ammunations [i].explosionRadius;
				lifeTimeOfTheBullet = RTC_Ammunation.Instance.ammunations [i].lifeTimeOfTheProjectile;
				break;

			}

		}

	}

	void Update () {
	
		lifeTime += Time.deltaTime;

		if(gameObject.activeInHierarchy && lifeTime > lifeTimeOfTheBullet)
			Explosion();

	}
	

	void OnCollisionEnter (Collision col) {
	
		Explosion();
		
	}

	void Explosion(){

		Instantiate(explosionPrefab, transform.position, transform.rotation);
		Collider[] colliders = Physics.OverlapSphere(transform.position, 5f);

		foreach (Collider hit in colliders) {
			if (hit && hit.GetComponent<Rigidbody>()){
				hit.GetComponent<Rigidbody>().isKinematic = false;
				hit.GetComponent<Rigidbody>().AddExplosionForce(explosionForce, transform.position, explosionRadius, .3f);
			}
		}

		Destroy (gameObject);
		
	}

}
