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

[System.Serializable]
public class RTC_Ammunation : ScriptableObject {
	
	#region Singleton
	public static RTC_Ammunation instance;
	public static RTC_Ammunation Instance{	get{if(instance == null) instance = Resources.Load("RTC Assets/RTC_Ammunation") as RTC_Ammunation; return instance;}}
	#endregion

	[System.Serializable]
	public class Ammunation{
		
		public GameObject projectile;

		public float reloadTime = 3f;
		public int velocity = 250;
		public int recoilForce = 50000;

		public GameObject explosionPrefab;
		public float explosionForce = 20000f;
		public float explosionRadius = 5f;
		public int lifeTimeOfTheProjectile = 5;

		public AudioClip fireSoundClip;
		public GameObject groundSmoke;
		public GameObject fireSmoke;

	}
		
	public Ammunation[] ammunations;

}


