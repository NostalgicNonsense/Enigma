/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SpawnPoint.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	spawnpoint logic and gizmo rendering
//
/////////////////////////////////////////////////////////////////////////////////


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_5_4_OR_NEWER
using UnityEngine.SceneManagement;
#endif

[System.Serializable]
public class vp_SpawnPoint : MonoBehaviour
{

	public bool RandomDirection = false;
	public float Radius = 0.0f;
	public float GroundSnapThreshold = 2.5f;
	public bool LockGroundSnapToRadius = true;

	protected static List<vp_SpawnPoint> m_MatchingSpawnPoints = new List<vp_SpawnPoint>(50);	// work variable

	protected static List<vp_SpawnPoint> m_SpawnPoints = null;
	public static List<vp_SpawnPoint> SpawnPoints
	{
		get
		{
			// if we have no list of spawnpoints (which is the case if the
			// game just booted up or a new level was loaded) - attempt to
			// populate a new one by scanning the scene for vp_SpawnPoints
			if (m_SpawnPoints == null)
				m_SpawnPoints = new List<vp_SpawnPoint>(FindObjectsOfType(typeof(vp_SpawnPoint)) as vp_SpawnPoint[]);
			return m_SpawnPoints;
		}
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
	/// picks a random spawnpoint and returns a placement that is snapped
	/// to the ground and adjusted not to intersect other physics blockers.
	/// if 'physicsCheckRadius' is set, collision checking will be performed
	/// against pre-existing objects
	/// </summary>
	public static vp_Placement GetRandomPlacement()
	{
		return GetRandomPlacement(0.0f, null);
	}
	public static vp_Placement GetRandomPlacement(float physicsCheckRadius)
	{
		return GetRandomPlacement(physicsCheckRadius, null);
	}
	public static vp_Placement GetRandomPlacement(string tag)
	{
		return GetRandomPlacement(0.0f, tag);
	}
	public static vp_Placement GetRandomPlacement(float physicsCheckRadius, string tag)
	{

		// abort if scene contains no spawnpoints
		if((SpawnPoints == null) || (SpawnPoints.Count < 1))
			return null;

		// fetch a random spawnpoint
		vp_SpawnPoint spawnPoint = null;
		if (string.IsNullOrEmpty(tag))
			spawnPoint = GetRandomSpawnPoint();
		else
		{
			spawnPoint = GetRandomSpawnPoint(tag);
			if (spawnPoint == null)
			{
				spawnPoint = GetRandomSpawnPoint();
				Debug.LogWarning("Warning (vp_SpawnPoint --> GetRandomPlacement) Could not find a spawnpoint tagged '" + tag + "'. Falling back to 'any random spawnpoint'.");
			}
		}
		
		// if no spawnpoint was found, revert to world origin
		if (spawnPoint == null)
		{
			Debug.LogError("Error (vp_SpawnPoint --> GetRandomPlacement) Could not find a spawnpoint" + (!string.IsNullOrEmpty(tag) ? (" tagged '" + tag + "'") : ".") + " Reverting to world origin.");
			return null;
		}

		// found a spawnpoint! use it to create a placement
		return spawnPoint.GetPlacement(physicsCheckRadius);

	}


	/// <summary>
	/// returns a placement confined to this spawnpoint's settings
	/// if 'physicsCheckRadius' is set collision checking will be
	/// performed against pre-existing objects
	/// </summary>
	public virtual vp_Placement GetPlacement(float physicsCheckRadius = 0.0f)
	{

		vp_Placement p = new vp_Placement();
		p.Position = transform.position;
		if(Radius > 0.0f)
		{
			Vector3 newPos = (Random.insideUnitSphere * Radius);
			p.Position.x += newPos.x;
			p.Position.z += newPos.z;
		}

		// stay clear of other physics blockers and snap to ground
		if (physicsCheckRadius != 0.0f)
		{
			if (!vp_Placement.AdjustPosition(p, physicsCheckRadius))
				return null;
			vp_Placement.SnapToGround(p, physicsCheckRadius, GroundSnapThreshold);
		}

		// if spawnpoint is set to use random rotation - create one.
		// otherwise use the rotation of the spawnpoint
		if(RandomDirection)
			p.Rotation = Quaternion.Euler(Vector3.up * Random.Range(0.0f, 360.0f));
		else
			p.Rotation = transform.rotation;
	
		return p;

	}


	/// <summary>
	/// 
	/// </summary>
	public static vp_SpawnPoint GetRandomSpawnPoint()
	{

		if (SpawnPoints.Count < 1)
			return null;

		return SpawnPoints[Random.Range(0, SpawnPoints.Count)];

	}


	/// <summary>
	/// 
	/// </summary>
	public static vp_SpawnPoint GetRandomSpawnPoint(string tag)
	{

		m_MatchingSpawnPoints.Clear();

		for (int v = 0; v < SpawnPoints.Count; v++)
		{
			if(m_SpawnPoints[v].tag == tag)
				m_MatchingSpawnPoints.Add(m_SpawnPoints[v]);
		}

		if (m_MatchingSpawnPoints.Count < 1)
			return null;

		if (m_MatchingSpawnPoints.Count == 1)
			return m_MatchingSpawnPoints[0];

		return m_MatchingSpawnPoints[Random.Range(0, m_MatchingSpawnPoints.Count)];

	}

	
	/// <summary>
	/// erases the global list of spawnpoints every time a level is loaded.
	/// (the list will be automatically recreated and repopulated the next
	/// time the 'SpawnPoints' property is read)
	/// </summary>
#if UNITY_5_4_OR_NEWER
	protected virtual void OnLevelLoad(Scene scene, LoadSceneMode mode)
#else
	protected virtual void OnLevelWasLoaded()
#endif
	{
		m_SpawnPoints = null;
	}


	// ---- editor stuff below this line. NOTE: there is also 'vp_SpawnPointEditor.cs' ----


#if UNITY_EDITOR

	public Color m_Color = new Color32(160, 255, 100, 60);
	public MarkerStyle m_MarkerStyle = MarkerStyle.Ball;

	protected Vector3 m_PhoneBoothSize = new Vector3(0.8f, 1.8f, 0.8f);
	protected Vector3 m_BoxSize = new Vector3(0.8f, 0.8f, 0.8f);
	protected float m_BallSize = 0.6f;
	protected Color m_HalfAlphaColor = new Color32(160, 255, 100, 30);
	protected Color m_SelectedColor = new Color32(130, 255, 70, 120);
	protected Color m_GroundSnapGizmoColor = new Color(1, 1, 1, 0.2f);
	protected Color m_RefreshedColor = Color.white;
	protected float m_LastGroundSnapThreshold;
	protected bool m_DrawGroundSnapGizmos = false;
	protected bool m_LastGroundSnap = false;

	public Color SelectedColor
	{
		get
		{
			return m_SelectedColor;
		}
	}
	
	public enum MarkerStyle
	{
		Ball,
		Box,
		PhoneBooth
	}
	

	/// <summary>
	/// rendering of spawnpoint editor gizmos
	/// </summary>
	public virtual void OnDrawGizmos()
	{

		transform.localScale = Vector3.one;


		Gizmos.matrix = transform.localToWorldMatrix;
		if (m_Color != m_RefreshedColor)
			RefreshColors();

		if (m_LastGroundSnap != LockGroundSnapToRadius)
		{
			EditorUtility.SetDirty(this);
			m_LastGroundSnap = LockGroundSnapToRadius;
		}

		// user can enable gizmos in the game view if playing in the editor,
		// but we won't let the weapon camera draw gizmos
		if (!((Camera.current.cullingMask & vp_Layer.Weapon) > 0))
			return;

		// cap scale, radius and rotation
		transform.localScale = Vector3.one;
		transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
		Radius = Mathf.Max(0, Radius);
		GroundSnapThreshold = (LockGroundSnapToRadius && (Radius > 0)) ? (Radius + 1.0f) : Mathf.Max(0, GroundSnapThreshold);
		
		// spawn point & area visualization
		if (Radius <= 0.0f)
		{
			// draw marker
			Gizmos.color = m_Color;
			DrawSolidMarker();
		}
		else
		{
			// draw marker + area sphere
			Gizmos.color = m_HalfAlphaColor;
			DrawSolidMarker();
			Gizmos.DrawSphere(Vector3.zero, Radius);
		}

		// revert the ground snap gizmo to invisible every time it is deselected
		// NOTE: this does not pertain to multi-selections since we want to be
		// able to visualize the ground snap threshold on such
		if ((Selection.gameObjects.Length < 2) && (Selection.activeGameObject != gameObject))
		{
			m_LastGroundSnapThreshold = GroundSnapThreshold;
			m_DrawGroundSnapGizmos = false;
		}

	}


	/// <summary>
	/// additional rendering on top of selected spawnpoint editor gizmos
	/// </summary>
	public virtual void OnDrawGizmosSelected()
	{

		transform.localScale = Vector3.one;

		// don't let the weapon camera draw gizmos
		if (!((Camera.current.cullingMask & (vp_Layer.Weapon)) > 0))
			return;

		Gizmos.matrix = transform.localToWorldMatrix;

		// spawn point & area visualization
		if (Radius <= 0.0f)
		{
			Gizmos.color = m_Color;
			DrawSolidMarker();	// fill in marker
			Gizmos.color = m_SelectedColor;
			DrawWireMarker();	// draw wireframe
		}
		else
		{
			Gizmos.color = m_Color;
			DrawSolidMarker();	// fill in marker
			Gizmos.color = m_SelectedColor;
			DrawWireMarker();	// draw wireframe
			Gizmos.DrawWireSphere(Vector3.zero, Radius);	// draw area sphere
		}

		// draw directional arrows
		float rad = ((Radius > 0.0f) ? (Radius * 0.5f) : 1.0f);
		if (RandomDirection)
		{
			// draw four big arrows, relative to the spawnpoint and perpendicular to each other
			Gizmos.DrawLine(Vector3.zero + (Vector3.back * 2.0f) * rad, Vector3.zero + (Vector3.forward * 2.0f) * rad);
			Gizmos.DrawLine(Vector3.zero + (Vector3.left * 2.0f) * rad, Vector3.zero + (Vector3.right * 2.0f) * rad);
			Gizmos.DrawLine(Vector3.zero + (Vector3.forward * 2.0f) * rad, Vector3.zero + ((Vector3.forward * 1.5f * rad) + (Vector3.left * 0.5f) * rad));
			Gizmos.DrawLine(Vector3.zero + (Vector3.forward * 2.0f) * rad, Vector3.zero + ((Vector3.forward * 1.5f * rad) + (Vector3.right * 0.5f) * rad));
			Gizmos.DrawLine(Vector3.zero + (Vector3.back * 2.0f) * rad, Vector3.zero + ((Vector3.back * 1.5f * rad) + (Vector3.left * 0.5f) * rad));
			Gizmos.DrawLine(Vector3.zero + (Vector3.back * 2.0f) * rad, Vector3.zero + ((Vector3.back * 1.5f * rad) + (Vector3.right * 0.5f) * rad));
			Gizmos.DrawLine(Vector3.zero + (Vector3.left * 2.0f) * rad, Vector3.zero + ((Vector3.left * 1.5f * rad) + (Vector3.forward * 0.5f) * rad));
			Gizmos.DrawLine(Vector3.zero + (Vector3.left * 2.0f) * rad, Vector3.zero + ((Vector3.left * 1.5f * rad) + (Vector3.back * 0.5f) * rad));
			Gizmos.DrawLine(Vector3.zero + (Vector3.right * 2.0f) * rad, Vector3.zero + ((Vector3.right * 1.5f * rad) + (Vector3.forward * 0.5f) * rad));
			Gizmos.DrawLine(Vector3.zero + (Vector3.right * 2.0f) * rad, Vector3.zero + ((Vector3.right * 1.5f * rad) + (Vector3.back * 0.5f) * rad));
		}
		else
		{
			// draw a single big arrow pointing in the spawnpoint's forward direction
			Gizmos.DrawLine(Vector3.zero, Vector3.zero + (Vector3.forward * 2.0f) * rad);
			Gizmos.DrawLine(Vector3.zero + (Vector3.forward * 2.0f) * rad, Vector3.zero + ((Vector3.forward * 1.5f * rad) + (Vector3.left * 0.5f) * rad));
			Gizmos.DrawLine(Vector3.zero + (Vector3.forward * 2.0f) * rad, Vector3.zero + ((Vector3.forward * 1.5f * rad) + (Vector3.right * 0.5f) * rad));
		}

		// draw ground snap gizmos
		if (GroundSnapThreshold != m_LastGroundSnapThreshold)
			m_DrawGroundSnapGizmos = true;
		if (m_DrawGroundSnapGizmos && (GroundSnapThreshold > 0.0f))
		{

			Gizmos.color = m_GroundSnapGizmoColor;

			// draw a transparent white plane at the top and bottom of the range
			float size = Mathf.Max(5, Radius * 2.0f);
			Gizmos.DrawCube((Vector3.zero + (Vector3.up * GroundSnapThreshold)), (Vector3.forward + Vector3.right) * size);
			Gizmos.DrawCube((Vector3.zero + (Vector3.down * GroundSnapThreshold)), (Vector3.forward + Vector3.right) * size);

			// draw a white line from each top corner to the corresponding bottom corner
			Vector3 corner1 = ((Vector3.forward + Vector3.right) * size * 0.5f);
			Vector3 corner2 = (Vector3.forward + Vector3.left) * size * 0.5f;
			Vector3 corner3 = (Vector3.back + Vector3.right) * size * 0.5f;
			Vector3 corner4 = (Vector3.back + Vector3.left) * size * 0.5f;
			Vector3 up = (Vector3.up * GroundSnapThreshold);
			Vector3 down = (Vector3.down * GroundSnapThreshold);
			Gizmos.DrawLine(corner1 + up, corner1 + down);
			Gizmos.DrawLine(corner2 + up, corner2 + down);
			Gizmos.DrawLine(corner3 + up, corner3 + down);
			Gizmos.DrawLine(corner4 + up, corner4 + down);

		}

		m_LastGroundSnapThreshold = GroundSnapThreshold;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void DrawSolidMarker()
	{

		switch (m_MarkerStyle)
		{
			case MarkerStyle.Ball: Gizmos.DrawSphere(Vector3.zero, m_BallSize); break;
			case MarkerStyle.Box: Gizmos.DrawCube(Vector3.zero, m_BoxSize); break;
			case MarkerStyle.PhoneBooth: Gizmos.DrawCube((Vector3.up * (m_PhoneBoothSize.y * 0.5f)), m_PhoneBoothSize); break;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	void DrawWireMarker()
	{

		switch (m_MarkerStyle)
		{
			case MarkerStyle.Ball: Gizmos.DrawWireSphere(Vector3.zero, m_BallSize); break;
			case MarkerStyle.Box: Gizmos.DrawWireCube(Vector3.zero, m_BoxSize); break;
			case MarkerStyle.PhoneBooth: Gizmos.DrawWireCube((Vector3.up * (m_PhoneBoothSize.y * 0.5f)), m_PhoneBoothSize); break;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void RefreshColors()
	{

		// fainter color for when drawing sphere on top of marker
		m_HalfAlphaColor = new Color(m_Color.r, m_Color.g, m_Color.b, (m_Color.a * 0.5f));
		m_SelectedColor = GetContrastColor();
		m_RefreshedColor = m_Color;

	}


	/// <summary>
	/// calculates a highlight color for wireframe that contrasts well
	/// against the solid color
	/// </summary>
	protected virtual Color GetContrastColor()
	{

		float max = 0;
		if ((m_Color.r > max)) max = m_Color.r;
		if ((m_Color.g > max)) max = m_Color.g;
		if ((m_Color.b > max)) max = m_Color.b;

		return new Color(
			((m_Color.r == max) ? m_Color.r : m_Color.r * 0.75f),
			((m_Color.g == max) ? m_Color.g : m_Color.g * 0.75f),
			((m_Color.b == max) ? m_Color.b : m_Color.b * 0.75f),
			Mathf.Clamp01((m_Color.a * 2.0f)));

	}

	#endif

}

