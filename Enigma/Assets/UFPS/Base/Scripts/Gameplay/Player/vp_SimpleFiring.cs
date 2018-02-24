/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SimpleFiring.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this simple demo script forwards fire events to a weapon.
//					for a more complex weapon handler that can switch between
//					weapons and more, see 'vp_FPWeaponHandler'
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;


public class vp_SimpleFiring : MonoBehaviour
{

#if UNITY_EDITOR
	[vp_HelpBox("This simple demo script forwards fire events to a weapon. For a more complex weapon handler that can switch between weapons and more, see 'vp_FPWeaponHandler'.", UnityEditor.MessageType.Info, typeof(vp_SimpleFiring), null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox;
#endif

	protected vp_PlayerEventHandler m_Player = null;


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		// store the first player event handler found in the top of our transform hierarchy
		m_Player = (vp_PlayerEventHandler)transform.root.GetComponentInChildren(typeof(vp_PlayerEventHandler));

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		// allow this monobehaviour to talk to the player event handler
		if (m_Player != null)
			m_Player.Register(this);

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		// unregister this monobehaviour from the player event handler
		if (m_Player != null)
			m_Player.Unregister(this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{
		
		// continuously try to fire the weapon while player is attacking
		if (m_Player.Attack.Active)
			m_Player.Fire.Try();

	}


}


