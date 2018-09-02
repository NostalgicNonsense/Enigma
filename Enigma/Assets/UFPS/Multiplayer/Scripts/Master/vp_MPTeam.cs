/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPTeam.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	base class for a multiplayer team. defines basic properties
//					such as team name (used for spawnpoints), color (nametags)
//					and the player type to use (local/remote body prefabs).
//				
//					NOTE: this class works in conjunction with 'vp_MPTeamManager'.
//					you can inherit both classes to declare teams with
//					further functionality. for an example of this, see the
//					deathmatch demo scripts 'vp_DMTeam' and 'vp_DMTeamManager'
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class vp_MPTeam
{

	public string Name = "Unnamed";		// used for spawnpoints
	public Color Color = Color.blue;	// used for nametags
	public vp_MPPlayerType PlayerType;	// determines which local and remote body prefab to use for team members


	/// <summary>
	/// returns the team number (0 means 'NoTeam')
	/// </summary>
	public int Number
	{
		get
		{
			return vp_MPTeamManager.Instance.Teams.IndexOf(this);
		}
	}

	/// <summary>
	/// constructor
	/// </summary>
	public vp_MPTeam(string name, Color color, vp_MPPlayerType playerType = null)
	{

		Name = name;
		Color = color;
		PlayerType = (playerType != null) ? playerType : vp_MPPlayerSpawner.GetDefaultPlayerType();

	}
	

}
