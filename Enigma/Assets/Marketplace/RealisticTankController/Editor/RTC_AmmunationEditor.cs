//----------------------------------------------
//            Realistic Tank Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RTC_Ammunation))]
public class RTC_AmmunationEditor : Editor {

	RTC_Ammunation prop;

	Vector2 scrollPos;
	List<RTC_Ammunation.Ammunation> ammunations = new List<RTC_Ammunation.Ammunation>();

	Color orgColor;

	public override void OnInspectorGUI (){

		serializedObject.Update();
		prop = (RTC_Ammunation)target;
		orgColor = GUI.color;

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Ammunation Editor", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("This editor will keep update necessary .asset files in your project. Don't change directory of the ''Resources/RTC Assets''.", EditorStyles.helpBox);
		EditorGUILayout.Space();

		scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false );

		EditorGUIUtility.labelWidth = 110f;
		//EditorGUIUtility.fieldWidth = 10f;

		GUILayout.Label("Ammunation", EditorStyles.boldLabel);

		for (int i = 0; i < prop.ammunations.Length; i++) {

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();

			if(prop.ammunations[i].projectile)
				EditorGUILayout.LabelField(prop.ammunations[i].projectile.name + (i == 0 ? " (Default)" : ""), EditorStyles.boldLabel);

			GUILayout.FlexibleSpace ();

			GUI.color = Color.red;

			if (GUILayout.Button ("X"))
				RemoveAmmo (i);

			GUI.color = orgColor;

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			prop.ammunations[i].projectile = (GameObject)EditorGUILayout.ObjectField("Projectile", prop.ammunations[i].projectile, typeof(GameObject), false, GUILayout.Width(250f));

			EditorGUILayout.Space();

			prop.ammunations[i].velocity = EditorGUILayout.IntField("Velocity", prop.ammunations[i].velocity, GUILayout.Width(250f));
			prop.ammunations[i].reloadTime = EditorGUILayout.FloatField("Reload Time", prop.ammunations[i].reloadTime, GUILayout.Width(250f));
			prop.ammunations[i].recoilForce = EditorGUILayout.IntField("Recoil Force", prop.ammunations[i].recoilForce, GUILayout.Width(250f));

			EditorGUILayout.Space();

			prop.ammunations[i].explosionPrefab = (GameObject)EditorGUILayout.ObjectField("Explosion Prefab", prop.ammunations[i].explosionPrefab, typeof(GameObject), false, GUILayout.Width(250f));
			prop.ammunations[i].explosionForce = EditorGUILayout.FloatField("Explosion Force", prop.ammunations[i].explosionForce, GUILayout.Width(250f));
			prop.ammunations[i].explosionRadius = EditorGUILayout.FloatField("Explosion Radius", prop.ammunations[i].explosionRadius, GUILayout.Width(250f));
			prop.ammunations[i].lifeTimeOfTheProjectile = EditorGUILayout.IntField("Life Time Of The Projectile", prop.ammunations[i].lifeTimeOfTheProjectile, GUILayout.Width(250f));

			EditorGUILayout.Space();

			prop.ammunations[i].fireSoundClip = (AudioClip)EditorGUILayout.ObjectField("Fire Sound Clip", prop.ammunations[i].fireSoundClip, typeof(AudioClip), false, GUILayout.Width(250f));
			prop.ammunations[i].groundSmoke = (GameObject)EditorGUILayout.ObjectField("Ground Smoke", prop.ammunations[i].groundSmoke, typeof(GameObject), false, GUILayout.Width(250f));
			prop.ammunations[i].fireSmoke = (GameObject)EditorGUILayout.ObjectField("Fire Smoke", prop.ammunations[i].fireSmoke, typeof(GameObject), false, GUILayout.Width(250f));

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();

		}

		GUI.color = Color.cyan;

		if(GUILayout.Button("Create New Ammunation")){

			AddNewAmmo();

		}

		if(GUILayout.Button("--< Return To Asset Settings")){

			OpenGeneralSettings();

		}

		GUI.color = orgColor;

		EditorGUILayout.EndScrollView();

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Created by Buğra Özdoğanlar\nBoneCrackerGames", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

		serializedObject.ApplyModifiedProperties();

		if(GUI.changed)
			EditorUtility.SetDirty(prop);

	}

	void AddNewAmmo(){

		ammunations.Clear();
		ammunations.AddRange(prop.ammunations);
		RTC_Ammunation.Ammunation newAmmo = new RTC_Ammunation.Ammunation();
		ammunations.Add(newAmmo);
		prop.ammunations = ammunations.ToArray();

	}

	void RemoveAmmo(int index){

		ammunations.Clear();
		ammunations.AddRange(prop.ammunations);
		ammunations.RemoveAt(index);
		prop.ammunations = ammunations.ToArray();

	}

	void OpenGeneralSettings(){

		Selection.activeObject =RTC_Settings.Instance;

	}

}
