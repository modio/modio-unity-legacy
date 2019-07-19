using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component for easily displaying a mod gallery image.</summary>
    public class GalleryImageDisplay : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Image component used to display the gallery image.</summary>
        public Image image = null;

        /// <summary>Preferred image size.</summary>
        public ModGalleryImageSize imageSize = ModGalleryImageSize.Original;

        /// <summary>Current modId for the displayed image.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>Gallery image locator for the displayed image.</summary>
        private GalleryImageLocator m_locator = null;

        // --- ACCESSORS ---
        /// <summary>Current modId for the displayed image.</summary>
        public int ModId
        { get { return this.m_modId;    } }

        /// <summary>Locator for the displayed image.</summary>
        public GalleryImageLocator Locator
        { get { return this.m_locator;  } }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays a Mod Gallery Image.</summary>
        public virtual void DisplayGalleryImage(int modId, GalleryImageLocator locator)
        {
            this.m_modId = modId;

            if(this.m_locator != locator)
            {
                this.image.enabled = false;
                this.m_locator = locator;

                if(locator != null)
                {
                    System.Action<Texture2D> displayDelegate = (t) => ApplyTexture(locator, t);
                    System.Action<Texture2D> fallbackDelegate = null;
                    if(imageSize == ModGalleryImageSize.Original)
                    {
                        fallbackDelegate = displayDelegate;
                    }

                    ImageRequestManager.instance.RequestModGalleryImage(modId, locator, this.imageSize,
                                                                        displayDelegate,
                                                                        fallbackDelegate,
                                                                        WebRequestError.LogAsWarning);
                }
            }
        }

        /// <summary>Internal function for applying the texture.</summary>
        protected virtual void ApplyTexture(GalleryImageLocator locator, Texture2D texture)
        {
            if(this != null
               && texture != null
               && this.m_locator == locator)
            {
                this.image.sprite = UIUtilities.CreateSpriteFromTexture(texture);
                this.image.enabled = true;
            }
        }
    }
}
