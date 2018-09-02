/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPPlayerType.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this ScriptableObject declares a visual player type for
//					multiplayer. it determines what local- and remote player
//					prefab to spawn for a certain type of player. you can create
//					new player type objects by going to the UFPS editor menu ->
//					Multiplayer -> Create Player Type.
//
//					TIP: can be used to implement things like class systems,
//					where you may want to spawn separate models not only for
//					teams, but also for roles / races / monsters. theoretically
//					you could extend this scriptable object with gameplay info
//					like max health, initial items and so on, and set these on
//					a new player post initial spawn
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

[System.Serializable]
public class vp_MPPlayerType : ScriptableObject
{

	public vp_MPPlayerType()
	{
	}

#if UNITY_EDITOR
	[vp_Separator]	public vp_Separator s1;
#endif
	public string DisplayName;
	public string Description;
	public Texture2D Icon;

#if UNITY_EDITOR
	[vp_Separator]	public vp_Separator s2;
#endif

	public GameObject LocalPrefab;
	public GameObject RemotePrefab;

#if UNITY_EDITOR
	[vp_HelpBox("This object declares a visual player type for multiplayer. It determines what local- and remote player prefab to spawn for a certain type of player.\n", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float mpPlayerTypeHelp;
#endif


}

