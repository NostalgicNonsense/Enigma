/////////////////////////////////////////////////////////////////////////////////
//
//	vp_DecalManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this class handles the fading out and removal of decals.
//					by default there can be 100 decals in the scene. as new
//					decals appear, older ones are weathered and eventually removed.
//					
//					the system works with the surface manager by default in a
//					static fashion, however it can optionally be added as an
//					instance to a scene, enabling more advanced features such
//					as detecting badly placed decals and fading them out in non-
//					intrusive and elegant ways. the system can be tweaked for
//					realism vs performance in a very flexible manner.
//
//					see the manual and / or help texts in the editor for more
//					detailed info on all the features.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

#if UNITY_5_4_OR_NEWER
using UnityEngine.SceneManagement;
#endif

public class vp_DecalManager : MonoBehaviour
{

	// --- decal limits ---

	// max amount of decals
	[SerializeField]
	protected float m_DecalLimit = 100;
	public float DecalLimit
	{
		get
		{
			return m_DecalLimit;
		}
		set
		{
			m_DecalLimit = value;
			Refresh();
		}
	}

	// share of the total amount of decals that should be considered 'aging' and very slowly and incrementally fade out
	[SerializeField]
	protected float m_WeatheredDecals = 20;
	public float WeatheredLimit
	{
		get { return m_WeatheredDecals; }
		set
		{
			if (value > m_DecalLimit)
			{
				value = m_DecalLimit;
				//Debug.LogError("WeatheredDecals can't be larger than MaxDecals");
				return;
			}
			m_WeatheredDecals = value;
			Refresh();
		}
	}

	// --- placement tests ---

	// single-corner-test cleanup over time
	public bool CleanupOverTime = true;			// should the scene be slowly and gradually cleaned of badly placed decals?
	public float VertexRaycastInterval = 0.5f;	// how often should a new decal corner somewhere in the scene be tested for surface contact?
	public int DecalsPerCleanupBatch = 4;		// this many vertices will be tested per gradual cleanup interval

	// instant quad-corner test
	public bool InstantQuadCornerTest = true;	// should there be 4 additional raycasts (one per corner) for each nearby decal to check for surface contact?
	public int QuadRaycastRange = 5;			// used to limit the distance of quad raycasts
	public int MaxQuadRaycastsPerSecond = 3;	// used to spread out quad raycasts over time
	
	// stretched decals
	public bool AllowStretchedDecals = true;	// should decals be allowed to attach to non-static, non-uniformly scaled objects?

	// --- removal on fail ---

	public float RemoveDelay = 10.0f;					// for how long after a removal decision should an (onscreen) decal linger before fading out?
	public float RemoveFadeoutSpeed = 10;				// how fast should decals that have been flagged for removal fade out
	public bool AllowInstaRemoveIfOffscreen = true;		// if true, as soon as the player looks away, any decals flagged for removal will disappear instantly instead of being faded out

	private const int INSTANT_INVISIBLE_SPEED = 25;		// if fadeout speed is set to this value, fadeout will snap to instant
	private const float FOV_PADDING = 0.9f;				// proportion of the field of view inside which decals are considered visible
	private const float ROUGH_REMOVE_INTERVAL = 0.25f;	// any decals that have been flagged for removal will be removed at a random interval between 'value' and 'value * 2'

	// --- internals ---

	public static vp_DecalManager Instance = null;
	protected static bool m_HaveInstance = false;
	protected bool m_WasAutoAdded = false;				// true if this instance was created by the surface manager, false if not

	protected static float m_NonFadedDecals = 0.0f;
	protected static float m_FadeAmount = 0.0f;
	protected static bool m_Inited = false;
	protected static float m_QuadTestRate = 0;			// keeps track of how rapidly we are currently performing quad raycasts

	protected static float m_NextAllowedVertexTestTime = 0.0f;
	protected static float m_NextAllowedRemoveTime = 0.0f;
	protected static float m_NextAllowedQuadTestTime = 0.0f;

	protected static Dictionary<GameObject, Renderer> m_DecalRenderers = new Dictionary<GameObject, Renderer>();
	protected static Dictionary<GameObject, Mesh> m_DecalMeshFilters = new Dictionary<GameObject, Mesh>();
	public static List<DecalToCheckLater>[] m_Queue = new List<DecalToCheckLater>[4];	// one list of decals per possible quad corner (NOTE: assuming quad decals throughout!)
	public static List<DecalToCheckLater> m_DecalsToRemoveWhenOffscreen = new List<DecalToCheckLater>();
	public static List<Renderer> m_RenderersToQuickFade = new List<Renderer>();

