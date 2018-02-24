/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MaterialUtility.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	miscellaneous Material related utility functions
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public static class vp_MaterialUtility
{
	

	/// <summary>
	/// sets rendering mode of 'material' to transparent
	/// </summary>
	public static void MakeMaterialTransparent(Material material)
	{

		material.SetFloat("_Mode", 2);
		material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		material.SetInt("_ZWrite", 0);
		material.DisableKeyword("_ALPHATEST_ON");
		material.EnableKeyword("_ALPHABLEND_ON");
		material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		material.renderQueue = 3000;

	}


	/// <summary>
	/// returns the likely color property name of 'material'
	/// </summary>
	public static string GetColorPropertyName(Material material)
	{

		if (material.HasProperty("_TintColor"))
			return "_TintColor";
		else
			return "_MainColor";

	}


}

