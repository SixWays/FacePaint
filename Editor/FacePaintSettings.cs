using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sigtrap.FacePaint {
	public class FacePaintSettings : ScriptableObject {
		#region Colors
		[HideInInspector]
		public Color paintColor = Color.black;
		[HideInInspector]
		public Color defaultColor = Color.black;
		[HideInInspector]
		public List<Color> palette = new List<Color>{
			Color.black, Color.white
		};
		#endregion

		#region Highlighting
		[HideInInspector]
		public Color hlCol = Color.green;
		[HideInInspector]
		public int hlThick = 5;
		#endregion

		#region Panel folding
		[HideInInspector]
		public bool showPalette = true;
		[HideInInspector]
		public bool showChannels = true;
		[HideInInspector]
		public bool showModes = true;
		[HideInInspector]
		public bool showPlugins = false;
		[HideInInspector]
		public bool showSettings = false;
		#endregion

		[HideInInspector]
		public bool hideLogo = false;

		#region Plugins
		[SerializeField][HideInInspector]
		private List<string> _activePlugins = new List<string>();
		[SerializeField][HideInInspector]
		private List<string> _unfoldedPluginSettings = new List<string>();

		private bool PluginInList(List<string> list, IFacePaintPlugin plugin){
			return list.Contains(plugin.title);
		}
		private void SetPluginInList(List<string> list, IFacePaintPlugin plugin, bool inList){
			string title = plugin.title;
			if (inList){
				if (!list.Contains(title)){
					list.Add(title);
				}
			} else {
				if (list.Contains(title)){
					list.Remove(title);
				}
			}
		}

		public bool PluginIsActive(IFacePaintPlugin plugin) {
			return PluginInList(_activePlugins, plugin);
		}
		public void SetPluginActive(IFacePaintPlugin plugin, bool active){
			SetPluginInList(_activePlugins, plugin, active);
		}
		public bool PluginSettingsUnfolded(IFacePaintPlugin plugin){
			return PluginInList(_unfoldedPluginSettings, plugin);
		}
		public void SetPluginSettingsUnfolded(IFacePaintPlugin plugin, bool unfold){
			SetPluginInList(_unfoldedPluginSettings, plugin, unfold);
		}
		#endregion
	}
}