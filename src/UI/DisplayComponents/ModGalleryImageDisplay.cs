using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component for easily displaying a mod gallery image.</summary>
    public class ModGalleryImageDisplay : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Image component used to display the gallery image.</summary>
        public Image image = null;

        /// <summary>Preferred image size.</summary>
        public ModGalleryImageSize imageSize = ModGalleryImageSize.Original;

        /// <summary>Gallery image locator for the displayed image.</summary>
        private GalleryImageLocator m_locator = null;

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays a Mod Gallery Image.</summary>
        public virtual void DisplayGalleryImage(int modId, GalleryImageLocator newLocator)
        {
            if(this.m_locator != newLocator)
            {
                this.image.enabled = false;
                this.m_locator = newLocator;

                if(newLocator != null)
                {
                    System.Action<Texture2D> displayDelegate = (t) => ApplyTexture(newLocator, t);
                    System.Action<Texture2D> fallbackDelegate = null;
                    if(imageSize == ModGalleryImageSize.Original)
                    {
                        fallbackDelegate = displayDelegate;
                    }

                    ImageRequestManager.instance.RequestModGalleryImage(modId, newLocator, this.imageSize,
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
