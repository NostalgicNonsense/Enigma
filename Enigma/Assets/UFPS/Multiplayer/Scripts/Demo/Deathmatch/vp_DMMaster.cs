/////////////////////////////////////////////////////////////////////////////////
//
//	vp_DMMaster.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	an example of how to extend the base (vp_MPMaster) class
//					with a call to show the deathmatch scoreboard when the game
//					pauses on end-of-match, and to restore it when game resumes
//
//					TIP: study the base class to learn how the game state works
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class vp_DMMaster : vp_MPMaster
{
	

	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	protected override void ReceiveFreeze(PhotonMessageInfo info)
	{

		if (!info.sender.IsMasterClient)
			return;

		base.ReceiveFreeze(info);

		vp_DMDemoScoreBoard.ShowScore = true;

	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	protected override void ReceiveUnFreeze(PhotonMessageInfo info)
	{

		if (!info.sender.IsMasterClient)
			return;

		base.ReceiveUnFreeze(info);

		vp_DMDemoScoreBoard.ShowScore = false;
		
	}
	
	
}
