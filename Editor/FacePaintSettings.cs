using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FacePaintSettings : ScriptableObject {
	[HideInInspector]
	public List<Color> palette = new List<Color>{
		Color.black, Color.white
	};
	[HideInInspector]
	public Texture2D lut;
	[HideInInspector]
	public bool useLut = false;
}
