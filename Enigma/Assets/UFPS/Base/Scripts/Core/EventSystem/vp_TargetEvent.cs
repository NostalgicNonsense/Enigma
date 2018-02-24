/////////////////////////////////////////////////////////////////////////////////
//
//	vp_TargetEvent.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this class allows the sending of generic events to a single, specific
//					object. events can have 0-3 arguments and a return value.
//
//					EXAMPLE SYNTAX:
//
//					registering an event 'Hello', with 'MyMethod' in 'this' object:
//						vp_TargetEvent.Register(this, "Hello", MyMethod);				// no arguments, no return value
//						vp_TargetEvent<int, string>.Register(this, "Hello", MyMethod);	// 2 arguments, no return value
//						vp_TargetEventReturn<int, string, float, bool>.Register(this, "Hello", MyMethod);	// 3 arguments + return value
//
//					triggering the event from another object:
//						vp_TargetEvent.Send(someObject, "Hello");						// no arguments, no return value
//						vp_TargetEvent<int, string>.Send(someObject, "Hello", 242, "blabla");	// 2 arguments, no return value
//						bool result = vp_TargetEventReturn<int, bool>.Send(someObject, "Hello", 242);	// 1 argument + return value
//
//					FURTHER NOTES:
//
//					1) the 'Send' method can target any object regardless of type. the 'SendUpwards'
//					method can target any object of type 'UnityEngine.Component' and will scan for
//					a matching event name registered with the component, its parent transform or any
//					ancestor transform. as an example this allows you to send an event to the collider
//					of a gameobject and trigger a method registered with its root transform.
//
//					2) 'vp_GlobalEventHandler.UnregisterAll' will unregister all the events registered
//					using this system (which might be useful when loading a new level).
//				
//					3) in order to support muting events on disabled components, you would typically
//					unregister them in the component's "OnDisable" method, and re-register them in the
//					"OnEnable" method (for performance reasons the target event handler does not check
//					whether an object is a component or whether it is enabled or disabled: this means
//					that disabled components and inactive gameobjects will receive and act on events
//					by default). 
//
//					4) setting the optional 'vp_TargetEventOptions' parameter to 'RequireReceiver'
//					can be used to make error detection stricter. this is useful for debugging.
//
//					5) if you register more than one event with the same name and identical arguments
//					+ return value, only the first registered method will trigger upon invocation
//
//					6) 'Send' will allocate ~1 Kb the first time around but should not feed the
//					garbage collector upon further calls (unless a target method feeds it, that is).
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;
using System;



public enum vp_TargetEventOptions
{
	DontRequireReceiver = 1,
	RequireReceiver = 2,
	IncludeInactive = 4
}

internal static class vp_TargetEventHandler
{

	// this list contains one dictionary with event names and delegates for each
	// object registered with - and each signature supported by - the system
	static List<Dictionary<object, Dictionary<string, Delegate>>> m_TargetDict = null;
	static List<Dictionary<object, Dictionary<string, Delegate>>> TargetDict
	{
		get
		{
			if (m_TargetDict == null)
			{
				m_TargetDict = new List<Dictionary<object, Dictionary<string, Delegate>>>(100);
				for (int v = 0; v < 8; v++)
				{
					m_TargetDict.Add(new Dictionary<object, Dictionary<string, Delegate>>(100));
				}
			}
			return m_TargetDict;
		}
	}

	// supported signatures by index in the list
	// 0	no arguments
	// 1	1 generic argument
	// 2	2 generic arguments
	// 3	3 generic arguments
	// 4	no arguments + return value
	// 5	1 generic argument + return value
	// 6	2 arguments + return value
	// 7	3 arguments + return value


