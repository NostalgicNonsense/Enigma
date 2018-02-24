/////////////////////////////////////////////////////////////////////////////////
//
//	vp_UFPSHelp.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this class contains common UFPS help texts. TIP: it can be removed
//					to automatically disable Inspector help texts for newer (1.4.7)
//					classes, but mind that most old classes don't use the new help
//					system yet.
//
/////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR
using System;
using System.Collections.Generic;

public class vp_UFPSHelp : vp_Help
{

	private static Dictionary<Type, vp_HelpInfo> m_HelpInfos = new Dictionary<Type, vp_HelpInfo>
	{
		{typeof(vp_ItemPickup.ItemSection), new vp_HelpInfo(new string[]{
		    "Click the circle to choose from available item types.",
		    "'ID' is optional, and can be used by scripts to identify an item beyond its type.",
		    "For weapons, 'Units' refers to the pre-loaded amount of ammo.",
		    "The minimum addable amount of plain 'Units' is 1 (regulated at runtime)."},
		    "")},
		{typeof(vp_ItemPickup.RecipientTagsSection), new vp_HelpInfo(new string[]{
		    "Add Recipient Tags to allow ONLY properly tagged objects to trigger the pickup."},
		    "")},
		{typeof(vp_ItemPickup.MessageSection), new vp_HelpInfo(new string[]{
		    "The above strings are sent as GUI messages on collision.","The following codes can be used in the text:\n\n  {0} IndefiniteArticle\n  {1} DisplayName\n  {2} DisplayNameFull\n  {3} Description\n  {4} Amount of items successfully picked up"},
		    "")},
		{typeof(vp_Respawner), new vp_HelpInfo(new string[]{
			"'Spawn Mode -> SpawnPoint' requires the scene to have atleast one vp_SpawnPoint. If more than one is present, a random spawnpoint with matching 'Spawn Point Tag' will be chosen.","'Obstruction Solver' determines whether the respawner should wait until a 'Radius' around the target position is clear, or if it should just move the target position onto a clear spot."},
			"")},
		{typeof(vp_Inventory.ItemRecordsSection), new vp_HelpInfo(new string[]{
		    "This list shows the items currently contained in the inventory.", "Click on an item card to locate its ItemType object in the Project View.","New item type objects can be created from the Unity main menu -> UFPS -> Create -> Item Type"},
		    "")},
		{typeof(vp_Inventory.ItemCapsSection), new vp_HelpInfo(new string[]{
		    "Enable 'Item Caps' if you wish to put a limit on certain item types.","Enable 'Allow only listed types' if the inventory is intended for storing certain types of items only."},
		    "")},
		{typeof(vp_Inventory.SpaceLimitSection), new vp_HelpInfo(new string[]{
		    "Enable 'Space Limit' to restrict inventory capacity by weight or volume.","In 'Volume' mode, bullets will consume space if added straight to the inventory, but will 'disappear' when loaded into weapons.", "To modify the space consumption of an item type: click it in the 'Items' list, select it in the Project View and edit it in the Inspector."},
		    "")},
		{typeof(vp_DamageHandler), new vp_HelpInfo(new string[]{
			"Looking for respawn parameters? Since UFPS v1.4.7, this component no longer handles respawning. To create a vp_Respawner component with your previous settings restored, go to the UFPS menu -> Wizards -> Convert Old DamageHandlers."},
			"")},
		{typeof(vp_PlayerInventory.AutoWieldSection), new vp_HelpInfo(new string[]{
		    "These flags determine when weapons should be automatically wielded due to picking up weapons, or ammo for weapons."},
		    "")},


	};

	public static vp_HelpInfo Get(Type type)
	{
		vp_HelpInfo s;
		if (!m_HelpInfos.TryGetValue(type, out s) || s == null)
			return new vp_HelpInfo(new string[]{}, "");
		return s;
	}


}
#endif

