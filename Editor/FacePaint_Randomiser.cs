using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Sigtrap.FacePaint{
	public class FacePaint_Randomiser : FacePaintPluginBase {
		#region IFacePaintPlugin
		public override string title {get {return "Randomiser";}}

		public override void OnPluginPanel (FacePaint fp, FacePaintData fpData){
			_perPoly = EditorGUILayout.Toggle("Per Face",_perPoly);
			EditorGUILayout.LabelField("Ranges");
			_h = EditorGUILayout.Slider("Hue", _h, 0, 1);
			_s = EditorGUILayout.Slider("Sat", _s, 0, 1);
			_v = EditorGUILayout.Slider("Val", _v, 0, 1);
			_a = EditorGUILayout.Slider("Alpha", _a, 0, 1);
			if (GUILayout.Button("Randomise")){
				Color[] c = fpData.GetColors();
				if (_perPoly){
					int[] t = fpData.GetTris();
					for (int i=0; i<(t.Length/3); ++i){
						int j = i*3;
						c[t[j]] = c[t[j+1]] = c[t[j+2]] = Randomise(c[t[j]]);;
					}
				} else {
					for (int i=0; i<c.Length; ++i){
						c[i] = Randomise(c[i]);
					}
				}
				fpData.SetColors(c);
			}
		}
		#endregion

		float _h, _s, _v, _a;
		bool _perPoly = true;

		Color Randomise(Color c){
			float h, s, v;
			float a = c.a;
			Color.RGBToHSV(c, out h, out s, out v);

			if (_h > 0){
				h += Random.Range(-_h, _h);
				if (h > 1){h -= 1;}
				if (h < 0){h += 1;}
			}

			if (_s > 0){
				s += Random.Range(-_s, _s);
				s = Mathf.Clamp01(s);
			}

			if (_v > 0){
				v += Random.Range(-_v, _v);
				v = Mathf.Clamp01(v);
			}

			if (_a > 0){
				a += Random.Range(-_a, _a);
				a = Mathf.Clamp01(a);
			}

			Color result = Color.HSVToRGB(h,s,v,false);
			result.a = a;
			return result;
		}
	}
}