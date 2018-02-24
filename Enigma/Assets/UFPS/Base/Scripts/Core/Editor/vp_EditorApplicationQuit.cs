/////////////////////////////////////////////////////////////////////////////////
//
//	vp_EditorApplicationQuit.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this is a small convenience feature allowing the game to quit
//					itself when running in the editor by means of vp_GlobalEvent
//					"EditorApplicationQuit"
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class vp_EditorApplicationQuit : Editor
{

	static vp_EditorApplicationQuit()
	{
		vp_GlobalEvent.Register("EditorApplicationQuit", () => { EditorApplication.isPlaying = false; });
	}
			
}

