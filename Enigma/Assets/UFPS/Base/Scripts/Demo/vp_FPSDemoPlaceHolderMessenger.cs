/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPSDemoPlaceHolderMessenger.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	just a script to disclaim about temp / crappy demo animations =)
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;

public class vp_FPSDemoPlaceHolderMessenger : MonoBehaviour
{

	private vp_FPPlayerEventHandler Player = null;
	private bool m_WasSwingingMaceIn3rdPersonLastFrame = false;
	private bool m_WasClimbingIn3rdPersonLastFrame = false;


	/// <summary>
	/// 
	/// </summary>
	void Start()
	{

		Player = transform.root.GetComponent<vp_FPPlayerEventHandler>();
		if (Player == null)
			this.enabled = false;

	}
	

	/// <summary>
	/// 
	/// </summary>
	void Update()
	{

		if (Player == null)
			return;

		// --- 3rd person climb ---
		if (!Player.IsFirstPerson.Get() && Player.Climb.Active)
		{
			if (!m_WasClimbingIn3rdPersonLastFrame)
			{
				m_WasClimbingIn3rdPersonLastFrame = true;
				vp_Timer.In(0, delegate()
				{
					Player.HUDText.Send("PLACEHOLDER CLIMB ANIMATION");
				}, 3, 1.0f);
			}
		}
		else
			m_WasClimbingIn3rdPersonLastFrame = false;

		// --- 3rd person melee attack ---
		if (!Player.IsFirstPerson.Get()
			&& (Player.CurrentWeaponIndex.Get() == 4)
			&& (Player.Attack.Active))
		{
			if (!m_WasSwingingMaceIn3rdPersonLastFrame)
			{
				m_WasSwingingMaceIn3rdPersonLastFrame = true;
				vp_Timer.In(0, delegate()
				{
					Player.HUDText.Send("PLACEHOLDER MELEE ANIMATION");
				}, 3, 1.0f);
			}
		}
		else
			m_WasSwingingMaceIn3rdPersonLastFrame = false;

	}


}

