/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPClock.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	time management class for multiplayer time keeping. this is
//					used instead of vp_Timer for syncing current game time across
//					several physical machines, since vp_Timer will pause when unity
//					freezes (such as when dragging game window around on desktop)
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class vp_MPClock
{

	private static float m_EndTime = 0.0f;		// when the ongoing game is going to end
	private static float m_Duration = 0.0f;		// duration of a typical game. change with the 'Set' methods


	/// <summary>
	/// returns whether there is time left in the current game
	/// </summary>
	public static bool Running
	{
		get
		{
			return (Duration == 0.0f) || (m_EndTime > LocalTime);
		}
	}


	/// <summary>
	/// returns the standard total duration of a game with the current
	/// setting
	/// </summary>
	public static float Duration
	{
		get
		{
			return m_Duration;
		}
	}


	/// <summary>
	/// returns time left of the current game
	/// </summary>
	public static float TimeLeft
	{
		get
		{
			return (m_EndTime - LocalTime);
		}
	}


	/// <summary>
	/// returns the real time in seconds since the game started
	/// on this machine
	/// </summary>
	public static float LocalTime
	{
		get
		{
			return UnityEngine.Time.realtimeSinceStartup;
		}
	}


	/// <summary>
	/// starts a new timer from zero with the previous duration
	/// </summary>
	public static void Reset()
	{

		m_EndTime = LocalTime + m_Duration;

	}


	/// <summary>
	/// starts a new timer from zero. for starting a game as master
	/// </summary>
	public static void Set(float duration)
	{

		m_EndTime = LocalTime + duration;
		m_Duration = duration;

	}


	/// <summary>
	/// sets a timer to inside a duration. for joining mid-game
	/// </summary>
	public static void Set(float timeLeft, float totalDuration)
	{

		m_EndTime = LocalTime + timeLeft;
		m_Duration = totalDuration;
	
	}
	

	/// <summary>
	/// sets the time to zero. this is for when a game is not running
	/// </summary>
	public static void Stop()
	{

		m_EndTime = 0.0f;

	}

}
