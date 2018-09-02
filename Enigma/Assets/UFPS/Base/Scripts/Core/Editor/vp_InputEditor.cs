/////////////////////////////////////////////////////////////////////////////////
//
//	vp_InputEditor.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	custom inspector for the vp_InputEditor class
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(vp_Input))]

public class vp_InputEditor : Editor
{

	/// <summary>
	/// 
	/// </summary>
	public override void OnInspectorGUI()
	{

		GUI.color = Color.white;
		
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		if (GUILayout.Button("Open UFPS Input Manager", GUILayout.MinWidth(150), GUILayout.MinHeight(25)))
			vp_InputWindow.Init();
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		GUILayout.Space(10);

		// update
		if (GUI.changed)
		{

			EditorUtility.SetDirty(target);

		}

	}

}

