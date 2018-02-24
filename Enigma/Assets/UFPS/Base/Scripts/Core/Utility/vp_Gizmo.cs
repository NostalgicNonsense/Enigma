/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Gizmo.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this simple gizmo script can be dragged onto gameobjects
//					to visualize things like spawnpoints in the editor
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;

public class vp_Gizmo : MonoBehaviour
{

	public Color gizmoColor = new Color(1f, 1f, 1f, .4f);
	public Color selectedGizmoColor = new Color(1f, 1f, 1f, .4f);

	protected Collider m_Collider = null;
	protected Collider Collider
	{
		get
		{
			if (m_Collider == null)
				m_Collider = GetComponent<Collider>();
			return m_Collider;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public void OnDrawGizmos()
	{

		Vector3 center = Collider.bounds.center;
		Vector3 size = Collider.bounds.size;
		Gizmos.color = gizmoColor;
		Gizmos.DrawCube(center, size);
		Gizmos.color = new Color(0f, 0f, 0f, 1f);
		Gizmos.DrawLine(Vector3.zero, Vector3.forward);

	}


	/// <summary>
	/// 
	/// </summary>
	public void OnDrawGizmosSelected()
	{

		Vector3 center = Collider.bounds.center;
		Vector3 size = Collider.bounds.size;
		Gizmos.color = selectedGizmoColor;
		Gizmos.DrawCube(center, size);
		Gizmos.color = new Color(0f, 0f, 0f, 1f);
		Gizmos.DrawLine(Vector3.zero, Vector3.forward);

	}


}
