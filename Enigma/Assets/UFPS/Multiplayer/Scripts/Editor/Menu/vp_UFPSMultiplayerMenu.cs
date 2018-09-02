/////////////////////////////////////////////////////////////////////////////////
//
//	vp_UFPSMultiplayerMenu.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	unity editor main menu items for UFPS multiplayer
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

public class vp_UFPSMultiplayerMenu 
{
	
	[MenuItem("UFPS/Multiplayer/Manual", false, 131)]
	public static void Manual()
	{
		Application.OpenURL("http://www.opsive.com/assets/UFPS/hub/assets/ufpsmp/manual");
	}

	[MenuItem("UFPS/Multiplayer/Create Player Type", false, 151)]
	public static void CreateItemTypeVpItemType()
	{
		vp_MPPlayerType asset = (vp_MPPlayerType)vp_EditorUtility.CreateAsset("UFPS/Multiplayer/Content/PlayerTypes", typeof(vp_MPPlayerType));
		if (asset != null)
			asset.DisplayName = "player";
	}

}