	/// <summary>
	/// NOTE: this is an internal method. to register target events, instead
	/// use 'vp_TargetEvent.Register' or 'vp_TargetEventReturn.Register'.
	/// (this method adds the passed delegate to the dictionary of the target
	/// object, using the event name as key.)
	/// </summary>
	public static void Register(object target, string eventName, Delegate callback, int dictionary)
	{

		// return if we have insufficient or bad data
		if (target == null) { Debug.LogWarning("Warning: (" + vp_Utility.GetErrorLocation(2) + " -> vp_TargetEvent.Register) Target object is null."); return; }
		if (string.IsNullOrEmpty(eventName)) { Debug.LogWarning("Warning: (" + vp_Utility.GetErrorLocation(2) + " -> vp_TargetEvent.Register) Event name is null or empty."); return; }
		if (callback == null) { Debug.LogWarning("Warning: (" + vp_Utility.GetErrorLocation(2) + " -> vp_TargetEvent.Register) Callback is null."); return; }
		if (callback.Method.Name.StartsWith("<")) { Debug.LogWarning("Warning: (" + vp_Utility.GetErrorLocation(2) + " -> vp_TargetEvent.Register) Target events can only be registered to declared methods."); return; }

		// add target object to the appropriate target dictionary if needed
		if (!TargetDict[dictionary].ContainsKey(target))
			TargetDict[dictionary].Add(target, new Dictionary<string, Delegate>(100));

		// retrieve the event callback dictionary for the target object
		Dictionary<string, Delegate> callbacks;
		TargetDict[dictionary].TryGetValue(target, out callbacks);

		// append underscores to subsequent identical event names so they
		// can be added to the dictionary (this allows for storing events
		// with the same names but different signatures)
	retry:

		Delegate existingCallback;
		callbacks.TryGetValue(eventName, out existingCallback);

		if (existingCallback == null)
		{
			// store an event for the eventName + callback
			callbacks.Add(eventName, callback);
			return;
		}

		//Debug.Log(
		//    "have an existing callback for " + eventName +
		//    ".\nyou tried to add type " + callback.GetType() +
		//    "\nexisting type is: " + existingCallback.GetType() +
		//    "\nthis means they are " + ((callback.GetType() == existingCallback.GetType()) ? "EQUAL" : "NOT EQUAL"));
		
		// have an existing callback. if parameters differ from 'callback',
		// retry adding this callback under a modified name
		if (existingCallback.GetType() != callback.GetType())
		{
			eventName += "_";
			goto retry;
		}
		else
		{
			// callback and existing callback have the same parameter types
			// so combine them and refresh event
			callback = Delegate.Combine(existingCallback, callback);
			if (callback != null)
			{
				callbacks.Remove(eventName);
				callbacks.Add(eventName, callback);
			}
		}

	}


	/// <summary>
	/// NOTE: this is an internal method. to unregister specific target
	/// events, use 'vp_TargetEvent&lt&gt.Unregister' or
	/// 'vp_TargetEventReturn&lt&gt.Unregister'. To unregister all events,
	/// use 'vp_TargetEventHandler.UnregisterAll'. (this method unregisters
	/// an event by target object, event name and callback)
	/// </summary>
	public static void Unregister(object target, string eventName = null, Delegate callback = null)
	{

		if ((eventName == null) && (callback != null))
			return;

		if ((callback == null) && (eventName != null))
			return;

		// iterate through the 8 lists of possible delegate signatures and
		// unregister delegates associated with 'target' and 'eventName'
		for (int v = 0; v < 8; v++)
		{
			Unregister(target, v, eventName, callback);
		}

	}


	/// <summary>
	/// unregisters all events targeting a specific component,
	/// also if the events were registered with the component's
	/// transform.
	/// </summary>
	public static void Unregister(Component component)
	{

		if (component == null)
			return;

		// iterate through the 8 lists of possible delegate signatures and
		// unregister delegates associated with 'target' and 'eventName'
		for (int v = 0; v < 8; v++)
		{
			Unregister(v, component);
		}

	}


	/// <summary>
	/// unregisters all events targeting a specific component,
	/// also if the events were registered with the component's
	/// transform.
	/// </summary>
	private static void Unregister(int dictionary, Component component)
	{

		if (component == null)
			return;

		Dictionary<string, Delegate> callbacks;
		if (TargetDict[dictionary].TryGetValue(component, out callbacks))
			TargetDict[dictionary].Remove(component);

		object transform = component.transform;
		if (transform == null)
			return;

		if (!TargetDict[dictionary].TryGetValue(transform, out callbacks))
			return;

		List<string> eventNames = new List<string>(callbacks.Keys);
		foreach (string eventName in eventNames)
		{
			Delegate del;

			if (eventName == null)
				continue;

			if (!callbacks.TryGetValue(eventName, out del))
				continue;

			if (del == null)
				continue;

			Delegate[] invList = del.GetInvocationList();

			if ((invList == null) || (invList.Length < 1))
				continue;

			for (int v = invList.Length - 1; v > -1; v--)
			{
				if ((Component)invList[v].Target == component)
				{

					// unregister the whole event
					callbacks.Remove(eventName);

					// remove the specific 'callback' from retrieved event delegates
					Delegate callback = Delegate.Remove(del, invList[v]);

					// re-register event if delegate invocation list wasn't emptied
					if (callback.GetInvocationList().Length > 0)
						callbacks.Add(eventName, callback);

				}
			}
		}

	}


