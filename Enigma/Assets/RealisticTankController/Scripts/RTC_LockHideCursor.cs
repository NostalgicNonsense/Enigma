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

[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/UI/Lock Cursor")]
public class RTC_LockHideCursor : MonoBehaviour {

	CursorLockMode wantedMode;

	void SetCursorState (){
		
		Cursor.lockState = wantedMode;
		Cursor.visible = (CursorLockMode.Locked != wantedMode);

	}
	
	void OnGUI (){
		
		GUILayout.BeginVertical ();

		if (Input.GetKeyDown (KeyCode.Escape))
			Cursor.lockState = wantedMode = CursorLockMode.None;
		
		switch (Cursor.lockState)
		{
		case CursorLockMode.None:
			GUILayout.Label ("Cursor is normal");
			if (GUILayout.Button ("Lock cursor"))
				wantedMode = CursorLockMode.Locked;
			break;
		case CursorLockMode.Confined:
			GUILayout.Label ("Cursor is confined");
			if (GUILayout.Button ("Lock cursor"))
				wantedMode = CursorLockMode.Locked;
			break;
		case CursorLockMode.Locked:
			GUILayout.Label ("Cursor is locked");
			if (GUILayout.Button ("ESC to unlock cursor"))
				wantedMode = CursorLockMode.None;
			break;
		}
		
		GUILayout.EndVertical ();
		
		SetCursorState ();

	}

}
