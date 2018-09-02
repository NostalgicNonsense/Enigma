/////////////////////////////////////////////////////////////////////////////////
//
//	vp_DMTeam.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	base class for a deathmatch team. this is an example of how to
//					extend the base (vp_MPTeam) class with an additional stat, "Score"
//				
//					NOTE: this class works in conjunction with 'vp_DMTeamManager'.
//					for more information about how multiplayer teams work, see the
//					base (vp_MPTeam and vp_MPTeamManager) classes
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class vp_DMTeam : vp_MPTeam
{

	public vp_DMTeam(string name, Color color, vp_MPPlayerType playerType = null) : base(name, color, playerType) { }

	public int Score = 0;
	
}
