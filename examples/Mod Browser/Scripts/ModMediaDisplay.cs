using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModMediaDisplay : ModMediaDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ModMediaDisplayComponent> logoClicked;
        public override event Action<ModMediaDisplayComponent> galleryImageClicked;
        public override event Action<ModMediaDisplayComponent> youTubeThumbnailClicked;

        [Header("Settings")]
        [SerializeField] private LogoSize m_logoSize;
        [SerializeField] private ModGalleryImageSize m_galleryImageSize;

        [Header("UI Components")]
        public Image image;
        public AspectRatioFitter fitter;
        public GameObject loadingOverlay;
        public GameObject logoOverlay;
        public GameObject galleryImageOverlay;
        public GameObject youTubeOverlay;

        [Header("Display Data")]
        [SerializeField] private ImageDisplayData m_data = new ImageDisplayData();
        private Action m_clickNotifier = null;

        // --- ACCESSORS ---
        public override LogoSize logoSize
        { get { return this.m_logoSize; } }
        public override ModGalleryImageSize galleryImageSize
        { get { return this.m_galleryImageSize; } }

        public override ImageDisplayData data
        {
            get { return m_data; }
            set
            {
                m_data = data;

                switch(data.mediaType)
                {
                    case ImageDisplayData.MediaType.ModLogo:
                    {
                        m_clickNotifier = NotifyLogoClicked;
                    }
                    break;
                    case ImageDisplayData.MediaType.ModGalleryImage:
                    {
                        m_clickNotifier = NotifyImageClicked;
                    }
                    break;
                    case ImageDisplayData.MediaType.ModYouTubeThumbnail:
                    {
                        m_clickNotifier = NotifyYouTubeClicked;
                    }
                    break;
                }

                PresentData();
            }
        }

        private void PresentData()
        {
            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }

            if(logoOverlay != null)
            {
                logoOverlay.SetActive(m_data.mediaType == ImageDisplayData.MediaType.ModLogo);
            }
            if(galleryImageOverlay != null)
            {
                galleryImageOverlay.SetActive(m_data.mediaType == ImageDisplayData.MediaType.ModGalleryImage);
            }
            if(youTubeOverlay != null)
            {
                youTubeOverlay.SetActive(m_data.mediaType == ImageDisplayData.MediaType.ModYouTubeThumbnail);
            }

            if(m_data.texture != null)
            {
                image.sprite = UIUtilities.CreateSpriteFromTexture(m_data.texture);

                if(fitter != null)
                {
                    fitter.aspectRatio = ((float)m_data.texture.width
                                          / (float)m_data.texture.height);
                }
            }
            else
            {
                image.sprite = null;
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

            // TODO(@jackson): connect onclick
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayLogo(int modId, LogoImageLocator locator)
        {
            Debug.Assert(locator != null);

            ImageDisplayData displayData = new ImageDisplayData()
            {
                modId = modId,
                mediaType = ImageDisplayData.MediaType.ModLogo,
                fileName = locator.fileName,
                texture = null,
            };
            m_data = displayData;
            m_clickNotifier = NotifyLogoClicked;

            DisplayLoading();

            ModManager.GetModLogo(displayData.modId,
                                  locator,
                                  logoSize,
                                  (t) =>
                                  {
                                    if(!Application.isPlaying) { return; }

                                    if(m_data.Equals(displayData))
                                    {
                                        m_data.texture = t;
                                        PresentData();
                                    }
                                  },
                                  WebRequestError.LogAsWarning);
        }

        public override void DisplayGalleryImage(int modId, GalleryImageLocator locator)
        {
            Debug.Assert(locator != null);


            ImageDisplayData displayData = new ImageDisplayData()
            {
                modId = modId,
                mediaType = ImageDisplayData.MediaType.ModGalleryImage,
                fileName = locator.fileName,
                texture = null,
            };
            m_data = displayData;
            m_clickNotifier = NotifyImageClicked;

            DisplayLoading();

            ModManager.GetModGalleryImage(displayData.modId,
                                          locator,
                                          galleryImageSize,
                                          (t) =>
                                          {
                                            if(!Application.isPlaying) { return; }
                                            if(m_data.Equals(displayData))
                                            {
                                                m_data.texture = t;
                                                PresentData();
                                            }
                                          },
                                          WebRequestError.LogAsWarning);
        }

        public override void DisplayYouTubeThumbnail(int modId, string youTubeVideoId)
        {
            Debug.Assert(!String.IsNullOrEmpty(youTubeVideoId),
                         "[mod.io] youTubeVideoId needs to be set to a valid YouTube video id.");


            ImageDisplayData displayData = new ImageDisplayData()
            {
                modId = modId,
                mediaType = ImageDisplayData.MediaType.ModYouTubeThumbnail,
                youTubeId = youTubeVideoId,
                texture = null,
            };
            m_data = displayData;
            m_clickNotifier = NotifyYouTubeClicked;

            DisplayLoading();

            ModManager.GetModYouTubeThumbnail(displayData.modId,
                                              displayData.youTubeId,
                                              (t) =>
                                              {
                                                if(!Application.isPlaying) { return; }
                                                if(m_data.Equals(displayData))
                                                {
                                                    m_data.texture = t;
                                                    PresentData();
                                                }
                                              },
                                              WebRequestError.LogAsWarning);
        }

        public override void DisplayLoading()
        {
            image.sprite = null;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }

            if(logoOverlay != null)
            {
                logoOverlay.SetActive(false);
            }
            if(galleryImageOverlay != null)
            {
                galleryImageOverlay.SetActive(false);
            }
            if(youTubeOverlay != null)
            {
                youTubeOverlay.SetActive(false);
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyClicked()
        {
            m_clickNotifier();
        }

        private void NotifyLogoClicked()
        {
            if(logoClicked != null)
            {
                logoClicked(this);
            }
        }

        private void NotifyImageClicked()
        {
            if(galleryImageClicked != null)
            {
                galleryImageClicked(this);
            }
        }

        private void NotifyYouTubeClicked()
        {
            if(youTubeThumbnailClicked != null)
            {
                youTubeThumbnailClicked(this);
            }
        }
    }
}
