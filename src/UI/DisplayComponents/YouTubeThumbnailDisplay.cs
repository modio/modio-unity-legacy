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

        /// <summary>Current modId for the displayed thumbnail.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>YouTube id for the displayed thumbnail.</summary>
        private string m_youTubeId = string.Empty;

        // --- ACCESSORS ---
        /// <summary>Current modId for the displayed thumbnail.</summary>
        public int ModId
        { get { return this.m_modId;    } }

        /// <summary>YouTube id for the displayed thumbnail.</summary>
        public string YouTubeId
        { get { return this.m_youTubeId;  } }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays a YouTube thumbnail.</summary>
        public virtual void DisplayThumbnail(int modId, string youTubeId)
        {
            this.m_modId = modId;

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

        /// <summary>Opens the web browser for the displaying YouTube Thumbnail.</summary>
        public virtual void OpenVideoInBrowser()
        {
            if(!string.IsNullOrEmpty(this.m_youTubeId))
            {
                UIUtilities.OpenYouTubeVideoURL(this.m_youTubeId);
            }
        }
    }
}
