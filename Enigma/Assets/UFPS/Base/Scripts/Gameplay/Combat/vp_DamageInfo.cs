/////////////////////////////////////////////////////////////////////////////////
//
//	vp_DamageInfo.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	carries information about damage Type (such as bullet or
//					explosion damage) and Mode (such as whether to send damage
//					in UFPS format or as a Unity message).
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class vp_DamageInfo
{

	public float Damage;				// how much damage was done?
	public Transform Source;			// from what object did it come (directly)? common use: HUD / GUI
	public Transform OriginalSource;	// what object initially caused this to happen? common use: game logic, score
	public DamageType Type;				// what type of damage is this?
	
	public enum DamageType
	{
		Unknown,
		KillZone,
		Fall,
		Impact,
		Bullet,
		Explosion,
		// the above are the types represented in the UFPS demo but can be easily
		// extended: e.g. blunt, electrical, cutting, piercing, freezing, crushing
		// drowning, gas, acid, freezing, burning, scolding, magical, plasma etc.
	}

	public enum DamageMode
	{
		None,
		DamageHandler,
		UnityMessage,
		Both,
		// should a script transmit UFPS damage, or a Unity Message, or both?
		// NOTE: this is not sent with the vp_DamageInfo object, but provided
		// as a common feature of external systems that deal with damage
	}


	/// <summary>
	/// 
	/// </summary>
	public vp_DamageInfo(float damage, Transform source, DamageType type = DamageType.Unknown)
	{
		Damage = damage;
		Source = source;
		OriginalSource = source;
		Type = type;
	}


	/// <summary>
	/// 
	/// </summary>
	public vp_DamageInfo(float damage, Transform source, Transform originalSource, DamageType type = DamageType.Unknown)
	{
		Damage = damage;
		Source = source;
		OriginalSource = originalSource;
		Type = type;
	}
	

}

