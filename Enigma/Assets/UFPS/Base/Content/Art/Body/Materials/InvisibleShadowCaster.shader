// Description: shader that is invisible but casts shadows
// verified to work in unity 5
// courtesy of Rogdor

Shader "Invisible/InvisibleShadowCaster" {
	SubShader {
		Tags { 
			"Queue"="Transparent"
			"RenderType"="Transparent" 
		}
		CGPROGRAM
		#pragma surface surf Lambert alpha addshadow

		struct Input {
			float nothing; // just a dummy because surf expects something
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Alpha = 0;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}

// --- below is a simpler shader that works on unity versions prior to unity 5 ---

//Shader "Transparent/InvisibleShadowCaster"
//{
//    Subshader
//    {
//       UsePass "VertexLit/SHADOWCOLLECTOR"    
//       UsePass "VertexLit/SHADOWCASTER"
//    }
// 
//    Fallback off
//}