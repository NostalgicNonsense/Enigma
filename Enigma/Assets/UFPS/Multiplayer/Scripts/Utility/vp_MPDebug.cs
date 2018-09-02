/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPDebug.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	simple debug message functionality for multiplayer. messages
//					are re-routed to the chat by default, but could also be pushed
//					to a console of any kind. this is work in progress
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_MPDebug
{

	/// <summary>
	/// prints a message to an appropriate multiplayer gui target
	/// </summary>
	public static void Log(string msg)
	{

		vp_GlobalEvent<string, bool>.Send("ChatMessage", msg, false, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
		//Debug.Log(msg);

	}

}