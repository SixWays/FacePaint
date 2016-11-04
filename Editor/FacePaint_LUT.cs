using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.ObjectModel;

namespace Sigtrap.FacePaint {
	public class FacePaint_LUT : FacePaintPluginBase {
		private Color GetLut(Texture2D tex, float channel){
			return tex.GetPixel(
				Mathf.Min(
					Mathf.FloorToInt(tex.width * channel), 
					tex.width-1
				),
				0
			);
		}
		private const string ASSIST_SHADER = "Hidden/FacePaintLutDebug";
		public override string title {get {return "LUT";}}
		public override string description {get {return "Applies a 1D LUT to assist mode. Forces use of a single channel.";}}
		public override void OnPaletteToolbar(FacePaint fp, FacePaintData fpData, ReadOnlyCollection<Color> palette) {
			FacePaint_LUT_Settings data = fp.GetCustomSettings<FacePaint_LUT_Settings>();
			if (!data.useLut || !data.lutIsReadable) return;
			for (int i=0; i<palette.Count; ++i){
				Color c = palette[i];
				float channel = c.r;
				if (fp.writeR){
					channel = c.r;
				} else if (fp.writeG){
					channel = c.g;
				} else if (fp.writeB){
					channel = c.b;
				} else if (fp.writeA){
					channel = c.a;
				}
				EditorGUI.DrawRect(
					EditorGUILayout.GetControlRect(GUILayout.Width(44)),
					GetLut(data.lut, channel)
				);
				GUILayout.Space(5);
			}
		}
		public override void OnColorToolbar(FacePaint fp, FacePaintData fpData) {
			FacePaint_LUT_Settings data = fp.GetCustomSettings<FacePaint_LUT_Settings>();
			if (!data.useLut) return;
			if (data.lutIsReadable){
				fp.DrawPluginTitle();
				GUILayout.Label("Result ", EditorStyles.miniLabel);
				EditorGUI.DrawRect(
					EditorGUILayout.GetControlRect(GUILayout.Width(45)),
					GetLut(data.lut, fp.activeChannel)
				);
				GUILayout.FlexibleSpace();
			} else {
				EditorGUILayout.LabelField("!! LUT not readable in import settings", EditorStyles.miniLabel);
			}
		}
		public override void OnChannelToolbar(FacePaint fp, FacePaintData fpData) {
			FacePaint_LUT_Settings data = fp.GetCustomSettings<FacePaint_LUT_Settings>();
			if (!data.useLut) return;

			fp.DrawPluginTitle();
			GUILayout.Label("Override", EditorStyles.miniLabel);
			GUILayout.FlexibleSpace();
			if (fp.DrawBtn("R", fp.writeR ? Color.red : Color.white, Color.white)) data.channel = 0;
			if (fp.DrawBtn("G", fp.writeG ? Color.green : Color.white, Color.white)) data.channel = 1;
			if (fp.DrawBtn("B", fp.writeB ? new Color(0.5f,0.5f,1) : Color.white, Color.white)) data.channel = 2;
			if (fp.DrawBtn("A", fp.writeA ? Color.gray : Color.white, Color.white)) data.channel = 3;

			bool r, g, b, a;
			r = g = b = a = false;
			switch (data.channel){
				case 0:
					r = true;
					break;
				case 1:
					g = true;
					break;
				case 2:
					b = true;
					break;
				case 3:
					a = true;
					break;
			}
			fp.SetChannels(r,g,b,a);
		}
		public override void OnModesToolbar (FacePaint fp, FacePaintData fpData){
			fp.DrawPluginTitle();
			FacePaint_LUT_Settings data = fp.GetCustomSettings<FacePaint_LUT_Settings>();
			if (data.lut == null){
				GUILayout.Label("No LUT selected", EditorStyles.miniLabel);
				return;
			}
			data.useLut = fp.ToggleBtn("Use\nLUT", "", data.useLut);
			if (data.useLut){
				Material m = fp.OverrideAssistShader(Shader.Find(ASSIST_SHADER));
				m.SetTexture("_LUT", data.lut);
				int channel = 0;
				if (fp.writeR){
					channel = 1;
				} else if (fp.writeG){
					channel = 2;
				} else if (fp.writeB){
					channel = 3;
				} else if (fp.writeA){
					channel = 4;
				}
				m.SetInt("_Mask", channel);
			} else {
				fp.OverrideAssistShader();
			}
		}
		public override void OnSettingsPanel (FacePaint fp){
			fp.DrawPluginTitle();
			++EditorGUI.indentLevel;
			FacePaint_LUT_Settings data = fp.GetCustomSettings<FacePaint_LUT_Settings>();
			data.lut = EditorGUILayout.ObjectField("Texture", data.lut, typeof(Texture2D), false) as Texture2D;
			--EditorGUI.indentLevel;
		}
	}

	public class FacePaint_LUT_Settings : FacePaintCustomSettings {
		public Texture2D lut;
		[SerializeField]
		private bool _useLut;
		public bool useLut {
			get {return _useLut && (lut != null);}
			set {_useLut = value;}
		}
		public bool lutIsReadable {
			get {
				return ((TextureImporter)(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lut)))).isReadable;
			}
		}
		public int channel = 0;
	}
}