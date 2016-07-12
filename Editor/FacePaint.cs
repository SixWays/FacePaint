using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections;

namespace Sigtrap.FacePaint {
	public class FacePaint : EditorWindow {
		#region Static

		[MenuItem("Window/FacePaint")]
		public static void Open(){
			// Get existing open window or if none, make a new one:
			FacePaint window = (FacePaint)EditorWindow.GetWindow(typeof(FacePaint));
			window.Show();
		}

		#endregion

		#region Edit data

		private GameObject _go;
		private MeshFilter _mf;

		private bool _editing {
			get {
				return (_go != null && _mf != null);
			}
		}

		#endregion

		#region Color settings
		private Color _defaultColor;
		private Color _c;
		bool[] _mask = new bool[]{ true, true, true, true };

		bool _mR { get { return _mask[0]; } set { _mask[0] = value; } }

		bool _mG { get { return _mask[1]; } set { _mask[1] = value; } }

		bool _mB { get { return _mask[2]; } set { _mask[2] = value; } }

		bool _mA { get { return _mask[3]; } set { _mask[3] = value; } }

		int _channels {
			get {
				int c = 0;
				for (int i = 0; i < 4; ++i) {
					if (_mask[i]) ++c;
				}
				return c;
			}
		}

		float _activeChannel {
			get {
				if (_channels == 1) {
					if (_mR) return _c.r;
					if (_mG) return _c.g;
					if (_mB) return _c.b;
					if (_mA) return _c.a;
				}
				return -1;
			}
			set {
				if (_mR) _c.r = value;
				if (_mG) _c.g = value;
				if (_mB) _c.b = value;
				if (_mA) _c.a = value;
			}
		}

		bool _debug = false;
		bool _wasDebug = false;
		Material __debugMat;
		Material _debugMat {
			get {
				if (__debugMat == null){
					__debugMat = new Material(Shader.Find("Hidden/FacePaintDebug"));
				}
				return __debugMat;
			}
		}
		Material[] _origMats = null;
		int __debugMask = 0;
		int _debugMask {
			get {return __debugMask;}
			set {
				__debugMask = value;
				_debugMat.SetInt("_Mask", __debugMask);
			}
		}

		#endregion

		#region UI settings

		Color _hlCol = Color.green;
		int _hlThick = 5;
		bool _hl = true;

		Color _btnCol = new Color(0.7f, 1f, 0.7f);

		Vector2 _scroll = new Vector2();
		Texture _bucketIcon;

		#endregion

		#region Subscription

		void OnEnable(){
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			EditorApplication.update += EditorUpdate;
			Undo.undoRedoPerformed += OnUndoRedo;

			_bucketIcon = Resources.Load("paint-can-icon") as Texture;
			titleContent = new GUIContent("FacePaint");
		}

		void OnDisable(){
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			EditorApplication.update -= EditorUpdate;
			Undo.undoRedoPerformed -= OnUndoRedo;

			if (__debugMat != null){
				DestroyImmediate(__debugMat);
			}
		}

		#endregion

		#region Editor/GUI helper methods

		// Most of these are a bit more stateful than they should be, but oh well...

		void EditorUpdate(){
			if (Selection.activeGameObject != null) {
				CheckSelection();
			}
			// Force GUI updates even when not focused
			Repaint();
		}

		/// <summary>
		/// Automatically finish editing if user selects another object
		/// </summary>
		void CheckSelection(){
			if (!_editing) return;
			if (Selection.activeGameObject != _go) {
				Done();
			}
		}

		/// <summary>
		/// Finish editing and reset all temporary stuff
		/// </summary>
		void Done(){
			if (!_editing) return;
			if (_debug) {
				DisableDebug();
			}
			FacePaintData fpd = _mf.GetComponent<FacePaintData>();
			if (fpd) {
				fpd.Apply();
			}
			_go = null;
			_mf = null;
		}

		/// <summary>
		/// Draw a button with a background color
		/// </summary>
		/// <returns><c>true</c>, if button pressed, <c>false</c> otherwise.</returns>
		/// <param name="label">Label.</param>
		/// <param name="bCol">Button color.</param>
		bool DrawBtn(string label, Color bCol){
			Color gbc =	GUI.backgroundColor;
			GUI.backgroundColor = bCol;
			bool result = GUILayout.Button(label);
			GUI.backgroundColor = gbc;
			return result;
		}

		/// <summary>
		/// Draw a button with a background color and text color
		/// </summary>
		/// <returns><c>true</c>, if button pressed, <c>false</c> otherwise.</returns>
		/// <param name="label">Label.</param>
		/// <param name="bCol">Button color.</param>
		/// <param name="tCol">Text color.</param>
		bool DrawBtn(string label, Color bCol, Color tCol){
			Color gcc =	GUI.contentColor;
			GUI.contentColor = tCol;
			bool result = DrawBtn(label, bCol);
			GUI.contentColor = gcc;
			return result;
		}

