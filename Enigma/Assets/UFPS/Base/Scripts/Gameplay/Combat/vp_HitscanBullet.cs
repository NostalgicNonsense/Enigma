/////////////////////////////////////////////////////////////////////////////////
//
//	vp_HitscanBullet.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this is the old UFPS (1.5.x and earlier) bullet class. it has
//					hard-coded surface effect logic that is being made redundant
//					by the new UFPS surface system. please use vp_FXBullet instead!
//
//					PLEASE NOTE:
//						1) this script is provided for backwards compatibility only.
//					it will post a warning to the console when used. use only if you're
//					maintaining an older UFPS code base, or if you have no need for the
//					UFPS 1.6+ surface system (in which case you may want to use the base
//					class (vp_Bullet) instead to avoid problems when this script is removed.
//
//						2) this script does not work with the UFPS 1.6+ surface system. it is
//					partially supported by vp_DecalManager (the decals will be capped and
//					weathered by amount, but they will not be subject to the more advanced
//					decal features like placement tests and smart removal.
//
//					USAGE: this script should be attached to a gameobject with a mesh
//					to be used as the impact decal (bullet hole). everything happens in the
//					DoHit method. the script that spawns the bullet is responsible for setting
//					its position and angle. after being instantiated, the bullet immediately
//					raycasts ahead for its full range, then snaps itself to the surface of the
//					first object hit. it then spawns a number of particle effects and plays a
//					random impact sound.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]

public class vp_HitscanBullet : vp_Bullet
{

	public float m_SparkFactor = 0.5f;		// chance of bullet impact generating a spark

	// these gameobjects will all be spawned at the point and moment
	// of impact. technically they could be anything, but their
	// intended uses are as follows:
	public GameObject m_ImpactPrefab = null;	// a flash or burst illustrating the shock of impact
	public GameObject m_DustPrefab = null;		// evaporating dust / moisture from the hit material
	public GameObject m_SparkPrefab = null;		// a quick spark, as if hitting stone or metal
	public GameObject m_DebrisPrefab = null;	// pieces of material thrust out of the bullet hole and / or falling to the ground

	// sound
	public List<AudioClip> m_ImpactSounds = new List<AudioClip>();	// list of impact sounds to be randomly played
	public Vector2 SoundImpactPitch = new Vector2(1.0f, 1.5f);	// random pitch range for impact sounds

	// the gameobject will not be able to attach itself to these layers (useful for things like enemies)
	public int[] NoDecalOnTheseLayers;

#if UNITY_EDITOR
	[vp_HelpBox("This script is obsolete! Please replace it with 'vp_FXBullet' and assign the 'Bullet' ImpactEvent.", UnityEditor.MessageType.Warning, null, null, false, vp_PropertyDrawerUtility.Space.EmptyLine)]
	public float hitscanBulletHelp;
#endif
	protected static bool m_DidWarn = false;	// don't spam the console with deprecation info for every bullet


	/// <summary>
	/// 
	/// </summary>
	protected override void Start()
	{

		if (!m_DidWarn)
		{
			Debug.LogWarning("Warning (" + this + ") This script is obsolete. Please use 'vp_FXBullet' or 'vp_Bullet' instead!");
			m_DidWarn = true;
		}

		base.Start();

	}


	/// <summary>
	/// plays a random audio clip on the attached audio source, from the
	/// list of provided impact sounds
	/// </summary>
	protected override void TryPlaySound()
	{

		if (m_ImpactSounds.Count < 1)
			return;

		if (m_Audio == null)
			return;

		m_Audio.clip = m_ImpactSounds[(int)Random.Range(0, (m_ImpactSounds.Count))];

		if (m_Audio.clip == null)
			return;

		m_Audio.pitch = Random.Range(SoundImpactPitch.x, SoundImpactPitch.y) * Time.timeScale;
		m_Audio.Stop();
		m_Audio.Play();

	}
	

	/// <summary>
	/// sets the position and rotation of the bullet on impact  (for proper 3d
	/// audio positioning). this is the old, more hard-coded approach to effects.
	/// instead use 'vp_FXBullet' for integration with the UFPS surface system
	/// </summary>
	protected override void TrySpawnFX()
	{

		// attach this gameobject (the decal) to the hit object
		Vector3 scale = m_Transform.localScale;	// save scale to handle scaled parent objects
		m_Transform.parent = m_Hit.transform;
		m_Transform.localPosition = m_Hit.transform.InverseTransformPoint(m_Hit.point);
		m_Transform.rotation = Quaternion.LookRotation(m_Hit.normal);				// face away from hit surface
		if (m_Hit.transform.lossyScale == Vector3.one)								// if hit object has normal scale
			m_Transform.Rotate(Vector3.forward, Random.Range(0, 360), Space.Self);	// spin randomly
		else
		{
			// rotated child objects will get skewed if the parent object has been
			// unevenly scaled in the editor, so on scaled objects we don't support
			// spin, and we need to unparent, rescale and reparent the decal.
			m_Transform.parent = null;
			m_Transform.localScale = scale;
			m_Transform.parent = m_Hit.transform;
		}

		// prevent adding decals to objects based on layer
		if ((m_Renderer != null) && NoDecalOnTheseLayers.Length > 0)
		{
			foreach (int layer in NoDecalOnTheseLayers)
			{

				if (m_Hit.transform.gameObject.layer != layer)
					continue;
				m_Renderer.enabled = false;
				TryDestroy();
			}
		}

		// spawn impact effect
		if (m_ImpactPrefab != null)
			vp_Utility.Instantiate(m_ImpactPrefab, m_Transform.position, m_Transform.rotation);

		// spawn dust effect
		if (m_DustPrefab != null)
			vp_Utility.Instantiate(m_DustPrefab, m_Transform.position, m_Transform.rotation);

		// spawn spark effect
		if (m_SparkPrefab != null)
		{
			if (Random.value < m_SparkFactor)
				vp_Utility.Instantiate(m_SparkPrefab, m_Transform.position, m_Transform.rotation);
		}

		// spawn debris particle fx
		if (m_DebrisPrefab != null)
			vp_Utility.Instantiate(m_DebrisPrefab, m_Transform.position, m_Transform.rotation);

	}


	/// <summary>
	/// applies damage in the UFPS format, with the amount of damage,
	/// its source and the dmaage type 'Bullet'
	/// </summary>
	protected override void DoUFPSDamage()
	{

		m_TargetDHandler.Damage(new vp_DamageInfo(Damage, m_Source, vp_DamageInfo.DamageType.Bullet));

	}


	/// <summary>
	/// if the object has a visible renderer, adds it to the decal manager.
	/// if not, schedules it for destruction as soon as it's silent
	/// </summary>
	protected override void TryDestroy()
	{

		if ((m_Renderer != null) && m_Renderer.enabled)
		{
			vp_DecalManager.Add(gameObject);
			return;
		}

		base.TryDestroy();

	}


	/// <summary>
	/// identifies the source transform of a bullet's damage
	/// (typically the person shooting)
	/// </summary>
	[System.Obsolete("Please use 'SetSource' instead.")]
	public void SetSender(Transform sender)
	{
		SetSource(sender);
	}


}

