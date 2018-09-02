/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SpawnPointEditor.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	editor for vp_SpawnPoints, with lots of special logic for mouse
//					interaction and playing nicely with the regular unity tools
//
//					NOTE: most spawnpoint gizmo rendering logic is actually in
//					the 'vp_SpawnPoint.cs' file, since gizmos are drawn as part
//					of monobehaviors. this class is responsible for interaction
//
/////////////////////////////////////////////////////////////////////////////////


using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(vp_SpawnPoint))]
public class vp_SpawnPointEditor : Editor
{

	// misc cached things
	private vp_SpawnPoint m_Component = null;
	private Transform m_EditorCameraTransform = null;
	private Vector3 m_TransformPosition = Vector3.zero;
	private Vector3 m_TransformForward = Vector3.zero;
	private Vector3 m_TransformRight = Vector3.zero;

	// states
	private bool m_Modifying = false;
	private bool m_Mousedown = false;
	private Quaternion m_LastCameraRotation = Quaternion.identity;
	private Vector3 m_LastCameraPosition = Vector3.zero;
	private Vector3 m_LastObjectAngle = Vector3.zero;
	private Vector3 m_LastObjectPosition = Vector3.zero;
	public float SerializedAngle = 0.0f;
	bool m_MouseOverAnyHandle;
	private float m_DistanceToObject2D = 0.0f;
	private float m_DistanceToObject3D = 0.0f;

	// variables for avoiding collisions with the move tool
	private Vector3 MoveToolForwardArrowPosition = Vector3.zero;
	private Vector3 MoveToolRightArrowPosition = Vector3.zero;
	private Vector3 MoveToolUpArrowPosition = Vector3.zero;
	private float m_DistanceToMoveToolForwardArrow = 0.0f;
	private float m_DistanceToMoveToolRightArrow = 0.0f;
	private float m_DistanceToMoveToolUpArrow = 0.0f;
	private float m_MoveToolHandleGlobalRotationOffset = 0.0f;
	private const float MoveToolArrowCheckRadius = 15.0f;
	private const float MoveToolArrowLength = 65;
	private const float m_HandleMouseZoneSize = 0.5f;
	private Vector3 m_HitPoint = Vector3.zero;
	private float m_PixelSize = 0.0f;
	
	// raycasting
	private Ray m_Ray;
	private Plane m_Plane;
	private float m_Distance;


	/// <summary>
	/// 
	/// </summary>
	private void OnEnable()
	{
		m_Component = (vp_SpawnPoint)target;
	}
	