		/// <summary>
		/// Paint over existing color, respecting channel settings
		/// </summary>
		/// <param name="baseCol">Color to paint over</param>
		/// <param name="paintCol">Paint color</param>
		Color Paint(Color baseCol, Color paintCol){
			if (_mR) {
				baseCol.r = paintCol.r;
			}
			if (_mG) {
				baseCol.g = paintCol.g;
			}
			if (_mB) {
				baseCol.b = paintCol.b;
			}
			if (_mA) {
				baseCol.a = paintCol.a;
			}
			return baseCol;
		}

		void EnableDebug(){
			if (!_editing) return;

			_debug = true;
			MeshRenderer mr = _mf.GetComponent<MeshRenderer>();
			// Store 'real' materials
			_origMats = mr.sharedMaterials;
			// Assign new materials with debug shader
			Material[] newMats = new Material[_origMats.Length];
			for (int i = 0; i < newMats.Length; ++i) {
				newMats[i] = _debugMat;
			}
			mr.sharedMaterials = newMats;
			// Make sure shader settings are set
			_debugMask = _debugMask;
		}

		void DisableDebug(){
			if (_origMats == null) return;

			_debug = false;
			MeshRenderer mr = _mf.GetComponent<MeshRenderer>();
			// Restore 'real' materials
			mr.sharedMaterials = _origMats;
			_origMats = null;
		}

		#endregion


		#region Data

		FacePaintData GetColorData(MeshFilter mf){
			FacePaintData cd = mf.GetComponent<FacePaintData>();
			if (cd == null) {
				cd = Undo.AddComponent<FacePaintData>(mf.gameObject);
				cd.Init(_defaultColor);
			}
			return cd;
		}

		void OnUndoRedo(){
			if (_mf) {
				FacePaintData fpd = _mf.GetComponent<FacePaintData>();
				if (fpd) {
					fpd.Apply();
				}
			}
		}

		#endregion

		#region Main

		void OnGUI(){
			Color gc = GUI.color;
			Color gcc = GUI.contentColor;
			CheckSelection();
			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			if (Selection.activeGameObject == null) {
				EditorGUILayout.HelpBox("No Object Selected",MessageType.Info);
				EditorGUILayout.Space();
			} else {

				MeshFilter tmf = Selection.activeGameObject.GetComponentInChildren<MeshFilter>();

				EditorGUILayout.Space();
				if (!_editing) {
					if (!tmf) {
						GUI.contentColor = Color.red;
					}
					EditorGUILayout.HelpBox("Selected: " + Selection.activeGameObject.name, MessageType.None);
					GUI.contentColor = gcc;

					if (tmf) {
						MeshFilter[] mfs = Selection.activeGameObject.GetComponentsInChildren<MeshFilter>();
						string blPrefix = "";
						if (mfs.Length > 1) {
							EditorGUILayout.LabelField("Edit: ");
						} else {
							EditorGUILayout.Space();
							blPrefix = "Edit ";
						}
						foreach (MeshFilter m in mfs) {
							string bl = blPrefix;
							if (Selection.activeGameObject != m.gameObject) {
								bl += m.gameObject.name + "  >  ";
							}
							bl += m.sharedMesh.name;
								
							if (DrawBtn(bl, _btnCol)) {
								_go = Selection.activeGameObject = m.gameObject;
								_mf = m;
							}
						}
					}
				} else if (_editing) {
					if (Selection.activeGameObject != _mf.gameObject) {
						EditorGUILayout.LabelField("Editing: "
						+ Selection.activeGameObject.name + " > "
						+ " > " + _mf.gameObject.name
						+ _mf.sharedMesh.name);
					} else {
						EditorGUILayout.LabelField("Editing: "
						+ Selection.activeGameObject.name + " > "
						+ _mf.sharedMesh.name);
					}
					EditorGUILayout.Space();

					FacePaintData fpd = GetColorData(_mf);

					if (DrawBtn("DONE", _btnCol)) {
						Done();
					} 

					EditorGUILayout.Space();
					EditorGUILayout.Space();
					if (_channels != 0) {
						EditorGUILayout.BeginHorizontal();

						if (_channels == 1) {
							_activeChannel = EditorGUILayout.Slider("Value ", _activeChannel, 0f, 1f);
						} else {
							_c = EditorGUILayout.ColorField("Colour", _c);
						}

						if (GUILayout.Button(_bucketIcon)) {
							Color[] cols = fpd.GetColors();
							for (int i = 0; i < _mf.sharedMesh.vertexCount; ++i) {
								cols[i] = Paint(cols[i], _c);
							}
							fpd.SetColors(cols);
						}
						EditorGUILayout.EndHorizontal();
					}
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("Channels");
					if (DrawBtn("R", _mR ? Color.red : Color.white, Color.white)) _mR = !_mR;
					if (DrawBtn("G", _mG ? Color.green : Color.white, Color.white)) _mG = !_mG;
					if (DrawBtn("B", _mB ? new Color(0.5f,0.5f,1) : Color.white, Color.white)) _mB = !_mB;
					if (DrawBtn("A", _mA ? Color.gray : Color.white, Color.white)) _mA = !_mA;
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("");
					if (GUILayout.Button("[Colour]")) {
						_mR = _mG = _mB = true;
						_mA = false;
					}
					if (GUILayout.Button("[Alpha]")) {
						_mR = _mG = _mB = false;
						_mA = true;
					}
					if (GUILayout.Button("[All]")) {
						_mR = _mG = _mB = _mA = true;
					}
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.Space();
					EditorGUILayout.Space();
					_debug = EditorGUILayout.Toggle("View Vertex Colours", _debug);
					if (_debug && !_wasDebug) {
						EnableDebug();
					} else if (!_debug && _wasDebug) {
						DisableDebug();
					}
					if (_debug) {
						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("");
						GUILayout.Label("Show:");
						GUI.enabled = (_debugMask != 0);
						if (DrawBtn("RGB", GUI.enabled ? Color.white : Color.grey)) _debugMask = 0;

						GUI.enabled = (_debugMask != 1);
						if (DrawBtn("R", GUI.enabled ? Color.white : Color.red)) _debugMask = 1;

						GUI.enabled = (_debugMask != 2);
						if (DrawBtn("G", GUI.enabled ? Color.white : Color.green)) _debugMask = 2;

						GUI.enabled = (_debugMask != 3);
						if (DrawBtn("B", GUI.enabled ? Color.white : Color.blue)) _debugMask = 3;

						GUI.enabled = (_debugMask != 4);
						if (DrawBtn("A", GUI.enabled ? Color.white : Color.black, Color.white)) _debugMask = 4;

						GUI.enabled = true;

						EditorGUILayout.EndHorizontal();
					}
					_wasDebug = _debug;
				}
				if (_editing && (_hl || _debug)){
					EditorGUILayout.Space();
				}
			}

			_hl = EditorGUILayout.Toggle("Highlight faces (slower)", _hl);
			if (_hl) {
				++EditorGUI.indentLevel;
				_hlCol = EditorGUILayout.ColorField("Poly Highlight Colour", _hlCol);
				_hlThick = (int)EditorGUILayout.Slider("Thickness", (float)_hlThick, 5, 20);
				--EditorGUI.indentLevel;
			}

			if (_hl){
				EditorGUILayout.Space();
			}

			_defaultColor = EditorGUILayout.ColorField("Default Colour", _defaultColor);

			GUI.color = gc;
			GUI.contentColor = gcc;
			EditorGUILayout.EndScrollView();
		}

