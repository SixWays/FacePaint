using UnityEngine;
using System.Collections;

namespace Sigtrap.FacePaint {
	public interface IFacePaintPlugin {
		string title {get;}
		/// <summary>
		/// If true, will force per-triangle mouseover check every frame (slower on large meshes)
		/// </summary>
		bool forceTriangleHover {get;}
		void OnColorToolbar(FacePaint fp, FacePaintData fpData);
		void OnPaletteToolbar(FacePaint fp, FacePaintData fpData);
		void OnChannelToolbar(FacePaint fp, FacePaintData fpData);
		void OnModesToolbar(FacePaint fp, FacePaintData fpData);
		void OnPluginPanel(FacePaint fp, FacePaintData fpData);
		void OnSettingsPanel(FacePaint fp);
		void OnSceneGUI(FacePaint fp, FacePaintData fpData, FacePaintSceneGUIData data);
	}
	/// <summary>
	/// Face paint custom data. Inherit to store data in FacePaintSettings.
	/// </summary>
	[System.Serializable]
	public class FacePaintCustomSettings {
		public FacePaintCustomSettings(){}
	}
	public class FacePaintSceneGUIData {
		public enum SceneGUIEvent {
			NONE,
			HOVER_MESH,
			HOVER_TRIS,
			M_DOWN,
			M_DRAG,
			M_UP
		};
		public SceneGUIEvent evt {get; private set;}
		public int triHit {get; private set;}
		public int[] vertsHit {get; private set;}

		public FacePaintSceneGUIData(SceneGUIEvent e, int triIndex=-1, int vert1=-1, int vert2=-1, int vert3=-1){
			evt = e;
			triHit = triIndex;
			vertsHit = new int[]{vert1,vert2,vert3};
		}
	}

	public abstract class FacePaintPluginBase : IFacePaintPlugin {
		public abstract string title {get;}
		public virtual bool forceTriangleHover {get {return false;}}
		public virtual void OnColorToolbar (FacePaint fp, FacePaintData fpData){}
		public virtual void OnPaletteToolbar (FacePaint fp, FacePaintData fpData){}
		public virtual void OnChannelToolbar (FacePaint fp, FacePaintData fpData){}
		public virtual void OnModesToolbar (FacePaint fp, FacePaintData fpData){}
		public virtual void OnPluginPanel (FacePaint fp, FacePaintData fpData){}
		public virtual void OnSettingsPanel (FacePaint fp){}
		public virtual void OnSceneGUI (FacePaint fp, FacePaintData fpData, FacePaintSceneGUIData data){}
	}
}