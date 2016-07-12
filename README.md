# FacePaint
Simple per-poly vertex painter for Unity. *Written/tested on Unity 5.4.0b21. 5.3+ should be ok.*

FacePaint paints triangles rather than individual vertices. This is mainly suited for materials and meshes using a low-poly / flat shaded / faceted look. *Note that this may have strange effects for meshes with smoothing groups!*

Editing is non-destructive; FacePaint stores its data in a FacePaintData component (automatically added to edited objects). This data is applied at runtime. Your mesh files aren't altered, and each instance of a mesh can have its own vertex colours.

## Installation and Use
Copy the FacePaint folder into the Assets folder of your project. Window > FacePaint will launch a dockable editor window. Select an object with a MeshRenderer and MeshFilter and press Edit in the FacePaint window.

## Aims
FacePaint is intended to be as simple as possible. Hopefully this allows you to extend and repurpose it easily for your own use-case.

If there's demand, I'd like to add a 'plugin'-type system whereby modular extensions can easily be written rather than adding bloat and complexity to the core code.

## Tips
To give multiple mesh instances the same colours, simply copy and paste the FacePaintData component.

To reset vertex colours entirely, make sure you're out of edit mode and then delete the FacePaintData component.

## Additional credits
This tool is inspired by the excellent [Unity 5.0 Vertex Painter](https://github.com/slipster216/VertexPaint "GitHub Page") by Jason Booth (particularly the use of MeshRenderer.additionalVertexStreams to apply colours non-destructively).

Paint bucket icon by [Yusuke Kamiyamane](http://p.yusukekamiyamane.com/)