		public void OnSceneGUI(SceneView sceneView){
			if (Selection.activeGameObject == null) return;
			CheckSelection();
			if (!_editing) return;
			Event e = Event.current;
			if (e.type == EventType.Used || e.type == EventType.used) return;

			if (e.modifiers == EventModifiers.None) {
				// Setup raycast from mouse position
				Ray mRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				RaycastHit hit;

				MeshRenderer mr = _mf.GetComponent<MeshRenderer>();
				if (mr == null) {
					Debug.LogWarning("FacePaint: No MeshRenderer found on " + _mf.name);
					return;
				}

				// Check renderer bounds for cheap initial raycast
				if (mr.bounds.IntersectRay(mRay)) {
					bool clicked = e.button == 0 && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown);
					// Tell Unity to ignore LMB click event to avoid selecting another object
					if (e.type == EventType.Layout){
						HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
					}
					if (!_hl && !clicked) return;

					// Grab meshcollider or create temporary one
					bool newCollider = false;
					MeshCollider mc = mr.GetComponent<MeshCollider>();
					if (mc == null) {
						newCollider = true;
						mc = mr.gameObject.AddComponent<MeshCollider>();
					}
					// Use mesh collider to get exact triangle hit
					if (mc.Raycast(mRay, out hit, 100f)) {
						// Get existing mesh data
						FacePaintData fpd = GetColorData(_mf);
						Undo.RecordObject(fpd, "Perform FacePaint color");
						Mesh m = _mf.sharedMesh;
						int[] tris = m.triangles;
						Color[] cols = fpd.GetColors();

						// If clicked on a triangle, paint
						if (clicked) {
							Event.current.Use();
							for (int i = 0; i < 3; ++i) {
								int cInt = tris[hit.triangleIndex * 3 + i];
								cols[cInt] = Paint(cols[cInt], _c);
							}
							fpd.SetColors(cols);

						} else {
							// Otherwise highlight hovered triangle
							Matrix4x4 hm = Handles.matrix;
							Handles.matrix = _mf.transform.localToWorldMatrix;
							Vector3[] verts = m.vertices;
							int i0 = hit.triangleIndex * 3;
							Color hc = Handles.color;
							Handles.color = _hlCol;
							Handles.DrawAAPolyLine(
								_hlThick, 
								verts[tris[i0]], verts[tris[i0 + 1]],
								verts[tris[i0 + 2]], verts[tris[i0]]);
							Handles.matrix = hm;
							Handles.color = hc;
						}
					}

					// Destroy temporary mesh collider if required
					if (newCollider) {
						DestroyImmediate(mc);
					}
				}
			}
		}

		#endregion
	}
}