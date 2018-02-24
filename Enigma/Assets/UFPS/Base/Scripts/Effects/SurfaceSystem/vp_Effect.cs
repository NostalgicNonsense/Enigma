/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Effect.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	effect spawning info base class. contains the data references
//					and logic for a simple one-shot effect spawning a bunch of random
//					prefabs and playing a random sound from a list. this can be extended
//					for more complex types of effects
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class vp_Effect : ScriptableObject
{


#if UNITY_EDITOR
	[vp_Separator]
	public vp_Separator s1;
#endif

	public SoundSection Sound = new SoundSection();
	public ObjectSection Objects = new ObjectSection();

	protected AudioClip m_SoundToPlay;
	protected AudioClip m_LastPlayedSound;	// prevents repetition
	protected int m_LastPlayFrame = 0;		// prevents excessive volume


	////////////// 'Objects' section ////////////////

	[System.Serializable]
	public struct ObjectSection
	{
		public List<ObjectSpawnInfo> m_Objects;
#if UNITY_EDITOR
		[vp_HelpBox("All prefabs in this list will attempt to spawn simultaneously, success depending on their respective 'Probability'. Perfect for particle fx and rubble prefabs!", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
		public float objectsHelp;
#endif
	}

	////////////// 'Sounds' section ////////////////

	[System.Serializable]
	public struct SoundSection
	{
		public List<AudioClip> m_Sounds;
		[Range(-2.0f, 1.0f)]
		public float MinPitch;
		[Range(1.0f, 2.0f)]
		public float MaxPitch;
		public bool MaxOnePerFrame;
#if UNITY_EDITOR
		[vp_HelpBox("• When the SurfaceEffect triggers, one AudioClip from 'Sounds' will be randomly chosen and played with a random pitch between 'MinPitch' and 'MaxPitch'.\n\n• For effects that may trigger many times at once (such as shotgun pellets) you should tick 'Max Once Per Frame' to avoid excessive sound volume on impact.\n", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
		public float soundsHelp;
#endif
	}


	[System.Serializable]
	public struct ObjectSpawnInfo
	{
		public ObjectSpawnInfo(bool init)
		{
			Prefab = null;
			Probability = 1.0f;
		}
		public GameObject Prefab;
		[Range(0.0f, 1.0f)]
		public float Probability;
	}


	/// <summary>
	/// sets some default values because structs cannot have field initializers.
	/// NOTE: this is never called at runtime. only once, by the external creator
	/// of the ScriptableObject. by default: vp_UFPSMenu.cs
	/// </summary>
	public virtual void Init()
	{
		Sound.MinPitch = 1.0f;
		Sound.MaxPitch = 1.0f;
		Objects.m_Objects = new List<ObjectSpawnInfo>();
		Objects.m_Objects.Add(new vp_Effect.ObjectSpawnInfo(true));
	}


	/// <summary>
	/// spawns fx objects according to their probabilities and plays a
	/// random sound at the hit point and using the audiosource. if
	/// audiosource is omitted or null, no sound will be played
	/// </summary>
	public virtual void Spawn(RaycastHit hit, AudioSource audioSource = null)
	{

		SpawnObjects(hit);

		PlaySound(audioSource);

	}


	/// <summary>
	/// spawns one instance each of any objects from the object list that
	/// manage a die roll against their spawn probability
	/// </summary>
	void SpawnObjects(RaycastHit hit)
	{

		for (int v = 0; v < Objects.m_Objects.Count; v++)
		{
			if (Objects.m_Objects[v].Prefab == null)
				continue;
			if (Random.value < Objects.m_Objects[v].Probability)
				vp_Utility.Instantiate(Objects.m_Objects[v].Prefab, hit.point, Quaternion.LookRotation(hit.normal));
		}

	}


	/// <summary>
	/// plays a randomly chosen sound from the sound list. if there is more
	/// than one sound, makes sure never to play the same sound twice in a
	/// row
	/// </summary>
	public virtual void PlaySound(AudioSource audioSource)
	{

		// must have an audio source
		if (audioSource == null)
			return;

		// prevent this effect from playing many sounds at once ( = excessive volumes)
		if (Sound.MaxOnePerFrame && (m_LastPlayFrame == Time.frameCount))
			goto abort;

		// only play sounds on active gameobjects
		if (!vp_Utility.IsActive(audioSource.gameObject))
			goto abort;

		// need a list of sounds ...
		if (Sound.m_Sounds == null)
			goto abort;

		// ... with sounds in it
		if (Sound.m_Sounds.Count == 0)
			goto abort;

		// don't allow audiosources in 'Logarithmic Rolloff' mode to be audible
		// beyond their max distance (Unity bug?)
		if ((UnityEngine.Camera.main != null) && Vector3.Distance(audioSource.transform.position, UnityEngine.Camera.main.transform.position) > audioSource.maxDistance)
			goto abort;

	reroll:
		m_SoundToPlay = Sound.m_Sounds[Random.Range(0, Sound.m_Sounds.Count)]; // get a random sound

		// if the sound is null, return
		if (m_SoundToPlay == null)
			goto abort;

		// if this sound was the last sound played, reroll for another sound
		if ((Sound.m_Sounds.Count > 1) && (m_SoundToPlay == m_LastPlayedSound))
			goto reroll;

		// play sound at same or random pitch
		if (Sound.MinPitch == Sound.MaxPitch)
			audioSource.pitch = (Sound.MinPitch * Time.timeScale);
		else
			audioSource.pitch = Random.Range(Sound.MinPitch, Sound.MaxPitch) * Time.timeScale;

		// set the sound on the audiosource
		audioSource.clip = m_SoundToPlay;

		// only play sounds on enabled audio sources
		if (!audioSource.enabled)	// need to check this here - it won't always be detected correctly at beginning of the method
			goto abort;

		// play the sound!
		audioSource.Play();

		// update state
		m_LastPlayedSound = m_SoundToPlay;
		m_LastPlayFrame = Time.frameCount;
		
		return;

	abort:
		// always keep the audiosource clip at null when not in use, or freak sounds may play in some situations
		audioSource.clip = null;

	}


}

