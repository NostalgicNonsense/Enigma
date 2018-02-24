/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPWeaponReloader.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this component adds firearm reload logic, sound, animation and
//					reload duration to an FPWeapon. it doesn't handle ammo max caps
//					or levels. instead this should be governed by an inventory
//					system via the event handler
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;


[RequireComponent(typeof(vp_FPWeapon))]

public class vp_FPWeaponReloader : vp_WeaponReloader
{

	public AnimationClip AnimationReload = null;

	vp_FPWeapon m_FPWeapon = null;
	vp_FPWeapon FPWeapon
	{
		get
		{
			if (m_FPWeapon == null)
				m_FPWeapon = (m_Weapon as vp_FPWeapon);
			return m_FPWeapon;
		}
	}

	public Animation WeaponAnimation
	{
		get
		{
			if (m_WeaponAnimation == null)
			{
				if (FPWeapon == null)
					return null;
				if (FPWeapon.WeaponModel == null)
					return null;
				m_WeaponAnimation = FPWeapon.WeaponModel.GetComponent<Animation>();
			}
			return m_WeaponAnimation;
		}
	}
	Animation m_WeaponAnimation = null;


	/// <summary>
	/// this callback is triggered right after the 'Reload' activity
	/// has been approved for activation
	/// </summary>
	protected override void OnStart_Reload()
	{

		base.OnStart_Reload();

		if (AnimationReload == null)
			return;

		// if reload duration is zero, fetch duration from the animation
		if (m_Player.Reload.AutoDuration == 0.0f)
			m_Player.Reload.AutoDuration = AnimationReload.length;

		WeaponAnimation.CrossFade(AnimationReload.name);

	}
	

}

