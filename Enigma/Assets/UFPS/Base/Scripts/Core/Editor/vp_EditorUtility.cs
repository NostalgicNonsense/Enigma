/////////////////////////////////////////////////////////////////////////////////
//
//	vp_EditorUtility.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	misc editor utility methods
//
/////////////////////////////////////////////////////////////////////////////////

// for Anti-Cheat Toolkit support (see the manual for more info)
#if ANTICHEAT
using CodeStage.AntiCheat.ObscuredTypes;
#endif

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public static class vp_EditorUtility
{


	/// <summary>
	/// 
	/// </summary>
	public static Object CreateAsset(string path, System.Type type)
	{

		if (!System.IO.Directory.Exists("Assets/" + path))
		{
			path = "UFPS";
			if (!System.IO.Directory.Exists("Assets/UFPS"))
				System.IO.Directory.CreateDirectory("Assets/UFPS");
		}

		string fileName = path + "/" + "New " + ((type.Name.StartsWith("vp_") ? type.Name.Substring(3) : type.Name));

		fileName = NewValidAssetName(fileName);

		Object asset = ScriptableObject.CreateInstance(type);

		AssetDatabase.CreateAsset(asset, "Assets/" + fileName + ".asset");
		if (asset != null)
		{
			EditorGUIUtility.PingObject(asset);
			Selection.activeObject = asset;
		}

		return asset;
	}
	
	
	/// <summary>
	/// creates a new filename from a base filename by appending
	/// the next number that will result in a non-existing filename
	/// NOTE: 'baseFileName' should not include "Assets/"
	/// </summary>
	public static string NewValidAssetName(string baseFileName)
	{

		string fileName = baseFileName;

		int n = 1;
		FileInfo fileInfo = null;
		fileInfo = new FileInfo("Assets/" + fileName + ".asset");
		while (fileInfo.Exists)
		{
			n++;
			fileName = fileName.Substring(0, baseFileName.Length) + " " + n.ToString();
			fileInfo = new FileInfo("Assets/" + fileName + ".asset");
		}

		return fileName;

	}


	/// <summary>
	/// creates a new directory name from a base name by appending
	/// the next number that will result in a non-existing dir name
	/// NOTE: 'baseFolderName' should not include "Assets/"
	/// </summary>
	public static string NewValidFolderName(string baseFolderName)
	{

		string folderName = baseFolderName;

		int n = 1;
		DirectoryInfo dirInfo = null;
		dirInfo = new DirectoryInfo("Assets/" + folderName);

		while (dirInfo.Exists)
		{
			n++;
			folderName = folderName.Substring(0, baseFolderName.Length) + ((n<10)?"0":"") + n.ToString();
			dirInfo = new DirectoryInfo("Assets/" + folderName);
		}

		return folderName;

	}


	/// <summary>
	/// 
	/// </summary>
	public static bool CopyValuesFromDerivedComponent(Component derivedComponent, Component baseComponent, bool copyValues, bool copyContent, string prefix, Dictionary<string, object> forcedValues = null)
	{

		System.Type derivedType = derivedComponent.GetType();
		System.Type baseType = baseComponent.GetType();

		if (!vp_EditorUtility.IsSameOrSubclass(baseType, derivedType))
			return false;

		foreach (FieldInfo f in baseType.GetFields())
		{

			if (!f.IsPublic)
				continue;

			if(!string.IsNullOrEmpty(prefix) && !f.Name.StartsWith(prefix))
			    continue;

			if(forcedValues != null)
			{
				object v = null;
				if(forcedValues.TryGetValue(f.Name, out v))
				{
					f.SetValue(baseComponent, v);
					continue;
				}
			}

			if (copyContent &&
				(f.FieldType == typeof(UnityEngine.GameObject) ||
				f.FieldType == typeof(UnityEngine.AudioClip)))
					goto copy;

			if (copyValues && (
					f.FieldType == typeof(float)
				||	f.FieldType == typeof(Vector4)
				||	f.FieldType == typeof(Vector3)
				||	f.FieldType == typeof(Vector2)
				||	f.FieldType == typeof(int)
				||	f.FieldType == typeof(bool)
				||	f.FieldType == typeof(string)
#if ANTICHEAT
				||	f.FieldType == typeof(ObscuredFloat)
				||	f.FieldType == typeof(ObscuredVector3)
				||	f.FieldType == typeof(ObscuredVector2)
				||	f.FieldType == typeof(ObscuredInt)
				||	f.FieldType == typeof(ObscuredBool)
				||	f.FieldType == typeof(ObscuredString)
#endif
				))
				goto copy;

			continue;

		copy:

			f.SetValue(baseComponent, derivedType.GetField(f.Name).GetValue(derivedComponent));

			//Debug.Log(f.Name + " (" + f.FieldType + ")");

		}
		
		return true;

	}


	/// <summary>
	/// 
	/// </summary>
	public static void GenerateStatesAndPresetsFromDerivedComponent(Component derivedComponent, Component baseComponent, string path)
	{

		//System.Type derivedType = derivedComponent.GetType();	// TEST (see below)
		System.Type baseType = baseComponent.GetType();

		// TEST: disabled to allow converting from vp_FPController to
		// vp_CapsuleController. evaluate this down the line
			//if (!vp_EditorUtility.IsSameOrSubclass(baseType, derivedType))
			//	return;

		vp_Component vpDerived = derivedComponent as vp_Component;
		vp_Component vpBase = baseComponent as vp_Component;

		if (vpDerived == null)
			return;

		if (vpBase == null)
			return;

		for (int v = 0; v < vpDerived.States.Count; v++)
		{

			// abort if old state has no text asset
			vp_State oldState = vpDerived.States[v];
			if (oldState.TextAsset == null)
				continue;

			// abort if we fail to load old text asset into a preset
			vp_ComponentPreset preset = new vp_ComponentPreset();
			if (!preset.LoadFromTextAsset(oldState.TextAsset))
				continue;

			// try to make the preset compatible with the base component. this
			// will fail if it has no compatible fields, in which case we abort
			if (preset.TryMakeCompatibleWithComponent(vpBase) < 1)
				continue;

			// we have a new preset that is compatible with the base component.
			// save it at a temporary, auto-generated path
			string typeName = oldState.TypeName.Replace("vp_FP", "");
			typeName = typeName.Replace("vp_", "");
			string filePath = path + "/" + typeName + "_" + vpBase.gameObject.name + "_" + oldState.Name + ".txt";
			vp_ComponentPreset.Save(preset, filePath);
			AssetDatabase.Refresh();

			// add a corresponding state, into which we load the new preset
			vp_State newState = new vp_State(baseType.Name, vpDerived.States[v].Name, null, null);
			vpBase.States.Add(newState);
			// System.Threading.Thread.Sleep(100);	// might come in handy on slow disk (?)
			newState.TextAsset = AssetDatabase.LoadAssetAtPath(filePath, typeof(TextAsset)) as TextAsset;

		}

	}



	/// <summary>
	/// 
	/// </summary>
	public static Component GetFPCameraChildComponent(System.Type type, string name)
	{

		vp_FPCamera camera = GameObject.FindObjectOfType<vp_FPCamera>();
		if (camera == null)
		{
			EditorUtility.DisplayDialog("Failed to find vp_FPCamera", "Make sure your scene has a gameobject with a vp_FPCamera component on it. ", "OK");
			return null;
		}

		foreach (Component c in camera.GetComponentsInChildren(type, true))
		{
			if (c.name == name)
				return c;
		}

		EditorUtility.DisplayDialog("Failed to find a matching " + type.Name, "Make sure the FPS camera object has a child gameobject with the exact name of \"" + name + "\", and that it has a " + type.Name + " component on it. ", "OK");

		return null;

	}


	/// <summary>
	/// determines if type 'potentialDescendant' is derived from or
	/// equal to 'potentialBase'
	/// </summary>
	public static bool IsSameOrSubclass(System.Type potentialBase, System.Type potentialDescendant)
	{
		return potentialDescendant.IsSubclassOf(potentialBase)
			   || potentialDescendant == potentialBase;
	}


}