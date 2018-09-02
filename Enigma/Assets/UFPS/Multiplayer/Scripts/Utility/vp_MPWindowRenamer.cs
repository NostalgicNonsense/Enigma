/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPWindowRenamer.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	if included in a standalone build, this script will force the window title
//					to a string composed of photon player ID and current client / master status.
//					it can be used with a desktop tool to rearrange multiple client windows
//					dynamically for better development workflow. see the manual for more info
//
//					NOTES:
//						1) works only on windows standalone
//						2) not intended for inclusion in a final game
//						3) if two client windows log on almost simultaneously the script may
//							confuse the windows. it's a good idea to wait a couple of seconds
//							between each logon
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class vp_MPWindowRenamer : MonoBehaviour
{

#if UNITY_EDITOR
	[vp_HelpBox("If included in a standalone build, this script will force the window title to a string composed of Photon Player ID and current Client / Master status. It can be used with a desktop tool to rearrange multiple client windows dynamically for better development workflow.\n\nNOTES:\n\t1) Works only on Windows Standalone.\n\t2) Not intended for inclusion in a final game.", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpBox;
#endif

	[HideInInspector]
	public string ProductName = "UFPS";		// will be set to 'PlayerSettings.productName' by the editor script
											// TIP: this can be defined in the editor: 'ProjectSettings -> Player -> Product Name'
#if UNITY_STANDALONE_WIN

#if !UNITY_EDITOR	// if editor is allowed to execute the below it will update external windows with its own status = bad

	private string WindowName = null;

	[DllImport("user32.dll", EntryPoint = "SetWindowText")]
	public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
	[DllImport("user32.dll", EntryPoint = "FindWindow")]
	public static extern System.IntPtr FindWindow(System.String className, System.String windowName);


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		WindowName = ProductName;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void RefreshWindowTitle()
	{

		string newName = "";

		if (PhotonNetwork.connectionState == ConnectionState.Disconnected)
			newName = ProductName;
		else
			newName = "Player " + PhotonNetwork.player.ID + ((PhotonNetwork.isMasterClient) ? " (Master)" : " (Client)");

		SetWindowText(FindWindow(null, WindowName), newName);
		WindowName = newName;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnJoinedRoom()
	{
		RefreshWindowTitle();
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnPhotonPlayerConnected(PhotonPlayer player)
	{
		RefreshWindowTitle();
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnPhotonPlayerDisconnected(PhotonPlayer player)
	{
		RefreshWindowTitle();
	}

#endif

#endif


}