	/// <summary>
	/// this method unregisters a whole target object, or optionally a
	/// single event by name in the target.
	/// </summary>
	private static void Unregister(object target, int dictionary, string eventName, Delegate callback)
	{

		if (target == null)
			return;

		// verify that target dictionary has any events in it
		Dictionary<string, Delegate> callbacks;
		if (!TargetDict[dictionary].TryGetValue(target, out callbacks) ||
			(callbacks == null) ||
			(callbacks.Count == 0))
			return;

		if (eventName == null && callback == null)
		{
			TargetDict[dictionary].Remove(callbacks);
			return;
		}

		// retrieve delegates registered under 'eventName'
		Delegate delegates;
		if (callbacks.TryGetValue(eventName, out delegates))
		{

			if (delegates != null)
			{

				// unregister event
				callbacks.Remove(eventName);

				// remove the specific 'callback' from retrieved event delegates
				delegates = Delegate.Remove(delegates, callback);

				// re-register event if delegate invocation list wasn't emptied
				if (delegates != null && delegates.GetInvocationList() != null)
					callbacks.Add(eventName, delegates);

			}
			else
				callbacks.Remove(eventName);

			// if more events remain, return. but if none remains:
			// allow execution to continue to remove the whole event
			if (callbacks.Count > 0)
				return;
		}
		else
			return;

		// unregister the whole object
		TargetDict[dictionary].Remove(target);

	}


	/// <summary>
	/// removes all objects and callbacks. NOTE: you may or may not
	/// want to call this upon level load
	/// </summary>
	public static void UnregisterAll()
	{

		// this will cause the 'TargetDict' property to create new,
		// empty dictionaries the next time it is fetched
		m_TargetDict = null;

	}


	/// <summary>
	/// NOTE: this is an internal method. to register target events,
	/// instead use 'vp_TargetEvent.Register' or 'vp_TargetEventReturn.Register'.
	/// (this method looks for an event name registered to the target object and
	/// - if found - returns the delegate.)
	/// </summary>
	public static Delegate GetCallback(object target, string eventName, bool upwards, int d, vp_TargetEventOptions options)
	{
#if UNITY_EDITOR
		if (string.IsNullOrEmpty(eventName))
		{
			Debug.LogError("Error: (" + vp_Utility.GetErrorLocation(2) + ") vp_TargetEvent.Send: Name is null or empty.");
			return null;
		}
#endif

		if (target == null)
			return null;

		if (string.IsNullOrEmpty(eventName))
			return null;

	retry:

		Delegate callback = null;

		if (!((options & vp_TargetEventOptions.IncludeInactive) == vp_TargetEventOptions.IncludeInactive))
		{
			GameObject gameObject = target as GameObject;
			if (gameObject != null)
			{
				if (!vp_Utility.IsActive(gameObject))
				{
					if (upwards)
						goto recursive;
					return null;
				}
			}
			else
			{
				Behaviour component = target as Behaviour;
				if (component != null)
				{
					if (!component.enabled || !vp_Utility.IsActive(component.gameObject))
					{
						if (upwards)
							goto recursive;
						return null;
					}
				}
			}
		}

		// get list of callbacks for this object
		Dictionary<string, Delegate> callbacks = null;
		if (!TargetDict[d].TryGetValue(target, out callbacks))
		{
			if (upwards)
				goto recursive;
			return null;
		}

		// get specific callback from list of callbacks
		if (!callbacks.TryGetValue(eventName, out callback))
		{
			if (upwards)
				goto recursive;
			return null;
		}

	recursive:

		// if we are sending a message upwards, scan upwards recursively
		if ((callback == null) && upwards)
		{

			// try to find a transform or parent transform
			target = vp_Utility.GetParent(target as Component);

			// couldn't find a new target: our work here is done
			if (target == null)
				goto done;

			// found a new target: retry!
			goto retry;

		}

	done:

		return callback;

	}


	/// <summary>
	/// dumps an error to the console if 'options' is set to 'RequireReceiver'
	/// </summary>
	public static void OnNoReceiver(string eventName, vp_TargetEventOptions options)
	{
		if (!((options & vp_TargetEventOptions.RequireReceiver) == vp_TargetEventOptions.RequireReceiver))
			return;
		Debug.LogError("Error: (" + vp_Utility.GetErrorLocation(2) + ") vp_TargetEvent '" + eventName + "' has no receiver!");
	}


