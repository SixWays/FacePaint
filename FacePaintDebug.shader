Shader "Hidden/FacePaintDebug" {
	Properties {
		 _Mask ("0:RGBA 1:R 2:G 3:B 4:A", Int) = 0
		 _LUT ("LUT", 2D) = "white" {}
		 _UseLUT ("Use LUT", Int) = 0
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
				fixed4 vcol = v.color;
				if (_UseLUT == 1){
					vcol = tex2Dlod(_LUT, float4(vcol.xy,0,0));
				}
				if (_Mask == 0){
					o.color = vcol;
				} else if (_Mask == 1){
					o.color = vcol.r;
				} else if (_Mask == 2){
					o.color = vcol.g;
				} else if (_Mask == 3){
					o.color = vcol.b;
				} else if (_Mask == 4){
					o.color = vcol.a;
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
