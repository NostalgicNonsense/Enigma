/////////////////////////////////////////////////////////////////////////////////
//
//	vp_UFPSMenu.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	unity editor main menu items for UFPS
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

public class UFPSMenu 
{

	[MenuItem("UFPS/About UFPS", false, 0)]
	public static void About()
	{
		vp_AboutBox.Create();
	}

	/////////////////////////////////////////////////////////////////

	[MenuItem("UFPS/UFPS Manual", false, 22)]
	public static void Manual()
	{
		Application.OpenURL("http://www.opsive.com/assets/UFPS/hub/assets/ufps/manual");
	}


	[MenuItem("UFPS/UFPS Add-ons", false, 23)]
	public static void Addons()
	{
		vp_AddonBrowser.Create();
	}

	/////////////////////////////////////////////////////////////////

	[MenuItem("UFPS/Input Manager", false, 100)]
	public static void InputManager()
	{
		vp_InputWindow.Init();
	}

	[MenuItem("UFPS/Wizards/Surfaces/Create Surface Type", false, 101)]
	public static void CreateVpSurfaceType()
	{
		vp_SurfaceType asset = (vp_SurfaceType)vp_EditorUtility.CreateAsset("UFPS/Base/Content/Surfaces/SurfaceTypes", typeof(vp_SurfaceType));
		if (asset != null)
		{
			asset.Init();
		}

	}

	[MenuItem("UFPS/Wizards/Surfaces/Create Impact Event", false, 101)]
	public static void CreateVpImpactEvent()
	{
		vp_EditorUtility.CreateAsset("UFPS/Base/Content/Surfaces/ImpactEvents", typeof(vp_ImpactEvent));
	}
	
	[MenuItem("UFPS/Wizards/Surfaces/Create Surface Effect", false, 101)]
	public static void CreateVpSurfaceEffect()
	{
		vp_SurfaceEffect asset = (vp_SurfaceEffect)vp_EditorUtility.CreateAsset("UFPS/Base/Content/Surfaces/SurfaceFX", typeof(vp_SurfaceEffect));
		if (asset != null)
			asset.Init();
	}
	
	[MenuItem("UFPS/Wizards/Create Item Type/Item", false, 101)]
	public static void CreateItemTypeVpItemType()
	{
		vp_ItemType asset = (vp_ItemType)vp_EditorUtility.CreateAsset("UFPS/Base/Content/ItemTypes", typeof(vp_ItemType));
		if (asset != null)
			asset.DisplayName = "thing";
	}

	[MenuItem("UFPS/Wizards/Create Item Type/Unit", false, 101)]
	public static void CreateItemTypeVpUnitType()
	{
		vp_UnitType asset = (vp_UnitType)vp_EditorUtility.CreateAsset("UFPS/Base/Content/ItemTypes", typeof(vp_UnitType));
		if (asset != null)
			asset.DisplayName = "unit";
	}

	[MenuItem("UFPS/Wizards/Create Item Type/UnitBank", false, 101)]
	public static void CreateItemTypeVpUnitBankType()
	{
		vp_UnitBankType asset = (vp_UnitBankType)vp_EditorUtility.CreateAsset("UFPS/Base/Content/ItemTypes", typeof(vp_UnitBankType));
		if (asset != null)
			asset.DisplayName = "device";
	}

	[MenuItem("UFPS/Wizards/Convert Old DamageHandlers", false, 101)]
	public static void ConvertOldDamageHandlers()
	{
		int objectsUpdated = 0;
		if (EditorUtility.DisplayDialog("Convert Old DamageHandlers?", "In UFPS v1.4.7, DamageHandlers no longer handle respawning.\n\nThis command will strip EVERY vp_DamageHandler component in the scene of its obsolete respawn variables and add a vp_Respawner component with corresponding settings to its gameobject.\n\nAlso, any (old) 'vp_PlayerDamageHandler' will be replaced by the renamed script 'vp_FPPlayerDamageHandler'.\n\nMany objects will potentially be affected.\nAre you sure?", "Yes", "No"))
			objectsUpdated = vp_DamageHandler.GenerateRespawnersForAllDamageHandlers();

		EditorUtility.DisplayDialog("Convert Old DamageHandlers", objectsUpdated + " gameobjects updated." + ((objectsUpdated > 0) ? "\n\nNOTE: You may want to update your prefabs too. This is best done by dragging the un-modified prefab into the scene, running this wizard again and pressing \"Apply\" on the prefab." : ""), "OK");

	}

	[MenuItem("UFPS/Wizards/Generate Remote Player", false, 106)]
	public static void GenerateRemotePlayer()
	{
		vp_RemotePlayerWizard.Generate();
	}

	// -------- duplicate this code block to create your own custom item type --------
	// for more info, see the comments in "vp_CustomType.cs"
	[MenuItem("UFPS/Wizards/Create Item Type/Custom", false, 101)]
	public static void CreateItemTypeVpCustomType()
	{
		vp_CustomType asset = (vp_CustomType)vp_EditorUtility.CreateAsset("UFPS/Base/Content/ItemTypes", typeof(vp_CustomType));
		if (asset != null)
			asset.DisplayName = "custom";
	}
	// -------------------------------------------------------------------------------
	
	[MenuItem("UFPS/Event Handler", false, 110)]
	public static void EventHandler()
	{

		vp_EventHandler EventHandler = (vp_EventHandler)GameObject.FindObjectOfType(typeof(vp_EventHandler));
		vp_EventDumpWindow.Create((vp_EventHandler)EventHandler);

	}
	
	[MenuItem("UFPS/Event Handler", true)]
	public static bool ValidateEventHandler()
	{
		return Application.isPlaying;
	}

	
	/////////////////////////////////////////////////////////////////

	// AI

	// Mobile

	// Multiplayer

	/////////////////////////////////////////////////////////////////

	[MenuItem("UFPS/Help/F.A.Q.", false, 201)]
	public static void FAQ()
	{
		Application.OpenURL("http://www.opsive.com/assets/UFPS/hub/assets/ufps/faq");
	}

	[MenuItem("UFPS/Help/Support Info", false, 202)]
	public static void SupportInfo()
	{
		Application.OpenURL("http://www.opsive.com/assets/UFPS/hub/assets/ufps/supportinfo");
	}
	
	[MenuItem("UFPS/Help/Release Notes", false, 203)]
	public static void ReleaseNotes()
	{
		Application.OpenURL("http://www.opsive.com/assets/UFPS/hub/assets/ufps/releasenotes");
	}

	[MenuItem("UFPS/Help/Update Instructions", false, 204)]
	public static void UpdateInstructions()
	{
		Application.OpenURL("http://www.opsive.com/assets/UFPS/hub/assets/ufps/upgrading");
	}

	[MenuItem("UFPS/Community/Follow us on Twitter", false, 205)]
	public static void Twitter()
	{
		Application.OpenURL("http://www.opsive.com/assets/UFPS/hub/twitter");
	}

	[MenuItem("UFPS/Community/YouTube Channel", false, 206)]
	public static void YouTube()
	{
		Application.OpenURL("http://www.opsive.com/assets/UFPS/hub/youtube");
	}

	[MenuItem("UFPS/Community/Official UFPS Forum", false, 207)]
	public static void OfficialUFPSForum()
	{
		Application.OpenURL("http://www.opsive.com/assets/UFPS/hub/assets/ufps/forum");
	}

	/////////////////////////////////////////////////////////////////

	[MenuItem("UFPS/Check for Updates", false, 300)]
	public static void CheckForUpdates()
	{
		vp_UpdateDialog.Create("ufps", UFPSInfo.Version);
	}

}
