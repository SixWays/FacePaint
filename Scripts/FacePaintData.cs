#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;

namespace Sigtrap.FacePaint {
	[ExecuteInEditMode]
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	public class FacePaintData : MonoBehaviour {
		MeshRenderer __mr;
		MeshRenderer _mr {
			get {
				if (__mr == null){
					__mr = GetComponent<MeshRenderer>();
				}
				return __mr;
			}
		}
		MeshFilter __mf;
		MeshFilter _mf {
			get {
				if (__mf == null){
					__mf = GetComponent<MeshFilter>();
				}
				return __mf;
			}
		}
		Mesh _m;

		[SerializeField][HideInInspector]
		Color[] __colors;
		Color[] _colors {
			get {
				if (__colors == null) {
					__colors = _m.colors;
				}
				return __colors;
			}
		}
		#if UNITY_EDITOR
		public Color[] GetColors(){
			return _colors;
		}
		public void SetColors(Color[] cols){
			__colors = cols;
			Apply();
		}
		/// <summary>
		/// Initialise mesh and colors. Should only be used from FacePaint.
		/// </summary>
		/// <param name="c">Color to flood fill.</param>
		public void Init(Color c){
			InitMesh();
			__colors = _mf.sharedMesh.colors;
			if (__colors == null || __colors.Length != _mf.sharedMesh.vertexCount){
				__colors = new Color[_mf.sharedMesh.vertexCount];
				for (int i=0; i<__colors.Length; ++i){
					__colors[i] = c;
				}
			}
			Apply();
		}
		#endif

		/// <summary>
		/// Initialize mesh for additionalVertexStreams
		/// </summary>
		void InitMesh(){
			_m = new Mesh();
			_m.vertices = _mf.sharedMesh.vertices;
			_mr.additionalVertexStreams = _m;
		}

		void Awake(){
			InitMesh();
			Apply();
		}
		void OnDestroy(){
			if (_m != null){
				#if UNITY_EDITOR
				DestroyImmediate(_m);
				#else
				Destroy(_m);
				#endif
				if (_mr != null) {
					_mr.additionalVertexStreams = null;
				}
			}
		}

		public bool Apply(){
			if (_colors != null && _colors.Length > 0) {
				_m.colors = _colors;
				return true;
			}
			return false;
		}
	}
}