using UnityEngine;
using System.Collections;

namespace Sigtrap.FacePaint {
	public interface IFacePaintPlugin {
		string title {get;}
		void DoGUI(FacePaintData data);
		void DoSceneGUI(FacePaintData data);
	}
}