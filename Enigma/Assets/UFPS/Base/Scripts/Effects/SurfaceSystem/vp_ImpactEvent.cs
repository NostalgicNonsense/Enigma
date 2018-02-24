/////////////////////////////////////////////////////////////////////////////////
//
//	vp_ImpactEvent.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	the ImpactEvent ScriptableObject is used to distinguish between
//					different types of collisions for surface effect and damage
//					logic purposes.
//					typical impact events are: BulletHit, FallImpact, Footstep,
//					ItemDrop, etc.
//
//					when a bullet hits a rock floor, the impact event is what makes
//					the SurfaceManager spawn a bullet hit effect instead of a footstep
//					effect. to achieve this, vp_ImpactEvent objects are paired with
//					vp_SurfaceEffect objects inside an encompassing vp_SurfaceType
//					object.
//
//					ImpactEvent objects are created from the top UFPS menu -> Wizards ->
//					Surfaces -> Create Impact Event.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class vp_ImpactEvent : ScriptableObject
{

#if UNITY_EDITOR
	[vp_Separator]
	public vp_Separator s1;
#endif

#if UNITY_EDITOR
	[vp_HelpBox("This object declares (by filename) an ImpactEvent, representing a particular type of collision that should generate an effect. Typical ImpactEvents are: BulletHit, FallImpact, Footstep, ItemDrop, etc.\n\n• When a bullet hits a rock floor, the ImpactEvent is what makes the SurfaceManager spawn a bullet hit effect instead of a footstep effect. To achieve this, vp_ImpactEvent objects are paired with vp_SurfaceEffect objects inside an encompassing vp_SurfaceType object.\n\n• You can create a new ImpactEvent object from the top UFPS menu -> Wizards -> Surfaces -> Create Impact Event.\n\n• Then, assign them to a vp_SurfaceType object to make that surface type recognize the ImpactEvent and pair it with a SurfaceEffect.\n\n• Finally, you can set a global fallback ImpactEvent in the SurfaceManager -> Default Fallbacks, for cases where the impact event is unknown (for example: someone forgot to set an ImpactEvent on a bullet component).\n", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float itemTypeHelp;
#endif


}

