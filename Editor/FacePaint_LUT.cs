using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Sigtrap.FacePaint {
	public class FacePaint_LUT : FacePaintPluginBase {
		public override string title {
			get {
				return "LUT";
			}
		}
		public override void OnModesToolbar (FacePaint fp, FacePaintData fpData){
			fp.DrawPluginTitle();
			FacePaint_LUT_Settings data = fp.GetCustomSettings<FacePaint_LUT_Settings>();
			data.useLut = fp.ToggleBtn("Use\nLUT", "", data.useLut);
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
		public bool useLut;
		public bool lutIsReadable {
			get {
				return ((TextureImporter)(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lut)))).isReadable;
			}
		}
	}
}