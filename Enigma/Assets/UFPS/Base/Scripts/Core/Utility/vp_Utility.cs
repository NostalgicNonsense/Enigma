/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Utility.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	miscellaneous utility functions
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

public static class vp_Utility
{


	/// <summary>
	/// Cleans non numerical values (NaN) from a float by
	/// retaining a previous property value. If 'prevValue' is
	/// omitted, the NaN will be replaced by '0.0f'.
	/// </summary>
	[Obsolete("Please use 'vp_MathUtility.NaNSafeFloat' instead.")]
	public static float NaNSafeFloat(float value, float prevValue = default(float))
	{

		return vp_MathUtility.NaNSafeFloat(value, prevValue);

	}


	/// <summary>
	/// Cleans non numerical values (NaN) from a Vector2 by
	/// retaining a previous property value. If 'prevVector' is
	/// omitted, the NaN will be replaced by '0.0f'.
	/// </summary>
	[Obsolete("Please use 'vp_MathUtility.NaNSafeVector2' instead.")]
	public static Vector2 NaNSafeVector2(Vector2 vector, Vector2 prevVector = default(Vector2))
	{

		return vp_MathUtility.NaNSafeVector2(vector, prevVector);

	}


	/// <summary>
	/// Cleans non numerical values (NaN) from a Vector3 by
	/// retaining a previous property value. If 'prevVector' is
	/// omitted, the NaN will be replaced by '0.0f'.
	/// </summary>
	[Obsolete("Please use 'vp_MathUtility.NaNSafeVector3' instead.")]
	public static Vector3 NaNSafeVector3(Vector3 vector, Vector3 prevVector = default(Vector3))
	{

		return vp_MathUtility.NaNSafeVector3(vector, prevVector);
		
	}


	/// <summary>
	/// Cleans non numerical values (NaN) from a Quaternion by
	/// retaining a previous property value. If 'prevQuaternion'
	/// is omitted, the NaN will be replaced by '0.0f'.
	/// </summary>
	[Obsolete("Please use 'vp_MathUtility.NaNSafeQuaternion' instead.")]
	public static Quaternion NaNSafeQuaternion(Quaternion quaternion, Quaternion prevQuaternion = default(Quaternion))
	{

		return vp_MathUtility.NaNSafeQuaternion(quaternion, prevQuaternion);

	}


	/// <summary>
	/// This can be used to snap individual super-small property
	/// values to zero, for avoiding some floating point issues.
	/// </summary>
	[Obsolete("Please use 'vp_MathUtility.SnapToZero' instead.")]
	public static Vector3 SnapToZero(Vector3 value, float epsilon = 0.0001f)
	{

		return vp_MathUtility.SnapToZero(value, epsilon);

	}


	/// <summary>
	/// This can be used to snap individual super-small property
	/// values to zero, for avoiding some floating point issues.
	/// </summary>
	[Obsolete("Please use 'vp_MathUtility.SnapToZero' instead.")]
	public static float SnapToZero(float value, float epsilon = 0.0001f)
	{

		return vp_MathUtility.SnapToZero(value, epsilon);

	}


	/// <summary>
	/// Reduces the number of decimals of a floating point number.
	/// This can be used to solve floating point imprecision cases.
	/// 'factor' determines the amount of decimals. Default is 1000
	/// which results in 3 decimals.
	/// </summary>
	[Obsolete("Please use 'vp_MathUtility.ReduceDecimals' instead.")]
	public static float ReduceDecimals(float value, float factor = 1000)
	{

		return vp_MathUtility.ReduceDecimals(value, factor);

	}

	
	/// <summary>
	/// Zeroes the y property of a Vector3, for some cases where you want to
	/// make 2D physics calculations.
	/// </summary>
	[Obsolete("Please use 'vp_3DUtility.HorizontalVector' instead.")]
	public static Vector3 HorizontalVector(Vector3 value)
	{

		return vp_3DUtility.HorizontalVector(value);

	}