	/// <summary>
	/// returns a formatted string with all the currently registered
	/// events, for debug purposes. TIP: bind this to a key and dump
	/// all registered events to the console
	/// </summary>
	public static string Dump()
	{

		Dictionary<object, string> targets = new Dictionary<object, string>();

		foreach (Dictionary<object, Dictionary<string, Delegate>> o in TargetDict)
		{

			foreach (object oo in o.Keys)
			{
				string s = "";
				//s += /*"    Target: " +*/ oo.ToString() + "\n";
				if (oo == null)
					continue;
				Dictionary<string, Delegate> delegates;
				if (o.TryGetValue(oo, out delegates))
				{
					foreach (string eventName in delegates.Keys)
					{
						s += "        \"" + eventName + "\" -> ";
						Delegate d;
						bool many = false;
						if (string.IsNullOrEmpty(eventName))
							continue;
						if (delegates.TryGetValue(eventName, out d))
						{
							if (d.GetInvocationList().Length > 1)
							{
								many = true;
								s += "\n";
							}
							foreach (Delegate dd in d.GetInvocationList())
							{
								s += (many ? "                        " : "") + dd.Method.ReflectedType + ".cs -> ";
								string p = "";

								foreach (System.Reflection.ParameterInfo i in dd.Method.GetParameters())
								{
									p += vp_Utility.GetTypeAlias(i.ParameterType) + " " + i.Name + ", ";
								}
								if (p.Length > 0)
									p = p.Remove(p.LastIndexOf(", "));

								s += vp_Utility.GetTypeAlias(dd.Method.ReturnType) + " ";

								if (dd.Method.Name.Contains("m_"))
								{
									string m = dd.Method.Name.TrimStart('<');
									m = m.Remove(m.IndexOf('>'));
									s += m + " -> delegate";
								}
								else
									s += dd.Method.Name;
								s += "(" + p + ")\n";
							}
						}
					}
				}
				string t;

				if (!targets.TryGetValue(oo, out t))
					targets.Add(oo, s);
				else
				{
					targets.Remove(oo);
					targets.Add(oo, t + s);
				}
			}

		}

		string result = "--- TARGET EVENT DUMP ---\n\n";
		foreach (object ss in targets.Keys)
		{
			if (ss == null)
				continue;
			result += ss.ToString() + ":\n";
			string n;
			if (targets.TryGetValue(ss, out n))
				result += n;

		}

		return result;

	}


}


/// <summary>
/// targets a method by name in a specific object
/// (no arguments).
/// </summary>
public static class vp_TargetEvent
{

	/// <summary>
	/// registers a callback (method or delegate) with the target object.
	/// the callback can be triggered later by sending the event 'eventName'.
	/// </summary>
	public static void Register(object target, string eventName, Action callback)
	{
		vp_TargetEventHandler.Register(target, eventName, (Delegate)callback, 0);
	}


	/// <summary>
	/// removes a callback from the target event handler by target object and event
	/// name. if name is null, the whole object will be unregistered. NOTES: 1) this
	/// method will disable _all_ events with the specified name and target object,
	/// regardless of the generic signature used. 2) be careful when omitting
	/// 'eventName': if doing so in a component 'OnDisable' method, all events for
	/// all other components on the target will be unregistered.
	/// </summary>
	public static void Unregister(object target, string eventName, Action callback)
	{
		vp_TargetEventHandler.Unregister(target, eventName, callback);
	}
	public static void Unregister(object target)
	{
		vp_TargetEventHandler.Unregister(target, null, null);
	}

	public static void Unregister(Component component)
	{
		vp_TargetEventHandler.Unregister(component);
	}


	/// <summary>
	/// attempts to trigger the pre-registered method or delegate associated
	/// with 'eventName' and the target object. if 'options' is set to
	/// 'vp_TargetEventOptions.RequireReceiver' there will be an error if
	/// no such callback can be found.
	/// </summary>
	public static void Send(object target, string eventName, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, false, 0, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return;
		}

		// NOTE: if the below 'try' fails, it is likely because we have an event
		// with the same name and amount of arguments + return value as another
		// event, in which case the dictionary happily returned an event with an
		// incompatible parameter signature = BOOM. we catch this and try again
		// with an underscore appended to the event name. if 'Register' has added
		// a matching event with underscores in the name we'll find it sooner or
		// later. if not, 'callback' will end up null and we'll return gracefully
		// on the above null check (which is also what will happen if the invocation
		// crashes for any other reason).

		try { ((Action)callback).Invoke(); }
		catch { eventName += "_"; goto retry; }

	}


	/// <summary>
	/// This method will send an event to an object of type 'UnityEngine.Component',
	/// then onward to its transform and all ancestor transforms recursively until
	/// a callback registered under 'eventName' is found in any of the objects. NOTES:
	/// 1) only the first matching callback detected will execute. The event will fail
	/// if the scan reaches the scene root with no match found. 2) it is best to register
	/// _transforms_ and not other types of component for use with 'SendUpwards', since
	/// components will be ignored unless the event is sent specifically to the correct
	/// component.
	/// </summary>
	public static void SendUpwards(Component target, string eventName, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, true, 0, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return;
		}
		try { ((Action)callback).Invoke(); }
		catch { eventName += "_"; goto retry; }
	}

}


/// <summary>
/// targets a method by name in a specific object
/// (1 generic argument).
/// </summary>
public static class vp_TargetEvent<T>
{

