/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SurfaceType.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	the SurfaceType ScriptableObject is the main surface concept
//					in UFPS, used for spawning effects on projectile or footstep
//					impact (and potentially many other things).
//
//					each surface has a list of 'ImpactFX' structs, each pairing a
//					vp_ImpactEvent object with a vp_SurfaceEffect object for every
//					type of collision effect response we want to cover.
//					the vp_SurfaceManager is responsible for reading this object and
//					spawning the effects.
//
//					the recommended usage is for this object to be attached to individual
//					gameobjects using a vp_SurfaceIdentifier component. when a projectile,
//					fall- or footstep impact occurs, the vp_SurfaceManager will read the
//					vp_SurfaceType to see if the ImpactEvent and SurfaceType is represented
//					in the 'ImpactFX' list. if so, the corresponding SurfaceEffect will be
//					played. if not, the SurfaceManager will try to revert to a good fallback.
//
//					SurfaceType objects are created from the top UFPS menu -> Wizards ->
//					Surfaces -> Create Surface Type.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class vp_SurfaceType : ScriptableObject
{


	/// <summary>
	/// used by the editor 'vp_UFPSMenu.cs' to initialize the scriptableobject
	/// with default variables
	/// </summary>
	public void Init()
	{

		ImpactFX.Add(new ImpactFXInfo(true));

	}
		
	// this struct is used to define a SurfaceType + ImpactEvent combo with a resulting SurfaceEffect
	[System.Serializable]
	public struct ImpactFXInfo
	{
		public ImpactFXInfo(bool init)
		{
			ImpactEvent = null;
			SurfaceEffect = null;
		}
		public vp_ImpactEvent ImpactEvent;
		public vp_SurfaceEffect SurfaceEffect;
	}

#if UNITY_EDITOR
	[vp_Separator]
	public vp_Separator s1;
#endif


	[SerializeField]
	public List<ImpactFXInfo> ImpactFX = new List<ImpactFXInfo>();

#if UNITY_EDITOR
	[vp_HelpBox("• 'ImpactFX' lists any ImpactEvents that should result in a certain SurfaceEffect when hitting this SurfaceType.\n\n• Whenever a projectile, fall- or footstep impact occurs, the vp_SurfaceManager looks for the particular SurfaceType + ImpactEvent combo here. If found, the resulting SurfaceEffect is played. If not, the SurfaceManager will try to come up with a good fallback effect.\n", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float itemTypeHelp;
#endif

	[SerializeField]
	public bool AllowFootprints = false;

#if UNITY_EDITOR
	[vp_HelpBox("• When walking on this surface, should vp_PlayerFootFXHandler send extra info (regarding foot and direction) to the SurfaceManager?\n\n• This setting will ONLY WORK if there are FX WITH DECALS in the ImpactFX list.\n\n• For performance reasons you should only enable this on soft ground surfaces like SNOW, MUD or SAND.\n", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float footPrintHelp;
#endif


	[System.NonSerialized]
	protected bool m_CanHaveFootprints = false;
	[System.NonSerialized]
	protected bool m_CachedCanHaveFootprints = false;

	/// <summary>
	/// returns true if this surface has one or more impact effects with 'footstep'
	/// in the name, that also has decals. the result will be cached and will stay
	/// the same for this surface type throughout the whole game session
	/// </summary>
	public bool CanHaveFootprints()
	{

		if (!m_CachedCanHaveFootprints)
		{
			if (!AllowFootprints)
			{
				m_CanHaveFootprints = false;
			}
			else
			{
				foreach (vp_SurfaceType.ImpactFXInfo f in ImpactFX)
				{
					if (f.SurfaceEffect == null)
						continue;
					if (f.SurfaceEffect.Decal.m_Prefabs.Count > 0)
					{
						m_CanHaveFootprints = true;
						break;
					}
				}
			}
			m_CachedCanHaveFootprints = true;
		}
		return m_CanHaveFootprints;

	}


}

