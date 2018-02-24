/////////////////////////////////////////////////////////////////////////////////
//
//	vp_GlobalEvent.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	This class allows the sending of generic events to and from
//					any class with generic listeners which register and unregister
//					from the events. Events can have 0-3 arguments.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public delegate void vp_GlobalCallback();									// 0 Arguments
public delegate void vp_GlobalCallback<T>(T arg1);							// 1 Argument
public delegate void vp_GlobalCallback<T, U>(T arg1, U arg2);				// 2 Arguments
public delegate void vp_GlobalCallback<T, U, V>(T arg1, U arg2, V arg3);	// 3 Arguments

public delegate R vp_GlobalCallbackReturn<R>();									// 0 Arguments and return
public delegate R vp_GlobalCallbackReturn<T, R>(T arg1);						// 1 Argument and return
public delegate R vp_GlobalCallbackReturn<T, U, R>(T arg1, U arg2);				// 2 Arguments and return
public delegate R vp_GlobalCallbackReturn<T, U, V, R>(T arg1, U arg2, V arg3);	// 3 Arguments and return

public enum vp_GlobalEventMode {
	DONT_REQUIRE_LISTENER,
	REQUIRE_LISTENER
}

static internal class vp_GlobalEventInternal
{

	public static Hashtable Callbacks = new Hashtable();
	
	public static UnregisterException ShowUnregisterException(string name)
	{
	
		return new UnregisterException(string.Format("Attempting to Unregister the event {0} but vp_GlobalEvent has not registered this event.", name));
		
	}
	
	public static SendException ShowSendException(string name)
	{
	
		return new SendException(string.Format("Attempting to Send the event {0} but vp_GlobalEvent has not registered this event.", name));
		
	}
 
	public class UnregisterException : Exception { public UnregisterException(string msg) : base(msg){} }
	public class SendException : Exception { public SendException(string msg) : base(msg){} }

}


// Event with no arguments
public static class vp_GlobalEvent
{

	private static Hashtable m_Callbacks = vp_GlobalEventInternal.Callbacks;
	
	/// <summary>
	/// Registers the event specified by name
	/// </summary>
	public static void Register(string name, vp_GlobalCallback callback)
	{
	
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(@"name");
			
    	if(callback == null)
        	throw new ArgumentNullException("callback");
        
    	List<vp_GlobalCallback> callbacks = (List<vp_GlobalCallback>)m_Callbacks[name];
    	if(callbacks == null)
    	{
        	callbacks = new List<vp_GlobalCallback>();
        	m_Callbacks.Add(name, callbacks);
    	}
   		callbacks.Add(callback);
   		
	}
	
	
	/// <summary>
	/// Unregisters the event specified by name
	/// </summary>
	public static void Unregister(string name, vp_GlobalCallback callback)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(callback == null)
	        throw new ArgumentNullException("callback");
	        
	    List<vp_GlobalCallback> callbacks = (List<vp_GlobalCallback>)m_Callbacks[name];
	    if(callbacks != null)
	        callbacks.Remove(callback);
		else
			throw vp_GlobalEventInternal.ShowUnregisterException(name);
	    
	}
	
	
	/// <summary>
	/// sends an event
	/// </summary>
	public static void Send(string name)
	{
	
		Send(name, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
		
	}
	
	
	/// <summary>
	/// sends an event
	/// </summary>
	public static void Send(string name, vp_GlobalEventMode mode)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    List<vp_GlobalCallback> callbacks = (List<vp_GlobalCallback>)m_Callbacks[name];
	    if(callbacks != null)
	        foreach(vp_GlobalCallback c in callbacks)
	            c();
		else if(mode == vp_GlobalEventMode.REQUIRE_LISTENER)
			throw vp_GlobalEventInternal.ShowSendException(name);
	
	}
	
}


// Accepts 1 Argument
public static class vp_GlobalEvent<T>
{

	private static Hashtable m_Callbacks = vp_GlobalEventInternal.Callbacks;
	
