/////////////////////////////////////////////////////////////////////////////////
//
//	vp_DMTeamManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	an example of how to extend the base (vp_MPTeamManager) class
//					with refresh logic for an additional stat: team 'Score'
//
//					NOTE: this class works in conjunction with 'vp_DMTeam'.
//					for more information about how multiplayer teams work, see the
//					base (vp_MPTeam and vp_MPTeamManager) classes
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class vp_DMTeamManager : vp_MPTeamManager
{
	
	/// <summary>
	/// 
	/// </summary>
	protected override void Start()
	{

		// convert all the teams to deathmatch teams

		base.Start();

		// convert the vp_MPTeams from the inspector list into vp_DMTeams
		List<vp_DMTeam> dmTeams = new List<vp_DMTeam>();
		for (int v = Teams.Count - 1; v > -1; v--)
		{
			vp_DMTeam dmt = new vp_DMTeam(Teams[v].Name, Teams[v].Color, Teams[v].PlayerType);
			dmTeams.Add(dmt);
		}

		// clear team list
		Teams.Clear();

		// add the new DM teams to the team list
		for (int v = dmTeams.Count - 1; v > -1; v--)
		{
			Teams.Add(dmTeams[v]);
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public override void RefreshTeams()
	{

		base.RefreshTeams();	// always remember to call base in subsequent overrides

		// begin by zeroing out team score
		foreach (vp_MPTeam t in Teams)
		{
			(t as vp_DMTeam).Score = 0;
		}

		// then add every team member's score to the team score, resulting
		// in a positive or negative number
		foreach (vp_MPNetworkPlayer p in vp_MPNetworkPlayer.Players.Values)
		{
			if (p == null)
				continue;
			(p.Team as vp_DMTeam).Score += (int)p.Stats.Get("Score");
			//Debug.Log("Team " + p.TeamNumber + " score = " + t.Score);
		}

	}


}
