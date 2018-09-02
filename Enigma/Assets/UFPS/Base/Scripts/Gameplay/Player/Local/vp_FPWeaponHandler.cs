/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPWeaponHandler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	weapon handler logic that is specific to a local first person
//					player should be added to this script
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;


public class vp_FPWeaponHandler : vp_WeaponHandler
{
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual bool OnAttempt_AutoReload()
	{

		if (!ReloadAutomatically)
			return false;

		if (CurrentWeapon == null)
			return false;

		if (CurrentWeapon.AnimationType == (int)vp_Weapon.Type.Melee)
			return false;

		return m_Player.Reload.TryStart();

	}


}