	/// <summary>
	/// Registers the event specified by name
	/// </summary>
	public static void Register(string name, vp_GlobalCallback<T> callback)
	{
	
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(@"name");
			
    	if(callback == null)
        	throw new ArgumentNullException("callback");
        
    	List<vp_GlobalCallback<T>> callbacks = (List<vp_GlobalCallback<T>>)m_Callbacks[name];
    	if(callbacks == null)
    	{
        	callbacks = new List<vp_GlobalCallback<T>>();
        	m_Callbacks.Add(name, callbacks);
    	}
   		callbacks.Add(callback);
   		
	}
	
	
	/// <summary>
	/// Unregisters the event specified by name
	/// </summary>
	public static void Unregister(string name, vp_GlobalCallback<T> callback)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(callback == null)
	        throw new ArgumentNullException("callback");
	        
	    List<vp_GlobalCallback<T>> callbacks = (List<vp_GlobalCallback<T>>)m_Callbacks[name];
	    if(callbacks != null)
	        callbacks.Remove(callback);
		else
			throw vp_GlobalEventInternal.ShowUnregisterException(name);
	    
	}
	
	
	/// <summary>
	/// sends an event with 1 argument
	/// </summary>
	public static void Send(string name, T arg1)
	{
	
		Send(name, arg1, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
		
	}
	
	
	/// <summary>
	/// sends an event with 1 argument
	/// </summary>
	public static void Send(string name, T arg1, vp_GlobalEventMode mode)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(arg1 == null)
	        throw new ArgumentNullException("arg1");
	        
	    List<vp_GlobalCallback<T>> callbacks = (List<vp_GlobalCallback<T>>)m_Callbacks[name];
	    if(callbacks != null)
	        foreach(vp_GlobalCallback<T> c in callbacks)
	            c(arg1);
		else if(mode == vp_GlobalEventMode.REQUIRE_LISTENER)
			throw vp_GlobalEventInternal.ShowSendException(name);
	
	}
	
}


// Accepts 2 arguments
public static class vp_GlobalEvent<T, U>
{

	private static Hashtable m_Callbacks = vp_GlobalEventInternal.Callbacks;
	
	/// <summary>
	/// Registers the event specified by name
	/// </summary>
	public static void Register(string name, vp_GlobalCallback<T, U> callback)
	{
	
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(@"name");
			
    	if(callback == null)
        	throw new ArgumentNullException("callback");
        
    	List<vp_GlobalCallback<T, U>> callbacks = (List<vp_GlobalCallback<T, U>>)m_Callbacks[name];
    	if(callbacks == null)
    	{
        	callbacks = new List<vp_GlobalCallback<T, U>>();
        	m_Callbacks.Add(name, callbacks);
    	}
   		callbacks.Add(callback);
   		
	}
	
	
	/// <summary>
	/// Unregisters the event specified by name
	/// </summary>
	public static void Unregister(string name, vp_GlobalCallback<T, U> callback)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(callback == null)
	        throw new ArgumentNullException("callback");
	        
	    List<vp_GlobalCallback<T, U>> callbacks = (List<vp_GlobalCallback<T, U>>)m_Callbacks[name];
	    if(callbacks != null)
	        callbacks.Remove(callback);
		else
			throw vp_GlobalEventInternal.ShowUnregisterException(name);
	    
	}
	
	
	/// <summary>
	/// sends an event with 2 arguments
	/// </summary>
	public static void Send(string name, T arg1, U arg2)
	{
	
		Send(name, arg1, arg2, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
		
	}
	
	
	/// <summary>
	/// sends an event with 2 arguments
	/// </summary>
	public static void Send(string name, T arg1, U arg2, vp_GlobalEventMode mode)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(arg1 == null)
	        throw new ArgumentNullException("arg1");
	        
		if(arg2 == null)
	        throw new ArgumentNullException("arg2");
	        
	    List<vp_GlobalCallback<T, U>> callbacks = (List<vp_GlobalCallback<T, U>>)m_Callbacks[name];
	    if(callbacks != null)
	        foreach(vp_GlobalCallback<T, U> c in callbacks)
	            c(arg1, arg2);
		else if(mode == vp_GlobalEventMode.REQUIRE_LISTENER)
			throw vp_GlobalEventInternal.ShowSendException(name);
	
	}
	
}


// Accepts 3 Arguments
public static class vp_GlobalEvent<T, U, V>
{

	private static Hashtable m_Callbacks = vp_GlobalEventInternal.Callbacks;
	