	/// <summary>
	/// registers a callback (method or delegate) with the target object.
	/// the callback can be triggered later by sending the event 'eventName'.
	/// example signature: 'vp_TargetEvent&ltfloat&gt.Register'
	/// </summary>
	public static void Register(object target, string eventName, Action<T> callback)
	{
		vp_TargetEventHandler.Register(target, eventName, (Delegate)callback, 1);
	}


	/// <summary>
	/// removes a callback from the target event handler by target object and event
	/// name. if name is null, the whole object will be unregistered. NOTE: this
	/// method will disable _all_ events with the specified name and target object,
	/// regardless of the generic signature used.
	/// </summary>
	public static void Unregister(object target, string eventName, Action<T> callback)
	{
		vp_TargetEventHandler.Unregister(target, eventName, callback);
	}
	public static void Unregister(object target)
	{
		vp_TargetEventHandler.Unregister(target, null, null);
	}


	/// <summary>
	/// attempts to trigger the pre-registered method or delegate associated
	/// with 'eventName' and the target object. if 'options' is set to
	/// 'vp_TargetEventOptions.RequireReceiver' there will be an error if
	/// no such callback can be found.
	/// example signature: 'vp_TargetEvent&lt;int&gt;.Send'
	/// </summary>
	public static void Send(object target, string eventName, T arg, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, false, 1, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return;
		}
		try { ((Action<T>)callback).Invoke(arg); }
		catch { eventName += "_"; goto retry; }
	}


	/// <summary>
	/// This method will send an event to an object of type 'UnityEngine.Component',
	/// then onward to its transform and all ancestor transforms recursively until
	/// a callback registered under 'eventName' is found in any of the objects.
	/// example signature: 'vp_TargetEvent&lt;int&gt;.SendUpwards'. NOTES:
	/// 1) only the first matching callback detected will execute. The event will fail
	/// if the scan reaches the scene root with no match found. 2) it is best to register
	/// _transforms_ and not other types of component for use with 'SendUpwards', since
	/// components will be ignored unless the event is sent specifically to the
	/// correct component.
	/// </summary>
	public static void SendUpwards(Component target, string eventName, T arg, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, true, 1, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return;
		}
		try { ((Action<T>)callback).Invoke(arg); }
		catch { eventName += "_"; goto retry; }
	}

}


/// <summary>
/// targets a method by name in a specific object
/// (2 generic arguments).
/// </summary>
public static class vp_TargetEvent<T, U>
{

	/// <summary>
	/// registers a callback (method or delegate) with the target object.
	/// the callback can be triggered later by sending the event 'eventName'.
	/// example signature: 'vp_TargetEvent&ltstring, string&gt.Register'
	/// </summary>
	public static void Register(object target, string eventName, Action<T, U> callback)
	{
		vp_TargetEventHandler.Register(target, eventName, (Delegate)callback, 2);
	}


	/// <summary>
	/// removes a callback from the target event handler by target object and event
	/// name. if name is null, the whole object will be unregistered. NOTE: this
	/// method will disable _all_ events with the specified name and target object,
	/// regardless of the generic signature used.
	/// </summary>
	public static void Unregister(object target, string eventName, Action<T, U> callback)
	{
		vp_TargetEventHandler.Unregister(target, eventName, callback);
	}
	public static void Unregister(object target)
	{
		vp_TargetEventHandler.Unregister(target, null, null);
	}


	/// <summary>
	/// attempts to trigger the pre-registered method or delegate associated
	/// with 'eventName' and the target object. if 'options' is set to
	/// 'vp_TargetEventOptions.RequireReceiver' there will be an error if
	/// no such callback can be found.
	/// example signature: 'vp_TargetEvent&lt;int, bool&gt;.Send'
	/// </summary>
	public static void Send(object target, string eventName, T arg1, U arg2, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, false, 2, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return;
		}
		try { ((Action<T, U>)callback).Invoke(arg1, arg2); }
		catch { eventName += "_"; goto retry; }
	}


	/// <summary>
	/// This method will send an event to an object of type 'UnityEngine.Component',
	/// then onward to its transform and all ancestor transforms recursively until
	/// a callback registered under 'eventName' is found in any of the objects.
	/// example signature: 'vp_TargetEvent&lt;int, bool&gt;.SendUpwards'. NOTES:
	/// 1) only the first matching callback detected will execute. The event will fail
	/// if the scan reaches the scene root with no match found. 2) it is best to register
	/// _transforms_ and not other types of component for use with 'SendUpwards', since
	/// components will be ignored unless the event is sent specifically to the
	/// correct component.
	/// </summary>
	public static void SendUpwards(Component target, string eventName, T arg1, U arg2, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, true, 2, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return;
		}
		try { ((Action<T, U>)callback).Invoke(arg1, arg2); }
		catch { eventName += "_"; goto retry; }
	}

}


