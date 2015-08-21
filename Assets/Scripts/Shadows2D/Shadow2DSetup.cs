using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shadow2D
{
    [ExecuteInEditMode]
    public class Shadow2DSetup : MonoBehaviour
    {
        // This class makes sure that the main camera, the texture camera and the render texture 
        // are kept in sync

        public Camera mainCamera;
        public Camera renderTextureCamera;
        public Material lightMaterial;
        public Material ambientMaterial;

        public void OnGUI()
        {
            SimpleSetup();
        }

        public void OnDrawGizmos()
        {
            SimpleSetup();
        }


        public void Update()
        {
            Setup();
        }

        private void Setup()
        {
            if (mainCamera == null)
            {
                Debug.LogWarning("No main camera in Shadow 2D setup!");
                return;
            }

            if (renderTextureCamera == null)
            {
                Debug.LogWarning("No Render Texture Camera in Shadow 2D setup!");
                return;
            }

            if (renderTextureCamera.orthographic != mainCamera.orthographic)
            {
                Debug.LogWarning("Changed Render Texture Camera to orthographic={0} to match the Main Camera.!");
                renderTextureCamera.orthographic = mainCamera.orthographic;
            }

            if (lightMaterial == null || ambientMaterial == null)
            {
                Debug.LogWarning("Light Material and Ambient Material must be assigned!");
                return;
            }

            RenderTexture renderTexture = renderTextureCamera.targetTexture;

            if (renderTexture == null || renderTexture.width != Screen.width || renderTexture.height != Screen.height)
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    DestroyImmediate(renderTexture);
                    Debug.LogFormat("Resizing render texture to: ({0}, {1})", Screen.height, Screen.width);
                }

                renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
                renderTexture.name = "Shadow2d Render Texture";
                renderTextureCamera.targetTexture = renderTexture;
                lightMaterial.mainTexture = renderTexture;
                ambientMaterial.mainTexture = renderTexture;
            }

            renderTextureCamera.transform.position = mainCamera.transform.position;
            renderTextureCamera.transform.rotation = mainCamera.transform.rotation;
            renderTextureCamera.orthographicSize = mainCamera.orthographicSize;
        }

        private void SimpleSetup()
        {
            if (renderTextureCamera != null)
            {
                RenderTexture renderTexture = renderTextureCamera.targetTexture;
                if (lightMaterial != null)
                {
                    lightMaterial.mainTexture = renderTexture;
                }
                if (ambientMaterial != null)
                {
                    ambientMaterial.mainTexture = renderTexture;
                }
            }
        }
    }
}