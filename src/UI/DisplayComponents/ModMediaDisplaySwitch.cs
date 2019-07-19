using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component used to display a mod media element by routing to various components.</summary>
    public class ModMediaDisplaySwitch : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Logo display component.</summary>
        public ModLogoDisplay logo;
        /// <summary>Gallery image display component.</summary>
        public GalleryImageDisplay galleryImage;
        /// <summary>YouTube Thumbnail display component.</summary>
        public YouTubeThumbnailDisplay youTubeThumbnail;

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Display a logo via the linked logo display.</summary>
        public void DisplayLogo(int modId, LogoImageLocator locator)
        {
            // disable other components
            if(this.galleryImage != null)
            {
                this.galleryImage.gameObject.SetActive(false);
            }
            if(this.youTubeThumbnail != null)
            {
                this.youTubeThumbnail.gameObject.SetActive(false);
            }

            // display logo
            if(this.logo != null)
            {
                this.logo.gameObject.SetActive(locator != null);

                if(locator != null)
                {
                    this.logo.DisplayLogo(modId, locator);
                }
            }
        }

        /// <summary>Copies the data of another component to display.</summary>
        public void DisplayLogo(ModLogoDisplay display)
        {
            int modId = ModProfile.NULL_ID;
            LogoImageLocator locator = null;

            if(display != null)
            {
                modId = display.ModId;
                locator = display.Locator;
            }

            this.DisplayLogo(modId, locator);
        }

        /// <summary>Display a gallery image via the linked image display.</summary>
        public void DisplayGalleryImage(int modId, GalleryImageLocator locator)
        {
            // disable other components
            if(this.logo != null)
            {
                this.logo.gameObject.SetActive(false);
            }
            if(this.youTubeThumbnail != null)
            {
                this.youTubeThumbnail.gameObject.SetActive(false);
            }

            // display gallery image
            if(this.galleryImage != null)
            {
                this.galleryImage.gameObject.SetActive(locator != null);

                if(locator != null)
                {
                    this.galleryImage.DisplayGalleryImage(modId, locator);
                }
            }
        }

        /// <summary>Copies the data of another component to display.</summary>
        public void DisplayGalleryImage(GalleryImageDisplay display)
        {
            int modId = ModProfile.NULL_ID;
            GalleryImageLocator locator = null;

            if(display != null)
            {
                modId = display.ModId;
                locator = display.Locator;
            }

            this.DisplayGalleryImage(modId, locator);
        }

        /// <summary>Display a YouTube thumbnail via the linked thumbnail display.</summary>
        public void DisplayYouTubeThumbnail(int modId, string youTubeId)
        {
            // disable other components
            if(this.logo != null)
            {
                this.logo.gameObject.SetActive(false);
            }
            if(this.galleryImage != null)
            {
                this.galleryImage.gameObject.SetActive(false);
            }

            // display gallery image
            if(this.youTubeThumbnail != null)
            {
                bool idExists = !string.IsNullOrEmpty(youTubeId);

                // display
                this.youTubeThumbnail.gameObject.SetActive(idExists);
                if(idExists)
                {
                    this.youTubeThumbnail.DisplayThumbnail(modId, youTubeId);
                }
            }
        }

        /// <summary>Copies the data of another component to display.</summary>
        public void DisplayYouTubeThumbnail(YouTubeThumbnailDisplay display)
        {
            int modId = ModProfile.NULL_ID;
            string youTubeId = null;

            if(display != null)
            {
                modId = display.ModId;
                youTubeId = display.YouTubeId;
            }

            this.DisplayYouTubeThumbnail(modId, youTubeId);
        }
    }
}