/// <summary>
/// targets a method by name in a specific object
/// (3 generic arguments).
/// </summary>
public static class vp_TargetEvent<T, U, V>
{

	/// <summary>
	/// registers a callback (method or delegate) with the target object.
	/// the callback can be triggered later by sending the event 'eventName'.
	/// example signature: 'vp_TargetEvent&ltint, bool, float&gt.Register'
	/// </summary>
	public static void Register(object target, string eventName, Action<T, U, V> callback)
	{
		vp_TargetEventHandler.Register(target, eventName, (Delegate)callback, 3);
	}


	/// <summary>
	/// removes a callback from the target event handler by target object and event
	/// name. if name is null, the whole object will be unregistered. NOTE: this
	/// method will disable _all_ events with the specified name and target object,
	/// regardless of the generic signature used.
	/// </summary>
	public static void Unregister(object target, string eventName, Action<T, U, V> callback)
	{
		vp_TargetEventHandler.Unregister(target, eventName, callback);
	}
	public static void Unregister(object target)
	{
		vp_TargetEventHandler.Unregister(target, null, null);
	}


	/// <summary>
	/// attempts to trigger the pre-registered method or delegate associated
	/// with 'eventName' and the target object. if 'options' is set to
	/// 'vp_TargetEventOptions.RequireReceiver' there will be an error if
	/// no such callback can be found.
	/// example signature: 'vp_TargetEvent&lt;int, bool, float&gt;.Send'
	/// </summary>
	public static void Send(object target, string eventName, T arg1, U arg2, V arg3, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, false, 3, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return;
		}
		try { ((Action<T, U, V>)callback).Invoke(arg1, arg2, arg3); }
		catch { eventName += "_"; goto retry; }
	}


	/// <summary>
	/// This method will send an event to an object of type 'UnityEngine.Component',
	/// then onward to its transform and all ancestor transforms recursively until
	/// a callback registered under 'eventName' is found in any of the objects.
	/// example signature: 'vp_TargetEvent&lt;int, bool, float&gt;.SendUpwards'. NOTES:
	/// 1) only the first matching callback detected will execute. The event will fail
	/// if the scan reaches the scene root with no match found. 2) it is best to register
	/// _transforms_ and not other types of component for use with 'SendUpwards', since
	/// components will be ignored unless the event is sent specifically to the
	/// correct component.
	/// </summary>
	public static void SendUpwards(Component target, string eventName, T arg1, U arg2, V arg3, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, true, 3, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return;
		}
		try { ((Action<T, U, V>)callback).Invoke(arg1, arg2, arg3); }
		catch { eventName += "_"; goto retry; }
	}

}


/// <summary>
/// targets a method by name in a specific object
/// (no arguments + return value).
/// </summary>
public static class vp_TargetEventReturn<R>
{

	/// <summary>
	/// registers a callback (method or delegate) with the target object.
	/// the callback can be triggered later by sending the event 'eventName'.
	/// example signature: 'vp_TargetEventReturn&ltbool&gt.Register' (where the
	/// type represents the return value).
	/// </summary>
	public static void Register(object target, string eventName, Func<R> callback)
	{
		vp_TargetEventHandler.Register(target, eventName, (Delegate)callback, 4);
	}


	/// <summary>
	/// removes a callback from the target event handler by target object and event
	/// name. if name is null, the whole object will be unregistered. NOTE: this
	/// method will disable _all_ events with the specified name and target object,
	/// regardless of the generic signature used.
	/// </summary>
	public static void Unregister(object target, string eventName, Func<R> callback)
	{
		vp_TargetEventHandler.Unregister(target, eventName, callback);
	}
	public static void Unregister(object target)
	{
		vp_TargetEventHandler.Unregister(target, null, null);
	}
	public static void Unregister(Component component)
	{
		vp_TargetEventHandler.Unregister(component);
	}


	/// <summary>
	/// attempts to trigger the pre-registered method or delegate associated
	/// with 'eventName' and the target object. if the callback successfully
	/// executes, it will return a value matching its generic signature. if
	/// 'options' is set to 'vp_TargetEventOptions.RequireReceiver' there
	/// will be an error if no such callback can be found.
	/// example signature: 'vp_TargetEventReturn&ltbool&gt.Send' (where the
	/// type represents the return value).
	/// </summary>
	public static R Send(object target, string eventName, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, false, 4, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return default(R);
		}
		try { return ((Func<R>)callback).Invoke(); }
		catch { eventName += "_"; goto retry; }
	}


	/// <summary>
	/// This method will send an event to an object of type 'UnityEngine.Component',
	/// then onward to its transform and all ancestor transforms recursively until
	/// a callback registered under 'eventName' is found in any of the objects. 
	/// example signature: 'vp_TargetEventReturn&ltbool&gt.SendUpwards' (where the
	/// type represents the return value). NOTES: 1) only the first matching callback
	/// detected will execute. The event will fail if the scan reaches the scene root
	/// with no match found. 2) it is best to register _transforms_ and not other
	/// types of component for use with 'SendUpwards', since components will be missed
	/// unless the event is sent specifically to the correct component.
	/// </summary>
	public static R SendUpwards(Component target, string eventName, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, true, 4, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return default(R);
		}
		try { return ((Func<R>)callback).Invoke(); }
		catch { eventName += "_"; goto retry; }
	}

}




