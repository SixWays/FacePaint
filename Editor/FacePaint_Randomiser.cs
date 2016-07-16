using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Sigtrap.FacePaint{
	public class FacePaint_Randomiser : IFacePaintPlugin {
		float _h, _s, _v, _a;
		bool _perPoly = true;

		public string title {
			get {
				return "Randomiser";
			}
		}
		Color Randomise(Color c){
			float h, s, v, a;
			Color.RGBToHSV(c, out h, out s, out v);

			h += Random.Range(-_h, _h);
			if (h > 1){h -= 1;}
			if (h < 0){h += 1;}

			s += Random.Range(-_s, _s);
			s = Mathf.Clamp01(s);

			v += Random.Range(-_v, _v);
			v = Mathf.Clamp01(v);

			a = c.a + Random.Range(-_a, _a);
			a = Mathf.Clamp01(a);

			Color result = Color.HSVToRGB(h,s,v,false);
			result.a = a;
			return result;
		}
		public void DoGUI (FacePaintData data){
			_perPoly = EditorGUILayout.Toggle("Per Face",_perPoly);
			EditorGUILayout.LabelField("Ranges");
			_h = EditorGUILayout.Slider("Hue", _h, 0, 1);
			_s = EditorGUILayout.Slider("Sat", _s, 0, 1);
			_v = EditorGUILayout.Slider("Val", _v, 0, 1);
			_a = EditorGUILayout.Slider("Alpha", _a, 0, 1);
			if (GUILayout.Button("Randomise")){
				Color[] c = data.GetColors();
				if (_perPoly){
					int[] t = data.GetTris();
					for (int i=0; i<(t.Length/3); ++i){
						int j = i*3;
						c[t[j]] = c[t[j+1]] = c[t[j+2]] = Randomise(c[t[j]]);;
					}
				} else {
					for (int i=0; i<c.Length; ++i){
						c[i] = Randomise(c[i]);
					}
				}
				data.SetColors(c);
			}
		}
		public void DoSceneGUI (FacePaintData data){
			
		}
	}
}