	/// <summary>
	/// Registers the event specified by name
	/// </summary>
	public static void Register(string name, vp_GlobalCallback<T, U, V> callback)
	{
	
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(@"name");
			
    	if(callback == null)
        	throw new ArgumentNullException("callback");
        
    	List<vp_GlobalCallback<T, U, V>> callbacks = (List<vp_GlobalCallback<T, U, V>>)m_Callbacks[name];
    	if(callbacks == null)
    	{
        	callbacks = new List<vp_GlobalCallback<T, U, V>>();
        	m_Callbacks.Add(name, callbacks);
    	}
   		callbacks.Add(callback);
   		
	}
	
	
	/// <summary>
	/// Unregisters the event specified by name
	/// </summary>
	public static void Unregister(string name, vp_GlobalCallback<T, U, V> callback)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(callback == null)
	        throw new ArgumentNullException("callback");
	        
	    List<vp_GlobalCallback<T, U, V>> callbacks = (List<vp_GlobalCallback<T, U, V>>)m_Callbacks[name];
	    if(callbacks != null)
	        callbacks.Remove(callback);
		else
			throw vp_GlobalEventInternal.ShowUnregisterException(name);
	    
	}
	
	
	/// <summary>
	/// sends an event with 3 arguments
	/// </summary>
	public static void Send(string name, T arg1, U arg2, V arg3)
	{
	
		Send(name, arg1, arg2, arg3, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
	
	}
	
	
	/// <summary>
	/// sends an event with 3 arguments
	/// </summary>
	public static void Send(string name, T arg1, U arg2, V arg3, vp_GlobalEventMode mode)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(arg1 == null)
	        throw new ArgumentNullException("arg1");
	        
		if(arg2 == null)
	        throw new ArgumentNullException("arg2");
	        
		if(arg3 == null)
	        throw new ArgumentNullException("arg3");
	        
	    List<vp_GlobalCallback<T, U, V>> callbacks = (List<vp_GlobalCallback<T, U, V>>)m_Callbacks[name];
	    if(callbacks != null)
	        foreach(vp_GlobalCallback<T, U, V> c in callbacks)
	            c(arg1, arg2, arg3);
		else if(mode == vp_GlobalEventMode.REQUIRE_LISTENER)
			throw vp_GlobalEventInternal.ShowSendException(name);
	
	}
	
}


// Event with no arguments and a return value
public static class vp_GlobalEventReturn<R>
{

	private static Hashtable m_Callbacks = vp_GlobalEventInternal.Callbacks;
	
	/// <summary>
	/// Registers the event specified by name
	/// </summary>
	public static void Register(string name, vp_GlobalCallbackReturn<R> callback)
	{
	
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(@"name");
			
    	if(callback == null)
        	throw new ArgumentNullException("callback");
        
    	List<vp_GlobalCallbackReturn<R>> callbacks = (List<vp_GlobalCallbackReturn<R>>)m_Callbacks[name];
    	if(callbacks == null)
    	{
        	callbacks = new List<vp_GlobalCallbackReturn<R>>();
        	m_Callbacks.Add(name, callbacks);
    	}
   		callbacks.Add(callback);
   		
	}
	
	
	/// <summary>
	/// Unregisters the event specified by name
	/// </summary>
	public static void Unregister(string name, vp_GlobalCallbackReturn<R> callback)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(callback == null)
	        throw new ArgumentNullException("callback");
	        
	    List<vp_GlobalCallbackReturn<R>> callbacks = (List<vp_GlobalCallbackReturn<R>>)m_Callbacks[name];
	    if(callbacks != null)
	        callbacks.Remove(callback);
		else
			throw vp_GlobalEventInternal.ShowUnregisterException(name);
	    
	}
	
	
	/// <summary>
	/// sends an event with 1 argument and returns a value
	/// </summary>
	public static R Send(string name)
	{
	
		return Send(name, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
	
	}
	
	
	/// <summary>
	/// sends an event with 1 argument and returns a value
	/// </summary>
	public static R Send(string name, vp_GlobalEventMode mode)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    List<vp_GlobalCallbackReturn<R>> callbacks = (List<vp_GlobalCallbackReturn<R>>)m_Callbacks[name];
	    if(callbacks != null)
	    {
	    	R val = default(R);
	        foreach(vp_GlobalCallbackReturn<R> c in callbacks)
	            val = c();
	        return val;
		}
		else
		{
			if(mode == vp_GlobalEventMode.REQUIRE_LISTENER)
				throw vp_GlobalEventInternal.ShowSendException(name);
			return default(R);
		}
	
	}
	
}


