# FacePaint
Simple per-poly vertex painter for Unity

Only tested with Unity 5.4.0b21 and above! However, should work ok on Unity 5.3+.

Unlike most vertex color tools, FacePaint paints triangles rather than individual vertices. This is mainly suited for materials and meshes using a low-poly / flat shaded / faceted look. *Note that this may have strange effects for meshes with smoothing groups!*

Editing is non-destructive; FacePaint stores its data in a FacePaintData component (automatically added to edited objects). This data is applied at runtime. Your mesh files aren't altered, and each instance of a mesh can have its own vertex colours.

*Tip: To give multiple mesh instances the same colours, simply copy and paste the FacePaintData component.*

This tool is inspired by the excellent [Unity 5.0 Vertex Painter](https://github.com/slipster216/VertexPaint "GitHub Page") by Jason Booth (particularly the use of MeshRenderer.additionalVertexStreams to apply colours non-destructively).

## Installation and Use
Copy the FacePaint folder into the Assets folder of your project. Window > FacePaint will launch a dockable editor window. Select an object with a MeshRenderer and MeshFilter and press Edit in the FacePaint window.

## Aims
FacePaint is intended to be as simple as possible. Hopefully this allows you to extend and repurpose it easily for your own use-case.

If there's demand, I'd like to add a 'plugin'-type system whereby modular extensions can easily be written rather than adding bloat and complexity to the core code.



Paint bucket icon by [Yusuke Kamiyamane](http://p.yusukekamiyamane.com/)