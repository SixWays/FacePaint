using UnityEngine;
using UnityEditor;

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