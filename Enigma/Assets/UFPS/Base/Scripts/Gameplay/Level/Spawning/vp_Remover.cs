/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Remover.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script makes an object disappear after a set amount of time.
//					default lifetime is ten seconds. minimum is a tenth of a second.
//					
///////////////////////////////////////////////////////////////////////////////// 

using UnityEngine;
using System.Collections;

public class vp_Remover : MonoBehaviour
{

	public float LifeTime = 10.0f;

	protected vp_Timer.Handle m_DestroyTimer = new vp_Timer.Handle();


	/// <summary>
	/// 
	/// </summary>
	void OnEnable()
	{

		vp_Timer.In(Mathf.Max(LifeTime, 0.1f), () =>
		{
			vp_Utility.Destroy(gameObject);
		}, m_DestroyTimer);

	}

	
	/// <summary>
	/// 
	/// </summary>
	void OnDisable()
	{
		m_DestroyTimer.Cancel();
	}
	

}
