/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPAntiCheatHelper.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this is a bridge between UFPS multiplayer and the third-party
//					'Anti-Cheat Toolkit' by focus (REQUIRED). adding it to the
//					scene will quickstart some chosen cheat detectors and hook up
//					a standard, UFPS multiplayer specific response to cheating.
//
//					USAGE:
//						1) install 'Anti-Cheat Toolkit' from Asset Store:
//							https://www.assetstore.unity3d.com/en/#!/content/10395
//						2) to enable the UFPS ACTk integration, go to:
//							'Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols'
//							 and add the following string to the text field:
//							;ANTICHEAT
//							as soon as Unity has recompiled, this component will be made
//							functional, and the UFPS component state & preset system + remote
//							player wizard will now support Anti-Cheat Toolkit's ObscuredTypes.
//						3) add a new gameobject to the scene, name it i.e. 'AntiCheatHelper'
//							and drag this script onto it to customize cheat detection and
//							response for your game
//						4) see the manual 'Cheat Detection' chapter for info on how to harden
//							UFPS and your game using Anti-Cheat Toolkit's ObscuredTypes.
//
/////////////////////////////////////////////////////////////////////////////////


#if ANTICHEAT

using CodeStage.AntiCheat.Detectors;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class vp_MPAntiCheatHelper : Photon.MonoBehaviour
{

	////////////// 'Detectors' section ////////////////

	[System.Serializable]
	public class DetectorSection
	{

		public ObscuredBool SpeedHack = true;
		public ObscuredBool ObscuredCheating = true;
		public ObscuredBool Injection = false;
		public ObscuredBool WallHack = false;
		public ObscuredVector3 WallHackSpawnPos = (Vector3.down * 200);

#if UNITY_EDITOR
		[vp_HelpBox("See the manual 'Cheat Detection' chapter for info on how to work with these detectors.", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
		public float help;
#endif

	}
	public DetectorSection Detectors = new DetectorSection();

	////////////// 'Standard Cheat Response' section ////////////////

	[System.Serializable]
	public class StandardCheatResponseSection
	{

		public ObscuredFloat RandomDelay = 0.0f;
		public ObscuredString ChatMessage = "I am a cheater and have attempted a {0}.";
		public ObscuredBool HideMessageLocally = true;
		public ObscuredString ErrorLogMessage = "Detected a {0}.";
		public ObscuredBool HideErrorDialog = false;
		public ObscuredBool Disconnect = true;
		public ObscuredBool PreventReconnect = true;
		public ObscuredBool QuitGame = false;

	}
	public StandardCheatResponseSection StandardCheatResponse = new StandardCheatResponseSection();

	public new ObscuredBool DontDestroyOnLoad = true;

	private const string PLACEHOLDER_HACK_NAME = "sneaky hack";
	protected string m_DetectedHackName;


	/// <summary>
	/// starts the user-specified detectors
	/// </summary>
	void Start()
	{

		m_DetectedHackName = PLACEHOLDER_HACK_NAME;

		if (Detectors.SpeedHack && (SpeedHackDetector.Instance == null))
			SpeedHackDetector.StartDetection(OnSpeedHackDetected);

		if (Detectors.ObscuredCheating && (ObscuredCheatingDetector.Instance == null))
			ObscuredCheatingDetector.StartDetection(OnObscuredCheatingDetected);

		if (Detectors.WallHack && (WallHackDetector.Instance == null))
			WallHackDetector.StartDetection(OnWallHackDetected, Detectors.WallHackSpawnPos);

		if (Detectors.Injection && (InjectionDetector.Instance == null))
			InjectionDetector.StartDetection(OnInjectionDetected);

		if (DontDestroyOnLoad)
			Object.DontDestroyOnLoad(transform.root.gameObject);

	}


	/// <summary>
	/// default callback for speed hack response
	/// </summary>
	public virtual void OnSpeedHackDetected()
	{
		m_DetectedHackName = "speed hack";
		OnHackDetected();
	}


	/// <summary>
	/// default callback for value hack response
	/// </summary>
	public virtual void OnObscuredCheatingDetected()
	{
		m_DetectedHackName = "value hack";
		OnHackDetected();
	}


	/// <summary>
	/// default callback for wall hack response
	/// </summary>
	public virtual void OnWallHackDetected()
	{
		m_DetectedHackName = "wall hack";
		OnHackDetected();
	}


	/// <summary>
	/// default callback for dll injection response
	/// </summary>
	public virtual void OnInjectionDetected()
	{
		m_DetectedHackName = "dll injection";
		OnHackDetected();
	}


	/// <summary>
	/// common callback for all hack types. triggers the cheat response after
	/// a random delay
	/// </summary>
	public virtual void OnHackDetected()
	{

		if (StandardCheatResponse.RandomDelay == 0.0f)
			TriggerCheatResponse();
		else
			vp_Timer.In(Random.Range(0, Mathf.Max(0, StandardCheatResponse.RandomDelay)), TriggerCheatResponse);

	}
	

	/// <summary>
	/// after a cheat has been detected, this method implements one or more
	/// of the following responses: sends a chat message, logs an error
	/// message, clears the local chat, hides the local error dialog,
	/// disconnects from the photon cloud, prevents reconnection until the
	/// game is restarted, quits the game.
	/// </summary>
	protected virtual void TriggerCheatResponse()
	{

		string chatMessage = (string.IsNullOrEmpty(StandardCheatResponse.ChatMessage) ? "" : string.Format(StandardCheatResponse.ChatMessage, m_DetectedHackName));
		string errorMessage = (string.IsNullOrEmpty(StandardCheatResponse.ErrorLogMessage) ? "" : string.Format(StandardCheatResponse.ErrorLogMessage, m_DetectedHackName));
		m_DetectedHackName = PLACEHOLDER_HACK_NAME;

		// if we have a chat message, send it to any script listening to the
		// vp_GlobalEvent 'ChatMessage' (by default: vp_MPDemoChat)
		if (!string.IsNullOrEmpty(StandardCheatResponse.ChatMessage))
		{
			vp_GlobalEvent<string, bool>.Send("ChatMessage", chatMessage, true, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
			if(StandardCheatResponse.HideMessageLocally)
				vp_GlobalEvent.Send("ClearChat", vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
		}

		// if we should hide the error dialog, send it to any script listening
		// to the vp_GlobalEvent 'EnableErrorDialog' (default: vp_CrashPopup)
		if (StandardCheatResponse.HideErrorDialog)
			vp_GlobalEvent<bool>.Send("EnableErrorDialog", false, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);

		// if we have an error message, save it to the unity log file ('output_log.txt')
		// unless 'HideErrorDialog' is true, by default this will also display a 'vp_CrashPopup'
		if (!string.IsNullOrEmpty(errorMessage))
			Debug.LogError(errorMessage);   

		// if we should disconnect, wait one frame to allow time for sending any chat message
		if (StandardCheatResponse.Disconnect)
		{
			if (!string.IsNullOrEmpty(chatMessage))
				vp_Timer.In(0, () => vp_MPConnection.Instance.Disconnect());
			else
				vp_MPConnection.Instance.Disconnect();
		}

		// impose a soft kick for the remainder of the session by having vp_MPConnection
		// refuse to connect to Photon Cloud. NOTE: ofcourse this will not stay in effect
		// when the executable is restarted. however, if the cheater was the master he
		// will no longer be master if he logs back into an ongoing game
		if (StandardCheatResponse.PreventReconnect)
		{
			vp_MPConnection.Instance.LogOnTimeOut = 0.0f;
			vp_MPConnection.Instance.MaxConnectionAttempts = -1;
		}

		if (StandardCheatResponse.QuitGame)
			vp_Gameplay.Quit();

	}

}
#elif UNITY_EDITOR
public class vp_MPAntiCheatHelper : Photon.MonoBehaviour
{
	[vp_HelpBox("To enable this component:\n\n• 1) Install 'Anti-Cheat Toolkit' from the Unity Asset Store.\n\n• 2) From the Unity main menu, go to 'Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols' and add the following string to the text field:\n    ;ANTICHEAT\n\n• For more info on how to use the component, see the manual 'Cheat Detection' chapter.", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float help;
}
#endif

