/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FadingDecal.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	use this manipulator on decals to spawn animated surface
//					effects for footsteps on sand, plasma gun impacts and more.
//					the decal will always fade out and can optionally be made
//					to shrink and rotate. when it reaches below 0.1 alpha it
//					will be destroyed
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;

public class vp_FadingDecal : MonoBehaviour
{

	// defaults
	protected Color m_OriginalColor = Color.white;
	protected Quaternion m_OriginalRotation = Quaternion.identity;

	// fadeout
	[Range(0, 5)]
	public float FadeoutDelay = 1.0f;
	[Range(0.1f, 5)]
	public float FadeoutSpeed = 0.25f;
	protected float m_ActualFadeoutSpeed = 0.0f;
	protected float m_StartFadeoutTime = 0.0f;

	// shrink
	[Range(0, 5)]
	public float ShrinkDelay = 2.0f;
	[Range(0, 5)]
	public float ShrinkSpeed = 0.1f;
	protected float m_ActualShrinkSpeed = 0.0f;
	protected float m_StartShrinkTime = 0.0f;
	protected Vector3 m_OriginalScale = Vector3.one;

	// rotate
	[Range(0, 5)]
	public float RotateDelay = 1.0f;
	[Range(0, 300)]
	public float RotateSpeed = 15.0f;
	protected float m_StartRotateTime = 0.0f;
	protected float m_CurrentRotateSpeed = 0.0f;
	protected float m_ActualRotateSpeed = 0.0f;
	protected bool m_FlipRotate = false;

	// acceleration
	public bool RotateAccelerate = true;


	Renderer m_Renderer = null;
	Renderer Renderer
	{
		get
		{
			if (m_Renderer == null)
				m_Renderer = GetComponent<Renderer>();
			if ((m_Renderer == null) || (m_Renderer.material == null))
				enabled = false;
			return m_Renderer;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		if (Renderer == null)
			return;

		// store defaults for pooling re-use
		m_OriginalColor = Renderer.material.color;
		m_OriginalRotation = transform.rotation;
		m_OriginalScale = transform.localScale;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		if (Renderer == null)
			return;
		
		// restore defaults in case object was recycled (pooled)
		Renderer.material.color = m_OriginalColor;
		transform.rotation = m_OriginalRotation;
		transform.localScale = m_OriginalScale;

		m_StartFadeoutTime = Time.time + FadeoutDelay;
		m_StartShrinkTime = Time.time + ShrinkDelay;
		m_StartRotateTime = Time.time + RotateDelay;

		m_FlipRotate = (Random.value < 0.5f);

		m_ActualFadeoutSpeed = FadeoutSpeed + (FadeoutSpeed * ((Random.value < 0.5f) ? -0.5f : 0.5f));
		m_ActualShrinkSpeed = ShrinkSpeed + (ShrinkSpeed * ((Random.value < 0.5f) ? -0.5f : 0.5f));
		m_ActualRotateSpeed = RotateSpeed + (RotateSpeed * ((Random.value < 0.5f) ? -0.5f : 0.5f));
		m_CurrentRotateSpeed = 0.0f;

	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		if (Renderer == null)
			return;

		if (Time.time > m_StartFadeoutTime)
		{
			Color c = Renderer.material.color;
			c.a = Mathf.Lerp(c.a, 0.0f, Time.deltaTime * m_ActualFadeoutSpeed);
			Renderer.material.color = c;
		}

		if ((ShrinkSpeed > 0.0f) && Time.time > m_StartShrinkTime)
			transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * m_ActualShrinkSpeed);

		if ((RotateSpeed > 0.0f) && Time.time > m_StartRotateTime)
		{
			if (RotateAccelerate)
				m_CurrentRotateSpeed = Mathf.SmoothStep(m_CurrentRotateSpeed, RotateSpeed, Time.deltaTime * 2);
			else
				m_CurrentRotateSpeed = RotateSpeed;
			transform.eulerAngles += Vector3.forward * Time.deltaTime * (m_FlipRotate ? -m_CurrentRotateSpeed : m_CurrentRotateSpeed);
		}

		if (Renderer.material.color.a < 0.1f)
			vp_Utility.Destroy(gameObject);

	}


}
