/////////////////////////////////////////////////////////////////////////////////
//
//	vp_EventHandler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	the event handler is a hub facilitating delegate based messaging
//					between components inside a gameobject hierarchy. components
//					may register or unregister with the event handler to gain
//					access to its delegates (which are declared in a derived class).
//					registered objects can execute event handler delegate fields
//					and add their own methods to the invocation lists.
//
///////////////////////////////////////////////////////////////////////////////// 

using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public abstract class vp_EventHandler: MonoBehaviour
{

#if UNITY_EDITOR
	public static bool RefreshEditor = false;
#endif

	protected bool m_Initialized = false;

	// events declared in derived eventhandler, by prefixed callback ('OnStart_' 'OnAttempt' etc.)
	protected Dictionary<string, vp_Event> m_EventsByCallback = new Dictionary<string, vp_Event>();
	protected List<vp_Event> m_Events = new List<vp_Event>();

	// objects that tried to register with the handler before it woke
	// up and will need revisiting
	protected List<object> m_PendingRegistrants = new List<object>();

	// compatible methods detected in all registered scripts
	protected static Dictionary<Type, ScriptMethods> m_StoredScriptTypes = new Dictionary<Type, ScriptMethods>();

	protected static string[] m_SupportedPrefixes = new string[] { "OnMessage_", "CanStart_", "CanStop_", "OnStart_", "OnStop_", "OnAttempt_", "get_OnValue_", "set_OnValue_", "OnFailStart_", "OnFailStop_"};


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		StoreHandlerEvents();

		m_Initialized = true;

		// register any objects that may have tried to register
		// to this handler before it woke up
		for (int v = m_PendingRegistrants.Count - 1; v > -1; v--)
		{
			Register(m_PendingRegistrants[v]);
			m_PendingRegistrants.Remove(m_PendingRegistrants[v]);
		}

	}
	
	
	/// <summary>
	/// detects all events declared in the class inherited from
	/// vp_EventHandler, creates instances of them and stores
	/// them in a dictionary of 'handler events'
	/// </summary>
	protected void StoreHandlerEvents()
	{

		object e = null;

		// fetch all fields (including inherited ones)
		List<FieldInfo> info = GetFields();
		if (info == null || info.Count == 0)
			return;

		// store handler events from the list of fields
		foreach (FieldInfo i in info)
		{
			try
			{
				e = Activator.CreateInstance(i.FieldType, i.Name);
			}
			catch
			{
				Debug.LogError("Error: (" + this + ") does not support the type of '" + i.Name + "' in '" + i.DeclaringType + "'.");
				continue;
			}

			if (e == null)
				continue;

			i.SetValue(this, e);

			// store all events in a list
			if (!m_Events.Contains((vp_Event)e))
				m_Events.Add((vp_Event)e);

			// store each event in a dictionary, once for every available prefix,
			// e.g. 'OnStart_Attack', 'OnStop_Attack' etc.
			foreach (string prefix in ((vp_Event)e).Prefixes.Keys)
			{
				m_EventsByCallback.Add(prefix + i.Name, (vp_Event)e);
			}

		}

	}


	/// <summary>
	/// returns a list of fields in this and every parent class
	/// up until (but not including) vp_StateEventHandler or
	/// vp_EventHandler
	/// </summary>
	public List<FieldInfo> GetFields()
	{

		List<FieldInfo> info = new List<FieldInfo>();
		Type currentType = this.GetType();
		Type nextType = null;
		do
		{
			if (nextType != null)
				currentType = nextType;
			info.AddRange(currentType.GetFields((BindingFlags.Public | BindingFlags.NonPublic |
													BindingFlags.Instance | BindingFlags.DeclaredOnly)));
			if (currentType.BaseType != typeof(vp_StateEventHandler) &&
				currentType.BaseType != typeof(vp_EventHandler))
				nextType = currentType.BaseType;
		}
		while (currentType.BaseType != typeof(vp_StateEventHandler) &&
				currentType.BaseType != typeof(vp_EventHandler) &&
				currentType != null);

		if(info == null || info.Count == 0)
			Debug.LogWarning("Warning: (" + this + ") Found no fields to store as events.");
		
		return info;

	}


	/// <summary>
	/// registers an external class with the event handler, detecting
	/// any method names that have event handler compatible prefixes
	/// and associating them with the corresponding event object
	/// </summary>
	public void Register(object target)
	{

		if(target == null)
		{
			Debug.LogError("Error: (" + this + ") Target object was null.");
			return;
		}
		
		if (!m_Initialized)
		{
			m_PendingRegistrants.Add(target);
			return;
		}

		ScriptMethods script = GetScriptMethods(target);
		if (script == null)
		{
			Debug.LogError("Error: (" + this + ") could not get script methods for '" + target + "'.");
			return;
		}

		vp_Event e;

		foreach (MethodInfo m in script.Events)
		{

			// method must have a corresponding event in the event handler
			if (!(m_EventsByCallback.TryGetValue(m.Name, out e)))
			{
				//Debug.LogWarning("Warning: (" + m.DeclaringType + ") Event handler can't register method '" + m.Name + "' because '" + this.GetType() + "' has not (successfully) registered any event named '" + m.Name.Substring(m.Name.Substring(0, m.Name.IndexOf('_', 4) + 1).Length));
				continue;
			}

			// extract prefix from method name to figure out which delegate index to use
			// (prefix is every initial character until and including the second underscore)
			int index;
			e.Prefixes.TryGetValue(m.Name.Substring(0, m.Name.IndexOf('_', 4) + 1), out index);	// '4' is for skipping past the initial underscore of properties, i.e. 'get_'

			// method signature must match event signature
			if (!CompareMethodSignatures(m, e.GetParameterType(index), e.GetReturnType(index)))
				continue;

			e.Register(target, m.Name, index);

			// the below snippet may be useful for debugging
			//Debug.Log(m.Name + ", index: " + index + ", parameterType: " + vp_Utility.GetTypeAlias(e.GetParameterType(index)) + ", returnType: " + vp_Utility.GetTypeAlias(e.GetReturnType(index)));

		}

	}


	/// <summary>
	/// unregisters all methods in the target class from the
	/// event handler
	/// </summary>
	public void Unregister(object target)
	{

		if (target == null)
		{
			Debug.LogError("Error: (" + this + ") Target object was null.");
			return;
		}

		// the below snippet may be useful for debugging
		//if (!m_StoredScriptTypes.ContainsKey(target.GetType()))
		//{
		//    Debug.LogWarning("Warning: (" + target.GetType() + ") Event handler can't unregister object of type '" + target.GetType() + "' because no object of that type has been (successfully) registered.");
		//    return;
		//}

		FieldInfo field;
		object obj;
		Delegate del;

		foreach (vp_Event e in m_Events)
		{

			if (e == null)
				continue;

			foreach (string f in e.InvokerFieldNames)
			{
				
				field = e.Type.GetField(f);
				if (field == null)
					continue;

				obj = field.GetValue(e);
				if (obj == null)
					continue;

				del = (Delegate)obj;
				if (del == null)
					continue;

				foreach (Delegate d in del.GetInvocationList())
				{

					if (d.Target != target)
						continue;

					e.Unregister(target);

				}

			}

		}

	}


	/// <summary>
	/// determines whether the passed script method has the exact
	/// matching siganture of the passed parameter- and return type
	/// </summary>
	protected bool CompareMethodSignatures(MethodInfo scriptMethod, Type handlerParameterType, Type handlerReturnType)
	{

		// validate return type
		if (scriptMethod.ReturnType != handlerReturnType)
		{
			Debug.LogError("Error: (" + scriptMethod.DeclaringType + ") Return type (" + vp_Utility.GetTypeAlias(scriptMethod.ReturnType) + ") is not valid for '" + scriptMethod.Name + "'. Return type declared in event handler was: (" + vp_Utility.GetTypeAlias(handlerReturnType) + ").");
			return false;
		}

		// validate parameter count and type
		if (scriptMethod.GetParameters().Length == 1)
		{
			// catch type mismatch, and cases where script has 1 parameter but handler has 0
			if (((ParameterInfo)scriptMethod.GetParameters().GetValue(0)).ParameterType != handlerParameterType)
			{
				// parameter type must match
				Debug.LogError("Error: (" + scriptMethod.DeclaringType + ") Parameter type (" + vp_Utility.GetTypeAlias(((ParameterInfo)scriptMethod.GetParameters().GetValue(0)).ParameterType) +	") is not valid for '" + scriptMethod.Name + "'. Parameter type declared in event handler was: (" +	vp_Utility.GetTypeAlias(handlerParameterType) + ").");
				return false;
			}
		}
		else if (scriptMethod.GetParameters().Length == 0)
		{
			// catch cases where script has 0 parameters but handler has 1
			if (handlerParameterType != typeof(void))
			{
				Debug.LogError("Error: (" + scriptMethod.DeclaringType + ") Can't register method '" + scriptMethod.Name + "' with 0 parameters. Expected: 1 parameter of type (" + vp_Utility.GetTypeAlias(handlerParameterType) + ").");
				return false;
			}
		}
		else if (scriptMethod.GetParameters().Length > 1)
		{
		    // catch cases where script has more than 1 parameters
		    Debug.LogError("Error: (" + scriptMethod.DeclaringType + ") Can't register method '" + scriptMethod.Name + "' with " + scriptMethod.GetParameters().Length + " parameters. Max parameter count: 1 of type (" + vp_Utility.GetTypeAlias(handlerParameterType) + ").");
		    return false;
		}

		return true;

	}


	/// <summary>
	/// returns a 'ScriptMethods' object with the methods found
	/// in the passed object type
	/// </summary>
	protected ScriptMethods GetScriptMethods(object target)
	{

		ScriptMethods script;
		if (!m_StoredScriptTypes.TryGetValue(target.GetType(), out script))
		{
			script = new ScriptMethods(target.GetType());
			m_StoredScriptTypes.Add(target.GetType(), script);
		}
		return script;

	}
	

	/// <summary>
	/// stores info on all the methods of a given script type.
	/// for event handler registration optimization (we only need
	/// to use reflection once per script type and session)
	/// </summary>
	protected class ScriptMethods
	{

		public List<MethodInfo> Events = new List<MethodInfo>();


		/// <summary>
		///
		/// </summary>
		public ScriptMethods(Type type)
		{

			Events = GetMethods(type);

		}


		/// <summary>
		/// returns a list of all methods in a script that could
		/// potentially be registered by this handler
		/// </summary>
		protected static List<MethodInfo> GetMethods(Type type)
		{

			List<MethodInfo> methods = new List<MethodInfo>();
			List<string> existingMethodNames = new List<string>();

			// create a list of all methods in the type hierarchy
			while (type != null)
			{
				foreach (MethodInfo i in type.GetMethods((BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
				{

					// ignore auto-generated delegates (delegates declared inside
					// method bodies, including by vp_Timers)
					if (i.Name.Contains(">m__"))
						continue;

					// don't add the same method name twice (child methods will hide
					// base methods with identical names)
					if (existingMethodNames.Contains(i.Name))
						continue;

					foreach (string p in m_SupportedPrefixes)
					{
						if (i.Name.Contains(p))
							goto FoundMethodWithSupportedPrefix;
					}
					
					continue;

				FoundMethodWithSupportedPrefix:

					methods.Add(i);
					existingMethodNames.Add(i.Name);

				}
				type = type.BaseType;
			}

			return methods;

		}

	}
	

}