	/// <summary>
	/// Performs a stack trace to see where things went wrong
	/// for error reporting.
	/// </summary>
	public static string GetErrorLocation(int level = 1, bool showOnlyLast = false)
	{

		StackTrace stackTrace = new StackTrace();
		string result = "";
		string declaringType = "";

		for (int v = stackTrace.FrameCount - 1; v > level; v--)
		{
			if (v < stackTrace.FrameCount - 1)
				result += " --> ";
			StackFrame stackFrame = stackTrace.GetFrame(v);
			if (stackFrame.GetMethod().DeclaringType.ToString() == declaringType)
				result = "";	// only report the last called method within every class
			declaringType = stackFrame.GetMethod().DeclaringType.ToString();
			result += declaringType + ":" + stackFrame.GetMethod().Name;
		}

		if (showOnlyLast)
		{
			try
			{
				result = result.Substring(result.LastIndexOf(" --> "));
				result = result.Replace(" --> ", "");
			}
			catch
			{
			}
		}

		return result;

	}


	/// <summary>
	/// Returns the 'syntax style' formatted version of a type name.
	/// for example: passing 'System.Single' will return 'float'.
	/// </summary>
	public static string GetTypeAlias(Type type)
	{

		string s = "";

		if (!m_TypeAliases.TryGetValue(type, out s))
			return type.ToString();

		return s;

	}


	/// <summary>
	/// Dictionary of type aliases for error messages.
	/// </summary>
	private static readonly Dictionary<Type, string> m_TypeAliases = new Dictionary<Type, string>()
	{

		{ typeof(void), "void" },
		{ typeof(byte), "byte" },
		{ typeof(sbyte), "sbyte" },
		{ typeof(short), "short" },
		{ typeof(ushort), "ushort" },
		{ typeof(int), "int" },
		{ typeof(uint), "uint" },
		{ typeof(long), "long" },
		{ typeof(ulong), "ulong" },
		{ typeof(float), "float" },
		{ typeof(double), "double" },
		{ typeof(decimal), "decimal" },
		{ typeof(object), "object" },
		{ typeof(bool), "bool" },
		{ typeof(char), "char" },
		{ typeof(string), "string" },
		{ typeof(UnityEngine.Vector2), "Vector2" },
		{ typeof(UnityEngine.Vector3), "Vector3" },
		{ typeof(UnityEngine.Vector4), "Vector4" }

	};


	/// <summary>
	/// Activates or deactivates a gameobject for any Unity version.
	/// </summary>
	public static void Activate(GameObject obj, bool activate = true)
	{

#if UNITY_3_5
		obj.SetActiveRecursively(activate);
#else
		obj.SetActive(activate);
#endif

	}


	/// <summary>
	/// Returns active status of a gameobject for any Unity version.
	/// </summary>
	public static bool IsActive(GameObject obj)
	{

#if UNITY_3_5
		return obj.active;
#else
		return obj.activeSelf;
#endif

	}


	/// <summary>
	/// shows or hides the mouse cursor in a way suitable for the
	/// current unity version
	/// </summary>
	public static bool LockCursor
	{

		// compile only for unity 5+
		#if (!(UNITY_4_6 || UNITY_4_5 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0 || UNITY_3_5))
		get
		{
			return ((Cursor.lockState == CursorLockMode.Locked) ? true : false);
		}
		set
		{
			// toggling cursor visible and invisible is currently buggy in the Unity 5
			// editor so we need to toggle brute force with custom arrow art
			#if UNITY_EDITOR
				Cursor.SetCursor((value ? InvisibleCursor : VisibleCursor), Vector2.zero, CursorMode.Auto);
				Cursor.visible = value ? InvisibleCursor : VisibleCursor;
			#else
				// running in a build so toggling visibility should work fine
				Cursor.visible = !value;
			#endif
			Cursor.lockState = (value ? CursorLockMode.Locked : CursorLockMode.None);
		}
#else
		// compile only for unity 4.6 and older
		get { return Screen.lockCursor; }
		set { Screen.lockCursor = value; }
#endif

	}


// compile only for unity 5+ editor
#if UNITY_EDITOR && (!(UNITY_4_6 || UNITY_4_5 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0 || UNITY_3_5))

	// properties for setting up cursor art in the Unity 5 editor
	// (see further comments in 'LockCursor')

	static Texture2D m_VisibleCursor = null;
	static Texture2D VisibleCursor
	{
		get
		{
			if (m_VisibleCursor == null)
				m_VisibleCursor = Resources.Load("Input/EditorCursorVisible") as Texture2D;
			return m_VisibleCursor;
		}
	}