/// <summary>
/// targets a method by name in a specific object
/// (1 generic argument + return value).
/// </summary>
public static class vp_TargetEventReturn<T, R>
{

	/// <summary>
	/// registers a callback (method or delegate) with the target object.
	/// the callback can be triggered later by sending the event 'eventName'.
	/// example signature: 'vp_TargetEventReturn&ltfloat, bool&gt.Register' (where
	/// the last type represents the return value).
	/// </summary>
	public static void Register(object target, string eventName, Func<T, R> callback)
	{
		vp_TargetEventHandler.Register(target, eventName, (Delegate)callback, 5);
	}


	/// <summary>
	/// removes a callback from the target event handler by target object and event
	/// name. if name is null, the whole object will be unregistered. NOTE: this
	/// method will disable _all_ events with the specified name and target object,
	/// regardless of the generic signature used.
	/// </summary>
	public static void Unregister(object target, string eventName, Func<T, R> callback)
	{
		vp_TargetEventHandler.Unregister(target, eventName, callback);
	}
	public static void Unregister(object target)
	{
		vp_TargetEventHandler.Unregister(target, null, null);
	}


	/// <summary>
	/// attempts to trigger the pre-registered method or delegate associated
	/// with 'eventName' and the target object. if the callback successfully
	/// executes, it will return a value matching its generic signature. if
	/// 'options' is set to 'vp_TargetEventOptions.RequireReceiver' there
	/// will be an error if no such callback can be found.
	/// example signature: 'vp_TargetEventReturn&ltfloat, bool&gt.Send' (where
	/// the last type represents the return value).
	/// </summary>
	public static R Send(object target, string eventName, T arg, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, false, 5, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return default(R);
		}
		try { return ((Func<T, R>)callback).Invoke(arg); }
		catch { eventName += "_"; goto retry; }
	}


	/// <summary>
	/// This method will send an event to an object of type 'UnityEngine.Component',
	/// then onward to its transform and all ancestor transforms recursively until
	/// a callback registered under 'eventName' is found in any of the objects.
	/// example signature: 'vp_TargetEventReturn&ltfloat, bool&gt.SendUpwards' (where
	/// the last type represents the return value).
	/// NOTES: 1) only the first matching callback detected will execute. The event
	/// will fail if the scan reaches the scene root with no match found. 2) it is
	/// best to register _transforms_ and not other types of component for use with
	/// 'SendUpwards', since components will be ignored unless the event is sent
	/// specifically to the correct component.
	/// </summary>
	public static R SendUpwards(Component target, string eventName, T arg, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, true, 5, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return default(R);
		}
		try { return ((Func<T, R>)callback).Invoke(arg); }
		catch { eventName += "_"; goto retry; }
	}

}


/// <summary>
/// targets a method by name in a specific object
/// (2 arguments + return value).
/// </summary>
public static class vp_TargetEventReturn<T, U, R>
{


	/// <summary>
	/// registers a callback (method or delegate) with the target object.
	/// the callback can be triggered later by sending the event 'eventName'.
	/// example signature: 'vp_TargetEventReturn&lt;string, int, bool&gt;.Register'
	/// (where the last type represents the return value).
	/// </summary>
	public static void Register(object target, string eventName, Func<T, U, R> callback)
	{
		vp_TargetEventHandler.Register(target, eventName, (Delegate)callback, 6);
	}


	/// <summary>
	/// removes a callback from the target event handler by target object and event
	/// name. if name is null, the whole object will be unregistered. NOTE: this
	/// method will disable _all_ events with the specified name and target object,
	/// regardless of the generic signature used.
	/// </summary>
	public static void Unregister(object target, string eventName, Func<T, U, R> callback)
	{
		vp_TargetEventHandler.Unregister(target, eventName, callback);
	}
	public static void Unregister(object target)
	{
		vp_TargetEventHandler.Unregister(target, null, null);
	}


