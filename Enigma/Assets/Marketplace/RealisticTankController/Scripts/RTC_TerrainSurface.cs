//----------------------------------------------
//            Realistic Tank Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;

public class RTC_TerrainSurface : MonoBehaviour {

	public static float[] GetTextureMix(Vector3 worldPos) {
		
		// Returns an array containing the relative mix of textures
		// on the main terrain at this world position.
		
		// The number of values in the array will equal the number
		// of textures added to the terrain.
		
		Terrain terrain = Terrain.activeTerrain;

		if(terrain == null)
			return null;

		TerrainData terrainData = terrain.terrainData;
		Vector3 terrainPos = terrain.transform.position;

		if(terrainData == null)
			return null;
		
		// Calculate which splat map cell the worldPos falls within (ignoring y)
		int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
		int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);
		
		// Get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
		float[,,] splatmapData = terrainData.GetAlphamaps(mapX,mapZ,1,1);
		
		// Extract the 3D array data to a 1D array:
		float[] cellMix = new float[splatmapData.GetUpperBound(2)+1];
		for (int n=0; n < cellMix.Length; ++n)
		{
			cellMix[n] = splatmapData[0,0,n];    
		}
		
		return cellMix;        
		
	}
	
	public static int GetMainTexture(Vector3 worldPos) {
		
		// Returns the zero-based index of the most dominant texture
		// on the main terrain at this world position.
		
		float[] mix = GetTextureMix(worldPos);
		
		
		float maxMix = 0;
		int maxIndex = 0;
		
		// Loop through each mix value and find the maximum
		for (int n=0; n < mix.Length; ++n)
		{
			if (mix[n] > maxMix)
			{
				maxIndex = n;
				maxMix = mix[n];
			}
		}
		
		return maxIndex;
		
	}
	
}