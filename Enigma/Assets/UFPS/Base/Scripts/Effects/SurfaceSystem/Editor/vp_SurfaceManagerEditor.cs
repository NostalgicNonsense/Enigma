/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SurfaceManagerEditor.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	custom inspector for the vp_SurfaceManager class
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(vp_SurfaceManager))]

public class vp_SurfaceManagerEditor : Editor
{

	// target component
	public vp_SurfaceManager m_Component;

	// foldouts
	public bool m_TextureFallbacksFoldout;
	public bool m_DefaultFallbacksFoldout;
	
	protected static GUIStyle m_HeaderStyle = null;
	protected static GUIStyle m_SmallButtonStyle = null;
	

	/// <summary>
	/// hooks up the component object as the inspector target
	/// </summary>
	public virtual void OnEnable()
	{

		m_Component = (vp_SurfaceManager)target;

		for (int i = 0; i < m_Component.ObjectSurfaces.Count; i++)
		{
			vp_SurfaceManager.ObjectSurface surface = m_Component.ObjectSurfaces[i];
			for (int x = 0; x < surface.UVTextures.Count; x++)
				surface.UVTextures[x].ShowUV = false;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public override void OnInspectorGUI()
	{

		GUI.color = Color.white;

		GUILayout.Space(10);
		m_Component.m_ShowHelp = GUILayout.Toggle(m_Component.m_ShowHelp, "Show help");
		GUILayout.Space(5);

		DoTextureFallbacksFoldout();
		DoDefaultFallbacksFoldout();

		if (m_Component.m_ShowHelp)
			vp_EditorGUIUtility.Separator();
		else
			GUILayout.Space(5);

			DoHelpBox();

		if (GUI.changed)
			EditorUtility.SetDirty(target);

	}


	/// <summary>
	/// 
	/// </summary>
	void DoHelpBox()
	{
		if (!m_Component.m_ShowHelp)
			return;
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		EditorGUILayout.HelpBox("\n• Surface particle effects in UFPS are usually defined by a SURFACE IDENTIFIER component on the target object. This allows the system to figure out which set of effects to play in response to an incoming bullet or footstep.\n\n• The (optional) vp_SurfaceManager component can be used to trigger effects depending on OBJECT TEXTURES, allowing effects on terrains as well as removing the need for assigning a vp_SurfaceIdentifier to every gameobject. It also has default fallbacks for (potentially missing) impact- and surface types.\n\n• If no vp_SurfaceManager is present, particle effects will only trigger if the target object has a vp_SurfaceIdentifier component.\n", MessageType.Info);
		GUILayout.Space(20);
		GUILayout.EndHorizontal();

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoDefaultFallbacksFoldout()
	{

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		m_DefaultFallbacksFoldout = EditorGUILayout.Foldout(m_DefaultFallbacksFoldout, "Default Fallbacks");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		if (m_DefaultFallbacksFoldout)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			m_Component.Fallbacks.ImpactEvent = (vp_ImpactEvent)EditorGUILayout.ObjectField("Impact Event", m_Component.Fallbacks.ImpactEvent, typeof(vp_ImpactEvent), false);
			if ((m_Component.Fallbacks.ImpactEvent != null) && GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(12), GUILayout.MaxWidth(12), GUILayout.MinHeight(12)))
				m_Component.Fallbacks.ImpactEvent = null;
			GUILayout.Space(20);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			m_Component.Fallbacks.SurfaceType = (vp_SurfaceType)EditorGUILayout.ObjectField("Surface Type", m_Component.Fallbacks.SurfaceType, typeof(vp_SurfaceType), false);
			if ((m_Component.Fallbacks.SurfaceType != null) && GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(12), GUILayout.MaxWidth(12), GUILayout.MinHeight(12)))
				m_Component.Fallbacks.SurfaceType = null;
			GUILayout.Space(20);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			m_Component.Fallbacks.AllowDecals = EditorGUILayout.Toggle("Allow decals", m_Component.Fallbacks.AllowDecals);
			GUILayout.Space(20);
			GUILayout.EndHorizontal();

			GUILayout.Space(5);

			if (m_Component.m_ShowHelp)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(10);
				GUI.enabled = false;
				EditorGUILayout.HelpBox("• These fallbacks will be used when the system can not determine the ImpactEvent and/or SurfaceType involved in an impact. This can happen due to a projectile not having its vp_ImpactEvent set, or a target object lacking both a vp_SurfaceIdentifier and a texture fallback.\n\n• When 'Allow decals' is false (recommended) default fallbacks will not be able to spawn decals, even if the vp_SurfaceEffect has them. This will prevent fallback decals that don't fit well with the target surface.\n", MessageType.None);
				GUI.enabled = true;
				GUILayout.Space(20);
				GUILayout.EndHorizontal();
			}

		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoTextureFallbacksFoldout()
	{

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);

		m_TextureFallbacksFoldout = EditorGUILayout.Foldout(m_TextureFallbacksFoldout, "Texture Fallbacks");

		GUILayout.Space(10);
		GUILayout.EndHorizontal();

		if (m_TextureFallbacksFoldout)
		{

			GUILayout.Space(0);

			if (m_Component.ObjectSurfaces != null)
			{
				for (int i = 0; i < m_Component.ObjectSurfaces.Count; i++)
				{

					vp_SurfaceManager.ObjectSurface surface = m_Component.ObjectSurfaces[i];

					if (surface.UVTextures.Count == 0)
					{
						vp_SurfaceManager.UVTexture texture = new vp_SurfaceManager.UVTexture(true);
						surface.UVTextures.Add(texture);
					}

					GUILayout.BeginHorizontal();
					GUILayout.Space(20);
					surface.Foldout = EditorGUILayout.Foldout(surface.Foldout, surface.Name);

					if (GUILayout.Button("Remove", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(50), GUILayout.MaxWidth(50), GUILayout.MinHeight(15)))
					{
						m_Component.ObjectSurfaces.RemoveAt(i);
						i--;
					}
					GUI.backgroundColor = Color.white;

					GUILayout.Space(20);

					GUILayout.EndHorizontal();

					GUILayout.Space(5);

					if (surface.Foldout)
					{

						GUILayout.BeginHorizontal();
						GUILayout.Space(35);
						vp_SurfaceType s = surface.SurfaceType;
						surface.SurfaceType = (vp_SurfaceType)EditorGUILayout.ObjectField("SurfaceType", surface.SurfaceType, typeof(vp_SurfaceType), false);

						if ((s != surface.SurfaceType))
						{
							for (int ii = 0; ii < m_Component.ObjectSurfaces.Count; ii++)
							{
								if (i != ii)
								{
									if (m_Component.ObjectSurfaces[ii].SurfaceType == m_Component.ObjectSurfaces[i].SurfaceType)
									{
										EditorUtility.DisplayDialog("Ooops!", "The SurfaceType '"+surface.SurfaceType.name+"' has already been added. Please choose a different SurfaceType.", "OK");
										surface.SurfaceType = s;
									}
								}
							}
						}

						if ((surface.SurfaceType != null) && (s != surface.SurfaceType))
							surface.Name = surface.SurfaceType.name;
						if ((surface.SurfaceType != null) && GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(12), GUILayout.MaxWidth(12), GUILayout.MinHeight(12)))
							surface.SurfaceType = null;
						GUILayout.Space(20);
						GUILayout.EndHorizontal();

						if (surface.SurfaceType == null)
							surface.Name = "Please assign a SurfaceType";

						GUILayout.BeginHorizontal();
						GUILayout.Space(38);

						if (surface.TexturesFoldout)
							surface.TexturesFoldout = EditorGUILayout.Foldout(surface.TexturesFoldout, "Textures", HeaderStyleSelected);
						else
							surface.TexturesFoldout = EditorGUILayout.Foldout(surface.TexturesFoldout, "Textures");

						GUILayout.EndHorizontal();

						if (surface.TexturesFoldout)
						{
							if (surface.UVTextures != null)
							{
								if (surface.UVTextures.Count > 0)
								{
									int counter = 0;

									for (int x = 0; x < surface.UVTextures.Count; x++)
									{
										if (counter == 0)
										{
											GUILayout.BeginHorizontal(GUILayout.MinHeight(100));
											GUILayout.Space(50);
										}

										GUILayout.BeginVertical(GUILayout.MinHeight(90));
										GUILayout.Space(12);	// moves everything vertically

										GUILayout.BeginHorizontal(GUILayout.MaxWidth(75), GUILayout.MinWidth(75));

										// --- uv editor closed ---
										GUILayout.Space(8);	// left margin for lower buttons
										if (!surface.UVTextures[x].ShowUV)
										{

											GUILayout.FlexibleSpace();
											if (GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(12), GUILayout.MaxWidth(12), GUILayout.MinHeight(12)))
											{
												if (((x > 0) || ((x == 0) && (surface.UVTextures[0].Texture != null) || (surface.UVTextures.Count > 1))))
												{
													if (surface.UVTextures[x].Texture != null)
													{
														surface.UVTextures[x].Texture = null;
														vp_SurfaceManager.Instance.Reset();
													}
													else
													{
														surface.UVTextures.RemoveAt(x);
														x = Mathf.Max(0, x - 1);
														GUI.FocusControl(null);
													}
												}
												//m_Component.SetDirty(true);
											}
											GUILayout.Space(-4);	// right margin for upper buttons

										}

										// --- uv editor open ---

										GUILayout.EndHorizontal();
			
										if (surface.UVTextures[x].Texture != null)
											GUILayout.Space(44);
										else
											GUILayout.Space(63);
										
										GUILayout.BeginHorizontal(GUILayout.MaxWidth(75), GUILayout.MinWidth(75));
										GUILayout.Space(4);	// left margin for upper buttons

										if (surface.UVTextures[x].Texture == null)
											surface.UVTextures[x].ShowUV = false;
										else if (!surface.UVTextures[x].ShowUV)
										{
											if (GUILayout.Button(("UV" + (surface.UVTextures[x].UV != m_Component.DefaultUV ? " *" : "")), vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(25), GUILayout.MaxWidth(25), GUILayout.MinHeight(13)))
												surface.UVTextures[x].ShowUV = true;
										}
										GUILayout.FlexibleSpace();

										GUILayout.Space(-4);	// right margin for upper buttons
										GUILayout.EndHorizontal();
										if (!surface.UVTextures[x].ShowUV)
											GUILayout.Space(-63);
										else
											GUILayout.Space(-25);

										GUILayout.Space(-17);	// moves texture + upper buttons vertically
										if (!surface.UVTextures[x].ShowUV)
											surface.UVTextures[x].Texture = (Texture)EditorGUILayout.ObjectField(surface.UVTextures[x].Texture, typeof(Texture), false, GUILayout.MinWidth(75), GUILayout.MaxWidth(75), GUILayout.MinHeight(75), GUILayout.MaxHeight((surface.UVTextures[x].ShowUV ? 75 : 75)));
										if (surface.UVTextures[x].ShowUV)
										{
											Rect v = surface.UVTextures[x].UV;
											GUILayout.BeginVertical(GUILayout.MaxWidth(75), (GUILayout.MinWidth(75)));
											GUILayout.BeginHorizontal();
											GUILayout.FlexibleSpace();
											GUI.SetNextControlName("reset");
											if (GUILayout.Button("Reset UV", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinHeight(15), GUILayout.MaxWidth(75)))
											{
												surface.UVTextures[x].UV = m_Component.DefaultUV;
												// focus the reset button to get rid of possible textfield focus
												// or the textfields won't update properly when resetting
												GUI.FocusControl("reset");
												GUI.FocusControl(null);	// unfocus or button highlight may go weird
											}
											GUILayout.FlexibleSpace();
											GUILayout.EndHorizontal();
											GUILayout.Space(4);
											GUILayout.BeginHorizontal(GUILayout.MaxWidth(75), GUILayout.MinWidth(75));

											surface.UVTextures[x].UV = EditorGUILayout.RectField(surface.UVTextures[x].UV, GUILayout.MinWidth(95), GUILayout.MaxWidth(95));
											if (v != surface.UVTextures[x].UV)
											{
												surface.UVTextures[x].UV = new Rect(
													Mathf.Clamp(surface.UVTextures[x].UV.xMin, 0, 1),
													Mathf.Clamp(surface.UVTextures[x].UV.yMin, 0, 1),
													Mathf.Clamp(surface.UVTextures[x].UV.width, 0, 1),
													Mathf.Clamp(surface.UVTextures[x].UV.height, 0, 1)
													);
											}
											GUILayout.EndHorizontal();

											GUILayout.BeginHorizontal();
											GUILayout.FlexibleSpace();

											if (GUILayout.Button("Show Texture", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(75), GUILayout.MaxWidth(75), GUILayout.MinHeight(15)))
												surface.UVTextures[x].ShowUV = !surface.UVTextures[x].ShowUV;
											GUILayout.FlexibleSpace();
											GUILayout.EndHorizontal();
											GUILayout.EndVertical();
										}

										GUILayout.Space(-80);	// top margin for upper buttons

										GUILayout.BeginHorizontal(GUILayout.MaxWidth(75), GUILayout.MinWidth(75));
										GUILayout.Space(8);	// left margin for upper buttons

										// --- uv 2 ---
										if (!surface.UVTextures[x].ShowUV)
										{
											if ((x == 0) && ((surface.UVTextures[0].Texture == null) && (surface.UVTextures.Count == 1)))
												GUI.color = Color.clear;
											GUILayout.FlexibleSpace();
											GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(12), GUILayout.MaxWidth(12), GUILayout.MinHeight(12));
											GUILayout.Space(-4);	// right margin for lower buttons
											GUI.color = Color.white;
										}

										GUI.backgroundColor = Color.white;

										GUILayout.EndHorizontal();
										GUILayout.Space(43);

										GUILayout.BeginHorizontal(GUILayout.MaxWidth(75), GUILayout.MinWidth(75));
										GUILayout.Space(4);	// left margin for upper buttons

										if (surface.UVTextures[x].Texture == null)
											surface.UVTextures[x].ShowUV = false;
										else if (!surface.UVTextures[x].ShowUV)
										{
											GUILayout.Button(("UV" + (surface.UVTextures[x].UV != m_Component.DefaultUV ? " *" : "")), vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(25), GUILayout.MaxWidth(25), GUILayout.MinHeight(13));
										}
										GUILayout.FlexibleSpace();
										if (!surface.UVTextures[x].ShowUV)
											GUILayout.Button("Select", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(35), GUILayout.MaxWidth(35), GUILayout.MinHeight(13));

										GUILayout.Space(-4);	// right margin for upper buttons
										GUILayout.EndHorizontal();
										
										GUILayout.EndVertical();

										counter++;

										if (counter == 4 || x == surface.UVTextures.Count - 1)
										{
											GUILayout.Space(20);

											GUILayout.EndHorizontal();
											counter = 0;
										}
									}
								}
							}

							GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							if (GUILayout.Button("Add Texture Slot", GUILayout.MinWidth(120), GUILayout.MaxWidth(120)))
							{
								vp_SurfaceManager.UVTexture texture = new vp_SurfaceManager.UVTexture(true);
								surface.UVTextures.Add(texture);
							}
							GUILayout.Space(20);
							GUI.backgroundColor = Color.white;
							GUILayout.EndHorizontal();

						}

						vp_EditorGUIUtility.Separator();

						GUILayout.Space(5);
					}
				}
			}

			if (m_Component.m_ShowHelp && (m_Component.ObjectSurfaces.Count == 0))
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(50);
				EditorGUILayout.HelpBox("There are no Texture Groups. Click the \"Add Texture Groups\" button to add one.", MessageType.Info);
				GUILayout.Space(20);
				GUILayout.EndHorizontal();
			}

			GUILayout.Space(8);

			GUILayout.BeginHorizontal();

			GUILayout.Space(10);

			if (GUILayout.Button("Add Texture Group", GUILayout.MinWidth(150), GUILayout.MinHeight(25)))
			{
				vp_SurfaceManager.ObjectSurface surface = new vp_SurfaceManager.ObjectSurface();
				m_Component.ObjectSurfaces.Add(surface);
			}
			GUI.backgroundColor = Color.white;
			GUILayout.Space(20);
			GUILayout.EndHorizontal();

			GUILayout.Space(10);

			if (m_Component.m_ShowHelp)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(10);
				GUI.enabled = false;
				EditorGUILayout.HelpBox("• To create a new SurfaceType fallback for a texture set, click 'Add Texture Group' and assign the vp_SurfaceType. Then, add all the textures you want associated with that particular surface type.\n\n• You can click the 'UV' button to restrict the surface inside a texture. Note that if you want to have several surfaces inside a single texture, you need to add the texture once for every UV region.\n", MessageType.None);
				GUI.enabled = true;
				GUILayout.Space(20);
				GUILayout.EndHorizontal();
			}

			vp_EditorGUIUtility.Separator();

		}

	}
	
	// --- GUI styles ---

	private static GUIStyle m_RectStyle = null;
	public static GUIStyle RectStyle
	{
		get
		{
			if (m_RectStyle == null)
			{
				m_RectStyle = new GUIStyle("Rectfield");
				m_RectStyle.fontSize = 5;
			}
			return m_RectStyle;
		}
	}

	public static GUIStyle SmallButtonStyle
	{
		get
		{
			if (m_SmallButtonStyle == null)
			{
				m_SmallButtonStyle = new GUIStyle("Button");
				m_SmallButtonStyle.fontSize = 10;
				m_SmallButtonStyle.alignment = TextAnchor.MiddleCenter;
				m_SmallButtonStyle.margin.left = 1;
				m_SmallButtonStyle.margin.right = 1;
				m_SmallButtonStyle.padding = new RectOffset(0, 4, 0, 2);
			}
			return m_SmallButtonStyle;
		}
	}


	public static GUIStyle HeaderStyleSelected
	{
		get
		{
			if (m_HeaderStyle == null)
			{
				m_HeaderStyle = new GUIStyle("Foldout");
				m_HeaderStyle.fontSize = 11;
				m_HeaderStyle.fontStyle = FontStyle.Bold;
			}
			return m_HeaderStyle;
		}
	}

}

