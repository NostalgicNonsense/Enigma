/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FXBullet.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this is the standard bullet class for UFPS. it raycasts ahead to
//					damage targets using the UFPS damage system or the common Unity
//					SendMessage: 'Damage(float)' approach. it also snaps to the hit point
//					and plays a sound there (as long as the bullet prefab has an
//					AudioSource with its AudioClip set).
//
//					this script can use the UFPS SurfaceManager to spawn unique impact
//					effects based on the bullet used and the surface hit. the resulting
//					effect will depend on the object assigned into the ImpactEvent slot.
//
//					NOTE: this script replaces the old 'vp_HitscanBullet' script
//					from UFPS 1.5.x and earlier.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]

public class vp_FXBullet : vp_Bullet
{

	public vp_ImpactEvent ImpactEvent = null;		// for spawning surface effects on impact with the UFPS surface system
#if UNITY_EDITOR
	[vp_HelpBox("Make sure to assign an ImpactEvent object into the above slot (click the small circle to select). This will allow the bullet to spawn impact effects intelligently depending on the surface hit, by hooking into the powerful UFPS surface system.", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.EmptyLine)]
	public float hitscanBulletHelp;
#endif


	/// <summary>
	/// snaps the bullet to the hit point (for proper 3d audio positioning)
	/// and tries to spawn a UFPS surface effect
	/// </summary>
	protected override void TrySpawnFX()
	{

		// move transform to impact point in order for the audio source to play
		// impact sound at the correct 3d position
		m_Transform.position = m_Hit.point;

		vp_SurfaceManager.SpawnEffect(m_Hit, ImpactEvent, m_Audio);

	}


	/// <summary>
	/// applies damage in the UFPS format, with the amount of damage, its source
	/// and the damage type 'Bullet'
	/// </summary>
	protected override void DoUFPSDamage()
	{

		m_TargetDHandler.Damage(new vp_DamageInfo(Damage, m_Source, vp_DamageInfo.DamageType.Bullet));

	}


}

