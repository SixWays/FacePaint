![FacePaint](http://www.sigtrapgames.com/wp-content/uploads/2016/11/header-logo.png)

# FacePaint
Simple per-poly vertex painter for Unity. *Written/tested on Unity 5.4.0b21. 5.3+ should be ok.*

FacePaint paints triangles rather than individual vertices. This is mainly suited for materials and meshes using a low-poly / flat shaded / faceted look. *Note that this may have strange effects for meshes with smoothing groups!*

Editing is non-destructive; FacePaint stores its data in a _FacePaintData_ component (automatically added to edited objects). This data is applied at runtime. Your mesh files aren't altered, and each instance of a mesh can have its own vertex colours.

![FacePaint UI](http://www.sigtrapgames.com/wp-content/uploads/2016/11/facepaint.png)

## Installation and Use
Copy the FacePaint folder into the Assets folder of your project. **Window > FacePaint** will launch a dockable editor window. Select an object with a MeshRenderer and MeshFilter and press Edit in the FacePaint window.

## Extensibility
FacePaint is designed to be ***minimal*** and ***extensible***, not feature-complete. One person's super-specialised tool is another's bloated mess!

FacePaint features a plugin system to easily extend the tool without having to modify core code - quick guide below. A few are included both as examples and as potentially helpful tools. In general, I'll add new features as plugins to avoid bloat, unless they should absolutely be core functionality.

## Issues
* _May interfere with unity-generated lightmapping UVs. This should be fixed soon._
* _If your mesh isn't totally flat shaded (i.e. each triangle must have completely unique vertices) you may see colours 'bleed' across triangles. I'll add a custom mesh import script soon._

## Tips
* _To give multiple mesh instances the same colours, simply copy and paste the FacePaintData component. Press the "Force Re-apply Vertex Colours" button if nothing happens._
* _To reset vertex colours entirely, ***make sure you're out of edit mode*** and then delete the FacePaintData component._

## Additional credits
This tool is inspired by the excellent [Unity 5.0 Vertex Painter](https://github.com/slipster216/VertexPaint "GitHub Page") by Jason Booth (particularly the use of _MeshRenderer.additionalVertexStreams_ to apply colours non-destructively).

Paint bucket icon by [Yusuke Kamiyamane](http://p.yusukekamiyamane.com/)

# Plugin Development Guide
Plugins implement the _IFacePaintPlugin_ interface. All classes implementing this will be automatically found by FacePaint and added to the plugins list, using spooky reflection magic. Alternatively, just inherit from the _FacePaintPluginBase_ "template" class to get a head start.

## Callbacks
Plugins (when set active in the 'Plugins' panel of FacePaint) get callbacks at various points in OnGUI. These are after a panel has been drawn by FacePaint, allowing plugins to augment the existing panels. The _OnSceneGUI_ callback allows plugins to act when the user interacts with the model being painted.

Most callbacks pass the FacePaint instance itself for access to the API, and the FacePaintData component currently being edited (this is the MonoBehaviour attached to a mesh which stores and applies vertex colours).

The _OnPluginPanel_ callback should be used to implement any general UI that's not appropriate for any of the existing panels.

The _OnSceneGUI_ callback passes data on what kind of mouse event has occurred, which triangle on the mesh is affected if any, and the verts which comprise that triangle. This data can be used to manually paint on the mesh, for example. _Currently it's not possible to 'eat' the event and prevent the default FacePaint behaviour from happening_.

## API
If in doubt, Intellisense!

GUI helper functions _FacePaint.DrawBtn()_, _FacePaint.ToggleButton()_, _FacePaint.DrawPluginTitle()_ are purely for convenience. Any standard IMGUI code can be used.

_FacePaint.paintColor_ and _FacePaint.writeX_/_SetChannels()_ control the paint colour and the channels paint will apply to. _FacePaint.Paint()_ calculates the resulting colour when a given colour is painted over with a new colour, respecting the channel settings. It returns this colour without applying it to anything.

To apply colours, use _FacePaintData.GetColors()_, modify the resulting array, and then _FacePaintData.SetColors()_. Changes to FacePaintData made in plugins are automatically added to undo.

The "Assist Mode" shader can be overridden using _FacePaint.OverrideAssistShader()_, or reset by passing null. This is used, for instance, by the FacePaint_LUT built-in plugin.