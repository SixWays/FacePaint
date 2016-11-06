using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Sigtrap.FacePaint {
	public class FacePaint_EvtDebug : FacePaintPluginBase {
		bool _collapse = true;
		bool _wasCollapse = true;
		FacePaintSceneGUIData _last = null;

		#region IFacePaintPlugin implementation
		public override string title {get {return "Event Debugger";}}
		public override string description {get {return "Tool to show developers how face paint events work.";}}
		public override bool forceTriangleHover {get {return true;}}

		public override void OnPluginPanel (FacePaint fp, FacePaintData fpData){
			_collapse = EditorGUILayout.Toggle("Collapse Output", _collapse);
		}

		public override void OnSceneGUI (FacePaint fp, FacePaintData fpData, FacePaintSceneGUIData data){
			if (_collapse != _wasCollapse){
				_last = null;
			}
			_wasCollapse = _collapse;

			bool triEvt = false;
			switch (data.evt){
				case FacePaintSceneGUIData.SceneGUIEvent.HOVER_TRIS:
				case FacePaintSceneGUIData.SceneGUIEvent.M_DOWN:
				case FacePaintSceneGUIData.SceneGUIEvent.M_DRAG:
					triEvt = true;
					break;
			}

			// Work out whether event is duplicate of last
			if (_last != null && _collapse){
				// If same event, don't print UNLESS triangle event
				if (_last.evt == data.evt){
					// If triangle event, check triangle has changed
					if (triEvt){
						// If triangle hasn't changed, don't print
						if (_last.triHit == data.triHit) return;
					} else {
						// If not triangle event, don't print
						return;
					}
				}
			}

			string log = "FacePaint Event: "+data.evt.ToString();
			if (triEvt){
				var t = fpData.GetTris();
				log += "  T:" + data.triHit.ToString()
					+ " V:[" + t[data.vertsHit[0]].ToString() + ", "
					+ t[data.vertsHit[1]].ToString() + ", "
					+ t[data.vertsHit[1]].ToString() + "]";
			}
			Debug.Log(log);

			_last = data;
		}
		#endregion
	}
}