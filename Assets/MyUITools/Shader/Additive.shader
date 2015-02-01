Shader "Custom/Toydea/Additive" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
        _FlashColor ("Flash Color", Color) = (0,0,0,0)
	}

	SubShader {
		Tags { 
			"Queue"="Transparent" 
			"RenderType"="Transparent" 
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Fog { Mode Off }
		Blend One OneMinusSrcAlpha

		Pass {
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			struct appdata_t {
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
			};
			
			v2f vert(appdata_t IN) {
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color;
				return OUT;
			}

			sampler2D _MainTex;
            fixed4 _FlashColor;

			fixed4 frag(v2f IN) : COLOR {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                c.rgb += _FlashColor.rgb;
                c.rgb *= c.a;
                return c;
			}
		ENDCG
		}
	}
}