// Accepts 1 argument with a return value
public static class vp_GlobalEventReturn<T, R>
{

	private static Hashtable m_Callbacks = vp_GlobalEventInternal.Callbacks;
	
	/// <summary>
	/// Registers the event specified by name
	/// </summary>
	public static void Register(string name, vp_GlobalCallbackReturn<T, R> callback)
	{
	
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(@"name");
			
    	if(callback == null)
        	throw new ArgumentNullException("callback");
        
    	List<vp_GlobalCallbackReturn<T, R>> callbacks = (List<vp_GlobalCallbackReturn<T, R>>)m_Callbacks[name];
    	if(callbacks == null)
    	{
        	callbacks = new List<vp_GlobalCallbackReturn<T, R>>();
        	m_Callbacks.Add(name, callbacks);
    	}
   		callbacks.Add(callback);
   		
	}
	
	
	/// <summary>
	/// Unregisters the event specified by name
	/// </summary>
	public static void Unregister(string name, vp_GlobalCallbackReturn<T, R> callback)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(callback == null)
	        throw new ArgumentNullException("callback");
	        
	    List<vp_GlobalCallbackReturn<T, R>> callbacks = (List<vp_GlobalCallbackReturn<T, R>>)m_Callbacks[name];
	    if(callbacks != null)
	        callbacks.Remove(callback);
		else
			throw vp_GlobalEventInternal.ShowUnregisterException(name);
	    
	}
	
	
	/// <summary>
	/// sends an event with 1 argument and returns a value
	/// </summary>
	public static R Send(string name, T arg1)
	{
	
		return Send(name, arg1, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
		
	}
	
	
	/// <summary>
	/// sends an event with 1 argument and returns a value
	/// </summary>
	public static R Send(string name, T arg1, vp_GlobalEventMode mode)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(arg1 == null)
	        throw new ArgumentNullException("arg1");
	        
	    List<vp_GlobalCallbackReturn<T, R>> callbacks = (List<vp_GlobalCallbackReturn<T, R>>)m_Callbacks[name];
	    if(callbacks != null)
	    {
	    	R val = default(R);
	        foreach(vp_GlobalCallbackReturn<T, R> c in callbacks)
	            val = c(arg1);
	        return val;
		}
		else
		{
			if(mode == vp_GlobalEventMode.REQUIRE_LISTENER)
				throw vp_GlobalEventInternal.ShowSendException(name);
			return default(R);
		}
	
	}
	
}


// Accepts 2 arguments with a return value
public static class vp_GlobalEventReturn<T, U, R>
{

	private static Hashtable m_Callbacks = vp_GlobalEventInternal.Callbacks;
	
	/// <summary>
	/// Registers the event specified by name
	/// </summary>
	public static void Register(string name, vp_GlobalCallbackReturn<T, U, R> callback)
	{
	
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(@"name");
			
    	if(callback == null)
        	throw new ArgumentNullException("callback");
        
    	List<vp_GlobalCallbackReturn<T, U, R>> callbacks = (List<vp_GlobalCallbackReturn<T, U, R>>)m_Callbacks[name];
    	if(callbacks == null)
    	{
        	callbacks = new List<vp_GlobalCallbackReturn<T, U, R>>();
        	m_Callbacks.Add(name, callbacks);
    	}
   		callbacks.Add(callback);
   		
	}
	
	
	/// <summary>
	/// Unregisters the event specified by name
	/// </summary>
	public static void Unregister(string name, vp_GlobalCallbackReturn<T, U, R> callback)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(callback == null)
	        throw new ArgumentNullException("callback");
	        