	static Texture2D m_InvisibleCursor = null;
	static Texture2D InvisibleCursor
	{
		get
		{
			if (m_InvisibleCursor == null)
				m_InvisibleCursor = Resources.Load("Input/EditorCursorInvisible") as Texture2D;
			return m_InvisibleCursor;
		}
	}
#endif


	/// <summary>
	/// Randomizes the order of the objects in the specified list.
	/// </summary>
	public static void RandomizeList<T>(this List<T> list)
	{

		int size = list.Count;

		for (int i = 0; i < size; i++)
		{
			int indexToSwap = UnityEngine.Random.Range(i, size);
			T oldValue = list[i];
			list[i] = list[indexToSwap];
			list[indexToSwap] = oldValue;
		}

	}


	/// <summary>
	/// Returns a random object from a list.
	/// </summary>
	public static T RandomObject<T>(this List<T> list)
	{

		List<T> newList = new List<T>();
		newList.AddRange(list);
		newList.RandomizeList();
		return newList.FirstOrDefault();

	}
	

	/// <summary>
	/// Returns a list of the specified child components
	/// </summary>
	public static List<T> ChildComponentsToList<T>( this Transform t ) where T : Component
	{

		return t.GetComponentsInChildren<T>().ToList();

	}


	/// <summary>
	/// 
	/// </summary>
	public static bool IsDescendant(Transform descendant, Transform potentialAncestor)
	{

		if (descendant == null)
			return false;

		if (potentialAncestor == null)
			return false;

		if (descendant.parent == descendant)
			return false;

		if (descendant.parent == potentialAncestor)
			return true;

		return IsDescendant(descendant.parent, potentialAncestor);

	}



	/// <summary>
	/// if target is a transform, returns its parent. if not, returns its
	/// transform. will return null if:
	/// 1) target is null
	/// 2) target's transform is null (has somehow been deleted)
	/// 3) target transform's parent is null (we have hit the scene root)
	/// </summary>
	public static Component GetParent(Component target)
	{

		if (target == null)
			return null;

		if (target != target.transform)
			return target.transform;

		return target.transform.parent;

	}


	/// <summary>
	/// 
	/// </summary>
	public static Transform GetTransformByNameInChildren(Transform trans, string name, bool includeInactive = false, bool subString = false)
	{

		name = name.ToLower();

		foreach (Transform t in trans)
		{
			if(!subString)
			{
				if ((t.name.ToLower() == name) && ((includeInactive) || t.gameObject.activeInHierarchy))
				return t;
			}
			else
			{
				if ((t.name.ToLower().Contains(name)) && ((includeInactive) || t.gameObject.activeInHierarchy))
				return t;
			}

			Transform ct = GetTransformByNameInChildren(t, name, includeInactive, subString);
			if (ct != null)
				return ct;
		}

		return null;

	}


	/// <summary>
	/// 
	/// </summary>
	public static Transform GetTransformByNameInAncestors(Transform trans, string name, bool includeInactive = false, bool subString = false)
	{

		if (trans.parent == null)
			return null;

		name = name.ToLower();

		if(!subString)
		{
			if ((trans.parent.name.ToLower() == name) && ((includeInactive) || trans.gameObject.activeInHierarchy))
				return trans.parent;
		}
		else
		{
			if ((trans.parent.name.ToLower().Contains(name)) && ((includeInactive) || trans.gameObject.activeInHierarchy))
				return trans.parent;
		}

		Transform ct = GetTransformByNameInAncestors(trans.parent, name, includeInactive, subString);
		if (ct != null)
			return ct;

		return null;

	}


