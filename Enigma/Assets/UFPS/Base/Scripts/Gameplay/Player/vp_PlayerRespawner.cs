/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PlayerRespawner.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this is an implementation of vp_Respawner that is aware of
//					the player event handler, for the purpose of accessing its
//					Position, Rotation and Stop events
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class vp_PlayerRespawner : vp_Respawner 
{

	private vp_PlayerEventHandler m_Player = null;	// should never be referenced directly
	public vp_PlayerEventHandler Player	// lazy initialization of the event handler field
	{
		get
		{
			if (m_Player == null)
				m_Player = transform.GetComponent<vp_PlayerEventHandler>();
			return m_Player;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Awake()
	{

		base.Awake();

	}


	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected override void OnEnable()
	{

		if (Player != null)
			Player.Register(this);

		base.OnEnable();

	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected override void OnDisable()
	{
	
		if (Player != null)
			Player.Unregister(this);

	}


	/// <summary>
	/// event target. resets position, angle and motion
	/// </summary>
	public override void Reset()
	{

		if (!Application.isPlaying)
			return;

		if (Player == null)
			return;

		Player.Position.Set(Placement.Position);
		Player.Rotation.Set(Placement.Rotation.eulerAngles);
		Player.Stop.Send();
		
	}


}