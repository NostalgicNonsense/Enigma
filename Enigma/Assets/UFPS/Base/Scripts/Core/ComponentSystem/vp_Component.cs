/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Component.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	base class for extended monobehaviours. these components have
//					the added functionality of support for the UFPS component and
//					event systems, an 'Init' method that gets executed once after
//					all 'Start' calls, and caching of gameobject properties and
//					startup gameobject hierarchy
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;
using System;

public class vp_Component : MonoBehaviour
{

	public bool Persist = false;
	protected vp_StateManager m_StateManager = null;

	protected vp_EventHandler m_EventHandler = null;
	public vp_EventHandler EventHandler
	{
		get
		{
			if (m_EventHandler == null)
				m_EventHandler = (vp_EventHandler)Transform.root.GetComponentInChildren(typeof(vp_EventHandler));
			return m_EventHandler;
		}
	}

	// NOTE: made non-serialized to prevent serialization depth error on Unity 4.5
	[System.NonSerialized]
	protected vp_State m_DefaultState = null;

	protected bool m_Initialized = false;

	protected Transform m_Transform = null;
	protected Transform m_Parent = null;
	protected Transform m_Root = null;
	protected AudioSource m_Audio = null;
	protected Collider m_Collider = null;
	protected Camera m_Camera = null;

	public List<vp_State> States = new List<vp_State>();				// list of state presets for this vp_Component
	public List<vp_Component> Children = new List<vp_Component>();		// list of  child vp_Components
	public List<vp_Component> Siblings = new List<vp_Component>();		// list of vp_Components sharing the same gameobject
	public List<vp_Component> Family = new List<vp_Component>();		// list of vp_Components sharing the same root object
	public List<Renderer> Renderers = new List<Renderer>();				// list of Renderers in this and all child vp_Components
	public List<AudioSource> AudioSources = new List<AudioSource>();	// list of AudioSources in this and all child vp_Components

	protected Type m_Type = null;
	protected System.Reflection.FieldInfo[] m_Fields = null;

	public Type Type
	{
		get
		{
			if (m_Type == null)
				m_Type = GetType();
			return m_Type;
		}
	}

	public System.Reflection.FieldInfo[] Fields
	{
		get
		{
			if (m_Fields == null)
				m_Fields = Type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			return m_Fields;
		}
	}

	protected vp_Timer.Handle m_DeactivationTimer = new vp_Timer.Handle();

#if UNITY_EDITOR
	// initial state is needed because we refresh default state upon
	// inspector changes, and this will mess with our ability to save
	// difference presets (save tweaks) by breaking the compare values.
	// on the other hand we need to be able to refresh default state in
	// order not to loose inspector changes (i.e. if we accidentally
	// press zoom or crouch when tweaking components in the inspector)
	protected vp_State m_InitialState = null;		// used at editor runtime only
	public vp_State InitialState { get { return m_InitialState; } }
#endif

	public vp_StateManager StateManager
	{
		get
		{
			if(m_StateManager == null)
				m_StateManager = new vp_StateManager(this, States);
			return m_StateManager;
		}
	}
	public vp_State DefaultState { get { return m_DefaultState; } }

	// an alternative delta time measurement where 'delta 1' corresponds
	// to '60 FPS' NOTE: you may want to alter this depending on your
	// application target framerate or other preferences
	public float Delta { get { return (Time.deltaTime * 60.0f); } }
	public float SDelta { get { return (Time.smoothDeltaTime * 60.0f); } }

	public Transform Transform
	{
		get
		{
			if(m_Transform == null)
				m_Transform = transform;
			return m_Transform;
		}
	}

	public Transform Parent
	{
		get
		{
			if (m_Parent == null)
				m_Parent = transform.parent;
			return m_Parent;
		}
	}

	public Transform Root
	{
		get
		{
			if (m_Root == null)
				m_Root = transform.root;
			return m_Root;
		}
	}

	public AudioSource Audio
	{
		get
		{
			if (m_Audio == null)
				m_Audio = GetComponent<AudioSource>();
			return m_Audio;
		}
	}

	public Collider Collider
	{
		get
		{
			if (m_Collider == null)
				m_Collider = GetComponent<Collider>();
			return m_Collider;
		}
	}
	
