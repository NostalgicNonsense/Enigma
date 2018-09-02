/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SurfaceIdentifier.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this component is used to determine what effects should emanate
//					from the surface of an object when hit by impact events, such as
//					bullet hits, footsteps, fall impacts and rigidbody collisions.
//
//					NOTE: the parameter 'SurfaceID' is only provided for backwards
//					compatibility with the old UFPS 'vp_FootStepManager'. it is not
//					used by vp_SurfaceManager.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_SurfaceIdentifier : MonoBehaviour
{

	public vp_SurfaceType SurfaceType;
	public bool AllowDecals = true;
	// NOTE: This parameter is only provided for backwards compatibility with the
	// old UFPS 'vp_FootStepManager'. it has no function in the new surface system
	public int SurfaceID;

#if UNITY_EDITOR
	[vp_HelpBox("• 'SurfaceType' determines what the object's surface is made of, and what vp_SurfaceEffect it will trigger when it gets hit by something.\n\n• 'Allow Decals' determines whether bullet holes and footprints can stick to the surface of the object. It will override any SurfaceManager settings for this particular object.\n\n• NOTE: 'SurfaceID' is only provided for backwards compatibility with the old UFPS 'vp_FootStepManager'. it is not used by vp_SurfaceManager", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float surfaceTypeHelp;
#endif


}
