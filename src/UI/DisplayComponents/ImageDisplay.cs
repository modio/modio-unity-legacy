using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ImageDisplay : ImageDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ImageDisplayComponent> onClick;

        [Header("Settings")]
        [Tooltip("Display the image at its original resolution rather than using the thumbnail")]
        [SerializeField] private bool m_useOriginal;

        [Header("UI Components")]
        public Image image;
        public AspectRatioFitter fitter;
        public GameObject loadingOverlay;
        public GameObject avatarOverlay;
        public GameObject logoOverlay;
        public GameObject galleryImageOverlay;
        public GameObject youTubeOverlay;

        [Header("Display Data")]
        [SerializeField] private ImageDisplayData m_data = new ImageDisplayData();

        // --- ACCESSORS ---
        public override bool useOriginal
        {
            get { return this.m_useOriginal; }
            set { this.m_useOriginal = value;}
        }
        public override ImageDisplayData data
        {
            get { return m_data; }
            set
            {
                m_data = value;

                #if UNITY_EDITOR
                if(Application.isPlaying)
                #endif
                {
                    PresentData();
                }
            }
        }

        private void PresentData()
        {
            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }

            string imageURL = this.m_data.GetImageURL(this.m_useOriginal);
            Texture2D texture = null;

            // attempt cache retrieval
            ImageRequestManager.instance.cache.TryGetValue(imageURL, out texture);
            if(texture != null)
            {
                DisplayTexture(texture);
                SetOverlayVisibility(true);
                return;
            }

            // get fallback?
            if(this.m_useOriginal
               && ImageRequestManager.instance.cache.TryGetValue(this.m_data.GetImageURL(false), out texture))
            {
                DisplayTexture(texture);
                SetOverlayVisibility(true);
            }

            // request missing texture
            if(texture == null)
            {
                DisplayLoading();
            }

            if(!string.IsNullOrEmpty(imageURL))
            {
                ImageDisplayData iData = this.m_data;
                ImageRequestManager.instance.RequestImage(imageURL,
                (t) =>
                {
                    if(this != null && iData.Equals(this.m_data))
                    {
                        DisplayTexture(t);
                        SetOverlayVisibility(true);
                    }
                },
                WebRequestError.LogAsWarning);
            }
        }

        private void DisplayTexture(Texture2D texture)
        {
            Debug.Assert(texture != null);

            if(image != null)
            {
                image.sprite = UIUtilities.CreateSpriteFromTexture(texture);

                if(fitter != null)
                {
                    fitter.aspectRatio = ((float)texture.width / (float)texture.height);
                }

                image.enabled = true;
            }
        }

        private void SetOverlayVisibility(bool isVisible)
        {
            if(avatarOverlay != null)
            {
                avatarOverlay.SetActive(isVisible &&
                                        m_data.mediaType == ImageDisplayData.MediaType.UserAvatar);
            }
            if(logoOverlay != null)
            {
                logoOverlay.SetActive(isVisible
                                      && m_data.mediaType == ImageDisplayData.MediaType.ModLogo);
            }
            if(galleryImageOverlay != null)
            {
                galleryImageOverlay.SetActive(isVisible
                                              && m_data.mediaType == ImageDisplayData.MediaType.ModGalleryImage);
            }
            if(youTubeOverlay != null)
            {
                youTubeOverlay.SetActive(isVisible
                                         && m_data.mediaType == ImageDisplayData.MediaType.YouTubeThumbnail);
            }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            if(Application.isPlaying)
            {
                Debug.Assert(image != null);
            }

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
            if(avatarOverlay != null)
            {
                avatarOverlay.SetActive(false);
            }
            if(logoOverlay != null)
            {
                logoOverlay.SetActive(false);
            }
            if(youTubeOverlay != null)
            {
                youTubeOverlay.SetActive(false);
            }
            if(galleryImageOverlay != null)
            {
                galleryImageOverlay.SetActive(false);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public void DisplayAvatar(int userId, AvatarImageLocator locator)
        {
            Debug.Assert(locator != null);

            ImageDisplayData displayData = ImageDisplayData.CreateForUserAvatar(userId, locator);
            m_data = displayData;

            PresentData();
        }
        public void DisplayLogo(int modId, LogoImageLocator locator)
        {
            Debug.Assert(locator != null);

            ImageDisplayData displayData = ImageDisplayData.CreateForModLogo(modId, locator);
            m_data = displayData;

            PresentData();
        }

        public void DisplayGalleryImage(int modId, GalleryImageLocator locator)
        {
            Debug.Assert(locator != null);

            ImageDisplayData displayData = ImageDisplayData.CreateForModGalleryImage(modId, locator);
            m_data = displayData;

            PresentData();
        }

        public void DisplayYouTubeThumbnail(int modId, string youTubeVideoId)
        {
            Debug.Assert(!String.IsNullOrEmpty(youTubeVideoId),
                         "[mod.io] youTubeVideoId needs to be set to a valid YouTube video id.");

            ImageDisplayData displayData = ImageDisplayData.CreateForYouTubeThumbnail(modId, youTubeVideoId);
            m_data = displayData;

            PresentData();
        }

        public override void DisplayLoading()
        {
            if(image != null)
            {
                image.enabled = false;
            }
            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }

            SetOverlayVisibility(false);
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyClicked()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }
    }
}