	public Camera Camera
	{
		get
		{
			if (m_Camera == null)
				m_Camera = GetComponent<Camera>();
			return m_Camera;
		}
	}


	/// <summary>
	/// returns true if the first found renderer in the gameobject
	/// or any of its children is enabled. when set, sets all
	/// renderers in the gameobject and its childen active.
	/// </summary>
	public bool Rendering
	{

		get
		{

			// NOTE: only returns status of first found renderer in order to
			// be very lightweight
			return (Renderers.Count > 0) ? Renderers[0].enabled : false;

		}

		set
		{
			// sets all found renderers true or false
			foreach (Renderer r in Renderers)
			{
				if (r == null)
					continue;
				r.enabled = value;
			}
		}

	}
			

	/// <summary>
	/// in 'Awake' we do things that need to be run once at the
	/// very beginning. NOTE: 1) this method must be run using
	/// 'base.Awake();' on the first line of the 'Awake' method
	/// in any derived class. 2) keep in mind that as of Unity 4,
	/// gameobject hierarchy can not be altered in 'Awake'
	/// </summary>
	protected virtual void Awake()
	{

		CacheChildren();
		CacheSiblings();
		CacheFamily();
		CacheRenderers();
		CacheAudioSources();

		StateManager.SetState("Default", enabled);

	}


	/// <summary>
	/// in 'Start' we do things that need to be run once at the
	/// beginning, but potentially depend on all other scripts
	/// first having run their 'Awake' calls.
	/// NOTE: 1) don't do anything here that depends on activity
	/// in other 'Start' calls. 2) if adding code here, remember
	/// to call it using 'base.Start();' on the first line of
	/// the 'Start' method in the derived classes
	/// </summary>
	protected virtual void Start()
	{

		ResetState();

	}