	protected static Renderer m_TargetRenderer;
	protected static Mesh m_TargetMesh;

	// below is a simple lookup table for prioritizing the checking of decals with a higher
	// probability of being incorrectly placed. the way this works is:
	//  - every decal has four vertices: 0, 1, 2, and 3  (NOTE: assuming classic quad decals!).
	//	- the 'UpdateGradualCleanup' method maintains 4 queues (lists) of decals, one per
	//		possible quad corner. the first list has all decals with no vertices checked.
	//		the second list has all the decals with a checked 'vertex 1', the third and
	//		fourth lists have the decals with 'vertex 2' and '3' checked, respectively.
	//	- every 'VertexRaycastInterval' (default: 0.5 secs) four decals in the scene
	//		will have one vertex (corner) each checked for surface contact. if any corner
	//		fails, that decal will be flagged for removal. if the corner is OK, its decal
	//		will be moved to the next list, in chronological order.
	//	- which decals to be checked are determined by the below lookup table, iterated
	//		through using 'm_Index'. on every interval, one batch of FOUR (default) decals
	//		are chosen, so if the iteration is just starting out, the algorithm will pick 4
	//		decals from list 0. on the next iteration it will do the same, on the third
	//		iteration it will pick four decals with their vertex 2 checked, and on the fourth
	//		iteration it will pick two with vertex 2 checked and 2 with vertex 1 checked.
	//		decals with three checked corners are very unlikely to be badly placed, so only
	//		two of them are checked in one full cycle, and not until the last batch.
	//	- the resulting effect is that decals that are more likely to be badly placed are
	//		tested first, and the unlikely ones are checked last.
	//	- to see the system in action: enable the debug visualization on a vp_DecalManager
	//		component, fire a long burst of machinegun bullets across a wall, and see debug
	//		balls appear as the system scans the decals.
	static int[] m_List = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 3, 3 };
	static int m_Index = 0;

	// list of all decals in the scene
	protected static List<GameObject> m_Decals = new List<GameObject>();
	public List<GameObject> Decals
	{
		get
		{
			return m_Decals;
		}
	}

	// this struct defines a decal that will be remembered for later surface contact checking and removal
	public struct DecalToCheckLater
	{
		public DecalToCheckLater(GameObject decal, float cornerOverlap, float timeOfBirth)
		{
			Decal = decal;
			CornerOverlap = cornerOverlap;
			TimeOfBirth = timeOfBirth;
		}
		public GameObject Decal;
		public float CornerOverlap;
		public float TimeOfBirth;
	}

	// --- editor ---

	protected bool m_DebugMode = false;
	public bool DebugMode
	{
		get
		{
			return m_DebugMode;
		}
		set
		{
			m_DebugMode = value;
		}
	}
	
	public bool m_PlacementFoldout;
	public bool m_RemovalFoldout;
	public bool m_LimitsFoldout;
	public bool m_DebugFoldout;
	public bool m_ShowHelp = true;
	

	/// <summary>
	/// 
	/// </summary>
	void Awake()
	{

		// create the vertex test queue lists
		m_Queue[0] = new List<DecalToCheckLater>();
		m_Queue[1] = new List<DecalToCheckLater>();
		m_Queue[2] = new List<DecalToCheckLater>();
		m_Queue[3] = new List<DecalToCheckLater>();

		if (Instance != null)
		{
			if (!Instance.m_WasAutoAdded)
				Debug.LogWarning("Warning (" + this + ") There can only be one vp_DecalManager in the scene! (destroying self)");
			enabled = false;
			Destroy(this);
			return;
		}

		Instance = this;
		m_HaveInstance = true;

	}


	/// <summary>
	/// 
	/// </summary>
	private void OnEnable()
	{
#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded += OnLevelLoad;
#endif
	}


	/// <summary>
	/// 
	/// </summary>
	private void OnDisable()
	{
#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded -= OnLevelLoad;
#endif
	}


	/// <summary>
	/// 
	/// </summary>
	void Update()
	{

		// since this monobehaviour seems to be alive, make it the instance in case Instance
		// was cleared (e.g. by recent level loading)
		if (Instance == null)
		{
			Instance = this;
			m_HaveInstance = true;
		}

		UpdateQuadTest();

		UpdateVertexTest();

		UpdateRemoval();

		UpdateQuickFading();

	}


	/// <summary>
	/// effectively caps the amount of quad tests 
	/// </summary>
	void UpdateQuadTest()
	{

		if (Time.time < m_NextAllowedQuadTestTime)
			return;

		m_NextAllowedQuadTestTime = Time.time + 1.0f;
		m_QuadTestRate = 0;

	}


	/// <summary>
	/// this is called by the surface manager to auto-create a decal manager
	/// in case there is none in the scene
	/// </summary>
	public static void AutoAddTo(GameObject target)
	{

		if (Instance != null)
		{
			Debug.LogError("Error (vp_DecalManager) Can't auto-add because there is already a vp_DecalManager in the scene.");
			return;
		}

		vp_DecalManager instance = target.AddComponent<vp_DecalManager>();
		Instance = instance;
		instance.m_WasAutoAdded = true;
		m_HaveInstance = true;

	}



	/// <summary>
	/// checks a batch of untested decal corners somewhere in the scene
	/// for surface contact and waits until the next allowed time to check
	/// the next four. NOTE: this is not the quad test! the four vertices
	/// may belong to different decals, depending on current priority
	/// </summary>
	protected void UpdateVertexTest()
	{

		if(!CleanupOverTime)
			return;

		if (Time.time < m_NextAllowedVertexTestTime)
			return;

		if (m_Queue[0].Count == 0
			&& m_Queue[1].Count == 0
			&& m_Queue[2].Count == 0
			&& m_Queue[3].Count == 0)
			return;	// no more queued checks

		// check one batch of four vertices
		for (int v = 0; v < DecalsPerCleanupBatch; v++)
		{
			DoVertexTest(m_List[m_Index], (RemoveFadeoutSpeed == INSTANT_INVISIBLE_SPEED));

			Step();	// iterate list pointer
		retry:
			if (m_Queue[m_List[m_Index]].Count == 0)
			{
				Step();	// iterate list pointer
				if (!(
			m_Queue[0].Count == 0
			&& m_Queue[1].Count == 0
			&& m_Queue[2].Count == 0
			&& m_Queue[3].Count == 0
			))
					goto retry;
			}

		}

		m_NextAllowedVertexTestTime = Time.time + VertexRaycastInterval;

	}


	/// <summary>
	/// increments the pointer for the decal corner list
	/// </summary>
	protected void Step()
	{

		m_Index++;
		if (m_Index > (m_List.Length-1))
			m_Index = 0;

	}


	/// <summary>
	/// checks a single vertex in a decal for surface contact
	/// </summary>
	protected static bool DoVertexTest(int list, bool instaRemoveOnFail)
	{

		if (m_Queue[list].Count == 0)
			return false;

		GameObject decal = m_Queue[list][0].Decal;
		float cornerOverlap = m_Queue[list][0].CornerOverlap;

		if (decal == null)
		{
			m_Queue[list].RemoveAt(0);
			return false;
		}

		m_DecalMeshFilters.TryGetValue(decal, out m_TargetMesh);
		if (m_TargetMesh == null)
			return false;

#if UNITY_EDITOR
		if (m_HaveInstance && Instance.m_DebugMode)
			SpawnDebugPoint(decal.transform.TransformPoint(m_TargetMesh.vertices[list] * (1.0f - (cornerOverlap * 2.0f))), decal.transform);	// DEBUG
#endif
			RaycastHit hit;
			if (!Physics.Raycast(
				new Ray(decal.transform.TransformPoint(m_TargetMesh.vertices[list] * (1.0f - (cornerOverlap * 2.0f))) + (decal.transform.forward * 0.1f), -decal.transform.forward),
				out hit,
				0.2f,
				vp_Layer.Mask.ExternalBlockers)
				|| ((decal.transform.parent != null) && (hit.transform != decal.transform.parent))
				)
			{
				// vertex was placed in thin air
				if (instaRemoveOnFail)
					vp_Utility.Destroy(decal);
				else
					m_DecalsToRemoveWhenOffscreen.Add(m_Queue[list][0]);
				m_Queue[list].RemoveAt(0);
			}
			else
			{
				// vertex was placed on a solid surface
				if (list == 3)
					m_Queue[list].RemoveAt(0);
				else
				{
					m_Queue[list + 1].Add(m_Queue[list][0]);
					m_Queue[list].RemoveAt(0);
				}

			}

		return true;

	}


	/// <summary>
	/// determines if the placement of 'decal' is eligible for an instant
	/// quad-test, and if so: orders one and returns true. does nothing and
	/// returns false if there is no decalmanager in the scene, if quad
	/// tests are disabled, if too many quad tests have been done recently,
	/// or if the decal is outside the quad test range
	/// </summary>
	protected static bool TryQuadTest(GameObject decal, float cornerOverlap)
	{
		
		if (!m_HaveInstance)
			return false;

		if(!Instance.InstantQuadCornerTest)
			return false;

		if(m_QuadTestRate >= Instance.MaxQuadRaycastsPerSecond)
			return false;

		// TODO: 'Camera.main' may or may not work with VR
		if(Vector3.Distance(Camera.main.transform.position, decal.transform.position) > Instance.QuadRaycastRange)
			return false;

		DoQuadTest(decal, cornerOverlap);

		return true;

	}


	/// <summary>
	/// instantly checks all four corners of a specific decal for surface
	/// contact. if any corner fails the decal will be instantly destroyed
	/// </summary>
	protected static void DoQuadTest(GameObject decal, float cornerOverlap)
	{

		if (decal == null)
			return;

		m_QuadTestRate++;

		m_DecalMeshFilters.TryGetValue(decal, out m_TargetMesh);
		if (m_TargetMesh == null)
			return;

		// check all the four vertices of a typical quad
		for (int v = 0; v < 4; v++)
		{

#if UNITY_EDITOR
			if (m_HaveInstance && (Instance.m_DebugMode))
				SpawnDebugPoint(decal.transform.TransformPoint(m_TargetMesh.vertices[v] * (1.0f - (cornerOverlap * 2.0f))), decal.transform);	// DEBUG
#endif

			RaycastHit hit;
			if (!Physics.Raycast(
				new Ray(decal.transform.TransformPoint(m_TargetMesh.vertices[v] * (1.0f - (cornerOverlap * 2.0f))) + (decal.transform.forward * 0.1f), -decal.transform.forward),
				out hit,
				0.2f,
				vp_Layer.Mask.ExternalBlockers)
				|| ((decal.transform.parent != null) && (hit.transform != decal.transform.parent))
				)
			{
				// at least one of the vertices was placed in thin air
				vp_Utility.Destroy(decal);
				return;
			}

		}

	}


	/// <summary>
	/// returns true if the decal is outside the main camera's field of view,
	/// false if not
	/// </summary>
	static bool IsOffScreen(GameObject decal)
	{

		if (decal == null)
			return true;

		// TODO: 'Camera.main' may or may not work for VR
		return Mathf.Abs(vp_3DUtility.LookAtAngle(Camera.main.transform.position, Camera.main.transform.forward, decal.transform.position)) > (Camera.main.fieldOfView * 0.9f);

	}




	/// <summary>
	/// removes any decals that have been flagged for removal. offscreen
	/// decals will typically be instantly removed. onscreen ones will
	/// typically be faded out
	/// </summary>
	static void UpdateRemoval()
	{
	
		if (Time.time < m_NextAllowedRemoveTime)
			return;

		m_NextAllowedRemoveTime = Time.time + (ROUGH_REMOVE_INTERVAL + (Random.value * ROUGH_REMOVE_INTERVAL));

		for (int v = m_DecalsToRemoveWhenOffscreen.Count - 1; v > -1; v--)
		{
			// if decal is flagged for removal - and offscreen: insta-remove it (if allowed)
			if ((m_HaveInstance && Instance.AllowInstaRemoveIfOffscreen) && IsOffScreen(m_DecalsToRemoveWhenOffscreen[v].Decal))
			{
				vp_Utility.Destroy(m_DecalsToRemoveWhenOffscreen[v].Decal);
				m_DecalsToRemoveWhenOffscreen.RemoveAt(v);
			}
				// if decal is not offscreen, but beyond the quad test distance OR really old, fade it instead
				// TODO: 'Camera.main' may or may not work for VR
			else if ((Vector3.Distance(Camera.main.transform.position, m_DecalsToRemoveWhenOffscreen[v].Decal.transform.position) > (m_HaveInstance ? Instance.QuadRaycastRange : 5))
				||
				m_DecalsToRemoveWhenOffscreen[v].TimeOfBirth < (Time.time - (m_HaveInstance ? Instance.RemoveDelay : 10)))
			{
				SetTargetRenderer(m_DecalsToRemoveWhenOffscreen[v].Decal);
				if (m_TargetRenderer != null)
				{
					m_RenderersToQuickFade.Add(m_TargetRenderer);
					m_DecalsToRemoveWhenOffscreen.RemoveAt(v);
				}
			}

		}

	}


	/// <summary>
	/// iterates the fading of any decals that should be quickly faded out
	/// </summary>
	static void UpdateQuickFading()
	{

		for (int v = m_RenderersToQuickFade.Count - 1; v > -1; v--)
		{
			if ((m_RenderersToQuickFade[v] == null))
			{
				m_RenderersToQuickFade.Remove(m_RenderersToQuickFade[v]);
				continue;
			}
			if (m_RenderersToQuickFade[v].material.color.a < 0.01f)
			{
				vp_Utility.Destroy(m_RenderersToQuickFade[v].gameObject);
				m_RenderersToQuickFade.Remove(m_RenderersToQuickFade[v]);
				continue;
			}
			Color c = m_RenderersToQuickFade[v].material.color;
			c.a = Mathf.Lerp(c.a, 0.0f, Time.deltaTime * (m_HaveInstance ? Instance.RemoveFadeoutSpeed : 10));
			m_RenderersToQuickFade[v].material.color = c;
		}


	}


	/// <summary>
	/// creates a decal from the passed gameobject at the raycast hit point,
	/// and childs it to the target object (if necessary and allowed)
	/// </summary>
	static GameObject InstantiateDecal(GameObject original, RaycastHit hit)
	{

		GameObject decal = vp_Utility.Instantiate(original, hit.point, Quaternion.Euler(hit.normal)) as GameObject;
		if (decal == null)
			return null;

		decal.layer = vp_Layer.Debris;	// set layer to debris, for damage handler to be able to remove decal before respawning an object

		// parent decal to the hit transform - but only if the target transform scale allows it!
		if (
			(m_HaveInstance
			&& Instance.AllowStretchedDecals						// if non-uniform scaling is allowed (object scaling is irrelevant) ...
			&& (!hit.transform.gameObject.isStatic))				// ... and the target object is not static
			|| vp_MathUtility.IsUniform(hit.transform.localScale, 0.00001f)	// OR if non-uniform scaling is prohibited, but this object is essentially uniformly scaled
			)
			decal.transform.parent = hit.transform;		// we're good! add the decal as a child to the hit object

		vp_DecalManager.Add(decal);

		return decal;

	}

	
	/// <summary>
	/// spawns an instance of 'prefab' at the 'hit' point. 'cornerOverlap'
	/// determines how close to a wall corner the decal is allowed to sit
	/// </summary>
	public static void Spawn(GameObject prefab, RaycastHit hit, float cornerOverlap, float scale)
	{

		Spawn(prefab, hit, 0, 360, cornerOverlap, scale);

	}


	/// <summary>
	/// spawns an instance of 'prefab' at the 'hit' point and 'angle'.
	/// 'cornerOverlap' determines how close to a wall corner the decal is
	/// allowed to sit
	/// </summary>
	public static void Spawn(GameObject prefab, RaycastHit hit, float angle, float cornerOverlap, float scale)
	{
		Spawn(prefab, hit, angle, angle, cornerOverlap, scale);
	}
	

	/// <summary>
	/// spawns an instance of 'prefab' at the 'hit' point, at a random angle
	/// between 'minAngle' and 'maxAngle'. 'cornerOverlap' determines how
	/// close to a wall corner the decal is allowed to sit
	/// </summary>
	public static void Spawn(GameObject prefab, RaycastHit hit, float minAngle, float maxAngle, float cornerOverlap, float scale)
	{

		if (prefab == null)
			return;

		// abort if a non-static target object has (more than insignificantly)
		// uneven scale (or footprints will be distorted)
		if ((!vp_MathUtility.IsUniform(hit.transform.localScale, 0.00001f) && !hit.transform.gameObject.isStatic)
			&& (m_HaveInstance && !Instance.AllowStretchedDecals)
			)
		{
#if UNITY_EDITOR
			Debug.LogWarning("Warning (vp_DecalManager.Spawn) Can't add decal to non-uniformly scaled object '" + hit.transform + "'. Please give the object uniform scale (same scale on all axes) or make it static.");
#endif
			return;
		}

		GameObject decal = InstantiateDecal(prefab, hit);

		if (decal == null)
			return;

		// apply scale on X and Y axes
		decal.transform.localScale = Vector3.Scale(decal.transform.localScale, new Vector3(scale, scale, 1.0f));

		// face away from hit surface and apply random rotation
		decal.transform.rotation = Quaternion.LookRotation(hit.normal);
		decal.transform.Rotate(Vector3.forward, Random.Range(minAngle, maxAngle), Space.Self);

		if (cornerOverlap < 0.5f)						// if this decal should be tested for surface contact ...
		{
			if (!TryQuadTest(decal, cornerOverlap))		// ... try to do a quad test - but if it didn't succeed ...
				CheckDecalLater(decal, cornerOverlap);	// ... add it to the queue of decals to be checked later
		}


	}


	/// <summary>
	/// adds 'decal' to the cueue of decals to be slowly and gradually checked
	/// for surface contact by the decal manager over time
	/// </summary>
	protected static void CheckDecalLater(GameObject decal, float cornerOverlap)
	{

		if (m_Queue[0] == null)
			return;

		// adding a decal to queue #0 means that it will get its first vertex (0)
		// checked for surface contact when its time is up. if the test succeeds,
		// the decal will be moved to the next list for the next check and so on.
		// if any of four vertex tests fail, the decal will be flagged for removal
		m_Queue[0].Add(new DecalToCheckLater(decal, cornerOverlap, Time.time));

	}



	/// <summary>
	/// spawns an instance of the footprint 'prefab' at the raycast 'hit' point.
	/// 'footprintDirection' should be the forward vector of the foot that placed
	/// the footprint. if 'flip' is true, local X-scale will be inverted.
	/// </summary>
	public static void SpawnFootprint(GameObject prefab, RaycastHit hit, Vector3 footprintDirection, bool flip, bool verifyGroundContact, float scale)
	{

		if (prefab == null)
			return;
		
		GameObject decal = InstantiateDecal(prefab, hit);

		if (decal == null)
			return;

		// apply scale on X and Y axes
		decal.transform.localScale = Vector3.Scale(decal.transform.localScale, new Vector3(scale, scale, 1.0f));

		// apply direction
		decal.transform.LookAt(
			(decal.transform.position - footprintDirection) - (decal.transform.up * Vector3.Dot(hit.normal, -footprintDirection)),
			hit.normal);
		decal.transform.Rotate(Vector3.left * 90);
		if (flip)
			decal.transform.localScale = Vector3.Scale(decal.transform.localScale, Vector3.left + Vector3.up + Vector3.forward);

		// WARNING: in 99% of all cases 'verifyGroundContact' should be off,
		// or the footprint feature will likely spam the quad-test buffer
		if (verifyGroundContact)				// if this decal should be tested for ground contact ...
		{
			if (!TryQuadTest(decal, 0.0f))		// ... try to do a quad test (with zero allowed overlap) ...
				CheckDecalLater(decal, 0.0f);	// ... and on fail: add it to the queue of decals to be checked later
		}

	}


	/// <summary>
	/// adds a gameobject to the decal manager, making it subject to later
	/// removal and deletion. NOTE: all items added to the decal manager
	/// should have a material with initial alpha set to 1.0
	/// </summary>
	public static void Add(GameObject decal)
	{

		if (!m_Inited)
		{
			m_Inited = true;
			Refresh();
		}

		if (decal == null)
			return;

		if(m_Decals.Contains(decal))
			m_Decals.Remove(decal);

		if (!CacheMeshAndRenderer(decal))
			return;

		m_Decals.Add(decal);

		WeatherAndRemove();

	}

	
	/// <summary>
	/// caches the renderer and mesh of 'decal' (if any) in a dictionary.
	/// also, verifies that there is a material, and resets the alpha of
	/// the material to 1.0. returns false if there is no renderer with a
	/// material, or no mesh filter with a mesh
	/// </summary>
	protected static bool CacheMeshAndRenderer(GameObject decal)
	{

		MeshFilter mf = decal.GetComponent<MeshFilter>();
		if (mf == null)
			return false;
		if (mf.mesh == null)
			return false;
		m_TargetMesh = mf.mesh;

		m_TargetRenderer = decal.GetComponent<Renderer>();
		if (m_TargetRenderer == null)
			return false;
		if (m_TargetRenderer.material == null)
			return false;

		Color col = m_TargetRenderer.material.color;
		col.a = 1;
		m_TargetRenderer.material.color = col;

		if (!m_DecalRenderers.ContainsKey(decal))
			m_DecalRenderers.Add(decal, m_TargetRenderer);

		if (!m_DecalMeshFilters.ContainsKey(decal))
			m_DecalMeshFilters.Add(decal, m_TargetMesh);

		return true;

	}


	/// <summary>
	/// fetches the renderer of 'decal' from the renderer cache, and
	/// sets 'm_TargetRenderer' to the returned value
	/// </summary>
	private static void SetTargetRenderer(GameObject decal)
	{

		m_DecalRenderers.TryGetValue(decal, out m_TargetRenderer);

	}


	/// <summary>
	/// this method ages a set number of the oldest decals, and destroys
	/// decals that are no longer needed
	/// </summary>
	private static void WeatherAndRemove()
	{

		if (m_Decals.Count > m_NonFadedDecals)
		{

			// loop a predetermined amount of the oldest decals
			for (int v = 0; v < (m_Decals.Count - m_NonFadedDecals); v++)
			{
				// and fade them out a tiny bit. older decals will
				// accumulate more and more transparency over time
				if (m_Decals[v] != null)
				{
					SetTargetRenderer(m_Decals[v]);
					if (m_TargetRenderer != null)
					{
						Color col = m_TargetRenderer.material.color;
						col.a -= m_FadeAmount;
						m_TargetRenderer.material.color = col;
					}
				}
			}

		}

		// kill the oldest decal as it becomes fully transparent
		if (m_Decals[0] != null)
		{
			SetTargetRenderer(m_Decals[0]);
			if (m_TargetRenderer != null)
			{
				if (m_TargetRenderer.material.color.a <= 0.0f)
				{
					vp_Utility.Destroy(m_Decals[0]);
					m_Decals.Remove(m_Decals[0]);
				}
			}
		}
		else
			m_Decals.RemoveAt(0);

	}


	/// <summary>
	/// calculates the variables used internally from the ones
	/// exposed through properties
	/// </summary>
	protected static void Refresh()
	{

		if (!m_HaveInstance)
		{
			m_FadeAmount = 0.05f;
			m_NonFadedDecals = 80;
			return;
		}

		if (Instance.m_DecalLimit < Instance.m_WeatheredDecals)
			Instance.m_DecalLimit = Instance.m_WeatheredDecals;
		m_FadeAmount = (Instance.m_DecalLimit / Instance.m_WeatheredDecals) / Instance.m_DecalLimit;
		m_NonFadedDecals = Instance.m_DecalLimit - Instance.m_WeatheredDecals;

	}


	/// <summary>
	/// spawns a transparent debug ball at 'pos', childed to 'parent'.
	/// sets the color to blue, green, yellow or red depending on the
	/// current state of the internal vertex list pointer
	/// </summary>
	protected static void SpawnDebugPoint(Vector3 pos, Transform parent)
	{
		GameObject g = vp_3DUtility.DebugBall();
		g.transform.localScale = Vector3.one * 0.05f;
		g.transform.position = pos;

		Color c = Color.white;
		if (m_List[m_Index] == 0)
			c = Color.blue;
		if (m_List[m_Index] == 1)
			c = Color.green;
		if (m_List[m_Index] == 2)
			c = Color.yellow;
		if (m_List[m_Index] == 3)
			c = Color.red;
		
		g.GetComponent<Renderer>().material.color = new Color(c.r, c.g, c.b, 0.5f);

	}


	/// <summary>
	/// resets the system when a new level is loaded
	/// </summary>
#if UNITY_5_4_OR_NEWER
	protected virtual void OnLevelLoad(Scene scene, LoadSceneMode mode)
#else
	protected virtual void OnLevelWasLoaded()
#endif
	{

		m_RenderersToQuickFade.Clear();
		m_DecalsToRemoveWhenOffscreen.Clear();
		m_DecalRenderers.Clear();
		m_DecalMeshFilters.Clear();

		m_Queue[0].Clear();
		m_Queue[1].Clear();
		m_Queue[2].Clear();
		m_Queue[3].Clear();

		Instance = null;
		m_HaveInstance = false;

		m_Inited = false;

	}

}

