using UnityEngine;

namespace Submersible.Runtime.Portals
{
    public class Portal : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private RenderTexture renderTextureTemplate;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private string materialTextureParameterName;
        
        private RenderTexture _renderTextureInstance;
        
        private void Awake()
        {
            // Create a new render texture instance with the same properties as the template
            _renderTextureInstance = new RenderTexture(renderTextureTemplate.descriptor);
            
            // Assign the instanced render texture to the camera and material
            viewCamera.targetTexture = _renderTextureInstance;
            targetRenderer.material.SetTexture(materialTextureParameterName, _renderTextureInstance);
        }
        
        private void OnDestroy()
        {
            // Clean up the render texture when the object is destroyed
            if (_renderTextureInstance != null)
            {
                _renderTextureInstance.Release();
                Destroy(_renderTextureInstance);
            }
        }
    }
}