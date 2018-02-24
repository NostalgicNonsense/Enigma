/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SecurityCamTurret.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	an angry security camera + machinegun turret that swivels
//					back and forth in search for the local player. see the included
//					prefabs for example component setup. NOTE: this is a basic demo
//					script and not designed for multiplayer
//
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_SecurityCamTurret : vp_SimpleAITurret
{

	vp_AngleBob m_AngleBob = null;

	public GameObject Swivel = null;
	Vector3 SwivelRotation = Vector3.zero;

	public float SwivelAmp = 100;
	public float SwivelRate = 0.5f;
	public float SwivelOffset = 0.0f;

	vp_Timer.Handle vp_ResumeSwivelTimer = new vp_Timer.Handle();


	/// <summary>
	/// 
	/// </summary>
	protected override void Start()
	{

		base.Start();

		m_Transform = transform;
		m_AngleBob = gameObject.AddComponent<vp_AngleBob>();
		m_AngleBob.BobAmp.y = SwivelAmp;
		m_AngleBob.BobRate.y = SwivelRate;
		m_AngleBob.YOffset = SwivelOffset;
		m_AngleBob.FadeToTarget = true;
	
		SwivelRotation = Swivel.transform.eulerAngles;

	}

	/// <summary>
	/// 
	/// </summary>
	protected override void Update()
	{

		base.Update();

		// if have a target and swiveling is enabled
		if ((m_Target != null) && m_AngleBob.enabled)
		{
			m_AngleBob.enabled = false;
			vp_ResumeSwivelTimer.Cancel();
		}

		// if we have no target and swiveling is not enabled
		if ((m_Target == null) && !m_AngleBob.enabled && !vp_ResumeSwivelTimer.Active)
		{
			vp_Timer.In(WakeInterval * 2.0f, delegate()
			{
				m_AngleBob.enabled = true;
			}, vp_ResumeSwivelTimer);
		}

#if UNITY_EDITOR
		m_AngleBob.BobAmp.y = SwivelAmp;
		m_AngleBob.BobRate.y = SwivelRate;
		m_AngleBob.YOffset = SwivelOffset;
#endif

		SwivelRotation.y = m_Transform.eulerAngles.y;
		Swivel.transform.eulerAngles = SwivelRotation;

	}

}