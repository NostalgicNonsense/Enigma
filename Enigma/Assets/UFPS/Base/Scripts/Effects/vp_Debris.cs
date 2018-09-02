/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Debris.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	death effect for breaking objects. to use this script:
//						1. create small objects to simulate pieces / debris of your
//						breaking object. add colliders and rigidbodies to them
//						2. create an empty gameobject and place your debris objects
//						inside it in the outline / shape of the breaking object
//						3. drag this script to the gameobject and tweak its values
//						4. make the gameobject into a prefab and use it as a death
//						effect on a vp_DamageHandler
//
//					NOTE: make sure that your death effect prefab and all of its
//					children are in the 'Debris' layer, otherwise they will
//					collide with the player and interfere with the controls,
//					which can be very annoying
//
///////////////////////////////////////////////////////////////////////////////// 

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]

public class vp_Debris : MonoBehaviour
{

	// gameplay
	public float Radius = 2.0f;					// any objects within radius will be affected by the force
	public float Force = 10.0f;					// amount of motion force to apply to affected objects
	public float UpForce = 1.0f;				// how much to push affected objects up in the air

	// sound
	public List<AudioClip> Sounds = new List<AudioClip>();
	public float SoundMinPitch = 0.8f;			// random pitch range for explosion sound
	public float SoundMaxPitch = 1.2f;
	AudioSource m_Audio = null;
	AudioSource Audio
	{
		get
		{
			if (m_Audio == null)
				m_Audio = GetComponent<AudioSource>();
			return m_Audio;
		}
	}

	public float LifeTime = 5.0f;				// total lifetime of effect, during which rigidbodies will be removed at random points

	protected bool m_Destroy = false;
	protected Collider[] m_Colliders = null;
	protected Dictionary<Collider, Dictionary<string, object>> m_PiecesInitial = new Dictionary<Collider, Dictionary<string, object>>();


	/// <summary>
	/// 
	/// </summary>
	void Awake()
	{
	
		m_Colliders = GetComponentsInChildren<Collider>();

		foreach (Collider col in m_Colliders)
		{
			if (col.GetComponent<Rigidbody>())
				m_PiecesInitial.Add(col, new Dictionary<string, object>() { { "Position", col.transform.localPosition }, { "Rotation", col.transform.localRotation } });
		}
	
	}
	

	/// <summary>
	/// 
	/// </summary>
	void OnEnable()
	{
	
		m_Destroy = false;
		if(Audio != null)
			Audio.playOnAwake = true;
		
		foreach (Collider col in m_Colliders)
		{
			Rigidbody r = col.GetComponent<Rigidbody>();
			if (r != null)
			{
				col.transform.localPosition = (Vector3)m_PiecesInitial[col]["Position"];
				col.transform.localRotation = (Quaternion)m_PiecesInitial[col]["Rotation"];
			
				r.velocity = Vector3.zero;
				r.angularVelocity = Vector3.zero;
			
				r.AddExplosionForce((Force / Time.timeScale) / vp_TimeUtility.AdjustedTimeScale, transform.position, Radius, UpForce);
				Collider c = col;
				vp_Timer.In(Random.Range(LifeTime * 0.5f, LifeTime * 0.95f), delegate()
				{
					if (c != null)
						vp_Utility.Activate(c.gameObject, false);
				});
			}
		}

		vp_Timer.In(LifeTime, delegate()
		{
			m_Destroy = true;
		});

		// play sound
		if ((Audio != null) && (Sounds.Count > 0))
		{
			Audio.rolloffMode = AudioRolloffMode.Linear;
			Audio.clip = Sounds[(int)Random.Range(0, (Sounds.Count))];
			Audio.pitch = Random.Range(SoundMinPitch, SoundMaxPitch) * Time.timeScale;
			Audio.Play();
		}

	}


	/// <summary>
	/// 
	/// </summary>
	void Update()
	{

		// the effect should be removed as soon as the 'm_Destroy' flag
		// has been set and the sound has stopped playing
		if (m_Destroy && (Audio != null) && (!Audio.isPlaying))
		{
			foreach (Collider col in m_Colliders)
			{
				vp_Utility.Activate(col.gameObject, true);
			}
			vp_Utility.Destroy(gameObject);
		}

	}


}

