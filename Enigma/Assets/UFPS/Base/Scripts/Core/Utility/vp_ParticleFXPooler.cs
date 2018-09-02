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
//						2) As soon as the particle system's 'IsAlive' flag turns false, the prefab 
//                          will be despawned and pooled for recycling. note that if the particle 
//                          prefab is childed to another prefab that is pooled, then you may not 
//                          need to assign this script to it - the Shuriken effect will be activated 
//                          and deactivated along with its parent and there's a change it will 
//                          retrigger nicely and properly. however, if the particle system is _not_ 
//                          childed to a pooled prefab, then without this script, its gameobject 
//                          will be left in the scene forever (unless you destroy or despawn it manually).
//
//						3) this script was developed and tested with one-shot particle effects
//							like explosions and bullet impact dust emissions, and may not handle
//							other types of effects properly
//
/////////////////////////////////////////////////////////////////////////////////


using UnityEngine;

public class vp_ParticleFXPooler : MonoBehaviour
{

	ParticleSystem m_ShurikenParticleSystem = null;

	/// <summary>
	/// 
	/// </summary>
	void Awake()
	{
		m_ShurikenParticleSystem = GetComponent<ParticleSystem>();
	}

    /// <summary>
    /// 
    /// </summary>
    void Update()
    {
        if (!m_ShurikenParticleSystem.IsAlive())
            vp_Utility.Destroy(gameObject);

    }
}