	/// <summary>
	/// Replacement for Object.Instantiate in order to utilize pooling
	/// </summary>
	public static UnityEngine.Object Instantiate( UnityEngine.Object original )
    {
    
    	return vp_Utility.Instantiate(original, Vector3.zero, Quaternion.identity);
    
    }
    
    
    /// <summary>
	/// Replacement for Object.Instantiate in order to utilize pooling
	/// </summary>
    public static UnityEngine.Object Instantiate( UnityEngine.Object original, Vector3 position, Quaternion rotation )
    {

		if (vp_PoolManager.Instance == null || !vp_PoolManager.Instance.enabled || !(original is GameObject))
		{
			return GameObject.Instantiate(original, position, rotation);
		}
		else
		{
			return vp_PoolManager.Spawn((original as GameObject), position, rotation);
		}
				//vp_GlobalEventReturn<UnityEngine.Object, Vector3, Quaternion, UnityEngine.Object>.Send("vp_PoolManager Instantiate", original, position, rotation);
    
    }
    
    
    /// <summary>
	/// Replacement for Object.Destroy in order to utilize pooling
	/// </summary>
    public static void Destroy(UnityEngine.Object obj)
    {

		if (vp_PoolManager.Instance == null || !vp_PoolManager.Instance.enabled || !(obj is GameObject))
			UnityEngine.Object.Destroy(obj);
		else
			vp_PoolManager.Despawn(obj as GameObject);

	}
    
    
    /// <summary>
	/// Replacement for Object.Destroy in order to utilize pooling
	/// </summary>
    public static void Destroy(UnityEngine.Object obj, float t)
    {

		if (vp_PoolManager.Instance == null || !vp_PoolManager.Instance.enabled || !(obj is GameObject))
			UnityEngine.Object.Destroy(obj, t);
		else
		{
			vp_Timer.In(Mathf.Max(0, t), () =>
				{
					vp_PoolManager.Despawn(obj as GameObject, t);
				});
		}

			//vp_GlobalEvent<UnityEngine.Object, float>.Send("vp_PoolManager Destroy", obj, t);
    
    }


	/// <summary>
	/// Returns a positive integer value that is guaranteed to be unique
	/// until one billion IDs have been generated.
	/// </summary>
	public static int UniqueID
	{

		get
		{
			int i;
		reroll:
			i = UnityEngine.Random.Range(0, 1000000000);
			if (m_UniqueIDs.ContainsKey(i))	// likely won't happen (ever)
			{
				if (m_UniqueIDs.Count >= 1000000000)
				{
					ClearUniqueIDs();
					UnityEngine.Debug.LogWarning("Warning (vp_Utility.UniqueID) More than 1 billion unique IDs have been generated. This seems like an awful lot for a game client. Clearing dictionary and starting over!");
				}
				goto reroll;
			}
			m_UniqueIDs.Add(i, 0);
			return i;
		}

	}
	private static Dictionary<int, int> m_UniqueIDs = new Dictionary<int, int>();


	/// <summary>
	/// clears all generated unique IDs
	/// </summary>
	public static void ClearUniqueIDs()
	{
		m_UniqueIDs.Clear();
	}


	/// <summary>
	/// generates an integer value based on a world position. this can
	/// be used to establish the same object IDs across clients without
	/// a lot of manual object ID assignment.
	/// NOTES:
	/// 1) this method should be run in Awake, before any object has
	/// had a chance to alter its start position
	/// 2) the blatant assumption here is that as long as every object
	/// using this method exists at a unique world coordinate on Awake
	/// - and this coordinate is the same on all clients - the IDs
	/// generated will be unique and deterministic. there may be some
	/// edge cases where the same IDs are generated but they should be
	/// very rare
	/// </summary>
	public static int PositionToID(Vector3 position)
	{

		return (int)Mathf.Abs(
			  (position.x * 10000)
			+ (position.y * 1000)
			+ (position.z * 100));

	}


	/// <summary>
	/// Plays a random sound from a list, with a random pitch.
	/// </summary>
	[Obsolete("Please use 'vp_AudioUtility.PlayRandomSound' instead.")]
	public static void PlayRandomSound(AudioSource audioSource, List<AudioClip> sounds, Vector2 pitchRange)
	{

		vp_AudioUtility.PlayRandomSound(audioSource, sounds, pitchRange);

	}


	/// <summary>
	/// Plays a random sound from a list.
	/// </summary>
	[Obsolete("Please use 'vp_AudioUtility.PlayRandomSound' instead.")]
	public static void PlayRandomSound(AudioSource audioSource, List<AudioClip> sounds)
	{
		vp_AudioUtility.PlayRandomSound(audioSource, sounds);
	}


}

