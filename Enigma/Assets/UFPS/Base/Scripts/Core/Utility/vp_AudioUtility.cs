/////////////////////////////////////////////////////////////////////////////////
//
//	vp_AudioUtility.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	miscellaneous audio utility functions
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

public static class vp_AudioUtility
{

	/// <summary>
	/// Plays a random sound from a list, with a random pitch.
	/// </summary>
	public static void PlayRandomSound(AudioSource audioSource, List<AudioClip> sounds, Vector2 pitchRange)
	{

		if (audioSource == null)
			return;

		if (sounds == null || sounds.Count == 0)
			return;

		AudioClip soundToPlay = sounds[UnityEngine.Random.Range(0, sounds.Count)];

		if (soundToPlay == null)
			return;

		if (pitchRange == Vector2.one)
			audioSource.pitch = Time.timeScale;
		else
			audioSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y) * Time.timeScale;

		audioSource.PlayOneShot(soundToPlay);

	}


	/// <summary>
	/// Plays a random sound from a list.
	/// </summary>
	public static void PlayRandomSound(AudioSource audioSource, List<AudioClip> sounds)
	{
		PlayRandomSound(audioSource, sounds, Vector2.one);
	}


}

