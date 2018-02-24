/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SurfaceEffect.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this object is a recipe for a bunch of effects to be spawned in
//					response to a certain type of collision. it might trigger when
//					a bullet hits a wall, or when a player makes a footstep, or falls
//					violently to the ground.
//
//					it also extends its base class to spawn vp_Effects with decals
//					using vp_DecalManager
//
//					the vp_SurfaceManager in a scene is typically responsible for
//					handling vp_SurfaceEffects, and chooses which effect to spawn
//					depending on what vp_SurfaceType and vp_ImpactEvent were involved
//					in a collision.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class vp_SurfaceEffect : vp_Effect
{

	public DecalSection Decal = new DecalSection();

	////////////// 'Decals' section ////////////////

	[System.Serializable]
	public struct DecalSection
	{

		public List<GameObject> m_Prefabs;
#if UNITY_EDITOR
		[vp_HelpBox("• When this SurfaceEffect is triggered, one prefab from the list will be randomly chosen, spawned, surface aligned and added to the decal manager cueue, for fading out over time.\n\n• IMPORTANT: All prefabs are assumed to have a MeshFilter and MeshRenderer on their main transform with a classic 2-TRIANGLE QUAD (normal aligned with the Z-vector). The overlap detection features of vp_DecalManager require this type of decal (!).", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
		public float prefabsHelp;
#endif
		[Range(0.1f, 2.0f)]
		public float MinScale;
		[Range(0.1f, 2.0f)]
		public float MaxScale;
		[Range(0.0f, 0.5f)]
		public float AllowEdgeOverlap;		// how close to wall corners / edges will this decal be allowed spawn? if 0, the full quad is required to sit firmly on the background surface. if 0.5f, some of the outer quad is allowed to deviate from the background
#if UNITY_EDITOR
		[vp_HelpBox("• With zero overlap (default) any decals in this effect that overlap the corner of a wall the SLIGHTEST BIT will be removed.\n\n• When overlap is maxed out (0.5) the decals are allowed to overlap corners all the way to their CENTER.\n\n• Please note that this feature only works if the scene has a vp_DecalManager component with active 'Placement Tests'.\n\n• For FOOTPRINTS, this setting is ignored unless your 'vp_PlayerFootFXHandler -> Verify ground contact' is enabled.", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
		public float edgeOverlapHelp;
#endif
		

	}
	

#if UNITY_EDITOR
	[vp_Separator]
	public vp_Separator s3;
#endif

	// for internal communication with vp_SurfaceManager
	public static Vector3 FootprintDirection = NO_DIRECTION;
	public static bool FootprintFlip = false;
	public static bool FootprintVerifyGroundContact = false;
	public static Vector3 NO_DIRECTION = new Vector3(-99999, -99999, -99999);

	// internal random decal choice logic
	protected GameObject m_DecalToSpawn = null;
	protected GameObject m_LastSpawnedDecal = null;


	/// <summary>
	/// sets some default values because structs cannot have field initializers.
	/// NOTE: this is never called at runtime. only once, by the external creator
	/// of the ScriptableObject in the editor. by default: vp_UFPSMenu.cs
	/// </summary>
	public override void Init()
	{
		base.Init();
		Decal.MinScale = 1.0f;
		Decal.MaxScale = 1.0f;
		Decal.AllowEdgeOverlap = 0.25f;
	}


	/// <summary>
	/// spawns a vp_Effect with a decal. if there is more than one prefab in
	/// the list, the same decal will never be spawned twice in a row by this
	/// object. a vp_Effect spawns fx objects according to their probabilities
	/// and plays a random sound at the hit point using the audiosource.
	/// if 'audiosource' is omitted or null, no sound will be played.
	/// LORE: if this is a footstep effect, the surface manager is responsible
	/// for setting the static 'FootprintDirection', 'FootprintFlip' and
	/// 'FootprintVerifyGroundContact' variables before caling this method
	/// </summary>
	public virtual void SpawnWithDecal(RaycastHit hit, AudioSource audioSource = null)
	{

		// spawn sound and objects
		base.Spawn(hit, audioSource);

		if (Decal.m_Prefabs == null)
			return;

		if (Decal.m_Prefabs.Count == 0)
			return;

		// we have decals!

	reroll:

		// pick a random decal from the list
		m_DecalToSpawn = Decal.m_Prefabs[Random.Range(0, Decal.m_Prefabs.Count)];

		if (m_DecalToSpawn == null)
			return;

		// if we have more than one possible decal, never spawn the same decal twice in a row
		if ((Decal.m_Prefabs.Count > 1) && (m_DecalToSpawn == m_LastSpawnedDecal))
				goto reroll;

		if (FootprintDirection == NO_DIRECTION)
		{
			// this is a bullet (or something else with a random spin direction)
			vp_DecalManager.Spawn(m_DecalToSpawn, hit, Decal.AllowEdgeOverlap, Random.Range(Decal.MinScale, Decal.MaxScale));
		}
		else
		{
			// this is a footprint, which means we must pass on a direction and flip
			// info (telling the decal manager which foot we have here)
			vp_DecalManager.SpawnFootprint(m_DecalToSpawn, hit, FootprintDirection, FootprintFlip, FootprintVerifyGroundContact, Random.Range(Decal.MinScale, Decal.MaxScale));
			FootprintDirection = NO_DIRECTION;	// set it back afterwards
		}

		m_LastSpawnedDecal = m_DecalToSpawn;	// remember this so we don't place the same decal twice in a row

	}


#if UNITY_EDITOR
	[vp_HelpBox("• This object is a recipe for a bunch of effects to be spawned in response to a certain type of collision.\n\n• It might trigger when a bullet hits a wall, or when a player makes a footstep, or crashes violently into the ground.\n\n• The vp_SurfaceManager in a scene is typically responsible for spawning vp_SurfaceEffects, and chooses which effect to spawn depending on what vp_SurfaceType and vp_ImpactEvent were involved in a collision.", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float surfaceEffectHelp;
#endif

}