	/// <summary>
	/// 
	/// </summary>
	private void OnSceneGUI()
	{

		// cache object & camera values that will be used a lot
		m_TransformPosition = m_Component.transform.position;
		m_TransformForward = m_Component.transform.forward;
		m_TransformRight = m_Component.transform.right;
		m_EditorCameraTransform = SceneView.lastActiveSceneView.camera.transform;

		// always hijack scale tool to use the spawnpoint's own logic
		if (m_Component.transform.localScale != Vector3.one)
		{
			Tools.current = Tool.None;
			m_Modifying = true;
		}

		// reset stuff
		m_DistanceToObject2D = 0;
		m_DistanceToObject3D = 0;
		m_DistanceToMoveToolForwardArrow = 0;
		m_DistanceToMoveToolRightArrow = 0;
		m_DistanceToMoveToolUpArrow = 0;
		MoveToolForwardArrowPosition = Vector3.zero;
		MoveToolRightArrowPosition = Vector3.zero;
		MoveToolUpArrowPosition = Vector3.zero;
		
		// --- perform all the logic for tool handle interactions ---

		Handles.BeginGUI();
		GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
		m_Ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		GUILayout.EndArea();
		Handles.EndGUI();
		m_Plane = new Plane(Vector3.up, m_TransformPosition);
		m_Distance = 0;
		m_HitPoint = Vector3.zero;
		m_MouseOverAnyHandle = false;
		if (m_Plane.Raycast(m_Ray, out m_Distance))
		{
			m_HitPoint = m_Ray.GetPoint(m_Distance);
			m_DistanceToObject2D = Vector3.Distance(m_HitPoint, m_TransformPosition);
	
			// NOTE: there is a bug where if you toggle the "Global / Local" button
			// back and forth after having rotated the object using the regular move
			// tool in global mode, the move tool mouse zones will be in the wrong
			// positions, but this is likely a rare edge case

			MoveToolUpArrowPosition = m_TransformPosition + Vector3.up * m_PixelSize * MoveToolArrowLength;

			if (Tools.pivotRotation == PivotRotation.Local)
			{
				m_MoveToolHandleGlobalRotationOffset = 0.0f;
				MoveToolForwardArrowPosition = m_TransformPosition + m_TransformForward * m_PixelSize * MoveToolArrowLength;
				MoveToolRightArrowPosition = m_TransformPosition + m_TransformRight * m_PixelSize * MoveToolArrowLength;
			}
			else
			{
				MoveToolForwardArrowPosition =
					m_TransformPosition +
					((m_MoveToolHandleGlobalRotationOffset == 0.0f) ?
									Vector3.forward :
									Quaternion.AngleAxis(-m_MoveToolHandleGlobalRotationOffset, Vector3.up) * Vector3.forward) *		// rotates tool handle position according to offset, if needed 
					m_PixelSize *
					MoveToolArrowLength;
				MoveToolRightArrowPosition =
					m_TransformPosition +
					((m_MoveToolHandleGlobalRotationOffset == 0.0f) ?
									Vector3.right :
									Quaternion.AngleAxis(-m_MoveToolHandleGlobalRotationOffset, Vector3.up) * Vector3.right) *		// rotates tool handle position according to offset, if needed
					m_PixelSize *
					MoveToolArrowLength;
			}
			
			m_DistanceToObject3D = vp_3DUtility.DistanceToRay(m_Ray, m_TransformPosition);
			m_DistanceToMoveToolForwardArrow = vp_3DUtility.DistanceToRay(m_Ray, MoveToolForwardArrowPosition);
			m_DistanceToMoveToolRightArrow = vp_3DUtility.DistanceToRay(m_Ray, MoveToolRightArrowPosition);
			m_DistanceToMoveToolUpArrow = vp_3DUtility.DistanceToRay(m_Ray, MoveToolUpArrowPosition);
			
			m_MouseOverAnyHandle = IsMouseOverAnyMoveToolArrow;

		}

		// --- handle mouse button input ---
		if (Event.current.isMouse && Event.current.button == 0)
		{
			if (Event.current.type == EventType.MouseDown)
			{
				m_Mousedown = true;
				if (m_MouseOverAnyHandle)
				{
					if (Tools.current != Tool.View &&	// never allow modifying while 'View' tool is active
						(Tools.current == Tool.None ||	// always allow modifying while no tool is active
						((m_DistanceToMoveToolForwardArrow > MoveToolArrowCheckRadius * m_PixelSize) &&
						(m_DistanceToMoveToolRightArrow > MoveToolArrowCheckRadius * m_PixelSize) &&
						(m_DistanceToMoveToolUpArrow > MoveToolArrowCheckRadius * m_PixelSize))))	// make sure not to overlap move tool handles
					{
						m_Modifying = true;
						Tools.current = Tool.None;
					}
				}
				if (m_DistanceToObject3D < m_Component.Radius)
				{
					if (Tools.current == Tool.Scale)
					{
						if (!m_Component.RandomDirection)
							Tools.current = Tool.None;
						m_Modifying = true;
					}
					else if (Tools.current == Tool.Rotate)
						m_Modifying = false;
				}
			}
			else if (m_Mousedown && Event.current.type == EventType.MouseUp)
			{
				m_Mousedown = false;
				m_Modifying = false;
			}
		}

		// always exit modify mode and update pixel size if camera is moved / rotated,
		// or the object is moved
		if ((m_LastCameraRotation != m_EditorCameraTransform.rotation) ||
			(m_LastCameraPosition != m_EditorCameraTransform.position) ||
			(m_LastObjectPosition != m_Component.transform.position))
		{
			m_PixelSize = vp_EditorGUIUtility.GetPixelScaleAtWorldPosition(m_TransformPosition, m_EditorCameraTransform);
			m_Modifying = false;
		}
		if (Event.current.alt)
			m_Modifying = false;
		
		// while modifying, always update spawn point radius according to mouse position
		if (m_Modifying)
		{
			m_Component.Radius = Mathf.Max(1.0f, m_DistanceToObject2D);
			if (m_Component.Radius <= 1.0f)
				m_Component.Radius = 0.0f;
			if (!m_Component.RandomDirection)
				m_Component.transform.LookAt(m_HitPoint);
			EditorUtility.SetDirty(m_Component);
		}
		
		// --- circle cap rendering ---

		Handles.color = m_Component.SelectedColor;

		if (!m_Component.RandomDirection)	// spawnpoint has a facing direction
		{

			// detect angle changes for serialization on editor play mode
			if (m_Component.transform.eulerAngles.y != 0.0f)
				SerializedAngle = m_Component.transform.eulerAngles.y;
			else if (SerializedAngle != 0.0f)
			{
				m_Component.transform.eulerAngles = new Vector3(m_Component.transform.eulerAngles.x, SerializedAngle, m_Component.transform.eulerAngles.z);
				EditorUtility.SetDirty(m_Component);
			}

			// draw circle cap

			// if radius is larger than one, this is an area spawnpoint
			if (m_Component.Radius > 1.0f || !m_Modifying)
			{
#if UNITY_5_6_OR_NEWER
                Handles.ScaleValueHandle(0,
                m_TransformPosition + (m_TransformForward * m_Component.Radius),    // draw circle cap at area boundary
                Quaternion.Euler(90, 0, 0),
                2.0f, Handles.CircleHandleCap, 0);
#else
                Handles.ScaleValueHandle(0,
				m_TransformPosition + (m_TransformForward * m_Component.Radius),	// draw circle cap at area boundary
				Quaternion.Euler(90, 0, 0),
				2.0f, Handles.CircleCap, 0);
#endif
            }
			else // radius is smaller than one: this is a non-area point
            {
#if UNITY_5_6_OR_NEWER
                Handles.ScaleValueHandle(0,
                m_TransformPosition + (m_TransformForward * m_DistanceToObject2D),  // draw circle cap at mouse position
                Quaternion.Euler(90, 0, 0),
                2.0f, Handles.CircleHandleCap, 0);
#else
				Handles.ScaleValueHandle(0,
				m_TransformPosition + (m_TransformForward * m_DistanceToObject2D),	// draw circle cap at mouse position
				Quaternion.Euler(90, 0, 0),
				2.0f, Handles.CircleCap, 0);
#endif
            }

			// this handles a case where tool handle positions will go wrong if the
			// regular rotate tool is used in the 'Global' pivot rotation mode.
			if (Tools.current == Tool.Rotate)
			{
				if (m_LastObjectAngle != m_Component.transform.eulerAngles)
				{
					if (Tools.pivotRotation == PivotRotation.Global)
						m_MoveToolHandleGlobalRotationOffset += m_LastObjectAngle.y - m_Component.transform.eulerAngles.y;	// store how much we have rotated every frame
				}
			}

			////uncomment to draw a debug sphere around the move tool forward arrow
			//Handles.SphereCap(0, MoveToolForwardArrowPosition, Quaternion.identity, (MoveToolArrowCheckRadius * m_PixelSize) * 2);
			//Handles.SphereCap(0, MoveToolRightArrowPosition, Quaternion.identity, (MoveToolArrowCheckRadius * m_PixelSize) * 2);
			//Handles.SphereCap(0, MoveToolUpArrowPosition, Quaternion.identity, (MoveToolArrowCheckRadius * m_PixelSize) * 2);

			////uncomment to check for move tool handle mouseover
			//if (m_DistanceToMoveToolForwardArrow < MoveToolArrowCheckRadius * m_PixelSize)
			//    Debug.Log("FORWARD: " + Random.value);
			//if (m_DistanceToMoveToolRightArrow < MoveToolArrowCheckRadius * m_PixelSize)
			//    Debug.Log("RIGHT: " + Random.value);
			//if (m_DistanceToMoveToolUpArrow < MoveToolArrowCheckRadius * m_PixelSize)
			//    Debug.Log("UP: " + Random.value);
			
		}
		else	// spawnpoint has random direction
		{

			// never allow the regular rotate tool for random direction spawnpoints
			if (Tools.current == Tool.Rotate)
				Tools.current = Tool.None;

			// force world rotation at all times
			m_Component.transform.rotation = Quaternion.identity;

			if (m_Modifying && m_Mousedown) // while modifying, show a single circle cap
            {
#if UNITY_5_6_OR_NEWER
                Handles.ScaleValueHandle(0,
                m_HitPoint,
                Quaternion.Euler(90, 0, 0),
                2.0f, Handles.CircleHandleCap, 0);
#else
				Handles.ScaleValueHandle(0,
				m_HitPoint,
				Quaternion.Euler(90, 0, 0),
				2.0f, Handles.CircleCap, 0);
#endif
            }
			else    // when not modifying, display four circle caps that all can be used to start scaling
            {
#if UNITY_5_6_OR_NEWER
                Handles.ScaleValueHandle(0,
                m_TransformPosition + (m_TransformForward * m_Component.Radius),
                Quaternion.Euler(90, 0, 0),
                2.0f, Handles.CircleHandleCap, 0);

                Handles.ScaleValueHandle(0,
                m_TransformPosition + (m_TransformRight * m_Component.Radius),
                Quaternion.Euler(90, 0, 0),
                2.0f, Handles.CircleHandleCap, 0);

                Handles.ScaleValueHandle(0,
                m_TransformPosition + ((-m_TransformRight) * m_Component.Radius),
                Quaternion.Euler(90, 0, 0),
                2.0f, Handles.CircleHandleCap, 0);

                Handles.ScaleValueHandle(0,
                m_TransformPosition + ((-m_TransformForward) * m_Component.Radius),
                Quaternion.Euler(90, 0, 0),
                2.0f, Handles.CircleHandleCap, 0);
#else
				Handles.ScaleValueHandle(0,
				m_TransformPosition + (m_TransformForward * m_Component.Radius),
				Quaternion.Euler(90, 0, 0),
				2.0f, Handles.CircleCap, 0);

				Handles.ScaleValueHandle(0,
				m_TransformPosition + (m_TransformRight * m_Component.Radius),
				Quaternion.Euler(90, 0, 0),
				2.0f, Handles.CircleCap, 0);

				Handles.ScaleValueHandle(0,
				m_TransformPosition + ((-m_TransformRight) * m_Component.Radius),
				Quaternion.Euler(90, 0, 0),
				2.0f, Handles.CircleCap, 0);

				Handles.ScaleValueHandle(0,
				m_TransformPosition + ((-m_TransformForward) * m_Component.Radius),
				Quaternion.Euler(90, 0, 0),
				2.0f, Handles.CircleCap, 0);
#endif
            }

		}

		// backup camera and object transform values
		m_LastCameraRotation = m_EditorCameraTransform.rotation;
		m_LastCameraPosition = m_EditorCameraTransform.position;
		m_LastObjectAngle = m_Component.transform.eulerAngles;
		m_LastObjectPosition = m_Component.transform.position;

	}

	
	/// <summary>
	/// returns true if the mouse is on top of any of the move tool arrows,
	/// in which case the circle cap handle will take a back seat
	/// </summary>
	private bool IsMouseOverAnyMoveToolArrow
	{
		get
		{
			return
			(
				(Vector3.Distance(m_HitPoint, m_TransformPosition + (m_TransformForward * m_Component.Radius)) < m_HandleMouseZoneSize) ||
				(Vector3.Distance(m_HitPoint, m_TransformPosition + (m_TransformRight * m_Component.Radius)) < m_HandleMouseZoneSize) ||
				(Vector3.Distance(m_HitPoint, m_TransformPosition + ((-m_TransformRight) * m_Component.Radius)) < m_HandleMouseZoneSize) ||
				(Vector3.Distance(m_HitPoint, m_TransformPosition + ((-m_TransformForward) * m_Component.Radius)) < m_HandleMouseZoneSize)
			);

		}

	}


}
