using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Variant of the AspectRatioFitter that can match the ratio of a given
    /// texture.</summary>
    public class TextureAspectRatioMatcher : AspectRatioFitter
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            this.SetDirty();
        }

        /// <summary>Sets the aspect ratio to match the given texture.</summary>
        public void MatchTexture(UnityEngine.Texture2D texture)
        {
            if(texture != null)
            {
                this.aspectRatio = ((float)texture.width / (float)texture.height);
            }
        }

#if UNITY_EDITOR
        /// <summary>Solves an issue where updates throw a warning in-editor.</summary>
        protected override void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    base.OnValidate();
                }
            };
        }
#endif
    }
}