	    List<vp_GlobalCallbackReturn<T, U, R>> callbacks = (List<vp_GlobalCallbackReturn<T, U, R>>)m_Callbacks[name];
	    if(callbacks != null)
	        callbacks.Remove(callback);
		else
			throw vp_GlobalEventInternal.ShowUnregisterException(name);
	    
	}
	
	
	/// <summary>
	/// sends an event with 2 arguments and returns a value
	/// </summary>
	public static R Send(string name, T arg1, U arg2)
	{
	
		return Send(name, arg1, arg2, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
	
	}
	
	
	/// <summary>
	/// sends an event with 2 arguments and returns a value
	/// </summary>
	public static R Send(string name, T arg1, U arg2, vp_GlobalEventMode mode)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(arg1 == null)
	        throw new ArgumentNullException("arg1");
	        
		if(arg2 == null)
	        throw new ArgumentNullException("arg2");
	        
	    List<vp_GlobalCallbackReturn<T, U, R>> callbacks = (List<vp_GlobalCallbackReturn<T, U, R>>)m_Callbacks[name];
	    if(callbacks != null)
	    {
	    	R val = default(R);
	        foreach(vp_GlobalCallbackReturn<T, U, R> c in callbacks)
	            val = c(arg1, arg2);
	        return val;
		}
		else
		{
			if(mode == vp_GlobalEventMode.REQUIRE_LISTENER)
				throw vp_GlobalEventInternal.ShowSendException(name);
			return default(R);
		}
	
	}
	
}


// Accepts 3 Arguments with a return value
public static class vp_GlobalEventReturn<T, U, V, R>
{

	private static Hashtable m_Callbacks = vp_GlobalEventInternal.Callbacks;
	
	/// <summary>
	/// Registers the event specified by name
	/// </summary>
	public static void Register(string name, vp_GlobalCallbackReturn<T, U, V, R> callback)
	{
	
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(@"name");
			
    	if(callback == null)
        	throw new ArgumentNullException("callback");
        
    	List<vp_GlobalCallbackReturn<T, U, V, R>> callbacks = (List<vp_GlobalCallbackReturn<T, U, V, R>>)m_Callbacks[name];
    	if(callbacks == null)
    	{
        	callbacks = new List<vp_GlobalCallbackReturn<T, U, V, R>>();
        	m_Callbacks.Add(name, callbacks);
    	}
   		callbacks.Add(callback);
   		
	}
	
	
	/// <summary>
	/// Unregisters the event specified by name
	/// </summary>
	public static void Unregister(string name, vp_GlobalCallbackReturn<T, U, V, R> callback)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(callback == null)
	        throw new ArgumentNullException("callback");
	        
	    List<vp_GlobalCallbackReturn<T, U, V, R>> callbacks = (List<vp_GlobalCallbackReturn<T, U, V, R>>)m_Callbacks[name];
	    if(callbacks != null)
	        callbacks.Remove(callback);
		else
			throw vp_GlobalEventInternal.ShowUnregisterException(name);
	    
	}
	
	
	/// <summary>
	/// sends an event with 3 arguments and returns a value
	/// </summary>
	public static R Send(string name, T arg1, U arg2, V arg3)
	{
	
		return Send(name, arg1, arg2, arg3, vp_GlobalEventMode.DONT_REQUIRE_LISTENER);
		
	}
	
	
	/// <summary>
	/// sends an event with 3 arguments and returns a value
	/// </summary>
	public static R Send(string name, T arg1, U arg2, V arg3, vp_GlobalEventMode mode)
	{
	
	    if(string.IsNullOrEmpty(name))
	        throw new ArgumentNullException(@"name");
	        
	    if(arg1 == null)
	        throw new ArgumentNullException("arg1");
	        
		if(arg2 == null)
	        throw new ArgumentNullException("arg2");
	        
		if(arg3 == null)
	        throw new ArgumentNullException("arg3");

		List<vp_GlobalCallbackReturn<T, U, V, R>> callbacks = (List<vp_GlobalCallbackReturn<T, U, V, R>>)m_Callbacks[name];
	    if(callbacks != null)
	    {
    		R val = default(R);
	        foreach(vp_GlobalCallbackReturn<T, U, V, R> c in callbacks)
            	val = c(arg1, arg2, arg3);
            return val;
		}
		else
	    {
	    	if(mode == vp_GlobalEventMode.REQUIRE_LISTENER)
				throw vp_GlobalEventInternal.ShowSendException(name);
			return default(R);
		}
	
	}
	
}
