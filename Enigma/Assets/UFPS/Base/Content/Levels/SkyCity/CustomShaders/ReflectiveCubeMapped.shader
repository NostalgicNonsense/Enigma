Shader "ReflectiveCubeMapped"
{
	Properties 
	{
_Glossiness("_Glossiness", Range(0.1,1) ) = 0.4300518
_SpecularColor("_SpecularColor", Color) = (0.7058823,0.7058823,0.7058823,1)
_ReflectionCube("_ReflectionCube", Cube) = "black" {}
_Normal("_Normal", 2D) = "black" {}
_Diffuse("_Diffuse", 2D) = "black" {}
_ReflectionMask("_ReflectionMask", 2D) = "black" {}
_ReflectionMaskPower("_ReflectionMaskPower", Range(0,1) ) = 0.5
_Specular("Specular", 2D) = "black" {}

	}
	
	SubShader 
	{
		Tags
		{
"Queue"="Geometry"
"IgnoreProjector"="False"
"RenderType"="Opaque"

		}

		
Cull Back
ZWrite On
ZTest LEqual
ColorMask RGBA
Fog{
Mode Global
}


		CGPROGRAM
#pragma surface surf BlinnPhongEditor  vertex:vert
#pragma target 3.0


float _Glossiness;
float4 _SpecularColor;
samplerCUBE _ReflectionCube;
sampler2D _Normal;
sampler2D _Diffuse;
sampler2D _ReflectionMask;
float _ReflectionMaskPower;
sampler2D _Specular;

			struct EditorSurfaceOutput {
				half3 Albedo;
				half3 Normal;
				half3 Emission;
				half3 Gloss;
				half Specular;
				half Alpha;
				half4 Custom;
			};
			
			inline half4 LightingBlinnPhongEditor_PrePass (EditorSurfaceOutput s, half4 light)
			{
half3 spec = light.a * s.Gloss;
half4 c;
c.rgb = (s.Albedo * light.rgb + light.rgb * spec);
c.a = s.Alpha;
return c;

			}

			inline half4 LightingBlinnPhongEditor (EditorSurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
			{
				half3 h = normalize (lightDir + viewDir);
				
				half diff = max (0, dot ( lightDir, s.Normal ));
				
				float nh = max (0, dot (s.Normal, h));
				float spec = pow (nh, s.Specular*128.0);
				
				half4 res;
				res.rgb = _LightColor0.rgb * diff;
				res.w = spec * Luminance (_LightColor0.rgb);
				res *= atten * 2.0;

				return LightingBlinnPhongEditor_PrePass( s, res );
			}
			
			struct Input {
				float2 uv_Diffuse;
float3 viewDir;
float2 uv_Normal;
float3 worldRefl;
float2 uv_ReflectionMask;
float2 uv_Specular;
INTERNAL_DATA

			};

			void vert (inout appdata_full v, out Input o) {
float4 VertexOutputMaster0_0_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_1_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_2_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_3_NoInput = float4(0,0,0,0);


			}
			

			void surf (Input IN, inout EditorSurfaceOutput o) {
				o.Normal = float3(0.0,0.0,1.0);
				o.Alpha = 1.0;
				o.Albedo = 0.0;
				o.Emission = 0.0;
				o.Gloss = 0.0;
				o.Specular = 0.0;
				o.Custom = 0.0;
				
float4 Sampled2D0=tex2D(_Diffuse,IN.uv_Diffuse.xy);
float4 Tex2DNormal0=float4(UnpackNormal( tex2D(_Normal,(IN.uv_Normal.xyxy).xy)).xyz, 1.0 );
float4 Add0=float4( IN.viewDir.x, IN.viewDir.y,IN.viewDir.z,1.0 ) + Tex2DNormal0;
float4 WorldReflection0=float4( WorldReflectionVector (IN, Add0), 1.0);
float4 TexCUBE0=texCUBE(_ReflectionCube,WorldReflection0);
float4 Sampled2D1=tex2D(_ReflectionMask,IN.uv_ReflectionMask.xy);
float4 Multiply1=Sampled2D1 * _ReflectionMaskPower.xxxx;
float4 Lerp0=lerp(float4( 0.0, 0.0, 0.0, 0.0 ),TexCUBE0,Multiply1);
float4 Add1=Sampled2D0 + Lerp0;
float4 Sampled2D2=tex2D(_Specular,IN.uv_Specular.xy);
float4 Multiply0=Sampled2D2 * _SpecularColor;
float4 Master0_2_NoInput = float4(0,0,0,0);
float4 Master0_5_NoInput = float4(1,1,1,1);
float4 Master0_7_NoInput = float4(0,0,0,0);
float4 Master0_6_NoInput = float4(1,1,1,1);
o.Albedo = Add1;
o.Normal = Tex2DNormal0;
o.Specular = _Glossiness.xxxx;
o.Gloss = Multiply0;

				o.Normal = normalize(o.Normal);
			}
		ENDCG
	}
	Fallback "Diffuse"
}