using UnityEngine;
using System.Collections;

namespace Sigtrap.FacePaint {
	public abstract class FacePaintPluginBase {
		public bool active;
		public abstract string title {get;}
		public abstract void DoGUI(FacePaintData data);
		public abstract void DoSceneGUI(FacePaintData data);
	}
}