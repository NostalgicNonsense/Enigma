/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPTeamManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script allows you to define multiplayer teams in the editor,
//					and contains a number of utility methods to work with them in code
//
//					NOTE: this class works in conjunction with 'vp_MPTeam'.
//					you can inherit both classes to declare teams with
//					further functionality. for an example of this, see the
//					deathmatch demo scripts 'vp_DMTeam' and 'vp_DMTeamManager'
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class vp_MPTeamManager : MonoBehaviour
{
	
	[SerializeField]
	public List<vp_MPTeam> Teams = new List<vp_MPTeam>();


	/// <summary>
	/// returns the amount of teams. NOTE: team 0 is always 'NoTeam'
	/// </summary>
	public static int TeamCount
	{
		get
		{
			return (Instance == null ? 0 : Instance.Teams.Count);
		}
	}


	/// <summary>
	/// returns true if the scene has a vp_MPTeamManager-derived component.
	/// if this returns false, the teams concept is largely ignored by
	/// other multiplayer scripts
	/// </summary>
	public static bool Exists
	{
		get
		{
			return Instance != null;
		}
	}
	

	// --- properties ---

	private static vp_MPTeamManager m_Instance = null;
	public static vp_MPTeamManager Instance
	{
		get
		{
			if (m_Instance == null)
				m_Instance = Component.FindObjectOfType(typeof(vp_MPTeamManager)) as vp_MPTeamManager;
			return m_Instance;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		// insert team zero as 'NoTeam'. players on this team are to be
		// considered 'teamless' and their team color will be white
		Teams.Insert(0, new vp_MPTeam("NoTeam", Color.white));

		// if player types have not been set for the teams, set team
		// player types to the default vp_MPPlayerSpawner player type
		foreach (vp_MPTeam t in Teams)
		{
			if (t.PlayerType == null)
				t.PlayerType = vp_MPPlayerSpawner.GetDefaultPlayerType();
		}

	}


	/// <summary>
	/// returns the player type for team of 'teamNumber'. this determines
	/// which local and remote body prefab to use for team members
	/// </summary>
	protected virtual vp_MPPlayerType GetTeamPlayerType(int teamNumber)
	{

		if (teamNumber < 1)
			return vp_MPPlayerSpawner.GetDefaultPlayerType();

		return Teams[teamNumber].PlayerType;

	}


	/// <summary>
	/// returns the player type name for team of 'teamNumber'. this determines
	/// which local and remote body prefab to use for team members
	/// </summary>
	public virtual string GetTeamPlayerTypeName(int teamNumber)
	{

		vp_MPPlayerType type = GetTeamPlayerType(teamNumber);
		return ((type != null) ? type.name : "");

	}


	/// <summary>
	/// this method can be overridden to refresh team logic such as team
	/// score, if implemented
	/// </summary>
	public virtual void RefreshTeams()
	{
	}


	/// <summary>
	/// returns the team with the lowest player count. used to even out
	/// the odds by assigning joining players to the smallest team
	/// </summary>
	public virtual int GetSmallestTeam()
	{

		vp_MPTeam smallestTeam = null;
		if (vp_MPNetworkPlayer.Players.Count > 0)
		{
			foreach (vp_MPTeam team in Teams)
			{
				if (Teams.IndexOf(team) > 0)
				{
					if ((smallestTeam == null) || (GetTeamSize(team) <= GetTeamSize(smallestTeam)))
					{
						smallestTeam = team;
					}
				}
			}
		}
		if (smallestTeam == null)
			smallestTeam = Teams[Random.Range(1, Teams.Count)];

		return (smallestTeam != null ? Teams.IndexOf(smallestTeam) : 0);

	}


	/// <summary>
	/// returns the player count of 'team'
	/// </summary>
	protected virtual int GetTeamSize(vp_MPTeam team)
	{

		int amount = 0;
		foreach (vp_MPNetworkPlayer p in vp_MPNetworkPlayer.Players.Values)
		{
			if (p == null)
				continue;
			if (p.TeamNumber == Teams.IndexOf(team))
				amount++;
		}

		return amount;
	}



	/// <summary>
	/// returns the name of team with 'teamNumber'
	/// </summary>
	public static string GetTeamName(int teamNumber)
	{

		if (!IsValidTeamNumber(teamNumber))
			return null;

		return Instance.Teams[teamNumber].Name;

	}


	/// <summary>
	/// returns the color of team with 'teamNumber'
	/// </summary>
	public static Color GetTeamColor(int teamNumber)
	{

		if (!IsValidTeamNumber(teamNumber))
			return Color.white;

		return Instance.Teams[teamNumber].Color;

	}


	/// <summary>
	/// used to verify if a team number will be out of bounds visavi
	/// the list of teams
	/// </summary>
	public static bool IsValidTeamNumber(int teamNumber)
	{

		// the number of teams is always one more than the number
		// of valid team numbers because of team 0 ('NoTeam')
		return (teamNumber < Instance.Teams.Count);

	}

	
}

