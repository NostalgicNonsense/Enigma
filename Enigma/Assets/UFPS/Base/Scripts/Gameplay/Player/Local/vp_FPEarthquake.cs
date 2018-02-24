/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPEarthquake.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	generates procedural noise for purposes of shaking the camera
//					in response to earthquakes and bombs. this class is currently
//					under construction and will change in upcoming releases.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class vp_FPEarthquake : MonoBehaviour
{

	protected Vector3 m_CameraEarthQuakeForce = new Vector3();

	protected float m_Endtime = 0.0f;
	protected Vector2 m_Magnitude = Vector2.zero;


	vp_FPPlayerEventHandler m_FPPlayer = null;
	vp_FPPlayerEventHandler FPPlayer
	{
		get
		{
			if (m_FPPlayer == null)
				m_FPPlayer = GameObject.FindObjectOfType(typeof(vp_FPPlayerEventHandler)) as vp_FPPlayerEventHandler;
			return m_FPPlayer;
		}
	}



	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected virtual void OnEnable()
	{

		if (FPPlayer != null)
			FPPlayer.Register(this);

	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected virtual void OnDisable()
	{


		if (FPPlayer != null)
			FPPlayer.Unregister(this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected void FixedUpdate()
	{

		if (Time.timeScale != 0.0f)
			UpdateEarthQuake();

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected void UpdateEarthQuake()
	{

		if (!FPPlayer.CameraEarthQuake.Active)
		{
			m_CameraEarthQuakeForce = Vector3.zero;
			return;
		}

		// the horizontal move is a perlin noise value between 0 and
		// 'm_EarthQuakeMagnitude' (depending on 'm_EarthQuakeTime').
		// horizMove will ease out during the last second.
		m_CameraEarthQuakeForce = Vector3.Scale(vp_SmoothRandom.GetVector3Centered(1),
							m_Magnitude.x * (Vector3.right + Vector3.forward) * Mathf.Min(m_Endtime - Time.time, 1.0f)
							* Time.timeScale);

		// vertMove will ease out during the last second.
		m_CameraEarthQuakeForce.y = 0;
		if (UnityEngine.Random.value < (0.3f * Time.timeScale))
			m_CameraEarthQuakeForce.y = UnityEngine.Random.Range(0, (m_Magnitude.y * 0.35f)) * Mathf.Min(m_Endtime - Time.time, 1.0f);

	}
	

	/// <summary>
	/// this callback is triggered right after the 'CameraEarthQuake
	/// activity has been approved for activation
	/// </summary>
	protected virtual void OnStart_CameraEarthQuake()
	{

		Vector3 arg = (Vector3)FPPlayer.CameraEarthQuake.Argument;

		m_Magnitude.x = arg.x;
		m_Magnitude.y = arg.y;

		m_Endtime = Time.time + arg.z;

		FPPlayer.CameraEarthQuake.AutoDuration = arg.z;

	}


	/// <summary>
	/// makes the ground shake as if a bomb has gone off nearby
	/// </summary>
	protected virtual void OnMessage_CameraBombShake(float impact)
	{

		FPPlayer.CameraEarthQuake.TryStart(new Vector3(impact * 0.5f, impact * 0.5f, 1.0f));

	}


	/// <summary>
	/// gets or sets the current earthquake force
	/// </summary>
	protected virtual Vector3 OnValue_CameraEarthQuakeForce
	{
		get
		{
			return m_CameraEarthQuakeForce;
		}
		set
		{
			m_CameraEarthQuakeForce = value;
		}
	}


}

	