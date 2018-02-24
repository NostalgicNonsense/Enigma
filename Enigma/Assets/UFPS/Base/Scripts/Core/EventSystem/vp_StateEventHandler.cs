/////////////////////////////////////////////////////////////////////////////////
//
//	vp_StateEventHandler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a version of vp_EventHandler that is aware of the vp_Component
//					state system and can bind its own actions to corresponding
//					states found on the components
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

public abstract class vp_StateEventHandler : vp_EventHandler
{

	List<vp_Component> m_StateTargets = new List<vp_Component>();
	List<vp_Activity> m_Activities = new List<vp_Activity>();


	/// <summary>
	/// 
	/// </summary>
	protected override void Awake()
	{

		base.Awake();

		StoreStateTargets();

		StoreActivities();

	}

	
	/// <summary>
	/// binds the event handler's activities to states with the
	/// same names found on components down the hierarchy
	/// </summary>
	protected void BindStateToActivity(vp_Activity a)
	{

		BindStateToActivityOnStart(a);
		BindStateToActivityOnStop(a);

	}


	/// <summary>
	/// binds the event handler's start activity to states with
	/// the same names found on components down the hierarchy
	/// </summary>
	protected void BindStateToActivityOnStart(vp_Activity a)
	{

		if (!ActivityInitialized(a))
			return;

		string s = a.EventName;

		a.StartCallbacks +=
		delegate()
		{
			foreach (vp_Component c in m_StateTargets)
			{
				c.SetState(s, true, true);
			}
		};
		// NOTE: this delegate won't show up in an event dump

	}


	/// <summary>
	/// binds the event handler's stop activity to states with
	/// the same names found on components down the hierarchy
	/// </summary>
	protected void BindStateToActivityOnStop(vp_Activity a)
	{

		if (!ActivityInitialized(a))
			return;

		string s = a.EventName;

		a.StopCallbacks +=
		delegate()
		{
			foreach (vp_Component c in m_StateTargets)
			{
				c.SetState(s, false, true);
			}
		};
		// NOTE: this delegate won't show up in an event dump

	}


	/// <summary>
	/// fetches all vp_Components that top their own hierarchy in the same
	/// hierarchy as the event handler. these will be used to block states
	/// recursively (down the hierarchy)
	/// </summary>
	protected void StoreStateTargets()
	{

		foreach (vp_Component c in transform.root.GetComponentsInChildren<vp_Component>(true))
		{
			if (c.Parent == null
				|| (c.Parent.GetComponent<vp_Component>() == null)
				)
			{
				m_StateTargets.Add(c);
			}
		}

	}


	/// <summary>
	/// stores all the known events of type vp_Activity in a list for
	/// quick iteration later
	/// </summary>
	protected void StoreActivities()
	{

		for (int e = 0; e < m_Events.Count; e++)
		{
			if ((m_Events[e] is vp_Activity) || (m_Events[e].Type.BaseType == typeof(vp_Activity)))
				m_Activities.Add(m_Events[e] as vp_Activity);
		}

		//Debug.Log("found: " + m_Activities.Count + " activities");

	}


	/// <summary>
	/// refreshes all component states bound to this event handler's
	/// activities
	/// </summary>
	public void RefreshActivityStates()
	{

		// NOTE: we must keep track of the top target transforms we have already
		// iterated recursively in order to avoid iterating components on child
		// transforms once for every component on the top transform
		Dictionary<Transform, bool> alreadyRecursedTargets = new Dictionary<Transform, bool>();

		for (int a = 0; a < m_Activities.Count; a++)
		{
			alreadyRecursedTargets.Clear();
			for (int t = 0; t < m_StateTargets.Count; t++)
			{
				bool alreadyRecursed = alreadyRecursedTargets.ContainsKey(m_StateTargets[t].transform);
				//Debug.Log("\t\t\t\t---------------- " + c.GetType() + ", " + activity.EventName.ToUpper() + " ----------------");
				m_StateTargets[t].SetState(m_Activities[a].EventName, m_Activities[a].Active, !alreadyRecursed, false);
				if (!alreadyRecursed)
					alreadyRecursedTargets.Add(m_StateTargets[t].transform, true);
			}
		}


	}


	/// <summary>
	/// resets all component states bound to this event handler's
	/// activities
	/// </summary>
	public void ResetActivityStates()
	{

		foreach (vp_Component c in m_StateTargets)
		{
			c.ResetState();
		}

	}


	/// <summary>
	/// sets a state on all components bound to this event handler's activities.
	/// NOTE: for forcing states, instead of vp_Activity -> 'TryStart' and 'TryStop',
	/// you would typically instead use vp_Activity -> 'Start' and 'Stop'. this method
	/// is just an optional way of forcing a state onto components without involving
	/// the player activity event. can be used for cutscenes, for freezing the player,
	/// and for other systems that must automatically force a player state on or off
	/// </summary>
	public void SetState(string state, bool setActive = true, bool recursive = true, bool includeDisabled = false)
	{

		foreach (vp_Component c in m_StateTargets)
		{
			c.SetState(state, setActive, recursive, includeDisabled);
		}

	}


	/// <summary>
	/// returns true if the passed activity has been initialized
	/// yet, false if not
	/// </summary>
	private bool ActivityInitialized(vp_Activity a)
	{

		if (a == null)
		{
			Debug.LogError("Error: (" + this + ") Activity is null.");
			return false;
		}

		if (string.IsNullOrEmpty(a.EventName))
		{
			Debug.LogError("Error: (" + this + ") Activity not initialized. Make sure the event handler has run its Awake call before binding layers.");
			return false;
		}

		return true;

	}

}

