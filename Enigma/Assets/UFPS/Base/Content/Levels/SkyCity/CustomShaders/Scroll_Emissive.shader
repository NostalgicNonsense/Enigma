Shader "ScrollEmissive"
{
	Properties 
	{
_Diffuse("_Diffuse", 2D) = "black" {}
_EmissivePower("_EmissivePower", Range(0,10) ) = 0.5
_ScrollSpeed_X("_ScrollSpeed_X", Float) = 0
_ScrollSpeed_Y("_ScrollSpeed_Y", Float) = 0

	}
	
	SubShader 
	{
		Tags
		{
"Queue"="Transparent"
"IgnoreProjector"="True"
"RenderType"="Opaque"

		}

		
Cull Off
ZWrite On
ZTest LEqual
ColorMask RGBA
Blend SrcAlpha DstAlpha
Fog{
Mode Off
}


		CGPROGRAM
#pragma surface surf BlinnPhongEditor  nofog noambient nolightmap vertex:vert
#pragma target 2.0


sampler2D _Diffuse;
float _EmissivePower;
float _ScrollSpeed_X;
float _ScrollSpeed_Y;

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
				float4 meshUV;

			};

			void vert (inout appdata_full v, out Input o) {
float4 VertexOutputMaster0_0_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_1_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_2_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_3_NoInput = float4(0,0,0,0);

o.meshUV.xy = v.texcoord.xy;
o.meshUV.zw = v.texcoord1.xy;

			}
			

			void surf (Input IN, inout EditorSurfaceOutput o) {
				o.Normal = float3(0.0,0.0,1.0);
				o.Alpha = 1.0;
				o.Albedo = 0.0;
				o.Emission = 1.0;
				o.Gloss = 0.0;
				o.Specular = 0.0;
				o.Custom = 0.0;
				
float4 Multiply0=_Time * _ScrollSpeed_X.xxxx;
float4 Add0=Multiply0 + (IN.meshUV.xyxy);
float4 Multiply1=_Time * _ScrollSpeed_Y.xxxx;
float4 Add1=Multiply1 + (IN.meshUV.xyxy);
float4 Assemble0_2_NoInput = float4(0,0,0,0);
float4 Assemble0_3_NoInput = float4(0,0,0,0);
float4 Assemble0=float4(Add0.x, Add1.y, Assemble0_2_NoInput.z, Assemble0_3_NoInput.w);
float4 Tex2D0=tex2D(_Diffuse,Assemble0.xy);
float4 Multiply2=Tex2D0 * _EmissivePower.xxxx;
float4 Master0_0_NoInput = float4(0,0,0,0);
float4 Master0_1_NoInput = float4(0,0,1,1);
float4 Master0_3_NoInput = float4(0,0,0,0);
float4 Master0_4_NoInput = float4(0,0,0,0);
float4 Master0_7_NoInput = float4(0,0,0,0);
float4 Master0_6_NoInput = float4(1,1,1,1);
o.Emission = Multiply2;
o.Alpha = Multiply2;

				o.Normal = normalize(o.Normal);
			}
		ENDCG
	}
	Fallback "Diffuse"
}