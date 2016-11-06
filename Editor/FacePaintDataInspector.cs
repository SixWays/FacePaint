using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Sigtrap.FacePaint {
	[CustomEditor(typeof(FacePaintData))]
	public class FacePaintDataInspector : Editor {
		private string info = 
@"
This component holds the vertex colour data from FacePaint.

Delete to reset this mesh to its original vertex colours.

Copy/paste component to another MeshRenderer using the exact same mesh to apply the same vertex colours.
";
		
		public override void OnInspectorGUI() {
			EditorGUILayout.HelpBox(info, MessageType.Info);
			if (GUILayout.Button("Force Re-apply Colors")){
				FacePaintData fpd = (FacePaintData)target; 
				fpd.Setup();
			}
		}
	}
}
