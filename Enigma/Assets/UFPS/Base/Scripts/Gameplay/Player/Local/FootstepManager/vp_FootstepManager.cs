/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FootstepManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a class that works with vp_FPPlayerController and vp_FPCamera
//					to play footstep sounds based on the textures that controller
//					is currently over.
//
//					NOTE: this class is obsolete! please use the new vp_PlayerFootFXHandler
//					component along with a scene vp_SurfaceManager instead.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_FootstepManager : MonoBehaviour
{
	
	/// <summary>
	/// surface type object for storing sounds in relation to textures
	/// </summary>
	[System.Serializable]
	public class vp_SurfaceTypes
	{

		public Vector2 RandomPitch = new Vector2( 1.0f, 1.5f ); // random pitch range for footsteps
		public bool Foldout = true; // used by the editor to allow folding this surface type
		public bool SoundsFoldout = true; // used by the editor to allow folding the sounds section
		public bool TexturesFoldout = true; // used by the editor to allow folding the textures section
		public string SurfaceName = ""; // Name of the surface for reference in the editor
		public List<AudioClip> Sounds = new List<AudioClip>(); // List of sounds to play randomly
		public List<Texture> Textures = new List<Texture>(); // list of the textures for this surface

	}
	
	static vp_FootstepManager[] m_FootstepManagers;
	public static bool mIsDirty = true;

	/// <summary>
	/// Retrieves the list of item databases, finding all instances if necessary.
	/// </summary>

	static public vp_FootstepManager[] FootstepManagers
	{
		get
		{
			if (mIsDirty)
			{
				mIsDirty = false;
				
				m_FootstepManagers = GameObject.FindObjectsOfType(typeof(vp_FootstepManager)) as vp_FootstepManager[];
				
				// Alternative method, considers prefabs:
				if(m_FootstepManagers == null)
					m_FootstepManagers = Resources.FindObjectsOfTypeAll(typeof(vp_FootstepManager)) as vp_FootstepManager[];
			}
			return m_FootstepManagers;
		}
	}
	
	public List<vp_SurfaceTypes> SurfaceTypes = new List<vp_SurfaceTypes>(); // list of all the surfaces created
	public bool IsDirty{ get{ return mIsDirty; } }
	
	protected vp_FPPlayerEventHandler m_Player = null;		// for caching the player
	protected vp_FPCamera m_Camera = null;					// for caching the FPCamera
	protected vp_FPController m_Controller = null;			// for caching the FPController
	protected AudioSource m_Audio = null;					// for caching the audio component
	protected AudioClip m_SoundToPlay = null;				// the current sound to be played
	protected AudioClip m_LastPlayedSound = null;			// used to make sure we don't place the same sound twice in a row
	
	
	/// <summary>
	/// cache all the necessary properties here
	/// </summary>
	protected virtual void Awake()
	{

		Debug.LogWarning("Warning (" + this + ") This component is obsolete! please use the new vp_PlayerFootFXHandler component along with a scene vp_SurfaceManager instead.");

		m_Player = transform.root.GetComponentInChildren<vp_FPPlayerEventHandler>();
		m_Camera = transform.root.GetComponentInChildren<vp_FPCamera>();
		m_Controller = transform.root.GetComponentInChildren<vp_FPController>();
		m_Audio = transform.root.GetComponentInChildren<AudioSource>();
		if (m_Audio == null)
			m_Audio = gameObject.AddComponent<AudioSource>();
	
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void SetDirty(bool dirty)
	{
		mIsDirty = dirty;
	}
	
	
	/// <summary>
	/// 
	/// </summary>
	void Update()
	{

		// if the camera bob step callback is null for some reason,
		// add our footstep callback again
		if (m_Camera.BobStepCallback == null)
			m_Camera.BobStepCallback += Footstep;

	}
	
	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{
		
		// add the footstep callback
		m_Camera.BobStepCallback += Footstep;
		
	}
	
	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{
		
		// remove the footstep callback
		m_Camera.BobStepCallback -= Footstep;
		
	}
	
	
	/// <summary>
	/// Here is where we check to see if the texture
	/// under the controller is assigned to a surface.
	/// If so, play a sound.
	/// </summary>
	protected virtual void Footstep()
	{

		// dead players don't make footsteps
		if (m_Player.Dead.Active)
			return;

		// return if the controller is not on the ground
		if(!m_Controller.Grounded)
			return;
		
		// return if no texture and no surface identifier is found
		if (GetGroundTexture() == null && GetSurfaceIdentifier() == null)
			return;
		
		if (GetSurfaceIdentifier() != null)
		{
			PlaySound(SurfaceTypes[GetSurfaceIdentifier().SurfaceID]);
			return;
		}
		
		// loop through the surfaces
		foreach(vp_SurfaceTypes st in SurfaceTypes)
		{
			// loop through the surfaces textures
			foreach(Texture tex in st.Textures)
			{
				// if the texture is the same as the ground texture...
				if (tex == GetGroundTexture())
				{
					// play random surface sound
					PlaySound( st );
					break;
				}
			}
		}
		
	}
	

	/// <summary>
	/// 
	/// </summary>
	vp_SurfaceIdentifier GetSurfaceIdentifier()
	{

		if(m_Controller.GroundTransform == null)
			return null;

		return m_Controller.GroundTransform.GetComponent<vp_SurfaceIdentifier>();

	}

	
	/// <summary>
	/// Plays a random sound from the surface the
	/// controller is currently over
	/// </summary>
	public virtual void PlaySound( vp_SurfaceTypes st )
	{

		// if the audiosource is null, return
		if (m_Audio == null)
			return;

		// if the audiosource is not enabled, return
		if (!m_Audio.enabled)
			return;

		// return if there are no sounds
		if(st.Sounds == null || st.Sounds.Count == 0)
			return;

		reroll:
		m_SoundToPlay = st.Sounds[Random.Range(0,st.Sounds.Count)]; // get a random sound

		// if the sound is null, return
		if(m_SoundToPlay == null)
			return;
		
		// if the sound was the last sound played, reroll for another sound
		if (m_SoundToPlay == m_LastPlayedSound && st.Sounds.Count > 1)
			goto reroll;
		
		// set a random pitch
		m_Audio.pitch = Random.Range(st.RandomPitch.x, st.RandomPitch.y) * Time.timeScale;
		m_Audio.clip = m_SoundToPlay;

		m_Audio.Play(); // play the sound
		m_LastPlayedSound = m_SoundToPlay; // cache this sound
		
	}
	
	
	/// <summary>
	/// Returns the zero-based index of the most dominant texture
	/// on the main terrain at this world position.
	/// </summary>
    public static int GetMainTerrainTexture(Vector3 worldPos, Terrain terrain)
	{
		
		TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
 
        // calculate which splat map cell the worldPos falls within (ignoring y)
        int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);
 
        // get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
        float[,,] splatmapData = terrainData.GetAlphamaps(mapX,mapZ,1,1);
 
        // extract the 3D array data to a 1D array:
        float[] mix = new float[splatmapData.GetUpperBound(2)+1];
        for (int n=0; n<mix.Length; ++n)
        {
            mix[n] = splatmapData[0,0,n];    
        }
  
        float maxMix = 0;
        int maxIndex = 0;
 
        // loop through each mix value and find the maximum
        for (int n=0; n<mix.Length; ++n)
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
	/// returns the current mainTexture under the controller.
	/// gets the texture from terrain if over terrain, else it
	/// looks in the transform of the current object the controller
	/// is over
	/// </summary>
	public virtual Texture GetGroundTexture()
	{

		if (m_Controller.GroundTransform == null)
			return null;

		Terrain terrain = m_Controller.GroundTransform.GetComponent<Terrain>();
		Renderer renderer = m_Controller.GroundTransform.GetComponent<Renderer>();

		// return if no renderer and no terrain under the controller
		if (renderer == null && (terrain == null))
			return null;

		int terrainTextureID = -1;

		// check to see if a main texture can be retrieved from the terrain
		if (terrain != null)
		{
			terrainTextureID = vp_FootstepManager.GetMainTerrainTexture(transform.position, terrain);
			if (terrainTextureID > terrain.terrainData.splatPrototypes.Length - 1)
				return null;
		}
		else
		{
			// terrain is null, try to return object texture
			if ((renderer != null) && (renderer.sharedMaterial != null))
				return renderer.sharedMaterial.mainTexture;
			else
				return null;	// fail
		}

		// return terrain texture
		return terrain.terrainData.splatPrototypes[terrainTextureID].texture;

	}


}
