/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SurfaceManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this component can be used to trigger surface effects depending on
//					the textures of the target objects, removing the need for assigning
//					a vp_SurfaceIdentifier to every gameobject. it also has default
//					fallbacks for (potentially missing) impact- and surface types.
//
//					this system will try to derive the surface type of a raycast hit
//					from various sources, including single textures, textures among
//					multi-materials, and terrain textures. it is also possible to define
//					UV regions for textures inside its editor (i.e. for atlas maps).
//
//					if no vp_SurfaceManager is present, particle effects will only
//					trigger if the target object has a vp_SurfaceIdentifier component.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_SurfaceManager : MonoBehaviour
{

	/// <summary> internal editor variable </summary>
	public bool m_ShowHelp = true;

	public List<ObjectSurface> ObjectSurfaces = new List<ObjectSurface>();
	public DefaultFallbacks Fallbacks;

	protected static bool m_UsingFallbackImpact = false;
	protected static bool m_UsingFallbackSurface = false;

	protected static ObjectSurface[] m_DefaultMaterial = { new ObjectSurface() };
	public Rect DefaultUV = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

	// --- internal classes ---

	[System.Serializable]
	public class UVTexture
	{
		public UVTexture(bool init)
		{
			Texture = null;	
			UV = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
			ShowUV = false;
		}
		public Texture Texture;
		public Rect UV;
		public bool ShowUV;

	}
	
	[System.Serializable]
	public class DefaultFallbacks
	{
		public vp_ImpactEvent ImpactEvent;
		public vp_SurfaceType SurfaceType;
		public bool AllowDecals;
	}
	
	[System.Serializable]
	public class ObjectSurface
	{
		public ObjectSurface()
		{
			Name = "";
			UVTextures = new List<UVTexture>();
		}
		public string Name;
		public vp_SurfaceType SurfaceType;
		public List<UVTexture> UVTextures;
		public bool Foldout = true;
		public bool TexturesFoldout = true;

	}
	
	// ---

	// terrain status
	protected static bool m_HaveTerrain = false;
	protected static bool m_CachedHaveTerrain = false;
	protected static bool HaveTerrain
	{
		get
		{
			if (!m_CachedHaveTerrain)
			{
				m_HaveTerrain = (FindObjectOfType<Terrain>() != null);
				m_CachedHaveTerrain = true;
			}
			return m_HaveTerrain;
		}
	}


	// surface manager data
	protected static Dictionary<Texture, vp_SurfaceType> m_SurfacesByTerrainTexture = new Dictionary<Texture, vp_SurfaceType>();
	protected static Dictionary<Texture, ObjectSurface> m_NonUVSurfaces = new Dictionary<Texture, ObjectSurface>();
	protected static Dictionary<UVTexture, ObjectSurface> m_UVSurfaces = new Dictionary<UVTexture, ObjectSurface>();
	protected static Dictionary<Texture, List<UVTexture>> m_UVTexturesByTexture = new Dictionary<Texture, List<UVTexture>>();
	protected static Dictionary<vp_SurfaceType, Dictionary<vp_ImpactEvent, vp_SurfaceEffect>> m_ImpactFXBySurface = new Dictionary<vp_SurfaceType, Dictionary<vp_ImpactEvent, vp_SurfaceEffect>>();
	protected static Dictionary<Collider, bool> m_UVColliders = new Dictionary<Collider, bool>();
	protected static Dictionary<Collider, bool> m_MultiMatColliders = new Dictionary<Collider, bool>();

	// current level data
	protected static Dictionary<Collider, Terrain> m_TerrainsByCollider = new Dictionary<Collider, Terrain>();
	protected static Dictionary<Collider, vp_SurfaceIdentifier> m_SurfaceIdentifiersByCollider = new Dictionary<Collider, vp_SurfaceIdentifier>();
	protected static Dictionary<Collider, Texture> m_MainTexturesByCollider = new Dictionary<Collider, Texture>();
	protected static Dictionary<Texture, vp_SurfaceType> m_SurfaceTypesByTexture = new Dictionary<Texture, vp_SurfaceType>();
	protected static Dictionary<Collider, vp_SurfaceType> m_SurfacesTypesByCollider = new Dictionary<Collider, vp_SurfaceType>();
	protected static Dictionary<Collider, bool> m_DecalsAllowedByCollider = new Dictionary<Collider, bool>();
	static Dictionary<Collider, Mesh> m_MeshesByCollider = new Dictionary<Collider, Mesh>();
	static Dictionary<Collider, Renderer> m_RenderersByCollider = new Dictionary<Collider, Renderer>();

	// instance
	protected static vp_SurfaceManager m_Instance = null;
	public static vp_SurfaceManager Instance
	{
		get
		{
			if (!m_CachedInstance && (m_Instance == null))
			{
				m_Instance = Object.FindObjectOfType<vp_SurfaceManager>();
				// NOTE: without an instance in the scene, the following features will not work:
				// object surfaces, terrain surfaces and impact / surface fallbacks.
				// however, surface identifiers will still work.
				m_CachedInstance = true;
			}
			return m_Instance;
		}
	}
	static bool m_CachedInstance = false;


	/// <summary>
	/// 
	/// </summary>
	void Awake()
	{

		if (m_Instance != null)
		{
			Debug.LogWarning("Warning (" + this + ") There can only be one vp_SurfaceManager in the scene! (destroying self)");
			enabled = false;
			Destroy(this);
			return;
		}

		if (vp_DecalManager.Instance == null)
			vp_DecalManager.AutoAddTo(gameObject);

		// initialize system. this must be called at the start of every
		// new level, but for Unity versions prior to Unity 5.4 it couldn't
		// be in e.g. 'OnLevelWasLoaded' which only triggered when
		// 'Application.LoadLevel' was used
		Reset();

	}


	/// <summary>
	/// spawns the effect resulting from 'impactEvent' + 'surface'. if 'surface'
	/// is null, derives it from the raycast hit
	/// </summary>
	public static bool SpawnEffect(RaycastHit hit, vp_ImpactEvent impact, vp_SurfaceType surface = null, AudioSource audioSource = null, bool allowDecal = true)
	{

		// 'surface' is optional. if we don't already know it we try to derive it from the RaycastHit
		if (surface == null)
			surface = GetSurfaceType(hit);	// if this returns null we will rely on fallbacks

		if (allowDecal)
		{

			// a surface identifier can always force 'allowdecal' to false
			allowDecal = AllowsDecals(hit.collider);

			// test against stretched decals on non-uniform objects
			if (allowDecal									// if decal is allowed ...
				&& (vp_DecalManager.Instance != null)		// ... and we have a decalmanager ...
				&& !vp_DecalManager.Instance.AllowStretchedDecals	// ... which is concered about stretching ...
				&& !hit.transform.gameObject.isStatic)		// ... then unless the target is static ... (decals don't get childed to static objects so there won't be stretching)
			{
				// ... then only allow decal in case the object has uniform scale!
				// (but use an epsilon of '0.00001' in case there is a slight but insignificant scale difference)
				allowDecal = vp_MathUtility.IsUniform(hit.transform.localScale, 0.00001f);
			}
		}

		vp_SurfaceEffect fx = GetResultingEffect(impact, surface, ref allowDecal);

		if (fx == null)
			return false;

		if (allowDecal)
			fx.SpawnWithDecal(hit, audioSource);
		else
			fx.Spawn(hit, audioSource);

		return true;

	}


	/// <summary>
	/// spawns the effect resulting from an impactEvent and the surface
	/// detected at the raycasthit
	/// </summary>
	public static bool SpawnEffect(RaycastHit hit, vp_ImpactEvent impactEvent, AudioSource audioSource, bool allowDecal = true)
	{
		return SpawnEffect(hit, impactEvent, null, audioSource, allowDecal);
	}


	/// <summary>
	/// spawns the footprint effect resulting from the 'impactEvent' + 'surface'.
	/// 'footprintDirection' should be the forward vector of the foot. if
	/// 'footprintFlip' is true, the decal's X-scale will be inverted. if
	/// 'footprintVerifyGroundContact' is true (not recommended) the decal
	/// system will perform four extra raycasts per footstep (!) to verify
	/// ground contact
	/// </summary>
	public static void SpawnFootprintEffect(RaycastHit hit, vp_SurfaceType surface, vp_ImpactEvent impactEvent, Vector3 footprintDirection, bool footprintFlip, bool footprintVerifyGroundContact = false, AudioSource audioSource = null, bool allowDecal = true)
	{

		// set footprint flags on the vp_SurfaceEffect system before spawning
		vp_SurfaceEffect.FootprintDirection = footprintDirection;
		vp_SurfaceEffect.FootprintFlip = footprintFlip;
		vp_SurfaceEffect.FootprintVerifyGroundContact = footprintVerifyGroundContact;

		SpawnEffect(hit, impactEvent, surface, audioSource, allowDecal);

		vp_SurfaceEffect.FootprintDirection = vp_SurfaceEffect.NO_DIRECTION;
		vp_SurfaceEffect.FootprintVerifyGroundContact = false;

	}

	
	/// <summary>
	/// spawns the footprint effect resulting from 'impactEvent' + the surface
	/// detected at the raycasthit. 'footprintDirection' should be the forward
	/// vector of the foot. if 'footPrintFlip' is true, the decal's X-scale
	/// will be inverted
	/// </summary>
	public static void SpawnFootprintEffect(RaycastHit hit, vp_ImpactEvent impactEvent, Vector3 footprintDirection, bool footprintFlip, bool footprintVerifyGroundContact = false, AudioSource audioSource = null, bool allowDecal = true)
	{
		SpawnFootprintEffect(hit, null, impactEvent, footprintDirection, footprintFlip, footprintVerifyGroundContact, audioSource, allowDecal);
	}


	/// <summary>
	/// returns the surface effect stored in 'surfaceType' under 'impact'
	/// (or null if no match)
	/// </summary>
	protected static vp_SurfaceEffect GetPrimaryEffect(vp_SurfaceType surfaceType, vp_ImpactEvent impact)
	{

		if (impact == null)
			return null;

		Dictionary<vp_ImpactEvent, vp_SurfaceEffect> impacts = GetImpactFXDictionary(surfaceType);

		if (impacts == null)
			return null;

		if (impacts.Count == 0)
			return null;

		vp_SurfaceEffect fx;
		impacts.TryGetValue(impact, out fx);
		return fx;

	}


	/// <summary>
	/// arrives at the most suitable surface effect based on one combo
	/// of surfaceType + impactEvent (or predefined fallbacks) plus the
	/// merged 'allowDecal' settings (from the calling method, any surface
	/// identifiers and the effect itself)
	/// </summary>
	protected static vp_SurfaceEffect GetResultingEffect(vp_ImpactEvent impact, vp_SurfaceType surface, ref bool allowDecals)
	{

		m_UsingFallbackImpact = false;
		m_UsingFallbackSurface = false;

		// if no IMPACT EVENT was provided - attempt to use fallback impact
		if ((impact == null) && (Instance != null))
		{
			impact = Instance.Fallbacks.ImpactEvent;
			m_UsingFallbackImpact = true;
		}

		if (impact == null)
			return null;

		// if no SURFACE TYPE could be found - attempt to use fallback surface
		if ((surface == null) && (Instance != null))
		{
			surface = Instance.Fallbacks.SurfaceType;
			m_UsingFallbackSurface = true;
		}

		if (surface == null)
			return null;

		if (surface.ImpactFX == null)
			return null;

		if (surface.ImpactFX.Count == 0)
			return null;

		vp_SurfaceEffect fx = GetPrimaryEffect(surface, impact);

		// if fx is null here and we are not using a fallback surface, it means the
		// level surface did not contain our impact event: so use fallback surface
		if ((fx == null) && !m_UsingFallbackSurface && (Instance != null))
		{
			surface = Instance.Fallbacks.SurfaceType;
			fx = GetPrimaryEffect(surface, impact);
		}

		// if fx is null here, the detected surface does not recognize the impact
		// event, so try again with the SurfaceManager's fallback impact event
		// (this can solve cases where the surface is the fallback surface and the
		// impact type is a new, unknown impact type that has not been assigned to
		// anything)
		if (fx == null)
			fx = GetPrimaryEffect(surface, Instance.Fallbacks.ImpactEvent);

		// if fx is null here, we have nothing to work with: abort
		if (fx == null)
			return null;

		// we have an effect! determine if it can be spawned with a decal
		// (global fallbacks are allowed to override 'allowDecals')
		if (allowDecals && (fx.Decal.m_Prefabs.Count > 0))
		{
			if (m_UsingFallbackSurface || m_UsingFallbackImpact)
			{
				allowDecals = Instance.Fallbacks.AllowDecals;
			}
		}
		else
		{
			allowDecals = false;	// spawn with sounds & objects only
		}

		return fx;

	}


	/// <summary>
	/// retrieves the dictionary of impact effects stored in a certain surfacetype object
	/// </summary>
	protected static Dictionary<vp_ImpactEvent, vp_SurfaceEffect> GetImpactFXDictionary(vp_SurfaceType surface)
	{

		if (surface == null)
			return null;

		//Debug.Log("surface : " + surface);

		Dictionary<vp_ImpactEvent, vp_SurfaceEffect> dict;
		if (!m_ImpactFXBySurface.TryGetValue(surface, out dict))
		{

			Dictionary<vp_ImpactEvent, vp_SurfaceEffect> impactFX = new Dictionary<vp_ImpactEvent, vp_SurfaceEffect>();
			for (int v = 0; v < surface.ImpactFX.Count; v++)
			{

				if (surface.ImpactFX[v].ImpactEvent == null)
					continue;

				if (impactFX.ContainsKey(surface.ImpactFX[v].ImpactEvent))
				{
					Debug.LogWarning("Warning (vp_SurfaceManager) Surface Type '"+surface+"' has more than one '"+surface.ImpactFX[v].ImpactEvent+"' added. Only the first one will be used.");
					continue;
				}
				impactFX.Add(surface.ImpactFX[v].ImpactEvent, surface.ImpactFX[v].SurfaceEffect);
			}
			//Debug.Log("impactFX: " + impactFX);

			m_ImpactFXBySurface.Add(surface, impactFX);
			dict = impactFX;

		}

		return dict;
	}

	
	/// <summary>
	///  gets the mesh of a collider. this is used for determining the
	///  texture coordinate of a hit point
	/// </summary>
	protected static Mesh GetMesh(Collider col)
    {

		if (col == null)
			return null;

		if (col.isTrigger)
			return null;

		Mesh mesh;
		if (!m_MeshesByCollider.TryGetValue(col, out mesh))
		{

			// try to find a mesh filter on the collider's transform
			MeshFilter meshFilter = col.GetComponent<MeshFilter>();

			// on fail, try to find a mesh filter in children
			if (meshFilter == null)
				meshFilter = col.transform.GetComponentInChildren<MeshFilter>();

			// on fail, try to find a mesh filter on parent
			if ((meshFilter == null) && (col.transform.parent != null))
				meshFilter = col.transform.parent.GetComponent<MeshFilter>();

			// on fail, try to find a mesh filter in all of the root object's hierarchy
			if ((meshFilter == null) && (col.transform.root != col.transform))
				meshFilter = col.transform.root.GetComponentInChildren<MeshFilter>();

			if (meshFilter == null)
				mesh = null;
			else
				mesh = ((meshFilter.sharedMesh != null) ? meshFilter.sharedMesh : meshFilter.mesh);

			// store what we got (even if null)
			m_MeshesByCollider.Add(col, mesh);

		}

		return mesh;

	}


	/// <summary>
	/// returns the main renderer of a collider (if any)
	/// </summary>
	protected static Renderer GetRenderer(Collider col)
	{

		if (col == null)
			return null;

		if (col.isTrigger)
			return null;

		if (col is TerrainCollider)		// Unity terrains have no renderers
			return null;

		Renderer renderer;
		if (!m_RenderersByCollider.TryGetValue(col, out renderer))
		{

			// try to find a renderer on the collider's transform
			renderer = col.GetComponent<Renderer>();

			// on fail, try to find a renderer in children
			if (renderer == null)
				renderer = col.transform.GetComponentInChildren<Renderer>();

			// on fail, try to find a renderer on parent
			if ((renderer == null) && (col.transform.parent != null))
				renderer = col.transform.parent.GetComponent<Renderer>();

			// on fail, try to find a renderer in all of the root object's hierarchy
			if ((renderer == null) && (col.transform.root != col.transform))
				renderer = col.transform.root.GetComponentInChildren<Renderer>();

			// skinned mesh renderers can not have their trianges fetched
			if (renderer != null && renderer is SkinnedMeshRenderer)
				renderer = null;

			// store what we got (even if null)
			m_RenderersByCollider.Add(col, renderer);

		}

		return renderer;

	}

	
	/// <summary>
	/// takes a texture coordinate and adjusts it for flipping, scale, offset and
	/// tiling. used for determining the exact texture coordinate of a hit point
	/// </summary>
	protected static Vector2 AdjustTextureCoord(Vector2 textureCoord, Material mat)
	{

		// material tiling
		textureCoord.x *= mat.mainTextureScale.x;
		textureCoord.y *= mat.mainTextureScale.y;

		// material offset
		textureCoord.x += mat.mainTextureOffset.x;
		textureCoord.y -= mat.mainTextureOffset.y;
		
		// tiling on mesh
		textureCoord.x %= 1;
		textureCoord.y %= 1;

		// back projection
		if (textureCoord.x < 0)
			textureCoord.x = 1 - Mathf.Abs(textureCoord.x);

		if (textureCoord.y < 0)
			textureCoord.y = 1 - Mathf.Abs(textureCoord.y);

		// flip UV upside down to have XY coordinates make more sense in the editor.
		// (comment out this line if you prefer 'proper' UV coordinates)
		textureCoord.y = 1 - textureCoord.y;
		
		return textureCoord;

	}

	
	/// <summary>
	/// returns the surface type at a raycast hit point. detects surfaces by
	/// surface identifier, texture / material, atlas texture and terrain
	/// </summary>
	public static vp_SurfaceType GetSurfaceType(RaycastHit hit)
	{

		// TIP: if you have problems getting the correct footstep- or projectile
		// FX to spawn on an object, uncomment all the debug lines in this method
		// to see what 'GetSurfaceType' detects (if anything)

		vp_SurfaceType s = null;

		// --- surface identifier ---
		// detect objects with a surface identifier
		vp_SurfaceIdentifier i = GetSurfaceIdentifier(hit);
		if (i != null)
		{

			s = i.SurfaceType;
			//Debug.Log("hit a surface identifier '" + s .name + "' @ " + Time.time);
			if (s != null)
			{
				return s;
			}
		} 

		// --- simple surface ---
		// detect objects with a single material and no texture regions
		s = GetSimpleSurface(hit);
		if (s != null)
		{
			//Debug.Log("hit a single material object with surface '" + s.name + "' @ " + Time.time);
			return s;
		}

		// --- complex surface ---
		// detect objects with texture regions (atlases) and / or multiple materials
		s = GetComplexSurface(hit);
		if (s != null)
		{
			//Debug.Log("hit a multi-material (or uv region fallback) object with surface '" + s.name + "' @ " + Time.time);
			return s;
		}

		// --- terrain surface ---
		// check the terrain for a surface if all of the above failed
		s = GetTerrainSurface(hit);
		if (s != null)
		{
			//Debug.Log("hit a terrain object with surface '" + s.name + "' @ " + Time.time);
			return s;
		}

		// --- no surface ---
		// failed to find a surface based on the raycast hit
		//Debug.Log("failed to find a surface on object '"+hit.transform.gameObject.name+"'");
		return null;

	}
	

	/// <summary>
	/// returns the surface type of a single material and no texture regions,
	/// by raycast hit point
	/// </summary>
	protected static vp_SurfaceType GetSimpleSurface(RaycastHit hit)
	{

		if (hit.collider == null)
			return null;

		vp_SurfaceType s = null;

		if (!m_SurfacesTypesByCollider.TryGetValue(hit.collider, out s))
		{

			if (!HasMultiMaterial(hit.collider))
			{

				Texture tex = GetMainTexture(hit);
				if ((tex != null) && (!IsUVTexture(tex)))
				{
					s = GetNonUVSurface(tex);
				}
			}

			// associate final result to this collider from now on, whether null or not
			m_SurfacesTypesByCollider.Add(hit.collider, s);

		}

		return s;

	}


	/// <summary>
	/// returns the surface type of a multi material object, or object
	/// with an atlas texture, by raycast hit point
	/// </summary>
	protected static vp_SurfaceType GetComplexSurface(RaycastHit hit)
	{

		Texture tex = null;
		Material mat = null;

		if (!HasMultiMaterial(hit.collider))
		{
			//Debug.Log("1");
			tex = GetMainTexture(hit);
		}
		else
		{
			mat = GetHitMaterial(hit);
			if (mat != null)
			{
				//Debug.Log("3");
				tex = mat.mainTexture;
			}
		}

		if (tex == null)
		{
			//Debug.Log("4");
			return null;
		}

		if (!IsUVTexture(tex))
		{
			//Debug.Log("5");
			return GetNonUVSurface(tex);
		}

		if (mat == null)
		{
			//Debug.Log("6");
			return null;
		}

		//Debug.Log("7");

		return GetUVSurface(mat, hit);

	}
	

	/// <summary>
	/// returns a surface type by texture
	/// </summary>
	protected static vp_SurfaceType GetNonUVSurface(Texture texture)
	{

		if (texture == null)
			return null;

		vp_SurfaceType type;
		if (!m_SurfaceTypesByTexture.TryGetValue(texture, out type))
		{
			ObjectSurface s;
			m_NonUVSurfaces.TryGetValue(texture, out s);

			if (s != null)
			{
				type = s.SurfaceType;
				m_SurfaceTypesByTexture.Add(texture, s.SurfaceType);
			}
			else
			{
				m_SurfaceTypesByTexture.Add(texture, null);
			}
		}

		return type;

	}


	/// <summary>
	/// returns a surface type from an object with an atlas (UV region) texture.
	/// </summary>
	protected static vp_SurfaceType GetUVSurface(Material mat, RaycastHit hit)
	{

		if (mat == null)
			return null;

		if (mat.mainTexture == null)
			return null;

		List<UVTexture> UVTextures;
		if (m_UVTexturesByTexture.TryGetValue(mat.mainTexture, out UVTextures))
		{

			for (int v = 0; v < UVTextures.Count; v++)
			{
				if (UVTextures[v].UV.Contains(AdjustTextureCoord(hit.textureCoord, mat)))
				{
					ObjectSurface ls;
					if (m_UVSurfaces.TryGetValue(UVTextures[v], out ls))
					{
						if (ls == null)
							return null;

						return ls.SurfaceType;
					}
				}
			}
		}

		if (!(hit.collider is MeshCollider))
			Debug.LogWarning("Warning (" + hit.collider.ToString().Replace("UnityEngine.", "") + ") Surface UV regions are only supported on MeshColliders (failed to spawn surface fx).");

		return null;

	}


	/// <summary>
	/// returns the surface type on the terrain position of the raycast hit
	/// point (if any)
	/// </summary>
	static vp_SurfaceType GetTerrainSurface(RaycastHit hit)
	{

		if (!HaveTerrain)
			return null;

		Texture tex = GetTerrainTexture(hit.collider, hit.point);
		if (tex == null)
			return null;

		vp_SurfaceType t;
		m_SurfacesByTerrainTexture.TryGetValue(tex, out t);
		return t;
	
	}
	

	/// <summary>
	/// retrieves, caches and returns surface identifier of the raycast hit
	/// point (if any)
	/// </summary>
	public static vp_SurfaceIdentifier GetSurfaceIdentifier(RaycastHit hit)
	{

		if (hit.collider == null)
			return null;

		vp_SurfaceIdentifier si;
		if (!m_SurfaceIdentifiersByCollider.TryGetValue(hit.collider, out si))
		{

			// try to find a surface identifier on the collider's transform
			si = hit.collider.GetComponent<vp_SurfaceIdentifier>();

			// on fail, try to find a surface identifier in children
			if (si == null)
				si = hit.collider.transform.GetComponentInChildren<vp_SurfaceIdentifier>();

			// TEST: the below may not work very well  with complex hierarchies
			// where gameobjects of the same 'type' are grouped under 'folder'
			// gameobjects

			// on fail, try to find a surface identifier on parent
			//if ((si == null) && (hit.collider.transform.parent != null))
			//	si = hit.collider.transform.parent.GetComponent<vp_SurfaceIdentifier>();

			//// on fail, try to find a surface identifier in all of the root object's hierarchy
			//if ((si == null) && (hit.collider.transform.root != hit.collider.transform))
			//	si = hit.collider.transform.root.GetComponentInChildren<vp_SurfaceIdentifier>();

			// store what we got (even if null)
			m_SurfaceIdentifiersByCollider.Add(hit.collider, si);
			if ((si != null) && (!m_DecalsAllowedByCollider.ContainsKey(hit.collider)))
				m_DecalsAllowedByCollider.Add(hit.collider, si.AllowDecals);

		}

		return si;

	}



	/// <summary>
	/// caches and returns the main texture of an object's renderer. returns
	/// null if the object has no renderer, material or main texture.
	/// NOTE: this is NOT FOR TERRAINS (instead use 'GetTerrainTexture')
	/// </summary>
	public static Texture GetMainTexture(RaycastHit hit)
	{

		if (hit.collider == null)
			return null;

		Texture mainTexture;
		if (!m_MainTexturesByCollider.TryGetValue(hit.collider, out mainTexture))
		{

			Renderer r = GetRenderer(hit.collider);
			if ((r != null) && (r.sharedMaterial != null) && (r.sharedMaterial.mainTexture != null))
			{
				mainTexture = r.sharedMaterial.mainTexture;
				m_MainTexturesByCollider.Add(hit.collider, mainTexture);
			}
			else
				m_MainTexturesByCollider.Add(hit.collider, null);

		}

		return mainTexture;

	}
	

	/// <summary>
	/// returns the texture at the raycast hit point of an object with multiple materials
	/// </summary>
	protected static Material GetHitMaterial(RaycastHit hit)
	{

		if (hit.triangleIndex < 0)	// this will abort SkinnedMeshRenderers
			return null;

		Mesh mesh = GetMesh(hit.collider);

		if (mesh == null)
			return null;

		if (!mesh.isReadable)
			return null;

		if (mesh.triangles == null)
			return null;

		int[] triangle = new int[] 
		{
			mesh.triangles[hit.triangleIndex * 3], 
			mesh.triangles[hit.triangleIndex * 3 + 1], 
			mesh.triangles[hit.triangleIndex * 3 + 2] 
		};
		
		for (int v = 0; v < mesh.subMeshCount; v++)
		{
			int[] subMeshTriangles = mesh.GetTriangles(v);
			for (int v2 = 0; v2 < subMeshTriangles.Length; v2 += 3)
			{
				if ((subMeshTriangles[v2] == triangle[0])
					&& (subMeshTriangles[v2 + 1] == triangle[1])
					&& (subMeshTriangles[v2 + 2] == triangle[2]))
				{
					Renderer r = GetRenderer(hit.collider);
					if (r == null)
						continue;
					if (r.sharedMaterials == null)
						continue;
					if (r.sharedMaterials.Length < v + 1)
						continue;
					if (r.sharedMaterials[v] == null)
						continue;
					//Debug.Log("hit: " + r.sharedMaterials[v].mainTexture);
					return r.sharedMaterials[v];
				}
			}
		}

		return null;

	}


	/// <summary>
	/// returns the dominant terrain texture at a terrain position
	/// </summary>
	public static Texture GetTerrainTexture(Collider col, Vector3 position)
	{

		if (col == null)
			return null;

		// retrieve and cache terrain of ground object
		Terrain terrain;
		if (!m_TerrainsByCollider.TryGetValue(col, out terrain))
		{
			terrain = col.GetComponent<Terrain>();
			m_TerrainsByCollider.Add(col, terrain);
		}

		if (terrain == null)
			return null;

		// return dominant ground texture at current position in terrain
		int terrainTextureID = -1;
		terrainTextureID = GetDominantTerrainTexture(position, terrain);
		if (terrainTextureID > terrain.terrainData.splatPrototypes.Length - 1)
			return null;

		return terrain.terrainData.splatPrototypes[terrainTextureID].texture;

	}


	/// <summary>
	/// Returns the zero-based index of the most dominant texture
	/// on the main terrain at this world position.
	/// </summary>
	public static int GetDominantTerrainTexture(Vector3 worldPos, Terrain terrain)
	{

		if (terrain == null)
			return 0;

		TerrainData terrainData = terrain.terrainData;
		Vector3 terrainPos = terrain.transform.position;

		// calculate which splat map cell the worldPos falls within (ignoring y)
		int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
		int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

		// get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
		float[, ,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

		// extract the 3D array data to a 1D array:
		float[] mix = new float[splatmapData.GetUpperBound(2) + 1];
		for (int n = 0; n < mix.Length; ++n)
		{
			mix[n] = splatmapData[0, 0, n];
		}

		float maxMix = 0;
		int maxIndex = 0;

		// loop through each mix value and find the maximum
		for (int n = 0; n < mix.Length; ++n)
		{
			if (mix[n] > maxMix)
			{
				maxIndex = n;
				maxMix = mix[n];
			}
		}

		return maxIndex;

	}


	/// <summary>
	/// returns a list of all the textures attached to all the terrains in the level
	/// </summary>
	protected List<Texture> GetLevelTerrainTextures()
	{

		Terrain[] terrains = FindObjectsOfType<Terrain>();

		if (terrains.Length == 0)
			return null;

		List<Texture> textures = new List<Texture>();

		for (int v = 0; v < terrains.Length; v++)
		{
			if (terrains[v] == null)
				continue;
			if (terrains[v].terrainData == null)
				continue;
			if (terrains[v].terrainData.splatPrototypes == null)
				continue;
			if (terrains[v].terrainData.splatPrototypes.Length < 1)
				continue;
			for (int s = 0; s < terrains[v].terrainData.splatPrototypes.Length; s++)
			{
				if (terrains[v].terrainData.splatPrototypes[s].texture == null)
					continue;
				if (textures.Contains(terrains[v].terrainData.splatPrototypes[s].texture))
					continue;
				textures.Add(terrains[v].terrainData.splatPrototypes[s].texture);
			}
		}

		return textures;

	}


	/// <summary>
	/// returns true if 'texture' occurs more than once in the surface
	/// manager. NOTE: this should only be called on initialization and
	/// not at runtime
	/// </summary>
	protected bool IsDuplicateObjectTexture(Texture texture)
	{

		int count = 0;
		for (int surf = 0; surf < ObjectSurfaces.Count; surf++)
		{
			for (int tex = 0; tex < ObjectSurfaces[surf].UVTextures.Count; tex++)
			{
				if (ObjectSurfaces[surf] == null)
					continue;
				if (ObjectSurfaces[surf].UVTextures == null)
					continue;
				if (ObjectSurfaces[surf].UVTextures[tex].Texture == null)
					continue;
				if (ObjectSurfaces[surf].UVTextures[tex].Texture == texture)
					count++;
			}
		}

		return count > 1;

	}


	/// <summary>
	/// returns false if collider 'col' has a surface identifier that does not 
	/// allow decals. otherwise returns true
	/// </summary>
	protected static bool AllowsDecals(Collider col)
	{

		if (col == null)
			return false;

		bool allowed = true;
		if (!m_DecalsAllowedByCollider.TryGetValue(col, out allowed))
			return true;
		return allowed;

	}


	/// <summary>
	/// returns true if the texture 'tex' has UV regions defined in the surface
	/// manager, otherwise returns false
	/// </summary>
	protected static bool IsUVTexture(Texture tex)
	{

		return m_UVTexturesByTexture.ContainsKey(tex);

	}


	/// <summary>
	/// returns true if the object associated with collider 'col' has multiple materials,
	/// otherwise returns false
	/// </summary>
	protected static bool HasMultiMaterial(Collider col)
	{

		if (col == null)
			return false;

		bool result = false;
		if (!m_MultiMatColliders.TryGetValue(col, out result))
		{

			Renderer r = GetRenderer(col);
			if (r != null)
			{

				result = (r.sharedMaterials.Length > 1);

				// guard against static renderers
				//	(Unity will not allow accessing the triangles of a static renderer)
				if ((result == true) && r.gameObject.isStatic)
				{
					//Debug.LogWarning("Warning (vp_SurfaceManager) '" + col.gameObject.name + "' has multiple materials but its Renderer is static. To use surface effects on it, make the Renderer non-static or split it into separate, single-material static objects and assign a vp_SurfaceIdentifier to each.");
					result = false;
				}

			}

			// guard against mesh colliders that are not the same as the rendered mesh
			//	(we must be able to map the hit collider triangle to an identical rendered triangle)
			if ((result == true) && (col is MeshCollider))
			{
				Mesh m = GetMesh(col);
				if ((m != null) && (m != (col as MeshCollider).sharedMesh))
				{
					Debug.LogWarning("Warning (vp_SurfaceManager) '" + col.gameObject.name + "' has multiple Materials and a MeshCollider. For surface effects to work with MeshColliders, the Renderer and Collider must share the same mesh.");
					result = false;
				}
			}

			m_MultiMatColliders.Add(col, result);
		}

		return result;

	}


	/// <summary>
	/// stores all the textures added as surface fallback to EITHER the
	/// 'm_NonUVSurfaces' dictionary (if they have default UV (0, 0, 1, 1))
	/// OR the 'm_UVSurfaces' dictionary (if they have any other UV)
	/// </summary>
	protected void InitObjectSurfaces()
	{

		for (int surf = 0; surf < ObjectSurfaces.Count; surf++)
		{
			for (int tex = 0; tex < ObjectSurfaces[surf].UVTextures.Count; tex++)
			{
				if (ObjectSurfaces[surf] == null)
					continue;
				if (ObjectSurfaces[surf].UVTextures == null)
					continue;
				if (ObjectSurfaces[surf].UVTextures[tex].Texture == null)
					continue;
				if (m_UVSurfaces.ContainsKey(ObjectSurfaces[surf].UVTextures[tex]))
				{
					// this is a duplicate
					//Debug.Log("duplicate!");
					continue;
				}
				if (ObjectSurfaces[surf].UVTextures[tex].UV == DefaultUV
					&& !IsDuplicateObjectTexture(ObjectSurfaces[surf].UVTextures[tex].Texture))
				{
					// simple surface (no UV)
					//Debug.Log("adding with SIMPLE UV: " + ObjectSurfaces[surf].UVTextures[tex].Texture + ", " +ObjectSurfaces[surf].UVTextures[tex].UV+ ", " + ObjectSurfaces[surf]);
					m_NonUVSurfaces.Add(ObjectSurfaces[surf].UVTextures[tex].Texture, ObjectSurfaces[surf]);
				}
				else
				{
					// complex surface (has UV)
					//Debug.Log("adding with COMPLEX UV: " + ObjectSurfaces[surf].UVTextures[tex].Texture + ", " + ObjectSurfaces[surf].UVTextures[tex].UV + ", " + ObjectSurfaces[surf]);

					m_UVSurfaces.Add(ObjectSurfaces[surf].UVTextures[tex], ObjectSurfaces[surf]);
					List<UVTexture> rects;
					if (!m_UVTexturesByTexture.TryGetValue(ObjectSurfaces[surf].UVTextures[tex].Texture, out rects))
					{
						// found no list of rects: create
						rects = new List<UVTexture>();
						rects.Add(ObjectSurfaces[surf].UVTextures[tex]);
						m_UVTexturesByTexture.Add(ObjectSurfaces[surf].UVTextures[tex].Texture, rects);
					}
					else
					{
						// found a list of rects: add new rect to list
						rects.Add(ObjectSurfaces[surf].UVTextures[tex]);
					}

				}
			}

		}
	}
	

	/// <summary>
	/// composes a dictionary of all the terrain textures in the level
	///	and their associated surface types
	/// </summary>
	protected void InitTerrainSurfaces()
	{

		List<Texture> levelTerrainTextures = GetLevelTerrainTextures();

		if (levelTerrainTextures == null)
			return;

		foreach (Texture tex in levelTerrainTextures)
		{

			// see if this terrain texture has been added to the surface manager as a NON-UV texture
			vp_SurfaceType surfaceType = GetNonUVSurface(tex);

			// if not, complain with a warning and skip it
			if (surfaceType == null)
			{
				if(IsUVTexture(tex))
					Debug.LogWarning("Warning (SurfaceManager) Terrain texture '" + tex.name + "' has an UV region in the Surface Manager. UV regions do not work with terrain textures. Ground with this texture will fallback to the default surface type.");
				else
					Debug.LogWarning("Warning (SurfaceManager) Terrain texture '" + tex.name + "' has not been added to the Surface Manager. Ground with this texture will fallback to the default surface type.");
				continue;
			}

			// if so, add this surface type to the terrain surface dictionary
			m_SurfacesByTerrainTexture.Add(tex, surfaceType);

		}

	}


	/// <summary>
	/// clears dictionaries used for caching and resets all variables
	/// </summary>
	public virtual void Reset()
	{

		m_SurfacesByTerrainTexture.Clear();
		m_NonUVSurfaces.Clear();
		m_UVSurfaces.Clear();
		m_UVTexturesByTexture.Clear();
		m_UVColliders.Clear();
		m_MultiMatColliders.Clear();
		m_ImpactFXBySurface.Clear();
		m_TerrainsByCollider.Clear();
		m_SurfaceIdentifiersByCollider.Clear();
		m_MainTexturesByCollider.Clear();
		m_SurfaceTypesByTexture.Clear();
		m_SurfacesTypesByCollider.Clear();
		m_DecalsAllowedByCollider.Clear();
		m_MeshesByCollider.Clear();
		m_RenderersByCollider.Clear();
		
		InitObjectSurfaces();

		m_CachedInstance = false;
		m_CachedHaveTerrain = false;
		m_HaveTerrain = false;

		InitTerrainSurfaces();

		vp_SurfaceEffect.FootprintDirection = vp_SurfaceEffect.NO_DIRECTION;
		vp_SurfaceEffect.FootprintVerifyGroundContact = false;

	}


}
