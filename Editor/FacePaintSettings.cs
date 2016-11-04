using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sigtrap.FacePaint {
	public class FacePaintSettings : ScriptableObject, ISerializationCallbackReceiver {
		[HideInInspector]
		public List<Color> palette = new List<Color>{
			Color.black, Color.white
		};
		[HideInInspector]
		public Texture2D lut;
		[HideInInspector]
		public bool useLut = false;
		[HideInInspector]
		public Color hlCol = Color.green;
		[HideInInspector]
		public int hlThick = 5;
		[HideInInspector]
		public bool hl = true;

		[SerializeField][HideInInspector]
		private List<System.Type> _customKeys = new List<System.Type>();
		[SerializeField][HideInInspector]
		private List<FacePaintCustomSettings> _customValues = new List<FacePaintCustomSettings>();

		private Dictionary<System.Type, FacePaintCustomSettings> _custom = new Dictionary<System.Type, FacePaintCustomSettings>();

		[HideInInspector]
		private List<System.Type> _activePlugins = new List<System.Type>();

		#region ISerializationCallbackReceiver implementation
		public void OnBeforeSerialize (){
			if (_custom != null){
				_customKeys.Clear();
				_customValues.Clear();
				foreach (var a in _custom){
					_customKeys.Add(a.Key);
					_customValues.Add(a.Value);
				}
			}
		}
		public void OnAfterDeserialize (){
			int c = Mathf.Min(_customKeys.Count, _customValues.Count);
			_custom.Clear();
			for (int i=0; i<c; ++i){
				_custom.Add(_customKeys[i], _customValues[i]);
			}
		}
		#endregion

		public T GetCustomData<T>() where T:FacePaintCustomSettings, new() {
			System.Type type = typeof(T);
			FacePaintCustomSettings result = null;
			if (_custom.TryGetValue(type, out result)){
				return (T)result;
			}
			T t = new T();
			_custom.Add(type, t);
			return t;
		}
		public bool PluginIsActive(System.Type t) {
			return _activePlugins.Contains(t);
		}
		public void SetPluginActive(System.Type t, bool active){
			if (active){
				if (!_activePlugins.Contains(t)){
					_activePlugins.Add(t);
				}
			} else {
				if (_activePlugins.Contains(t)){
					_activePlugins.Remove(t);
				}
			}
		}
	}
}