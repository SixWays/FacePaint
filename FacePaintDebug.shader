Shader "Hidden/FacePaintDebug" {
	Properties {
		[HideInInspector] _Mask ("0:RGBA 1:R 2:G 3:B 4:A", Int) = 0
		[HideInInspector] _LUT ("LUT", 2D) = "white" {}
		[HideInInspector] _UseLUT ("Use LUT", Int) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

		ZWrite On

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
			};

			uniform int _Mask;
			uniform int _UseLUT;
			sampler2D _LUT;
			v2f vert (appdata v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				if (_Mask == 0){
					o.color = v.color;
				} else if (_Mask == 1){
					o.color = v.color.r;
				} else if (_Mask == 2){
					o.color = v.color.g;
				} else if (_Mask == 3){
					o.color = v.color.b;
				} else if (_Mask == 4){
					o.color = v.color.a;
				}
				if (_UseLUT == 1){
					o.color = tex2Dlod(_LUT, float4(o.color.r,0,0,0));
				}
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				return i.color;
			}
			ENDCG
		}
	}
	Fallback "Unlit"
}
