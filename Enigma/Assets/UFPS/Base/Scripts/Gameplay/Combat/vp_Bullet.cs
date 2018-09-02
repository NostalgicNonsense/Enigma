/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Bullet.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	base class for hitscan projectiles. when spawned, it raycasts ahead
//					to damage targets using the UFPS damage system or the common Unity
//					SendMessage: 'Damage(float)' approach. also, snaps to the hit point
//					and plays a sound there (as long as the bullet prefab has an
//					AudioSource with its AudioClip set). note that this script is meant
//					to be inherited and extended. it is very crude on its own and has no
//					functionality to handle decals very well
//
//					NOTES:
//						1) this base class does not feature particle effects or decals.
//							instead use the derived class 'vp_FXBullet' which integrates
//							with the powerful UFPS surface fx system!
//						2) 'vp_HitScanBullet' is another derived class but is retained
//							for backwards compatibility ONLY. it is strongly recommended
//							to use 'vp_FXBullet' instead!
//						3) to prevent a trigger from interfering with bullets, put it in
//							the 'Trigger' layer (default: 27). LORE: you can't disregard
//							a hit based on !collider.isTrigger, because that would make
//							bullets DISAPPEAR if they hit a trigger
//						4) 	linecasts are more accurate than raycasts, but slow. only use
//							the 'Linecast' scan type if you experience blatant problems
//							with bullets not registering their targets
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]

public class vp_Bullet : MonoBehaviour
{


	// gameplay
	public float Range = 100.0f;				// max travel distance of this type of bullet in meters
	public float Force = 100.0f;				// force applied to any rigidbody hit by the bullet
	public float Damage = 1.0f;					// the damage transmitted to target by the bullet
	public vp_DamageInfo.DamageMode DamageMode = vp_DamageInfo.DamageMode.DamageHandler;	// should the bullet transmit UFPS damage, or a Unity Message, or both
	public string DamageMethodName = "Damage";	// user defined name of damage method on target
												// TIP: this can be used to call specialized damage methods directly,
												// for example: magical, freezing, poison, electric

	[System.Obsolete("Please use 'DamageMode instead.")]
	public bool RequireDamageHandler			// deprecated (retained for external script backwards compatibility only)
	{
		get { return ((DamageMode == vp_DamageInfo.DamageMode.DamageHandler) || (DamageMode == vp_DamageInfo.DamageMode.Both));	}
		set { DamageMode = (value ? vp_DamageInfo.DamageMode.DamageHandler : vp_DamageInfo.DamageMode.UnityMessage);	}
	}

	public HitScanType ScanType = HitScanType.Raycast;	// should the bullet use a raycast (for performance) or linecast (for accuracy) ?
	public enum HitScanType
	{
		Raycast,	// faster & less accurate hit detection
		Linecast	// slower & more accurate hit detection
	}


	// components
	protected Transform m_Transform = null;
	protected Renderer m_Renderer = null;
	protected AudioSource m_Audio = null;

	// internal state
	protected bool m_Initialized = false;
	protected Transform m_Source = null;						// inflictor / source of the damage
	protected static vp_DamageHandler m_TargetDHandler = null;

