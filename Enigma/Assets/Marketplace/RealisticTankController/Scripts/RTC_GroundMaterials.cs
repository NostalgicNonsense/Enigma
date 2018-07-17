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
public class RTC_GroundMaterials : ScriptableObject {
	
	#region singleton
	public static RTC_GroundMaterials instance;
	public static RTC_GroundMaterials Instance{	get{if(instance == null) instance = Resources.Load("RTC Assets/RTC_GroundMaterials") as RTC_GroundMaterials; return instance;}}
	#endregion

	[System.Serializable]
	public class GroundMaterialFrictions{
		
		public PhysicMaterial groundMaterial;
		public float forwardStiffness = 1f;
		public float sidewaysStiffness = 1f;
		public float slip = .25f;

		public float minimumDamp = 1f;
		public float maximumDamp = 1f;

		public GameObject groundParticles;

	}
		
	public GroundMaterialFrictions[] frictions;

	public bool useTerrainSplatMapForGroundFrictions = false;
	public PhysicMaterial terrainPhysicMaterial;
	public int[] terrainSplatMapIndex;

}


