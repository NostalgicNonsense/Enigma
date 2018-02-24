/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Billboard.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script will make its gameobject always face the camera.
//
//					NOTE: in VR the billboard will have a slightly off angle in
//					the editor, but in a standalone build it will look correct
//
///////////////////////////////////////////////////////////////////////////////// 

using UnityEngine;


public class vp_Billboard : MonoBehaviour
{

	Transform m_Transform = null;


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		m_Transform = transform;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		// in VR, we must rotate the billboard towards the eye camera that is
		// currently rendering, or the angle will become askew. however, this
		// only works in a standalone build since 'Camera.current' will return
		// an arbitrary scene view camera in the editor
		if (vp_Gameplay.IsVR && !Application.isEditor)
		{
			if (Camera.current != null)
				m_Transform.LookAt(Camera.current.transform);
		}
		else
		{
			// we are either in the editor or not in VR: look at the main camera
			if (Camera.main != null)
				m_Transform.LookAt(Camera.main.transform);
		}


	}


}