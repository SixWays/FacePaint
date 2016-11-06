using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Sigtrap.FacePaint {
	public class FacePaint : EditorWindow {
		private const string DEFAULT_ASSIST_SHADER = "Hidden/FacePaintDebug";

		#region Static
		[MenuItem("Window/FacePaint")]
		public static void Open(){
			// Get existing open window or if none, make a new one:
			FacePaint window = (FacePaint)EditorWindow.GetWindow(typeof(FacePaint));
			window.Show();
		}
		#endregion

		#region API
		#region Settings
		/// <summary>
		/// Color selected in main FacePaint GUI
		/// </summary>
		/// <value>The color of the paint.</value>
		public Color paintColor {
			get {return _settings.paintColor;}
			set {_settings.paintColor = value;}
		}

		bool[] _mask = new bool[]{ true, true, true, true };
		/// <summary>
		/// Is painting to RED channel enabled?
		/// </summary>
		public bool writeR { get { return _mask[0]; } set { _mask[0] = value; } }
		/// <summary>
		/// Is painting to GREEN channel enabled?
		/// </summary>
		public bool writeG { get { return _mask[1]; } set { _mask[1] = value; } }
		/// <summary>
		/// Is painting to BLUE channel enabled?
		/// </summary>
		public bool writeB { get { return _mask[2]; } set { _mask[2] = value; } }
		/// <summary>
		/// Is painting to ALPHA channel enabled?
		/// </summary>
		public bool writeA { get { return _mask[3]; } set { _mask[3] = value; } }

		/// <summary>
		/// Number of channels active.
		/// </summary>
		public int channels {
			get {
				int c = 0;
				for (int i = 0; i < 4; ++i) {
					if (_mask[i]) ++c;
				}
				return c;
			}
		}
		/// <summary>
		/// In single channel mode, gets/sets value of paint colour in that channel
		/// </summary>
		public float activeChannel {
			get {
				if (channels == 1) {
					if (writeR) return paintColor.r;
					if (writeG) return paintColor.g;
					if (writeB) return paintColor.b;
					if (writeA) return paintColor.a;
				}
				return -1;
			}
			set {
				if (channels == 1) {
					Color p = paintColor;
					if (writeR) p.r = value;
					if (writeG) p.g = value;
					if (writeB) p.b = value;
					if (writeA) p.a = value;
					paintColor = p;
				}
			}
		}

		/// <summary>
		/// Set which channels paint should write to.
		/// </summary>
		public void SetChannels(bool r, bool g, bool b, bool a){
			writeR = r;
			writeG = g;
			writeB = b;
			writeA = a;
		}
		/// <summary>
		/// Replace assist mode shader. Pass null to reset to default.
		/// Replacement shaders MUST have at least same properties as default.
		/// <returns>Material used by assist mode.</returns>
		/// </summary>
		public Material OverrideAssistShader(Shader s=null){
			if (s == null){
				return SetAssistShader(Shader.Find(DEFAULT_ASSIST_SHADER));
			} else {
				return SetAssistShader(s);
			}
		}
		private Material SetAssistShader(Shader s){
			if (__debugMat != null){
				if (__debugMat.shader == s) return __debugMat;
				DestroyImmediate(__debugMat);
			}
			__debugMat = new Material(s);
			_updateDebugMat = true;
			return __debugMat;
		}
		#endregion

		#region GUI helpers
		/// <summary>
		/// Draw a button with a background color and GUIStyle.
		/// </summary>
		/// <returns><c>true</c>, if button pressed, <c>false</c> otherwise.</returns>
		/// <param name="label">Label.</param>
		/// <param name="bCol">Button color.</param>
		/// <param name="style">GUIStyle to apply to button.</param>
		public bool DrawBtn(string label, Color bCol, GUIStyle style, params GUILayoutOption[] options){
			Color gbc =	GUI.backgroundColor;
			GUI.backgroundColor = bCol;
			bool result = false;
			if (style != null){
				result = GUILayout.Button(label, style, options);
			} else {
				result = GUILayout.Button(label, options);
			}
			GUI.backgroundColor = gbc;
			return result;
		}
		/// <summary>
		/// Draw a button with a background color, text color and GUIStyle.
		/// </summary>
		/// <returns><c>true</c>, if button pressed, <c>false</c> otherwise.</returns>
		/// <param name="label">Label.</param>
		/// <param name="bCol">Button color.</param>
		/// <param name="tCol">Text color.</param>
		/// <param name="style">GUIStyle to apply to button.</param>
		public bool DrawBtn(string label, Color bCol, Color tCol, GUIStyle style, params GUILayoutOption[] options){
			Color gcc =	GUI.contentColor;
			GUI.contentColor = tCol;
			bool result = DrawBtn(label, bCol, style, options);
			GUI.contentColor = gcc;
			return result;
		}
		/// <summary>
		/// Draw a button with a background color.
		/// </summary>
		/// <returns><c>true</c>, if button pressed, <c>false</c> otherwise.</returns>
		/// <param name="label">Label.</param>
		/// <param name="bCol">Button color.</param>
		public bool DrawBtn(string label, Color bCol, params GUILayoutOption[] options){
			return DrawBtn(label, bCol, null, options);
		}
		/// <summary>
		/// Draw a button with a background color and text color.
		/// </summary>
		/// <returns><c>true</c>, if button pressed, <c>false</c> otherwise.</returns>
		/// <param name="label">Label.</param>
		/// <param name="bCol">Button color.</param>
		/// <param name="tCol">Text color.</param>
		public bool DrawBtn(string label, Color bCol, Color tCol, params GUILayoutOption[] options){
			return DrawBtn(label, bCol, tCol, null, options);
		}
		/// <summary>
		/// Draw a toggle button.
		/// </summary>
		/// <returns><c>true</c>, if button pressed, <c>false</c> otherwise.</returns>
		/// <param name="label">Label.</param>
		/// <param name="tooltip">Tooltip.</param>
		/// <param name="state">Current state of property.</param>
		public bool ToggleBtn(string label, string tooltip, bool state){
			return GUILayout.Toggle(state, new GUIContent(label, tooltip), EditorStyles.miniButton);
		}
		#region Annoying stateful gui stuff because I can't work out how to draw a label to the left of something already drawn
		private GUIStyle _currentPluginTitleStyle;
		private IFacePaintPlugin _currentPlugin;
		/// <summary>
		/// Draws the plugin title. Use at start of GUI callbacks to print name of plugin in preformatted way.
		/// </summary>
		public void DrawPluginTitle(){
			if (_currentPlugin == null) return;
			if (_currentPluginTitleStyle != null){
				GUILayout.Label(_currentPlugin.title, _currentPluginTitleStyle);
			} else {
				GUILayout.Label(_currentPlugin.title);
			}
		}
		#endregion
		#endregion

		#region Data storage
		private Dictionary<IFacePaintPlugin, FacePaintCustomSettings> _customSettings = new Dictionary<IFacePaintPlugin, FacePaintCustomSettings>();
		public T GetCustomSettings<T>(IFacePaintPlugin plugin) where T:FacePaintCustomSettings {
			if (!_customSettings.ContainsKey(plugin)){			// Check if cached
				T cs = LoadSettings<T>();						// Load or create
				_customSettings.Add(plugin, cs);				// Cache
				return cs;
			}
			return (T)_customSettings[plugin];
		}
		public void SaveSettings(){
			for (int i=0; i<_plugins.Count; ++i){
				_settings.SetPluginActive(_plugins[i], _pluginsActive[i]);
				_settings.SetPluginSettingsUnfolded(_plugins[i], _pluginSettingsUnfolded[i]);
			}
			foreach (var a in _customSettings){
				EditorUtility.SetDirty(a.Value);
			}
			EditorUtility.SetDirty(_settings);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		#endregion

		#region Core methods
		/// <summary>
		/// Combine the existing color with the paint color, respecting channel settings.
		/// Apply the returned color.
		/// </summary>
		/// <param name="baseCol">Color to paint over</param>
		/// <param name="paintCol">Paint color</param>
		/// <returns>Color to be applied to face</returns>
		public Color Paint(Color baseCol, Color paintCol){
			if (writeR) {
				baseCol.r = paintCol.r;
			}
			if (writeG) {
				baseCol.g = paintCol.g;
			}
			if (writeB) {
				baseCol.b = paintCol.b;
			}
			if (writeA) {
				baseCol.a = paintCol.a;
			}
			return baseCol;
		}
		#endregion
		#endregion

		#region Internal Fields
		#region Settings data
		private const string ASSETS_PATH = "Assets/FacePaint/Resources/";
		private const string RESOURCES_PATH = "Settings/";

		private T LoadSettings<T>() where T:ScriptableObject {
			string name = typeof(T).Name;
			// Check if file exists
			T file = Resources.Load<T>(RESOURCES_PATH + name);
			if (file == null){
				// In not, create file
				file = ScriptableObject.CreateInstance<T>();
				AssetDatabase.CreateAsset(file, ASSETS_PATH + name + ".asset");
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			return file;
		}

		private FacePaintSettings __settings;
		private FacePaintSettings _settings {
			get {
				if (__settings == null){
					__settings = LoadSettings<FacePaintSettings>();
				}
				return __settings;
			}
		}
		#endregion

		#region Edit data
		private GameObject _go;
		private MeshFilter _mf;
		private bool _editing {get {return (_go != null && _mf != null);}}
		#endregion

		#region Color settings
		private bool _paintIsland = false;
		private Color _defaultColor {
			get {return _settings.defaultColor;}
			set {_settings.defaultColor = value;}
		}

		bool _debug = false;
		bool _wasDebug = false;
		Material __debugMat;
		Material _debugMat {
			get {
				if (__debugMat == null){
					OverrideAssistShader();
				}
				return __debugMat;
			}
		}
		bool _updateDebugMat = false;
		Material[] _origMats = null;
		int __debugMask = 0;
		int _debugMask {
			get {return __debugMask;}
			set {
				__debugMask = value;
				_debugMat.SetInt("_Mask", __debugMask);
			}
		}
		bool _debugLink = false;
		#endregion

		#region UI settings
		Color _hlCol {
			get {return _settings.hlCol;}
			set {_settings.hlCol = value;}
		}
		int _hlThick {
			get {return _settings.hlThick;}
			set {_settings.hlThick = value;}
		}
		bool _hl {
			get {return _settings.hl;}
			set {_settings.hl = value;}
		}

		Color _greenBtn = new Color(0.7f, 1f, 0.7f);
		Color _redBtn = new Color(1f, 0.7f, 0.7f);
		Vector2 _scroll = new Vector2();
		Texture _bucketIcon;
		Texture _headerLogo;

		GUIStyle __pluginBox;
		GUIStyle _pluginBox {
			get {
				if (__pluginBox == null){
					__pluginBox = new GUIStyle(EditorStyles.helpBox);
					__pluginBox.margin = new RectOffset(30,0,0,0);
				}
				return __pluginBox;
			}
		}
		GUIStyle __headerBox;
		GUIStyle _headerBox {
			get {
				if (__headerBox == null){
					__headerBox = new GUIStyle(EditorStyles.textArea);
					__headerBox.margin = new RectOffset(0,0,0,0);
				}
				return __headerBox;
			}
		}
		#endregion

		#region Plugins
		List<IFacePaintPlugin> _plugins = new List<IFacePaintPlugin>();
		List<bool> _pluginsActive = new List<bool>();
		List<bool> _pluginSettingsUnfolded = new List<bool>();
		int _numPluginsActive {
			get {
				if (_plugins.Count == 0) return 0;
				int result = 0;
				for (int i=0; i<_pluginsActive.Count; ++i){
					if (_pluginsActive[i]){
						++result;
					}
				}
				return result;
			}
		}
		bool _anyPluginsActive {get {return _numPluginsActive > 0;}}
		bool _anyPluginsHoverTris {
			get {
				if (!_anyPluginsActive) return false;
				for (int i=0; i<_plugins.Count; ++i){
					if (_plugins[i].forceTriangleHover){
						return true;
					}
				}
				return false;
			}
		}
		#endregion
		#endregion

		#region Internal Methods
		#region Subscription
		void OnEnable(){
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			EditorApplication.update += EditorUpdate;
			Undo.undoRedoPerformed += OnUndoRedo;

			_bucketIcon = Resources.Load("paint-can-icon") as Texture;
			_headerLogo = Resources.Load("header-logo") as Texture;
			titleContent = new GUIContent("FacePaint");

			// Get plugins
			_plugins.Clear();
			_pluginsActive.Clear();
			System.Type iPlugin = typeof(IFacePaintPlugin);
			foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()){
				foreach (var t in a.GetTypes()){
					if (t.IsPublic && !t.IsAbstract && !t.IsInterface && t.GetInterfaces().Contains(iPlugin)){
						IFacePaintPlugin plugin = (IFacePaintPlugin)System.Activator.CreateInstance(t);
						_plugins.Add(plugin);
						_pluginsActive.Add(_settings.PluginIsActive(plugin));
						_pluginSettingsUnfolded.Add(_settings.PluginSettingsUnfolded(plugin));
					}
				}
			}
		}
		void OnDisable(){
			SaveSettings();
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

			SaveSettings();
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
		#endregion

		#region Main GUI Methods
		void OnGUI(){
			Color gc = GUI.color;
			Color gcc = GUI.contentColor;
			CheckSelection();
			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			// Logo
			if (!_settings.hideLogo){
				EditorGUILayout.BeginHorizontal(_headerBox);
				int vh = 64;
				float vw = EditorGUIUtility.currentViewWidth;
				float tw = (float)_headerLogo.width * ((float)vh/(float)_headerLogo.height);
				if (vw >= tw){
					// Draw at regular scale with white sides
					EditorGUI.DrawRect(new Rect(0, 0, vw, vh+2), new Color(0.95f,0.95f,0.95f));
				} else {
					// Scale logo to fit
					vh = (int)(64f / (tw/vw));
				}
				EditorGUI.DrawPreviewTexture(new Rect(0,0,vw,vh), _headerLogo, null, ScaleMode.ScaleToFit);
				// Force height
				EditorGUILayout.BeginVertical();
				GUILayout.Space(vh+2);
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}

			if (Selection.activeGameObject == null) {
				EditorGUILayout.HelpBox("No Object Selected",MessageType.Info);
			} else {
				MeshFilter tmf = Selection.activeGameObject.GetComponentInChildren<MeshFilter>();
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
								
							if (DrawBtn(bl, _greenBtn)) {
								_go = Selection.activeGameObject = m.gameObject;
								_mf = m;
							}
						}
					}
					EditorGUILayout.Space();
				} else if (_editing) {
					#region HEADER SECTION
					Undo.RecordObject(_settings, "FacePaintSettings");
					if (Selection.activeGameObject != _mf.gameObject) {
						EditorGUILayout.HelpBox("Editing: "
						+ Selection.activeGameObject.name + " > "
						+ " > " + _mf.gameObject.name
						+ _mf.sharedMesh.name, MessageType.None);
					} else {
						EditorGUILayout.HelpBox("Editing: "
						+ Selection.activeGameObject.name + " > "
						+ _mf.sharedMesh.name, MessageType.None);
					}
					FacePaintData fpd = GetColorData(_mf);
					if (_paintIsland && !fpd.islandsMapped){
						EditorGUILayout.HelpBox("WARNING! Islands have not been calculated. Using ISLAND paint mode the first time may be very slow.",
							MessageType.Warning);
					}
					EditorGUILayout.Space();
					Undo.RecordObject(fpd, "FacePaint");
					#endregion

					#region COLOR PANEL
					EditorGUILayout.BeginVertical(EditorStyles.helpBox);
					// Color, fill
					if (channels != 0) {
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.BeginVertical();
						GUILayout.Space(5);
						if (channels == 1) {
							EditorGUILayout.BeginHorizontal();
							activeChannel = EditorGUILayout.Slider(activeChannel, 0f, 1f);
							Color swatch = Color.white;
							if (writeR){
								swatch = Color.red;
							} else if (writeG){
								swatch = Color.green;
							} else if (writeB){
								swatch = Color.blue;
							}
							swatch = Color.Lerp(Color.black, swatch, activeChannel);
							EditorGUI.DrawRect(
								EditorGUILayout.GetControlRect(GUILayout.Width(30)),
								swatch
							);
							EditorGUIUtility.DrawColorSwatch(
								EditorGUILayout.GetControlRect(GUILayout.Width(30)),
								paintColor
							);
							EditorGUILayout.EndHorizontal();
						} else {
							paintColor = EditorGUILayout.ColorField(paintColor, GUILayout.Width(60));
						}
						EditorGUILayout.EndVertical();
						GUILayout.Space(5);
						if (GUILayout.Button(new GUIContent(_bucketIcon, "Flood Mesh"))) {
							Color[] cols = fpd.GetColors();
							for (int i = 0; i < _mf.sharedMesh.vertexCount; ++i) {
								cols[i] = Paint(cols[i], paintColor);
							}
							fpd.SetColors(cols);
						}
						GUILayout.FlexibleSpace();
						EditorGUILayout.BeginVertical();
						GUILayout.Space(4);
						if (DrawBtn("DONE", _greenBtn)) {
							Done();
						}
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.Space();
					} else {
						EditorGUILayout.HelpBox("No channels selected", MessageType.Info);
					}

					// Draw Plugins
					for (int i=0; i<_plugins.Count; ++i){
						_currentPlugin = _plugins[i];
						_currentPluginTitleStyle = null;
						if (_pluginsActive[i]){
							EditorGUILayout.BeginHorizontal();
							_currentPlugin.OnColorToolbar(this, fpd);
							EditorGUILayout.EndHorizontal();
						}
					}
					EditorGUILayout.EndVertical();
					#endregion

					#region PALETTE PANEL
					// Palette
					EditorGUILayout.BeginVertical(EditorStyles.helpBox);
					EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
					_settings.showPalette = EditorGUILayout.Foldout(_settings.showPalette, "Palette");
					GUILayout.FlexibleSpace();
					if (_settings.showPalette){
						if (DrawBtn("+", _greenBtn, GUILayout.Height(15), GUILayout.Width(20))){
							_settings.palette.Add(paintColor);
						}
					}
					EditorGUILayout.EndHorizontal();

					if (_settings.showPalette){
						EditorGUILayout.BeginHorizontal();
						int cToRemove = -1;
						for (int i=0; i<_settings.palette.Count; ++i){
							Color c = _settings.palette[i];
							EditorGUILayout.BeginVertical(EditorStyles.helpBox);
							_settings.palette[i] = EditorGUILayout.ColorField(c, GUILayout.Width(33));
							EditorGUILayout.BeginHorizontal();
							if (DrawBtn("", _greenBtn, GUILayout.Width(15), GUILayout.Height(15))){
								paintColor = c;
							}
							if (DrawBtn("X", _redBtn, GUILayout.Width(17), GUILayout.Height(15))){
								cToRemove = i;
								break;
							}
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.EndVertical();
							GUILayout.Space(5);
						}
						GUILayout.FlexibleSpace();
						if (cToRemove >= 0){
							_settings.palette.RemoveAt(cToRemove);
						}
						EditorGUILayout.EndHorizontal();

						// Draw Plugins
						var palette = _settings.palette.AsReadOnly();
						for (int i=0; i<_plugins.Count; ++i){
							_currentPlugin = _plugins[i];
							_currentPluginTitleStyle = null;
							if (_pluginsActive[i]){
								EditorGUILayout.BeginHorizontal();
								_currentPlugin.OnPaletteToolbar(this, fpd, palette);
								EditorGUILayout.EndHorizontal();
							}
						}
					}
					EditorGUILayout.EndVertical();
					#endregion

					#region CHANNELS PANEL
					// Channels
					EditorGUILayout.BeginVertical(EditorStyles.helpBox);

					EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
					_settings.showChannels = EditorGUILayout.Foldout(_settings.showChannels, "Channels");

					EditorGUILayout.EndHorizontal();
					if (_settings.showChannels){
						EditorGUILayout.BeginHorizontal();
						if (DrawBtn("R", writeR ? Color.red : Color.white, Color.white)) writeR = !writeR;
						if (DrawBtn("G", writeG ? Color.green : Color.white, Color.white)) writeG = !writeG;
						if (DrawBtn("B", writeB ? new Color(0.5f,0.5f,1) : Color.white, Color.white)) writeB = !writeB;
						if (DrawBtn("A", writeA ? Color.gray : Color.white, Color.white)) writeA = !writeA;
						EditorGUILayout.EndHorizontal();

						// Channel Presets
						++EditorGUI.indentLevel;
						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("Presets", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
						if (GUILayout.Button("[Colour]", EditorStyles.miniButton)) {
							writeR = writeG = writeB = true;
							writeA = false;
						}
						if (GUILayout.Button("[Alpha]", EditorStyles.miniButton)) {
							writeR = writeG = writeB = false;
							writeA = true;
						}
						if (GUILayout.Button("[All]", EditorStyles.miniButton)) {
							writeR = writeG = writeB = writeA = true;
						}
						EditorGUILayout.EndHorizontal();

						// Draw Plugins
						for (int i=0; i<_plugins.Count; ++i){
							_currentPlugin = _plugins[i];
							_currentPluginTitleStyle = EditorStyles.miniLabel;
							if (_pluginsActive[i]){
								EditorGUILayout.BeginHorizontal();
								_currentPlugin.OnChannelToolbar(this, fpd);
								EditorGUILayout.EndHorizontal();
							}
						}
						--EditorGUI.indentLevel;
					}
					EditorGUILayout.EndVertical();
					#endregion

					#region MODES PANEL
					EditorGUILayout.BeginVertical(EditorStyles.helpBox);
					EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
					_settings.showModes = EditorGUILayout.Foldout(_settings.showModes, "Modes");
					EditorGUILayout.EndHorizontal();

					if (_settings.showModes){
						EditorGUILayout.BeginHorizontal();
						// Island mode
						_paintIsland = ToggleBtn(
							"Fill\nIslands",
							"Clicking a face will also paint all connected faces (slow first time!)",
							_paintIsland
						);

						// Assist mode
						_debug = ToggleBtn(
							"Assist\nMode",
							"Display vertex colours on model",
							_debug
						);
						if (_debug){
							if (!_wasDebug){
								EnableDebug();
							} else if (_updateDebugMat){
								DisableDebug();
								EnableDebug();
							}
						} else if (!_debug && _wasDebug) {
							DisableDebug();
						}

						// Face highlighting
						_hl = ToggleBtn(
							"Highlight\nFaces",
							"Highlight hovered face (slow for large meshes!)",
							_hl
						);
						EditorGUILayout.EndHorizontal();

						// Draw Plugins
						for (int i=0; i<_plugins.Count; ++i){
							_currentPlugin = _plugins[i];
							_currentPluginTitleStyle = null;
							if (_pluginsActive[i]){
								EditorGUILayout.BeginHorizontal();
								_currentPlugin.OnModesToolbar(this, fpd);
								EditorGUILayout.EndHorizontal();
							}
						}

						// Assist mode options
						if (_debug) {
							EditorGUILayout.BeginHorizontal();
							GUILayout.Label("Assist Channels:", EditorStyles.miniLabel);
							GUI.enabled = !_debugLink;
							_debugLink = ToggleBtn("LINK", "Link assist mode channel(s) to paint channel(s)", _debugLink);
							GUI.enabled = (_debugMask != 0);
							if (DrawBtn("RGB", (GUI.enabled ? Color.white : Color.grey), EditorStyles.miniButton)) _debugMask = 0;

							GUI.enabled = (_debugMask != 1);
							if (DrawBtn("R", (GUI.enabled ? Color.white : Color.red), EditorStyles.miniButton)) _debugMask = 1;

							GUI.enabled = (_debugMask != 2);
							if (DrawBtn("G", (GUI.enabled ? Color.white : Color.green), EditorStyles.miniButton)) _debugMask = 2;

							GUI.enabled = (_debugMask != 3);
							if (DrawBtn("B", (GUI.enabled ? Color.white : Color.blue), EditorStyles.miniButton)) _debugMask = 3;

							GUI.enabled = (_debugMask != 4);
							if (DrawBtn("A", (GUI.enabled ? Color.white : Color.black), Color.white, EditorStyles.miniButton)) _debugMask = 4;

							// Override with write channels in LINK mode
							if (_debugLink){
								if (channels == 1){
									if (writeR){
										_debugMask = 1;
									} else if (writeG){
										_debugMask = 2;
									} else if (writeB){
										_debugMask = 3;
									} else if (writeA){
										_debugMask = 4;
									}
								} else {
									_debugMask = 0;
								}
							}
							GUI.enabled = true;

							EditorGUILayout.EndHorizontal();
						}
						_wasDebug = _debug;
					}
					EditorGUILayout.EndVertical();
					#endregion

					#region PLUGINS PANEL
					if (_plugins.Count > 0){
						EditorGUILayout.BeginVertical(EditorStyles.helpBox);
						EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
						_settings.showPlugins = EditorGUILayout.Foldout(_settings.showPlugins, "Plugins");
						GUILayout.FlexibleSpace();
						GUILayout.Label(string.Format(
							"[Active: {0} / {1}]",
							_numPluginsActive.ToString(),
							_plugins.Count
						));
						EditorGUILayout.EndHorizontal();
						if (_settings.showPlugins){
							++EditorGUI.indentLevel;
							for (int i=0; i<_plugins.Count; ++i){
								IFacePaintPlugin fpp = _plugins[i];
								_pluginsActive[i] = EditorGUILayout.ToggleLeft(new GUIContent(fpp.title, fpp.description), _pluginsActive[i]);
								if (_pluginsActive[i]){
									EditorGUILayout.BeginVertical(_pluginBox);
									fpp.OnPluginPanel(this, fpd);
									EditorGUILayout.EndVertical();
								}
							}
							--EditorGUI.indentLevel;
						}
						EditorGUILayout.EndVertical();
					}
					#endregion
				}
			}

			#region SETTINGS PANEL
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			_settings.showSettings = EditorGUILayout.Foldout(_settings.showSettings, "Settings");
			EditorGUILayout.EndHorizontal();
			if (_settings.showSettings){
				++EditorGUI.indentLevel;
				_settings.hideLogo = EditorGUILayout.Toggle("Hide Header", _settings.hideLogo);
				GUIContent dctt = new GUIContent("Default Color", "When edited the very first time, meshes are filled with this color");
				_defaultColor = EditorGUILayout.ColorField(dctt, _defaultColor);

				EditorGUILayout.LabelField("Face Highlighting");
				++EditorGUI.indentLevel;
				_hlCol = EditorGUILayout.ColorField("Colour", _hlCol);
				_hlThick = (int)EditorGUILayout.Slider("Thickness", (float)_hlThick, 5, 20);
				--EditorGUI.indentLevel;

				// Draw Plugins
				for (int i=0; i<_plugins.Count; ++i){
					_currentPlugin = _plugins[i];
					_currentPluginTitleStyle = null;
					_pluginSettingsUnfolded[i] = EditorGUILayout.Foldout(_pluginSettingsUnfolded[i],_currentPlugin.title.ToUpper());
					if (_pluginSettingsUnfolded[i]){
						--EditorGUI.indentLevel;
						EditorGUILayout.BeginVertical(_pluginBox);
						_currentPlugin.OnSettingsPanel(this);
						EditorGUILayout.EndVertical();
						++EditorGUI.indentLevel;
					}
				}
			}
			EditorGUILayout.EndVertical();
			#endregion

			GUI.color = gc;
			GUI.contentColor = gcc;
			EditorGUILayout.EndScrollView();
		}

		bool _mouseWasDown = false;
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

					#region Plugins MouseUp hook
					if (_anyPluginsActive && _mouseWasDown && !clicked){
						// Pass mouseUp event to plugins
						// Get existing mesh data
						FacePaintData f = GetColorData(_mf);
						Undo.RecordObject(f, "FacePaint SceneGUI");
						FacePaintSceneGUIData data = new FacePaintSceneGUIData(
							FacePaintSceneGUIData.SceneGUIEvent.M_UP
						);
						for (int i=0; i<_plugins.Count; ++i){
							if (_pluginsActive[i]){
								_plugins[i].OnSceneGUI(this, f, data);
							}
						}
					}
					#endregion

					_mouseWasDown = clicked;
					bool hoverTris = _anyPluginsHoverTris;
					if (!_hl && !clicked && !hoverTris) return;

					// Grab meshcollider or create temporary one
					bool newCollider = false;
					MeshCollider mc = mr.GetComponent<MeshCollider>();
					if (mc == null) {
						newCollider = true;
						mc = mr.gameObject.AddComponent<MeshCollider>();
					}

					// Get existing mesh data
					FacePaintData fpd = GetColorData(_mf);
					Undo.RecordObject(fpd, "FacePaint SceneGUI");

					// Use mesh collider to get exact triangle hit
					if (mc.Raycast(mRay, out hit, 100f)) {
						Mesh m = _mf.sharedMesh;
						int[] tris = m.triangles;
						Color[] cols = fpd.GetColors();
						int i0 = hit.triangleIndex * 3;

						if (clicked){
							// If clicked on a triangle, paint
							Event.current.Use();
							List<int> allTris;
							if (_paintIsland){
								allTris = fpd.GetConnectedTriangles(hit.triangleIndex);
							} else {
								allTris = new List<int>{hit.triangleIndex};
							}
							for (int i = 0; i < allTris.Count; ++i){
								for (int j = 0; j < 3; ++j) {
									int cInt = tris[(allTris[i]*3) + j];
									cols[cInt] = Paint(cols[cInt], paintColor);
								}
							}
							fpd.SetColors(cols);

							#region Plugin hook
							// Pass click/drag events to plugins
							if (_anyPluginsActive){
								FacePaintSceneGUIData.SceneGUIEvent sge = FacePaintSceneGUIData.SceneGUIEvent.M_DOWN;
								if (e.type == EventType.MouseDrag){
									sge = FacePaintSceneGUIData.SceneGUIEvent.M_DRAG;
								}
								FacePaintSceneGUIData data = new FacePaintSceneGUIData(
									sge, hit.triangleIndex,
									tris[i0], tris[i0+1], tris[i0+2]
								);
								for (int i=0; i<_plugins.Count; ++i){
									if (_pluginsActive[i]){
										_plugins[i].OnSceneGUI(this, fpd, data);
									}
								}
							}
							#endregion
						} else if (hoverTris) {
							// If not clicked/dragging, pass triangle hover event to plugins
							FacePaintSceneGUIData data = new FacePaintSceneGUIData(
								FacePaintSceneGUIData.SceneGUIEvent.HOVER_TRIS,
								hit.triangleIndex,
								tris[i0], tris[i0+1], tris[i0+2]
							);
							for (int i=0; i<_plugins.Count; ++i){
								if (_pluginsActive[i]){
									_plugins[i].OnSceneGUI(this, fpd, data);
								}
							}
						}
						if (_hl) {
							// Highlight hovered triangle
							Matrix4x4 hm = Handles.matrix;
							Handles.matrix = _mf.transform.localToWorldMatrix;
							Vector3[] verts = m.vertices;
							Color hc = Handles.color;
							Handles.color = _hlCol;
							Handles.DrawAAPolyLine(
								_hlThick, 
								verts[tris[i0]], verts[tris[i0 + 1]],
								verts[tris[i0 + 2]], verts[tris[i0]]);
							Handles.matrix = hm;
							Handles.color = hc;
						}
					} else {
						#region Plugin hook
						if (_anyPluginsActive){
							FacePaintSceneGUIData data = new FacePaintSceneGUIData(
								FacePaintSceneGUIData.SceneGUIEvent.HOVER_MESH
							);
							for (int i=0; i<_plugins.Count; ++i){
								if (_pluginsActive[i]){
									_plugins[i].OnSceneGUI(this, fpd, data);
								}
							}
						}
						#endregion
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