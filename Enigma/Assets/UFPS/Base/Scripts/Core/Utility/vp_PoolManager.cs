/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PoolManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this class manages the pooling of objects. pooling will always occur
//					if this script is placed on a game object in the scene and enabled.
//
//					NOTES:
//
//						1) when a pool manager instance is found, 'vp_Utility.Instantiate' and
//							'vp_Utility.Destroy' will forward execution to 'vp_PoolManager.Spawn'
//							and 'vp_PoolManager.Despawn, respectively.
//
//						2) when a pool manager instance is _not_ present, Unity's regular
//							'Object.Instantiate' and 'Object.Destroy' will be used.
//
//						3) IMPORTANT: please bear in mind that objects must be re-initialized in
//							'OnEnable' instead of 'Awake / Start' when using pooling. failing to meet
//							this requirement will often result in strange and hard to find bugs, such
//							as every other grenade not exploding. for this reason, when you run into
//							strange bugs, always remember to test with the pool manager deactivated.
//							if it works all of the sudden, there's a big chance you need to move
//							your monobehaviour's initialization from 'Awake'/'Start' to 'OnEnable'.
//
//					TIP: this pool manager is effective but simple. you should be able
//					to hack 'vp_Utility.Instantiate' and 'vp_Utility.Destroy' to hook up
//					a third party pool manager.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class vp_PoolManager : MonoBehaviour
{

	/// <summary>
	/// a class that holds a few properties necessary for custom pooled objects
	/// </summary>
	[System.Serializable]
	public class vp_PreloadedPrefab
	{
	
		public GameObject Prefab = null;	// prefab to check for pooling
		public int Amount = 15;				// amount of objects to instantiate at start
	
	}

	public List<vp_PreloadedPrefab> PreloadedPrefabs = new List<vp_PreloadedPrefab>();
#if UNITY_EDITOR
	[vp_HelpBox("Add PRELOADED PREFABS to pre-instantiate objects on startup. This can be good for memory allocation and for getting any asset loading out of the way early to reduce hiccups during gameplay.", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float h1;
#endif

	/// <summary>
	/// prefabs in this list will not be pooled. IMPORTANT: you can't add to
	/// this list at runtime. instead, use the method 'AddIgnoredPrefab'
	/// </summary>
	public List<GameObject> IgnoredPrefabs = new List<GameObject>();
#if UNITY_EDITOR
	[vp_HelpBox("• Add IGNORED PREFABS to prevent certain objects from being pooled by this script. When destroyed, they will be destroyed completely and turned over to the garbage collector.\n\n• IMPORTANT: You can't add to this list at runtime! Instead, use the script method 'AddIgnoredPrefab'", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Separator)]
	public float h2;
#endif
	
	protected Dictionary<GameObject, bool> m_IgnoredPrefabsInternal = new Dictionary<GameObject, bool>();	// runtime dictionary for speed

	protected Transform m_Transform; // the transform of this object. Used for parenting
	protected Dictionary<string, List<GameObject>> m_AvailableObjects = new Dictionary<string, List<GameObject>>();	// the pooled objects currently available.
	protected Dictionary<string, int> m_UniquePrefabNames = new Dictionary<string, int>();	// used to distinquish between different prefabs with identical names

	protected static vp_PoolManager m_Instance = null;
	public static vp_PoolManager Instance{ get{ return m_Instance; } }
    
    
    /// <summary>
    /// 
    /// </summary>
    protected virtual void Awake()
    {
    
    	m_Instance = this;
    	m_Transform = transform;
    
    }
    

    /// <summary>
    /// 
    /// </summary>
    protected virtual void Start ()
    {

		// move the ignore list into a dictionary for faster lookup
		foreach (GameObject o in IgnoredPrefabs)
		{
			if (o == null)
				continue;
			if (!m_IgnoredPrefabsInternal.ContainsKey(o))
			{
				m_IgnoredPrefabsInternal.Add(o, false);
			}
		}

		// add any object instances that should be preloaded to the pool
		foreach (vp_PreloadedPrefab obj in PreloadedPrefabs)
		{
			if (obj == null)
				continue;
			if (obj.Prefab == null)
				continue;
			if (obj.Amount < 1)
				continue;
			AddToPool(obj.Prefab, Vector3.zero, Quaternion.identity, obj.Amount);
		}

    }
    
    
    /// <summary>
    /// adds one or more deactivated instances of a prefab to the pool
    /// </summary>
    public virtual void AddToPool( GameObject prefab, Vector3 position, Quaternion rotation, int amount = 1 )
    {
    
    	if(prefab == null)
    		return;

		// never add ignored prefabs
		if (m_IgnoredPrefabsInternal.ContainsKey(prefab))
		{
			return;
		}

		string uniqueName = GetUniqueNameOf(prefab);

		// add this prefab type to the available objects dictionary under a unique name
		if (!m_AvailableObjects.ContainsKey(uniqueName))
			m_AvailableObjects.Add(uniqueName, new List<GameObject>());
		
		// create amount of objects
		for(int i=0; i<amount; i++)
		{
			GameObject newObj = GameObject.Instantiate(prefab, position, rotation) as GameObject;
			newObj.name = uniqueName;
			newObj.transform.parent = m_Transform;
			vp_Utility.Activate(newObj, false);
			m_AvailableObjects[uniqueName].Add(newObj);
		}
    
    }


	/// <summary>
	/// spawns an instance of 'prefab' in the scene. if the prefab is not on the
	/// ignore list, it will be subject to pooling. if there are pooled (disabled)
	/// objects of the same type available, one of them will be recycled. if not,
	/// a new instance will be created
	/// </summary>
	public static GameObject Spawn(GameObject original, Vector3 position, Quaternion rotation)
	{

		if (Instance == null)
		{
			return null;
		}
		return Instance.SpawnInternal(original, position, rotation);
	}



    /// <summary>
    /// tries to look for an object already in the pool and returns it if found.
	/// if not found, an new object will be instantiated and added to the pool
    /// </summary>
	public virtual GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
    {

		if (prefab == null)
			return null;


		// if this prefab is in the ignore list, abort and instantiate a new object
		if (m_IgnoredPrefabsInternal.ContainsKey(prefab))
		{
			//Debug.Log("will instantiate " + prefab + " @ " + Time.time);
			return GameObject.Instantiate(prefab, position, rotation) as GameObject;
		}
		//Debug.Log("will try to spawn " + prefab + " @ " + Time.time);

		GameObject go = null;
    	List<GameObject> availableObjects = null;

		string uniqueName = GetUniqueNameOf(prefab);

    	// check if we have objects like 'prefab' in the pool
		if (m_AvailableObjects.TryGetValue(uniqueName, out availableObjects))
    	{

		Retry:

			// get the first available pooled object of same type as 'original'
			if(availableObjects.Count < 1)
				goto SpawnNew;

			go = availableObjects[0];

			// check if the object still exists
			if(go == null)
			{
				availableObjects.Remove(go);
				goto Retry;
			}
			
			// set the position and rotation
			go.transform.position = position;
			go.transform.rotation = rotation;
			
			// remove the object from the 'available' list
			availableObjects.Remove(go);

			// activate the object
			vp_Utility.Activate(go);
			for (int i = 0; i < go.transform.childCount; i++)
			{
				Transform child = go.transform.GetChild(i);
				if (child != null)
					vp_Utility.Activate(child.gameObject);
			}

			if (go.transform.parent == m_Transform)
				go.transform.parent = null;

			// return the object
			return go;

    	}

	SpawnNew:

    	// add a new object if this type of object isn't being pooled
    	AddToPool(prefab, position, rotation);
		
		// return the new object by calling this method again
		return SpawnInternal(prefab, position, rotation);
    
    }


	/// <summary>
	/// despawns (disables) an instance of a prefab in the scene after a time delay.
	/// if an object of the same type of prefab is spawned later, this instance will
	/// be recycled (reactivated and relocated)
	/// </summary>
	public static void Despawn(GameObject obj, float delay)
	{

		if (Instance == null)
			return;

		if (delay > 0)
		{
			vp_Timer.In(delay, () => { Instance.DespawnInternal(obj); });
		}
		else
		{
			Instance.DespawnInternal(obj);
		}

	}


	/// <summary>
	/// instantly despawns (disables) an instance of a prefab in the scene.
	/// if an object of the same type of prefab is spawned later, this instance will
	/// be recycled (reactivated and relocated)
	/// </summary>
	public static void Despawn(GameObject obj)
	{
		Instance.DespawnInternal(obj);
	}


	/// <summary>
    /// puts the object back into the pool if it's being pooled or destroys it if not
    /// </summary>
	protected virtual void DespawnInternal(GameObject obj)
    {

		// NOTE: we don't fetch object instance ids or use 'GetUniqueNameOf'in
		// this method, since we deal with scene instances with different ids
		// from the original (project view) prefabs

		if (obj == null)
    		return;

		// if this prefab is in the ignore list, abort and destroy it conventionally
		if (m_IgnoredPrefabsInternal.ContainsKey(obj))
		{
			GameObject.Destroy(obj);
			return;
		}

		List<GameObject> availableObjects = null;
		string objName = obj.name;
		bool isChild = false;
		Retry:
		m_AvailableObjects.TryGetValue(objName, out availableObjects);

		if (availableObjects == null)
		{
			// this particular object was not pooled, but it might be childed to a pooled object
			if (obj.transform.parent != null)
			{
				isChild = true;
				objName = obj.transform.parent.name;
				goto Retry;	// see if the parent was pooled
			}

			// if we end up here, neither the object nor any of its ancestors were pooled: destroy it for real
			UnityEngine.Object.Destroy(obj);
			return;
		}

		// deactivate the object
		vp_Utility.Activate(obj, false);

		// if we return here, the object was a child to a pooled object and will be
		// returned to the pool manager if / when the parent is
		if (isChild)
			return;

    	// if the object has no parent, child it to the pool manager to keep the scene tidy.
		// otherwise leave it as-is, to avoid messing with hierarchies (it's still managed
		// by the pool manager)
		if (obj.transform.parent == null)
			obj.transform.parent = m_Transform;

		// make the disabled object available for recycling
		availableObjects.Add(obj);
    
    }


	/// <summary>
	/// adds an ignored prefab at runtime. IMPORTANT: at runtime you must
	/// use this method instead of adding to the 'IgnoredPrefabs' list
	/// directly, or the prefab will not be cached and won't be ignored
	/// </summary>
	public void AddIgnoredPrefab(GameObject prefab)
	{

		if (!vp_PoolManager.Instance.IgnoredPrefabs.Contains(prefab))
			IgnoredPrefabs.Add(prefab);

		if(!vp_PoolManager.Instance.m_IgnoredPrefabsInternal.ContainsKey(prefab))
			m_IgnoredPrefabsInternal.Add(prefab, false);

	}


	/// <summary>
	/// used to distinquish between different prefabs with identical names. without
	/// this, the pool manager would have no way of knowing what prefab to spawn in
	/// cases where a project contains prefabs with identical names. basically: when
	/// a prefab is instantiated with the same name as another (previously spawned)
	/// prefab, the new instance will have the unique instance id of the prefab added
	/// to the in-world gameobject name. from then on, all instances of that prefab
	/// will go by the modified name. for performance reasons it is recommended to
	/// make sure all prefabs have unique names
	/// </summary>
	protected string GetUniqueNameOf(GameObject prefab)
	{

		int id;
		if (m_UniquePrefabNames.TryGetValue(prefab.name, out id))
		{

			// we have a pooled prefab with the same name + id: return the name
			if (prefab.GetInstanceID() == id)
				return prefab.name;

			// we have a pooled prefab with the same name but a different id, so make
			// sure we store this prefab under a new unique name (the prefab name + id)
			string newName = string.Join(" ", new string[] { prefab.name, prefab.GetInstanceID().ToString() });
			if (!m_UniquePrefabNames.ContainsKey(newName))
				m_UniquePrefabNames.Add(newName, prefab.GetInstanceID());
			return newName;

		}

		// we don't have a previously spawned prefab with this name: store and return the name
		m_UniquePrefabNames.Add(prefab.name, prefab.GetInstanceID());

		return prefab.name;

	}

#if UNITY_EDITOR
	[vp_HelpBox("• This script manages the pooling (recycling) of objects for better memory performance and smoother framerate.\n\n• Pooling will only occur if this script is placed on a game object in the scene and enabled.\n\n• When a vp_PoolManager instance is found, 'vp_Utility.Instantiate' and 'vp_Utility.Destroy' will forward execution to 'vp_PoolManager.Spawn' and 'vp_PoolManager.Despawn, respectively.\n\n• Most objects will be childed to this gameobject when pooled. Exception: bullet decals will remain on target transforms and just deactivated until reused.\n\n• When a pool manager instance is _not_ present, Unity's regular 'Object.Instantiate' and 'Object.Destroy' will be used (feeding the garbage collector).\n\n• IMPORTANT: Please bear in mind that objects must be re-initialized in 'OnEnable' instead of 'Awake / Start' when using pooling. Failing to meet this requirement will often result in strange and hard to find bugs (such as every other grenade not exploding). For this reason, when you run into strange bugs, always remember to test with the pool manager DEACTIVATED. If it works all of the sudden, there's a big chance you need to move your MonoBehaviour's initialization from 'Awake'/'Start' to 'OnEnable'.", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float h3;
#endif

}