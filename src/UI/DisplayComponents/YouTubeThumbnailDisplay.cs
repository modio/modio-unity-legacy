using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component for easily displaying a YouTube thumbnail.</summary>
    public class YouTubeThumbnailDisplay : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Image component used to display the YouTube thumbnail.</summary>
        public Image image = null;

        /// <summary>YouTube id for the displayed thumbnail.</summary>
        private string m_youTubeId = string.Empty;

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays a YouTube thumbnail.</summary>
        public virtual void DisplayThumbnail(int modId, string youTubeId)
        {
            if(this.m_youTubeId != youTubeId)
            {
                this.image.enabled = false;
                this.m_youTubeId = youTubeId;

                if(!string.IsNullOrEmpty(youTubeId))
                {
                    System.Action<Texture2D> displayDelegate = (t) => ApplyTexture(youTubeId, t);

                    ImageRequestManager.instance.RequestYouTubeThumbnail(modId, youTubeId,
                                                                         displayDelegate,
                                                                         WebRequestError.LogAsWarning);
                }
            }
        }

        /// <summary>Internal function for applying the texture.</summary>
        protected virtual void ApplyTexture(string youTubeId, Texture2D texture)
        {
            if(this != null
               && texture != null
               && this.m_youTubeId == youTubeId)
            {
                this.image.sprite = UIUtilities.CreateSpriteFromTexture(texture);
                this.image.enabled = true;
            }
        }
    }
}
