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

public class RTC_InitLoad : MonoBehaviour {

	[InitializeOnLoad]
	public class InitOnLoad {

		static InitOnLoad(){

			if(!EditorPrefs.HasKey("RTC2.0fInstalled")){
				
				EditorPrefs.SetInt("RTC2.0fInstalled", 1);
				EditorUtility.DisplayDialog("Regards from BoneCracker Games", "Thank you for purchasing and using Realistic Tank Controller. Please read the documentation before use. Also check out the online documentation for updated info. Have fun :)", "Let's get started");

				if(EditorUtility.DisplayDialog("Importing BoneCracker Games Shared Assets", "Do you want to import ''BoneCracker Games Shared Assets'' to your project? It will be used for all vehicles created by BoneCracker Games in future.", "Import it", "No"))
					AssetDatabase.ImportPackage("Assets/RealisticTankController/For BoneCracker Games Shared Assets/BoneCracker Games Shared Assets", true);
				
			}

		}

	}

}