	/// <summary>
	/// in 'Init' we do things that must be run once at the
	/// beginning - but only after all other components have
	/// run their 'Start' calls. this method is called once
	/// in the first 'Update'. NOTES: 1) unlike the standard Unity
	/// methods, this is a virtual method. 2) if adding code here,
	/// remember to call it using 'base.Init();' on the first
	/// line of the 'Init' method in any derived classes
	/// </summary>
	protected virtual void Init()
	{
	}


	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected virtual void OnEnable()
	{

		if (EventHandler != null)
			EventHandler.Register(this);

	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected virtual void OnDisable()
	{

		if (EventHandler != null)
			EventHandler.Unregister(this);

	}


	/// <summary>
	/// NOTE: to provide the 'Init' functionality, this method
	/// must be called using 'base.Update();' on the first line
	/// of the 'Update' method in the derived class
	/// </summary>
	protected virtual void Update()
	{

		// TODO: remove for performance (instead do this on next frame using coroutine)
		// initialize if needed. NOTE: this will run the inherited
		// 'Init' method (and if non-present: the one above)
		if (!m_Initialized)
		{
			Init();
			m_Initialized = true;
		}

	}


	/// <summary>
	/// TODO: remove for performance
	/// </summary>
	protected virtual void FixedUpdate()
	{
	}

	
	/// <summary>
	/// TODO: remove for performance
	/// </summary>
	protected virtual void LateUpdate()
	{
	}
	

	/// <summary>
	/// sets 'state' true / false on the component and refreshes it.
	/// NOTE: by default only affects active and enabled components
	/// </summary>
	public void SetState(string state, bool enabled = true, bool recursive = false, bool includeDisabled = false)
	{

		if (StateManager == null)
			return;

		if (state == null)
			return;

		StateManager.SetState(state, enabled);

		// scan the underlying hierarchy for vp_Components and
		// set 'state' on every object found
		if (recursive)
		{
			for (int c = 0; c < Children.Count; c++)
			{
				if (Children[c] == null)
					continue;
				if ((!includeDisabled) && !(vp_Utility.IsActive(Children[c].gameObject) && Children[c].enabled))
					continue;

				//Debug.Log("\t\t\t\t\tsetting '" + state + "' of '" + Children[c].name + "->" + Children[c].GetType() + "' to: " + enabled);

				Children[c].SetState(state, enabled, true, includeDisabled);

			}
		}

	}


	/// <summary>
	/// runs the overridable 'Activate' method on this
	/// vp_Component and all vp_Components on the same gameobject.
	/// if the optional input parameter is false, instead runs
	/// 'DeactivateWhenSilent'.
	/// </summary>
	public void ActivateGameObject(bool setActive = true)
	{

		if (setActive)
		{

			Activate();

			foreach (vp_Component s in Siblings)
			{
				s.Activate();
			}

			VerifyRenderers();

		}
		else
		{

			DeactivateWhenSilent();

			foreach (vp_Component s in Siblings)
			{
				s.DeactivateWhenSilent();
			}

		}


	}



	/// <summary>
	/// asks statemanager to disable all states except the default
	/// state, and enables the default state. then refreshes.
	/// </summary>
	public void ResetState()
	{

		StateManager.Reset();
		Refresh();

	}


	/// <summary>
	/// returns true if the state associated with the passed
	/// string is on the list and enabled, otherwise returns null
	/// </summary>
	public bool StateEnabled(string stateName)
	{

		return StateManager.IsEnabled(stateName);

	}


	/// <summary>
	/// sees if the component has a default state. if so, makes
	/// sure it's in index zero of the list, if not, creates it.
	/// </summary>
	public void RefreshDefaultState()
	{

		vp_State defaultState = null;

		if (States.Count == 0)
		{
			// there are no states, so create default state
			defaultState = new vp_State(Type.Name, "Default", null);
			States.Add(defaultState);
		}
		else
		{
			for (int v = States.Count - 1; v > -1; v--)
			{
				if (States[v].Name == "Default")
				{
					// found default state, make sure it's in the back
					defaultState = States[v];
					States.Remove(defaultState);
					States.Add(defaultState);
				}
			}
			if (defaultState == null)
			{
				// there are states, but no default state so create it
				defaultState = new vp_State(Type.Name, "Default", null);
				States.Add(defaultState);
			}
		}

		if (defaultState.Preset == null || defaultState.Preset.ComponentType == null)
			defaultState.Preset = new vp_ComponentPreset();

		if(defaultState.TextAsset == null)
			defaultState.Preset.InitFromComponent(this);

		defaultState.Enabled = true;	// default state is always enabled

		m_DefaultState = defaultState;

	}


	/// <summary>
	/// copies component values into the default state's preset.
	/// if needed, creates and adds default state to the state list.
	/// to be called on app startup and statemanager recombine
	/// </summary>
#if UNITY_EDITOR
	public void RefreshInitialState()
	{

		m_InitialState = null;
		m_InitialState = new vp_State(Type.Name, "Internal_Initial", null);
		m_InitialState.Preset = new vp_ComponentPreset();
		m_InitialState.Preset.InitFromComponent(this);

	}
#endif
	

	/// <summary>
	/// helper method to apply a preset from memory and refresh
	/// settings
	/// </summary>
	public void ApplyPreset(vp_ComponentPreset preset)
	{
		vp_ComponentPreset.Apply(this, preset);
		RefreshDefaultState();
		Refresh();
	}


	/// <summary>
	/// helper method to load a preset from the resources folder,
	/// and refresh settings
	/// </summary>
	public vp_ComponentPreset Load(string path)
	{
		vp_ComponentPreset preset = vp_ComponentPreset.LoadFromResources(this, path);
		RefreshDefaultState();
		Refresh();
		return preset;
	}


	/// <summary>
	/// helper method to load a preset from a text asset,
	/// and refresh settings
	/// </summary>
	public vp_ComponentPreset Load(TextAsset asset)
	{
		vp_ComponentPreset preset = vp_ComponentPreset.LoadFromTextAsset(this, asset);
		RefreshDefaultState();
		Refresh();
		return preset;
	}


	/// <summary>
	/// inits the 'Children' list will all child vp_Components
	/// </summary>
	public void CacheChildren()
	{

		Children.Clear();
		foreach (vp_Component c in GetComponentsInChildren<vp_Component>(true))
		{
			if (c.transform.parent == transform)
				Children.Add(c);
		}

	}


	/// <summary>
	/// inits the 'Siblings' list will all sibling vp_Components
	/// </summary>
	public void CacheSiblings()
	{

		Siblings.Clear();
		foreach (vp_Component c in GetComponents<vp_Component>())
		{
			if (c != this)
				Siblings.Add(c);
		}

	}


	/// <summary>
	/// inits the 'Family' list will all vp_Components existing
	/// in the same transform hierarchy as this, excluding this
	/// </summary>
	public void CacheFamily()
	{

		Family.Clear();
		foreach (vp_Component c in transform.root.GetComponentsInChildren<vp_Component>(true))
		{
			if (c != this)
				Family.Add(c);
		}

	}


	/// <summary>
	/// inits the 'Renderers' list with all Renderers existing
	/// in this plus all child vp_Components
	/// </summary>
	public void CacheRenderers()
	{

		Renderers.Clear();
		foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
		{
			Renderers.Add(r);
		}

	}


	/// <summary>
	/// makes sure that the main cached renderer still resides within
	/// the same hierarchy as this component. if not, re-caches all
	/// </summary>
	protected void VerifyRenderers()
	{

		if (Renderers.Count == 0)
			return;

		if ((Renderers[0] == null) || !vp_Utility.IsDescendant(Renderers[0].transform, Transform))
		{
			Renderers.Clear();
			CacheRenderers();
		}
	
	}


	/// <summary>
	/// inits the 'AudioSources' list will all AudioSources
	/// existing in this plus all child vp_Components
	/// </summary>
	public void CacheAudioSources()
	{

		AudioSources.Clear();
		foreach (AudioSource r in GetComponentsInChildren<AudioSource>(true))
		{
			AudioSources.Add(r);
		}

	}


	/// <summary>
	/// activates the gameobject containing this vp_Component.
	/// a virtual activation method provided for classes
	/// inheriting vp_Component to add additional activation
	/// routines
	/// </summary>
	public virtual void Activate()
	{

		m_DeactivationTimer.Cancel();
		vp_Utility.Activate(gameObject);
		//ActivateInternal(gameObject);		// TEMP: reverted due to weapons going invisible in 3rd person

	}


	/// <summary>
	/// deactivates the gameobject containing this vp_Component.
	/// a virtual deactivation method provided for classes
	/// inheriting vp_Component to add additional deactivation
	/// routines
	/// </summary>
	public virtual void Deactivate()
	{

		vp_Utility.Activate(gameObject, false);
		//ActivateInternal(gameObject, false);	// TEMP: reverted due to weapons going invisible in 3rd person

	}


	/// <summary>
	/// activates or deativates component in a way that allows the
	/// 'DeactivateWhenSilent' feature to function properly
	/// </summary>
	private void ActivateInternal(GameObject obj, bool activate = true)
	{

		if (activate && obj.activeInHierarchy)
			obj.SetActive(false);

		obj.SetActive(activate);

	}


	/// <summary>
	/// the purpose of this method is to allow disabling of a
	/// gameobject without cutting off its currently playing sounds.
	/// if a sound is playing on its audio source the gameobject
	/// will be made invisible and deactivated properly only after
	/// the sound has stopped playing. if the gameobject isn't
	/// playing any sound it will be instantly deactivated
	/// </summary>
	public void DeactivateWhenSilent()
	{

		if (this == null)
			return;

		if (vp_Utility.IsActive(gameObject))
		{
			foreach (AudioSource a in AudioSources)
			{
				if (a.isPlaying && !a.loop)		// looping sounds will be cut off
				{

					// temporarily make gameobject invisible by disabling its renderers
					Rendering = false;

					// can't disable gameobject properly yet because an audio-
					// source is still playing, so retry in 0.1 seconds
					vp_Timer.In(0.1f, delegate()
					{
						DeactivateWhenSilent();
					}, m_DeactivationTimer);
					return;

				}
			}
		}

		// if we reach this far there are no audio sources playing on
		// the transform, so deactivate gameobject instantly
		Deactivate();

	}


	/// <summary>
	/// to be overridden in inherited classes, for resetting
	/// various important variables on the component
	/// </summary>
	public virtual void Refresh()
	{
	}


}


