/////////////////////////////////////////////////////////////////////////////////
//
//	vp_ParticleFXPooler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	use this script on particle fx prefabs to enable them for use
//					with the vp_PoolManager system
//
//					NOTES:
//
//						1) make sure there's a vp_PoolManager in the scene.
//
//						2) if the prefab uses a Legacy particle system, its 'AutoDestruct'
//							feature will be disabled and its random 'Energy' (lifetime) will
//							be managed by this script. when the lifetime is up, the particle
//							prefab will be despawned and pooled for recycling (instead of
//							destroyed). without this script, the Legacy effect gameobjects
//							that use 'AutoDestruct' will be destroyed and garbage collected,
//							and the ones that don't will be left in the scene forever (unless
//							you	destroy or despawn them manually).
//
//						3) if the prefab uses a Shuriken particle system, then as soon as its
//							'IsAlive' flag turns false, the prefab will be despawned and pooled
//							for recycling. note that if the particle prefab is childed to another
//							prefab that is pooled, then you may not need to assign this script to
//							it - the Shuriken effect will be activated and deactivated along with
//							its parent and there's a change it will retrigger nicely and properly.
//							however, if the particle system is _not_ childed to a pooled prefab,
//							then without this script, its gameobject will be left in the scene
//							forever (unless you destroy or despawn it manually).
//
//						4) this script was developed and tested with one-shot particle effects
//							like explosions and bullet impact dust emissions, and may not handle
//							other types of effects properly
//
/////////////////////////////////////////////////////////////////////////////////


using UnityEngine;
using System.Collections;

public class vp_ParticleFXPooler : MonoBehaviour
{

	ParticleSystem m_ShurikenParticleSystem = null;
#if UNITY_5_2 || UNITY_5_3
    ParticleAnimator m_LegacyParticleAnimator = null;
	ParticleEmitter m_LegacyParticleEmitter = null;
	float m_LegacyOriginalMinEnergy = 0.0f;
	float m_LegacyOriginalMaxEnergy = 0.0f;

	bool m_IsLegacy = false;
#endif
    bool m_IsShuriken = false;


	/// <summary>
	/// 
	/// </summary>
	void Awake()
	{

		m_ShurikenParticleSystem = GetComponent<ParticleSystem>();
		if (m_ShurikenParticleSystem != null)
		{
			// detected a Shuriken particle system
			m_IsShuriken = true;
#if UNITY_5_2 || UNITY_5_3
			m_IsLegacy = false;
#endif
        }
#if UNITY_5_2 || UNITY_5_3
		else
		{
			// detected a Legacy particle system
			m_LegacyParticleAnimator = GetComponent<ParticleAnimator>();
			m_LegacyParticleEmitter = GetComponent<ParticleEmitter>();
			if ((m_LegacyParticleAnimator != null) && (m_LegacyParticleEmitter != null))
			{
				m_IsLegacy = true;
				m_IsShuriken = false;
				m_LegacyParticleAnimator.autodestruct = false;
				m_LegacyOriginalMinEnergy = m_LegacyParticleEmitter.minEnergy;
				m_LegacyOriginalMaxEnergy = m_LegacyParticleEmitter.maxEnergy;
			}

		}
#endif
        // detected no particle system: kill self
#if UNITY_5_2 || UNITY_5_3
		if (!m_IsLegacy && !m_IsShuriken)
#else
        if (!m_IsShuriken)
#endif
        {
			enabled = false;
			GameObject.Destroy(this);
		}

	}


	/// <summary>
	/// 
	/// </summary>
	void OnEnable()
	{

#if UNITY_5_2 || UNITY_5_3
		StopCoroutine(TimedDestroy());
		if (m_IsLegacy)
		{
			// simulate / take over the lifetime logic of the Legacy system in order
			// to know when to despawn the effect
			m_LegacyParticleEmitter.minEnergy = m_LegacyParticleEmitter.maxEnergy = 
				Random.Range(m_LegacyOriginalMinEnergy, m_LegacyOriginalMaxEnergy);
			StartCoroutine(TimedDestroy());
		}
#endif

    }


    /// <summary>
    /// 
    /// </summary>
    void Update()
	{

		// despawn when Shuriken says it's ready
		if (m_IsShuriken)
		{
			if (!m_ShurikenParticleSystem.IsAlive())
				vp_Utility.Destroy(gameObject);
		}

	}


#if UNITY_5_2 || UNITY_5_3
	/// <summary>
	/// using a coroutine here instead of vp_Timer for less overhead in case
	/// there's an insane amount of active particles
	/// </summary>
	System.Collections.IEnumerator TimedDestroy()
	{

		yield return new WaitForSeconds(m_LegacyParticleEmitter.minEnergy);

		vp_Utility.Destroy(gameObject);
	
	}
#endif

}
