#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

		[SerializeField]
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
		#region Islands
		// Stupid serialisation hack, since Unity won't serialise a List of Lists
		[System.Serializable]
		private class IntList : List<int> {}
		[SerializeField][HideInInspector]
		private List<IntList> __islands;
		/// <summary>
		/// List of islands, where an island is a list of triangles
		/// (indices into triangle array - use tris[(listValue*3) + 0,1,2] to access vert)
		/// </summary>
		private List<IntList> _islands {
			get {
				if (__islands == null || __islands.Count == 0){
					__islands = new List<IntList>();

					#region Pre-process to remove doubles
					Vector3[] verts = _mf.sharedMesh.vertices;

					// Map [vert index] to [list of duplicate vert indices]
					List<List<int>> vertsToDups = new List<List<int>>();
					for (int i=0; i<verts.Length; ++i){
						List<int> dups = new List<int>();
						vertsToDups.Add(dups);
						for (int j=0; j<verts.Length; ++j){
							if (i==j) continue;
							if (verts[i] == verts[j]){
								dups.Add(j);
							}
						}
					}

					// Rearrange duplicates
					int[] vertsToRemappedVerts = new int[verts.Length];
					List<int>[] remappedVertsToOriginalVerts = new List<int>[verts.Length];
					// Choose lowest-indexed dup for each vert, and remap all references to that
					for (int i=0; i<vertsToDups.Count; ++i){
						int lowest = i;
						List<int> vtd = vertsToDups[i];
						for (int j=0; j<vtd.Count; ++j){
							if (vtd[j] < lowest){
								lowest = vtd[j];
							}
						}
						vertsToRemappedVerts[i] = lowest;
						remappedVertsToOriginalVerts[lowest] = vtd;
					}

					// Generate remapped triangles
					int[] tris = GetTris();
					for (int i=0; i<tris.Length; ++i){
						tris[i] = vertsToRemappedVerts[tris[i]];
					}

					// Since dealing with triangle indices, not verts, don't need to "unmap" afterwards
					// Triangle array indices still map correctly to original mesh's triangle array
					#endregion

					// Loop through tris
					for (int i=0; i<tris.Length/3; ++i){
						// Check if this tri has already been mapped
						if (TriAlreadyMapped(i)) continue;

						// An unmapped tri means a new island
						IntList il = new IntList();
						il.Add(i);
						__islands.Add(il);

						// Recursively map connected triangles
						MapConnectedTris(il, tris, i);
					}
				}

				return __islands;
			}
		}
		private Dictionary<int, int> __triToIsland;
		/// <summary>
		/// Map of triangle index of its parent island
		/// </summary>
		private Dictionary<int, int> _triToIsland {
			get {
				if (__triToIsland == null){
					__triToIsland = new Dictionary<int, int>();

					// Loop through islands
					for (int i=0; i<_islands.Count; ++i){
						// Get all child triangles
						List<int> sm = _islands[i];
						for (int k=0; k<sm.Count; ++k){
							if (__triToIsland.ContainsKey(sm[k])) continue;
							__triToIsland.Add(sm[k], i);
						}
					}
				}
				return __triToIsland;
			}
		}
		public bool islandsMapped {
			get {
				return __islands != null;
			}
		}
		private bool TriAlreadyMapped(int t){
			if (__islands == null || __islands.Count == 0) return false;

			for (int s=0; s<__islands.Count; ++s){
				if (__islands[s].Contains(t))	return true;
			}

			return false;
		}
		private void MapConnectedTris(List<int> island, int[] tris, int tri){
			int v0 = tris[tri*3];
			int v1 = tris[(tri*3)+1];
			int v2 = tris[(tri*3)+2];
			// Loop through other tris
			for (int j=0; j<tris.Length/3; ++j){
				// Ignore same tri
				if (j == tri) continue;
				// Check if this tri has already been mapped
				if (TriAlreadyMapped(j)) continue;

				// See if tri shares any verts with current tri
				for (int k=0; k<3; ++k){
					int v = tris[(j*3)+k];
					if (v == v0 || v == v1 || v == v2){
						// Connected - add and recurse
						island.Add(j);
						MapConnectedTris(island, tris, j);
						// Don't break - vert may be shared by more than 2 tris!
					}
				}
			}
		}

		/// <summary>
		/// Returns a list of indices of triangles connected to specified triangle
		/// Argument and returned values used as tris[(value*3) + 0,1,2]
		/// </summary>
		public List<int> GetConnectedTriangles(int triIndex){
			int ilIndex = -1;
			if (_triToIsland.TryGetValue(triIndex, out ilIndex)){
				if (ilIndex < _islands.Count){
					return _islands[ilIndex];
				}
			}
			return null;
		}
		#endregion

		/// <summary>
		/// Returns a COPY of the vertex colors array
		/// </summary>
		/// <returns>Vertex colors.</returns>
		public Color[] GetColors(){
			Color[] cols = _colors;
			Color[] result = new Color[cols.Length];
			for (int i=0; i<result.Length; ++i){
				result[i] = cols[i];
			}
			return _colors;
		}
		/// <summary>
		/// Sets the vertex colours array, and applies to the mesh
		/// </summary>
		/// <param name="cols">Vertex colors.</param>
		public void SetColors(Color[] cols){
			__colors = cols;
			Apply();
		}
		/// <summary>
		/// Gets a COPY of the triangles array
		/// </summary>
		/// <returns>Triangle indices array.</returns>
		public int[] GetTris(){
			return _mf.sharedMesh.triangles;
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