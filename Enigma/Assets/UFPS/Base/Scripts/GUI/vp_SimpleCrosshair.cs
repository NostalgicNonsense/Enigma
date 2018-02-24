/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SimpleCrosshair.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script is just a stub for your own a way cooler crosshair
//					system. it simply draws a classic FPS crosshair center screen.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class vp_SimpleCrosshair : MonoBehaviour
{

	// crosshair texture
	public Texture m_ImageCrosshair = null;

	public bool Hide = false;					// use this if you want to hide the crosshair without disabling it (crosshair needs to be enabled for interaction to work)
	public bool HideOnFirstPersonZoom = true;
	public bool HideOnDeath = true;
	
	protected vp_FPPlayerEventHandler m_Player = null;
	
	
	protected virtual void Awake()
	{
		
		m_Player = GameObject.FindObjectOfType(typeof(vp_FPPlayerEventHandler)) as vp_FPPlayerEventHandler; // cache the player event handler
		
	}
	
	
	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected virtual void OnEnable()
	{

		// allow this monobehaviour to talk to the player event handler
		if (m_Player != null)
			m_Player.Register(this);

	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected virtual void OnDisable()
	{

		// unregister this monobehaviour from the player event handler
		if (m_Player != null)
			m_Player.Unregister(this);

	}


	/// <summary>
	/// draws the crosshair texture smack in the middle of the screen
	/// </summary>
	void OnGUI()
	{

		if (m_ImageCrosshair == null)
			return;

		if (Hide)
			return;

		if(HideOnFirstPersonZoom && m_Player.Zoom.Active && m_Player.IsFirstPerson.Get())
			return;

		if(HideOnDeath && m_Player.Dead.Active)
			return;

		GUI.color = new Color(1, 1, 1, 0.8f);
		GUI.DrawTexture(new Rect((Screen.width * 0.5f) - (m_ImageCrosshair.width * 0.5f),
			(Screen.height * 0.5f) - (m_ImageCrosshair.height * 0.5f), m_ImageCrosshair.width,
			m_ImageCrosshair.height), m_ImageCrosshair);
		GUI.color = Color.white;
	
	}
	
	
	protected virtual Texture OnValue_Crosshair
	{
		get { return m_ImageCrosshair; }
		set { m_ImageCrosshair = value; }
	}
	

}

