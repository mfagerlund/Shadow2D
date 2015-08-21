
Follow these steps to use Shadows2D in your scene:

1. Make sure you have a floor layer - this will be the shadowed-background layer.
2. Make sure that items that should be shadowed are placed in the floor layer.
3. Make sure the main camera excludes the floor layer - your background will now become invisible
4. Create a new camera (RenderTextureCamera)
  * Call it RenderTextureCamera
  * Parent it to the main camera
  * Make sure it's on the same z as the main camera - so it doesn't end up behind level
  * Remove the AudioListener
  * Remove the GUI Layer
  * Remove the Flare Layer
5. Add the Shadow 2D Setup component to one game object in your scene.
  * Hook in the main camera
  * Hook in the render texture camera
  * Select the ambient material (called Shadow2D Ambient)
  * Select the light material (called Shadow2D Light)
6. Create the ambient object
  * Add a new empty game object (locate it at 0,0,0)
  * Add the script "Shadow 2D Ambient"
  * In the MeshRenderer, set the material to the ambient material (called Shadow2D Ambient)
7. Make sure your foreground items have colliders - that's what'll be used for shadows.
8. Add a light
  * Create new empty game object
  * Add the script "Shadow 2D Light"
  * In the MeshRenderer, set the material to the light material (called Shadow2D Light)

And you should be done!