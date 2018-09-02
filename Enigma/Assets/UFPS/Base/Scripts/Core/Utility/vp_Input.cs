/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Input.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	This class handles mouse, keyboard and joystick input. All
//					UFPS input should run through this class to keep all input
//					in one centralized location.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_Input : MonoBehaviour
{

	[System.Serializable]
	public class vp_InputAxis
	{
		public KeyCode Positive;
		public KeyCode Negative;
	}

	public int ControlType = 0;

	// primary buttons
	public Dictionary<string, KeyCode> Buttons = new Dictionary<string, KeyCode>();
	public List<string> ButtonKeys = new List<string>();
	public List<KeyCode> ButtonValues = new List<KeyCode>();

	// secondary buttons
	public Dictionary<string, KeyCode> Buttons2 = new Dictionary<string, KeyCode>();
	public List<KeyCode> ButtonValues2 = new List<KeyCode>();

	// axis
	public Dictionary<string, vp_InputAxis> Axis = new Dictionary<string, vp_InputAxis>();
	public List<string> AxisKeys = new List<string>();
	public List<vp_InputAxis> AxisValues = new List<vp_InputAxis>();
	
	// Unity Input Axis
	public List<string> UnityAxis = new List<string>();
	
	// paths
	public static string FolderPath = "UFPS/Base/Content/Resources/Input";
	public static string PrefabPath = "Assets/UFPS/Base/Content/Resources/Input/vp_Input.prefab";
	
	
	public static bool mIsDirty = true;

	/// <summary>
	/// Retrieves an instance of the input manager
	/// </summary>
	protected static vp_Input m_Instance;
	static public vp_Input Instance
	{
		get
		{
			if (mIsDirty)
			{
				mIsDirty = false;
				
				if(m_Instance == null)
				{
					if(Application.isPlaying)
					{
						// if application is playing, load vp_Input from resources
						GameObject go = Resources.Load("Input/vp_Input") as GameObject;
						if(go == null)
						{
							m_Instance = new GameObject("vp_Input").AddComponent<vp_Input>();
						}
						else
						{
							m_Instance = go.GetComponent<vp_Input>();
							if(m_Instance == null)
								m_Instance = go.AddComponent<vp_Input>();
						}
					}
					m_Instance.SetupDefaults();
				}
			}
			return m_Instance;
		}
	}
	
	
	/// <summary>
	/// creates the required prefab if one doesn't exist
	/// </summary>
	public static void CreateMissingInputPrefab(string prefabPath, string folderPath)
	{

#if UNITY_EDITOR

		GameObject go = UnityEditor.AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
		if (go == null)
		{
			// create a directory hierarchy to store the prefab if one doesn't exist
			if (!Application.isPlaying)
			{
				bool needsRefresh = false;
				string path = "";
				string[] folders = folderPath.Split(new string[1] { "/" }, System.StringSplitOptions.None);
				foreach (string folder in folders)
				{
					path += "/";
					if (!System.IO.Directory.Exists(Application.dataPath + path + folder))
					{
						needsRefresh = true;
						System.IO.Directory.CreateDirectory(Application.dataPath + path + folder);
					}
					path += folder;
				}
				if (needsRefresh)
					UnityEditor.AssetDatabase.Refresh();
			}

			go = new GameObject("vp_Input") as GameObject;
			go.AddComponent<vp_Input>();
			UnityEditor.PrefabUtility.CreatePrefab(prefabPath, go);
			UnityEngine.Object.DestroyImmediate(go);
		}
		else
		{
			if (go.GetComponent<vp_Input>() == null)
				go.AddComponent<vp_Input>();
		}

#endif

	}

		
	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{
	
		if(m_Instance == null)
			m_Instance = Instance;

	}


	/// <summary>
	/// Makes this instance dirty
	/// </summary>
	public virtual void SetDirty( bool dirty )
	{
		mIsDirty = dirty;
	}
	
	
	/// <summary>
	/// Setups the defaults input buttons and axes
	/// </summary>
	public virtual void SetupDefaults( string type = "" )
	{

		if(type == "" || type == "Buttons")
		{
			if (ButtonKeys.Count == 0)
			{
				AddButton("Attack", KeyCode.Mouse0);
				AddButton("SetNextWeapon", KeyCode.E);
				AddButton("SetPrevWeapon", KeyCode.Q);
				AddButton("ClearWeapon", KeyCode.Backspace);
				AddButton("Zoom", KeyCode.Mouse1);
				AddButton("Reload", KeyCode.R);
				AddButton("Jump", KeyCode.Space);
				AddButton("Crouch", KeyCode.C);
				AddButton("Run", KeyCode.LeftShift);
				AddButton("Interact", KeyCode.F);
				AddButton("Accept1", KeyCode.Return);
				AddButton("Accept2", KeyCode.KeypadEnter);
				AddButton("Pause", KeyCode.P);
				AddButton("Menu", KeyCode.Escape);
				AddButton("Toggle3rdPerson", KeyCode.V);
				AddButton("ScoreBoard", KeyCode.Tab);
				AddButton("SetWeapon1", KeyCode.Alpha1);
				AddButton("SetWeapon2", KeyCode.Alpha2);
				AddButton("SetWeapon3", KeyCode.Alpha3);
				AddButton("SetWeapon4", KeyCode.Alpha4);
				AddButton("SetWeapon5", KeyCode.Alpha5);
				AddButton("SetWeapon6", KeyCode.Alpha6);
				AddButton("SetWeapon7", KeyCode.Alpha7);
				AddButton("SetWeapon8", KeyCode.Alpha8);
				AddButton("SetWeapon9", KeyCode.Alpha9);
				AddButton("SetWeapon10", KeyCode.Alpha0);
				AddButton("Teleport", KeyCode.None);

				CreateMissingSecondaryButtons();
				
				// these defaults are set up for the Xbox gamepad on PC.
				// NOTE: the Xbox gamepad trigger buttons are not really buttons, but input
				// axes. the UFPS default project binds its second "Fire1" axis to 'joystick axis 3'
				// which corresponds to the right trigger. see the snippet in 'vp_FPInput -> InputAttack' to enable this

				AddSecondaryButton("Attack", KeyCode.JoystickButton5);			// right bumper
				AddSecondaryButton("SetNextWeapon", KeyCode.JoystickButton3);	// Y button
				AddSecondaryButton("SetPrevWeapon", KeyCode.None);
				AddSecondaryButton("ClearWeapon", KeyCode.None);
				AddSecondaryButton("Zoom", KeyCode.JoystickButton4);			// left bumper
				AddSecondaryButton("Reload", KeyCode.JoystickButton2);			// X button
				AddSecondaryButton("Jump", KeyCode.JoystickButton0);			// A button
				AddSecondaryButton("Crouch", KeyCode.JoystickButton1);			// B button
				AddSecondaryButton("Run", KeyCode.JoystickButton8);				// left analog stick pressed
				AddSecondaryButton("Interact", KeyCode.JoystickButton2);		// X button
				AddSecondaryButton("Accept1", KeyCode.None);
				AddSecondaryButton("Accept2", KeyCode.None);
				AddSecondaryButton("Pause", KeyCode.P);
				AddSecondaryButton("Menu", KeyCode.JoystickButton6);			// back button
				AddSecondaryButton("Toggle3rdPerson", KeyCode.None);
				AddSecondaryButton("ScoreBoard", KeyCode.None);
				AddSecondaryButton("SetWeapon1", KeyCode.None);
				AddSecondaryButton("SetWeapon2", KeyCode.None);
				AddSecondaryButton("SetWeapon3", KeyCode.None);
				AddSecondaryButton("SetWeapon4", KeyCode.None);
				AddSecondaryButton("SetWeapon5", KeyCode.None);
				AddSecondaryButton("SetWeapon6", KeyCode.None);
				AddSecondaryButton("SetWeapon7", KeyCode.None);
				AddSecondaryButton("SetWeapon8", KeyCode.None);
				AddSecondaryButton("SetWeapon9", KeyCode.None);
				AddSecondaryButton("SetWeapon10", KeyCode.None);
				AddSecondaryButton("Teleport", KeyCode.None);

			}
		}
		
		if(type == "" || type == "Axis")
		{
			if(AxisKeys.Count == 0)
			{
				AddAxis("Vertical", KeyCode.W, KeyCode.S);
				AddAxis("Horizontal", KeyCode.D, KeyCode.A);
			}
		}
		
		if(type == "" || type == "UnityAxis")
		{
			if(UnityAxis.Count == 0)
			{
				AddUnityAxis("Mouse X");
				AddUnityAxis("Mouse Y");
				AddUnityAxis("Horizontal");
				AddUnityAxis("Vertical");
				AddUnityAxis("LeftTrigger");
				AddUnityAxis("RightTrigger");
			}
		}
		
		UpdateDictionaries();
	
	}


	/// <summary>
	/// this method enforces the creation of one secondary button
	/// per every primary one
	/// </summary>
	public virtual void CreateMissingSecondaryButtons()
	{

		foreach (KeyValuePair<string, KeyCode> k in Buttons)
		{
			if (!Buttons2.ContainsKey(k.Key))
				AddSecondaryButton(k.Key, default(KeyCode));
		}

	}


	/// <summary>
	/// 
	/// </summary>
	bool HaveBinding(string button)
	{

		if (Buttons.ContainsKey(button))
			return true;

		if (Buttons2.ContainsKey(button))
			return true;

		Debug.LogError("Error (" + this + ") \"" + button + "\" is not declared in the UFPS Input Manager. You must add it from the 'UFPS -> Input Manager' editor menu for this button to work.");

		return false;

	}


	/// <summary>
	/// Adds a button with a specified keycode
	/// </summary>
	public virtual void AddButton( string n, KeyCode k = KeyCode.None )
	{
	
		if(ButtonKeys.Contains(n))
			ButtonValues[ButtonKeys.IndexOf(n)] = k;
		else
		{
			ButtonKeys.Add(n);
			ButtonValues.Add(k);
		}
	
	}


	/// <summary>
	/// Adds a secondary button with a specified keycode
	/// </summary>
	public virtual void AddSecondaryButton(string n, KeyCode k = KeyCode.None)
	{

		if (ButtonKeys.Contains(n))
		{
			try
			{
				ButtonValues2[ButtonKeys.IndexOf(n)] = k;
			}
			catch
			{
				ButtonValues2.Add(k);
			}
		}
		else
		{
			ButtonValues2.Add(k);
		}

	}

	
	/// <summary>
	/// Adds an axis with a positive and negative key
	/// </summary>
	public virtual void AddAxis( string n, KeyCode pk = KeyCode.None, KeyCode nk = KeyCode.None )
	{
	
		if(AxisKeys.Contains(n))
			AxisValues[AxisKeys.IndexOf(n)] = new vp_InputAxis{ Positive = pk, Negative = nk };
		else
		{
			AxisKeys.Add(n);
			AxisValues.Add(new vp_InputAxis{ Positive = pk, Negative = nk });
		}
	
	}
	
	
	/// <summary>
	/// Adds a unity axis.
	/// </summary>
	public virtual void AddUnityAxis( string n )
	{
	
		if(UnityAxis.Contains(n))
			UnityAxis[UnityAxis.IndexOf(n)] = n;
		else
		{
			UnityAxis.Add(n);
		}
	
	}
	
	
	/// <summary>
	/// Updates the input dictionaries
	/// </summary>
	public virtual void UpdateDictionaries()
	{
	
		if(!Application.isPlaying)
			return;

		Buttons.Clear();
		for (int i = 0; i < ButtonKeys.Count; i++)
		{
			if(!Buttons.ContainsKey(ButtonKeys[i]))
				Buttons.Add(ButtonKeys[i], ButtonValues[i]);
		}

		try     // handles a harmless case in the mobile add-on
		{
			Buttons2.Clear();
			for (int i = 0; i < ButtonKeys.Count; i++)
			{
				if (!Buttons2.ContainsKey(ButtonKeys[i]))
					Buttons2.Add(ButtonKeys[i], ButtonValues2[i]);
			}
		}
		catch
		{
		}

		Axis.Clear();
		for(int i=0;i<AxisKeys.Count;i++)
		{
			Axis.Add(AxisKeys[i], new vp_InputAxis{ Positive = AxisValues[i].Positive, Negative = AxisValues[i].Negative});
		}

		CreateMissingSecondaryButtons();

	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input for any button state
	/// </summary>
	public static bool GetButtonAny( string button )
	{
	
		return Instance.DoGetButtonAny( button );
	
	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input for any button state
	/// </summary>
	public virtual bool DoGetButtonAny( string button )
	{

		if (!HaveBinding(button))	// post an error if binding is not found
			return false;

		if (Input.GetKey(Buttons[button]) || Input.GetKeyDown(Buttons[button]) || Input.GetKeyUp(Buttons[button]))
			return true;	// button held, pressed or released as primary binding

		if (Input.GetKeyDown(Buttons2[button]) || Input.GetKeyDown(Buttons2[button]) || Input.GetKeyUp(Buttons2[button]))
			return true;	// button held, pressed or released as secondary binding

		return false;	// button has not been touched in any way

	
	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input while a button is held
	/// </summary>
	public static bool GetButton(string button)
	{

		return Instance.DoGetButton( button );
	
	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input while a button is held
	/// </summary>
	public virtual bool DoGetButton( string button )
	{

		if (!HaveBinding(button))	// post an error if binding is not found
			return false;

		if (Input.GetKey(Buttons[button]))
			return true;	// button held down as primary binding

		if (Input.GetKey(Buttons2[button]))
			return true;	// button held down as secondary binding

		return false;	// button not held down

	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input for a button down event
	/// </summary>
	public static bool GetButtonDown(string button)
	{
	
		return Instance.DoGetButtonDown( button );
	
	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input for a button down event
	/// </summary>
	public virtual bool DoGetButtonDown( string button )
	{

		if (!HaveBinding(button))	// post an error if binding is not found
			return false;

		if (Input.GetKeyDown(Buttons[button]))
			return true;	// button pressed as primary binding

		if (Input.GetKeyDown(Buttons2[button]))
			return true;	// button pressed as secondary binding

		return false;	// button not pressed
	
	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input when a button is released
	/// </summary>
	public static bool GetButtonUp(string button)
	{
	
		return Instance.DoGetButtonUp( button );
	
	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input when a button is released
	/// </summary>
	public virtual bool DoGetButtonUp( string button )
	{

		if (!HaveBinding(button))	// post an error if binding is not found
			return false;

		if (Input.GetKeyUp(Buttons[button]))
			return true;	// button released as primary binding

		if (Input.GetKeyUp(Buttons2[button]))
			return true;	// button released as secondary binding

		return false;	// button not released
	
	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input for axes
	/// </summary>
	public static float GetAxisRaw(string axis)
	{
	
		return Instance.DoGetAxisRaw( axis );
	
	}
	
	
	/// <summary>
	/// handles keyboard, mouse and joystick input for axes
	/// </summary>
	public virtual float DoGetAxisRaw( string axis )
	{
	
		if(Axis.ContainsKey(axis) && ControlType == 0)
		{
			float val = 0;
			if( Input.GetKey( Axis[axis].Positive ) )
				val = 1;
			if( Input.GetKey( Axis[axis].Negative ) )
				val = -1;
			return val;
		}
		else if(UnityAxis.Contains(axis))
		{
			return Input.GetAxisRaw(axis);
		}
		else
		{
			Debug.LogError("Error ("+this+") \"" + axis + "\" is not declared in the UFPS Input Manager. You must add it from the 'UFPS -> Input Manager' editor menu for this axis to work.");
			return 0;
		}
	
	}
	

	/// <summary>
	/// Changes the key for a button. If save == true the key for
	/// that button will be saved for next runtime. Example usage:
	/// vp_Input.ChangeButtonKey("Jump", KeyCode.G);
	/// </summary>
	public static void ChangeButtonKey( string button, KeyCode keyCode, bool save = false )
	{

		if (Instance.Buttons.ContainsKey(button))
		{
			if (save)
				Instance.ButtonValues[vp_Input.Instance.ButtonKeys.IndexOf(button)] = keyCode;
			Instance.Buttons[button] = keyCode;
			return;
		}
		
		if (Instance.Buttons2.ContainsKey(button))
		{
			if (save)
				Instance.ButtonValues2[vp_Input.Instance.ButtonKeys.IndexOf(button)] = keyCode;
			Instance.Buttons2[button] = keyCode;
			return;
		}

		// will post an error message saying button doesn't exist
		Instance.HaveBinding(button);

	}


	/// <summary>
	/// Changes an input axis. If save == true the axis will be saved
	/// for next runtime
	/// </summary>
	public static void ChangeAxis(string n, KeyCode pk = KeyCode.None, KeyCode nk = KeyCode.None, bool save = false)
	{

		if (!Instance.AxisKeys.Contains(n))
		{
			Debug.LogWarning("The Axis \"" + n + "\" Doesn't Exist");
			return;
		}

		if (save)
			Instance.AxisValues[vp_Input.Instance.AxisKeys.IndexOf(n)] = new vp_InputAxis { Positive = pk, Negative = nk };

		Instance.Axis[n] = new vp_InputAxis { Positive = pk, Negative = nk };

	}


	/// <summary>
	/// 
	/// </summary>
	void DebugDumpCollections()
	{

		string buttonlist = "\n\n---- BUTTON KEYS: ---- \n";
		string keyCodelist = "\n\n---- BUTTON VALUES: ---- \n";
		string keyCodelist2 = "\n\n---- BUTTON VALUES 2: ---- \n";

		foreach (string s in ButtonKeys)
		{
			buttonlist += ("\t\t" + s + "\n");
		}

		foreach (KeyCode k in ButtonValues)
		{
			keyCodelist += ("\t\t" + k + "\n");
		}

		foreach (KeyCode k in ButtonValues2)
		{
			keyCodelist2 += ("\t\t" + k + "\n");
		}

		Debug.Log("-------- DUMP --------\n" + buttonlist + keyCodelist + keyCodelist2);

	}


}
