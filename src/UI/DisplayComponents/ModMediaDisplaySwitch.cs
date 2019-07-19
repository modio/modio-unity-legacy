using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component used to display a mod media element by routing to various components.</summary>
    public class ModMediaDisplaySwitch : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Logo display component.</summary>
        public ModLogoDisplay logo;
        /// <summary>Gallery image display component.</summary>
        public GalleryImageDisplay galleryImage;
        /// <summary>YouTube Thumbnail display component.</summary>
        public YouTubeThumbnailDisplay youTubeThumbnail;

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>ModProfile currently being displayed.</summary>
        private ModProfile m_profile = null;

        // ---------[ INITIALIZATION ]---------
        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged -= DisplayProfile;
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged += DisplayProfile;
                this.DisplayProfile(this.m_view.profile);
            }
            else
            {
                this.DisplayProfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Extracts the necessary display data from the profile and presents it.</summary>
        public void DisplayProfile(ModProfile profile)
        {
            if(this.m_profile != profile)
            {
                this.m_profile = profile;

                this.DisplayProfileLogo();
            }
        }

        /// <summary>Display logo for the assigned profile.</summary>
        public void DisplayProfileLogo()
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
                this.logo.gameObject.SetActive(this.m_profile != null);
            }
        }

        /// <summary>Display Gallery Image for the assigned profile.</summary>
        public void DisplayProfileGalleryImage(string imageFileName)
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
                // get locator
                GalleryImageLocator locator = null;
                if(this.m_profile != null
                   && this.m_profile.media != null)
                {
                    locator = this.m_profile.media.GetGalleryImageWithFileName(imageFileName);
                }

                // display
                this.galleryImage.gameObject.SetActive(locator != null);
                if(locator != null)
                {
                    this.galleryImage.DisplayGalleryImage(this.m_profile.id, locator);
                }
            }
        }

        /// <summary>Display YouTube Thumbnail for the assigned profile.</summary>
        public void DisplayProfileYouTubeThumbnail(string youTubeId)
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
                bool isDisplayable = (this.m_profile != null && !string.IsNullOrEmpty(youTubeId));

                // display
                this.youTubeThumbnail.gameObject.SetActive(isDisplayable);
                if(isDisplayable)
                {
                    this.youTubeThumbnail.DisplayThumbnail(this.m_profile.id, youTubeId);
                }
            }
        }
    }
}