	// raycasting
	protected Ray m_Ray;
	protected RaycastHit m_Hit;
	protected int LayerMask = vp_Layer.Mask.IgnoreWalkThru;

#if UNITY_EDITOR
	private bool m_DidWarnAboutBothMethodName = false;
#endif


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{
	
		m_Transform = transform;
		m_Renderer = GetComponent<Renderer>();
		m_Audio = GetComponent<AudioSource>();

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{
	
		m_Initialized = true;

		StartCoroutine(TryHitOnEndOfFrame());

	}



	/// <summary>
	/// in case of pooling
	/// </summary>
	protected virtual void OnEnable()
	{
	
		if(!m_Initialized)
			return;

		StartCoroutine(TryHitOnEndOfFrame());

	}



	/// <summary>
	/// hitscans against all big, solid objects that is not the local player
	/// who fired the bullet, and upon success: runs the DoHit method
	/// </summary>
	protected virtual bool TryHit()
	{

		m_Ray = new Ray(m_Transform.position, m_Transform.forward);

		// if this bullet was fired by the local player: don't allow it to hit the local player!
		if ((m_Source != null) && (m_Source.gameObject.layer == vp_Layer.LocalPlayer))
			LayerMask = vp_Layer.Mask.BulletBlockers;
		else
			LayerMask = vp_Layer.Mask.IgnoreWalkThru;

		// raycast against all big, solid objects
		switch (ScanType)
		{
			case HitScanType.Raycast:
				if (!Physics.Raycast(m_Ray, out m_Hit, Range, LayerMask))
					return false;
				break;
			case HitScanType.Linecast:
				if (!Physics.Linecast(m_Transform.position, m_Transform.position + (m_Transform.forward * Range), out m_Hit, LayerMask))
					return false;
				break;
		}

		DoHit();

		return true;

	}

	
	/// <summary>
	/// in the event of a succesful bullet hit: attempts to spawn effects,
	/// play an impact sound, add force to any rigidbody targets, damage
	/// damageable ones, and finally starts the process to remove the bullet
	/// when silent
	/// </summary>
	protected virtual void DoHit()
	{

		// spawn particle effects and decals
		TrySpawnFX();

		// play sound if we have an audio source + clip
		TryPlaySound();

		// if hit object has physics, add the bullet force to it
		TryAddForce();

		// try to make damage in the best supported way
		TryDamage();

		// remove the bullet - as long as it's invisible and silent
		TryDestroy();

	}


	/// <summary>
	/// override this method to spawn particles, decals and other FX.
	/// this base version of the method just sets the position and rotation
	/// of the bullet on impact  (for proper 3d audio positioning).
	/// see 'vp_FXBullet' for an example of integration with the UFPS surface
	/// system, or 'vp_HitscanBullet' for the old, more hard-coded approach
	/// </summary>
	protected virtual void TrySpawnFX()
	{

		// move transform to impact point in order for the audio source to play
		// impact sound at the correct 3d position
		m_Transform.position = m_Hit.point;

		// adopt the normal of the surface hit
		m_Transform.rotation = Quaternion.LookRotation(m_Hit.normal);

	}
	

	/// <summary>
	/// plays the audio clip of any attached audio source, as long as
	/// the bullet has an audio source with its audioclip set
	/// </summary>
	protected virtual void TryPlaySound()
	{

		if (m_Audio == null)
			return;

		if (m_Audio.clip == null)
			return;

		m_Audio.pitch = Time.timeScale;
		m_Audio.Stop();
		m_Audio.Play();

	}


	/// <summary>
	/// adds the bullet force to the non-kinematic rigidbody of the
	/// hit object (if any)
	/// </summary>
	protected virtual void TryAddForce()
	{

		Rigidbody body = m_Hit.collider.attachedRigidbody;

		if (body == null)
			return;

		if(body.isKinematic)
			return;

		body.AddForceAtPosition(((m_Ray.direction * Force) / Time.timeScale) / vp_TimeUtility.AdjustedTimeScale, m_Hit.point);
	
	}


	/// <summary>
	/// attempts to do damage using a regular Unity-message, and / or more advanced
	/// UFPS format damage (whichever is supported by the bullet and target)
	/// </summary>
	protected virtual void TryDamage()
	{

		// send primitive damage as UnityMessage. this allows support for many third party
		// systems (simply use a 'void Damage(float)' method in target MonoBehaviours)
		if ((DamageMode == vp_DamageInfo.DamageMode.UnityMessage)
			|| (DamageMode == vp_DamageInfo.DamageMode.Both))
		{
			m_Hit.collider.SendMessage(DamageMethodName, Damage, SendMessageOptions.DontRequireReceiver);
#if UNITY_EDITOR
			if (!m_DidWarnAboutBothMethodName
				&& (DamageMethodName == "Damage")
				&& (vp_DamageHandler.GetDamageHandlerOfCollider(m_Hit.collider) != null))
			{
				Debug.LogWarning("Warning (" + this + ") Target object has a vp_DamageHandler. When damaging it with DamageMode: 'UnityMessage' or 'Both', you probably want to change 'DamageMethodName' to something other than 'Damage', or too much damage might be applied.");
				m_DidWarnAboutBothMethodName = true;
			}
#endif
		}

		// send damage in UFPS format. this allows different damage types, and tracking damage source
		if ((DamageMode == vp_DamageInfo.DamageMode.DamageHandler)
			|| (DamageMode == vp_DamageInfo.DamageMode.Both))
		{
			m_TargetDHandler = vp_DamageHandler.GetDamageHandlerOfCollider(m_Hit.collider);
			if (m_TargetDHandler != null)
				DoUFPSDamage();
		}

	}


	/// <summary>
	/// applies damage in the UFPS format, with the amount of damage and its
	/// source. NOTE: this method is overridden by 'vp_FXBullet'
	/// </summary>
	protected virtual void DoUFPSDamage()
	{

		m_TargetDHandler.Damage(new vp_DamageInfo(Damage, m_Source));

	}


	/// <summary>
	/// checks if the impact sound is still playing and, if not, destroys the
	/// object. otherwise tries again in 1 sec. visible objects (typically -
	/// decals) will not be automatically destroyed
	/// </summary>
	protected virtual void TryDestroy()
	{

		if (this == null)
			return;

		// cancel destruction of a visible bullet object. NOTE: the object may
		// need to be removed later by some other script (the vp_FXBullet and
		// vp_HitscanBullet scripts rely on vp_DecalManager for this). visible
		// bullet objects using this script are left alone by default
		if((m_Renderer != null) && m_Renderer.enabled)
			return;

		// if audio is still playing, try again later. NOTE: not a good idea to use looping AudioClips with this script (!)
		if ((m_Audio != null) && (m_Audio.isPlaying))
		{
			vp_Timer.In(1, TryDestroy);
			return;
		}

		// restore the renderer for pooling (recycling)
		if (m_Renderer != null)
			m_Renderer.enabled = true;

		vp_Utility.Destroy(gameObject);

	}


	/// <summary>
	/// waits for end of frame then tries to hit something. waiting to move
	/// the bullet is required in order for shooter spread to be applied first
	/// </summary>
	protected IEnumerator TryHitOnEndOfFrame()
	{
		yield return new WaitForEndOfFrame();
		if (!TryHit())
			StartCoroutine(DestroyOnNextFrame());
	}


	/// <summary>
	/// despawns the bullet on next frame. this is used when the bullet
	/// didn't hit anything and was immediately discarded. in these cases
	/// we must wait one frame for the object to be properly reparented
	/// ot the pool manager when deactivated
	/// </summary>
	protected IEnumerator DestroyOnNextFrame()
	{
		yield return 0;
		vp_Utility.Destroy(gameObject);
	}


	/// <summary>
	/// identifies the source transform of a bullet's damage
	/// (typically the person shooting)
	/// </summary>
	public virtual void SetSource(Transform source)
	{
		m_Source = source;
	}

	
}

