// Unlit shader. Simplest possible textured shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "custom/Shadow2d/Light" {
Properties {
	_MainTex ("Base Texture", 2D) = "white" {}
	_CookieTex ("Cookie Texture", 2D) = "white" {}	
}

SubShader {
	Tags { "Queue" = "Transparent" }
      LOD 100
	
	Pass {  
	   Blend One One
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float2 cookieCoord: TEXCOORD1;
				fixed4 color: COLOR;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				half2 cookieCoord: TEXCOORD1;
				fixed4 color: COLOR;
			};
			
			sampler2D _MainTex;
			sampler2D _CookieTex;
			float4 _MainTex_ST;
			float4 _CookieTex_ST;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.cookieCoord = TRANSFORM_TEX(v.cookieCoord, _CookieTex);
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = 
					i.color					
					* tex2D(_MainTex, i.texcoord) 
					* tex2D(_CookieTex, i.cookieCoord) ;
				return col;
			}
		ENDCG
	}
}

}
