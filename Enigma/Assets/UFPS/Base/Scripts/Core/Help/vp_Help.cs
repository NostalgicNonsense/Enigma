/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Help.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	retrieves, formats and stores help info (texts, manual URLS)
//					from derived product-specific classes
//
/////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR

using System;
using System.Reflection;
using System.Collections.Generic;


/// <summary>
/// stores an array of strings representing help tips
/// regarding a single subject, along with an URL to
/// the corresponding manual section
/// </summary>
public class vp_HelpInfo
{
	public string [] HelpText;
	public string ManualURL;
	public vp_HelpInfo(string [] text, string manualURL)
	{
		HelpText = text;
		ManualURL = manualURL;
	}
}


/// <summary>
/// 
/// </summary>
public class vp_Help
{

	private static bool Initialized = false;
	public static List<MethodInfo> m_GetMethods = new List<MethodInfo>();
	static Dictionary<Type, string> Texts = new Dictionary<Type, string>();
	static Dictionary<Type, string> URLs = new Dictionary<Type, string>();


	/// <summary>
	/// retrieves the formatted help text associated with a
	/// specific type, if available
	/// </summary>
	public static string GetText(Type type)
	{

		if (!Initialized)
			Init();

		string text;
		if (!Texts.TryGetValue(type, out text))
		{
			RetrieveInfo(type);
			Texts.TryGetValue(type, out text);
		}

		return (text ?? "");

	}


	/// <summary>
	/// 
	/// </summary>
	public static string GetURL(Type type)
	{

		if (!Initialized)
			Init();

		string url;
		if (!URLs.TryGetValue(type, out url))
		{
			RetrieveInfo(type);
			URLs.TryGetValue(type, out url);
		}

		return (url ?? "");

	}


	/// <summary>
	/// retrieves help info (texts, manual URLS) from derived classes
	/// and puts them into separate dictionaries for fast lookup. this
	/// method is run once per compile and retrieval of a certain type.
	/// </summary>
	private static void RetrieveInfo(Type type)
	{
		
		foreach (MethodInfo i in m_GetMethods)
		{
			vp_HelpInfo info = i.Invoke(null, new object[] { type }) as vp_HelpInfo;
			if ((info != null) && !Texts.ContainsKey(type) && !URLs.ContainsKey(type))
			{
				if (info.HelpText.Length == 1)
					Texts.Add(type, info.HelpText[0]);
				else if (info.HelpText.Length > 1)
				{
					// format the array of text strings into a longer help text
					// with string divided into bullet points. if there is only
					// one text, there will be no bullet points
					string mergedText = "";
					foreach (string s in info.HelpText)
					{
						mergedText += "• " + s + "\n\n";
					}
					mergedText = mergedText.Remove(mergedText.LastIndexOf("\n\n"));
					Texts.Add(type, mergedText);
				}
				URLs.Add(type, info.ManualURL);
			}
		}
		
	}
	

	/// <summary>
	/// scans the current assembly for all classes derived from
	/// 'vp_Help' and stores a reference to their 'Get' methods.
	/// these are used by the base class to access their respective
	/// help info dictionaries
	/// </summary>
	private static void Init()
	{
		foreach (System.Type t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
		{
			if (t.IsSubclassOf(typeof(vp_Help)))
			{
				MethodInfo m = t.GetMethod("Get");
				if (m != null)
					m_GetMethods.Add(m);
			}
		}
		Initialized = true;
	}


}

#endif