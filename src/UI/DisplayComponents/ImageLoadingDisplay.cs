using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Component for displaying an object while a texture is loading.</summary>
    public class ImageLoadingDisplay : MonoBehaviour
    {
        /// <summary>Sets the GameObject active if the texture is null.</summary>
        public void EnableOnNullTexture(Texture2D texture)
        {
            this.gameObject.SetActive(texture == null);
        }
    }
}