	/// <summary>
	/// attempts to trigger the pre-registered method or delegate associated
	/// with 'eventName' and the target object. if the callback successfully
	/// executes, it will return a value matching its generic signature. if
	/// 'options' is set to 'vp_TargetEventOptions.RequireReceiver' there
	/// will be an error if no such callback can be found.
	/// example signature: 'vp_TargetEventReturn&lt;string, int, bool&gt;.Send'
	/// (where the last type represents the return value).
	/// </summary>
	public static R Send(object target, string eventName, T arg1, U arg2, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, false, 6, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return default(R);
		}
		try { return ((Func<T, U, R>)callback).Invoke(arg1, arg2); }
		catch { eventName += "_"; goto retry; }
	}


	/// <summary>
	/// This method will send an event to an object of type 'UnityEngine.Component',
	/// then onward to its transform and all ancestor transforms recursively until
	/// a callback registered under 'eventName' is found in any of the objects.
	/// example signature: 'vp_TargetEventReturn&lt;string, int, bool&gt;.SendUpwards'
	/// (where the last type represents the return value).
	/// NOTES: 1) only the first matching callback detected will execute. The event
	/// will fail if the scan reaches the scene root with no match found. 2) it is
	/// best to register _transforms_ and not other types of component for use with
	/// 'SendUpwards', since components will be ignored unless the event is sent
	/// specifically to the correct component.
	/// </summary>
	public static R SendUpwards(Component target, string eventName, T arg1, U arg2, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, true, 6, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return default(R);
		}
		try { return ((Func<T, U, R>)callback).Invoke(arg1, arg2); }
		catch { eventName += "_"; goto retry; }
	}

}


/// <summary>
/// targets a method by name in a specific object
/// (3 arguments + return value).
/// </summary>
public static class vp_TargetEventReturn<T, U, V, R>
{

	/// <summary>
	/// registers a callback (method or delegate) with the target object.
	/// the callback can be triggered later by sending the event 'eventName'.
	/// example signature: 'vp_TargetEventReturn&ltfloat, int, string, float&gt.Register'
	/// (where the last type represents the return value).
	/// </summary>
	public static void Register(object target, string eventName, Func<T, U, V, R> callback)
	{
		vp_TargetEventHandler.Register(target, eventName, (Delegate)callback, 7);
	}


	/// <summary>
	/// removes a callback from the target event handler by target object and event
	/// name. if name is null, the whole object will be unregistered. NOTE: this
	/// method will disable _all_ events with the specified name and target object,
	/// regardless of the generic signature used.
	/// </summary>
	public static void Unregister(object target, string eventName, Func<T, U, V, R> callback)
	{
		vp_TargetEventHandler.Unregister(target, eventName, callback);
	}
	public static void Unregister(object target)
	{
		vp_TargetEventHandler.Unregister(target, null, null);
	}


	/// <summary>
	/// attempts to trigger the pre-registered method or delegate associated
	/// with 'eventName' and the target object. if the callback successfully
	/// executes, it will return a value matching its generic signature. if
	/// 'options' is set to 'vp_TargetEventOptions.RequireReceiver' there
	/// will be an error if no such callback can be found.
	/// example signature: 'vp_TargetEventReturn&ltfloat, int, string, float&gt.Send'
	/// (where the last type represents the return value).
	/// </summary>
	public static R Send(object target, string eventName, T arg1, U arg2, V arg3, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, false, 7, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return default(R);
		}
		try { return ((Func<T, U, V, R>)callback).Invoke(arg1, arg2, arg3); }
		catch { eventName += "_"; goto retry; }
	}


	/// <summary>
	/// This method will send an event to an object of type 'UnityEngine.Component',
	/// then onward to its transform and all ancestor transforms recursively until
	/// a callback registered under 'eventName' is found in any of the objects.
	/// example signature: 'vp_TargetEventReturn&ltfloat, int, string, float&gt.SendUpwards'
	/// (where the last type represents the return value).
	/// NOTES: 1) only the first matching callback detected will execute. The event
	/// will fail if the scan reaches the scene root with no match found. 2) it is
	/// best to register _transforms_ and not other types of component for use with
	/// 'SendUpwards', since components will be ignored unless the event is sent
	/// specifically to the correct component.
	/// </summary>
	public static R SendUpwards(Component target, string eventName, T arg1, U arg2, V arg3, vp_TargetEventOptions options = vp_TargetEventOptions.DontRequireReceiver)
	{
	retry:
		Delegate callback = vp_TargetEventHandler.GetCallback(target, eventName, true, 7, options);
		if (callback == null)
		{
			vp_TargetEventHandler.OnNoReceiver(eventName, options);
			return default(R);
		}
		try { return ((Func<T, U, V, R>)callback).Invoke(arg1, arg2, arg3); }
		catch { eventName += "_"; goto retry; }
	}

}

