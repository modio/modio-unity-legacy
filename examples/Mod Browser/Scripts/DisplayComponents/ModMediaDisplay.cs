using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModMediaDisplay : ModMediaDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ImageDataDisplayComponent> onClick;

        [Header("Settings")]
        [SerializeField] private LogoSize m_logoSize;
        [SerializeField] private ModGalleryImageSize m_galleryImageSize;
        [Tooltip("Display the image and it's original resolution rather than the default size")]
        [SerializeField] private bool m_useOriginalRes;

        [Header("UI Components")]
        public Image image;
        public AspectRatioFitter fitter;
        public GameObject loadingOverlay;
        public GameObject logoOverlay;
        public GameObject galleryImageOverlay;
        public GameObject youTubeOverlay;

        [Header("Display Data")]
        [SerializeField] private ImageDisplayData m_data = new ImageDisplayData();

        // --- ACCESSORS ---
        public override LogoSize logoSize
        { get { return this.m_logoSize; } }
        public override ModGalleryImageSize galleryImageSize
        { get { return this.m_galleryImageSize; } }

        public override bool useOriginalRes
        {
            get { return m_useOriginalRes; }
            set
            {
                if(m_useOriginalRes != value)
                {
                    m_useOriginalRes = value;
                    PresentData();
                }
            }
        }
        public override ImageDisplayData data
        {
            get { return m_data; }
            set
            {
                m_data = value;
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
                youTubeOverlay.SetActive(m_data.mediaType == ImageDisplayData.MediaType.YouTubeThumbnail);
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
                mediaType = ImageDisplayData.MediaType.YouTubeThumbnail,
                youTubeId = youTubeVideoId,
                texture = null,
            };
            m_data = displayData;

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
            if(onClick != null)
            {
                onClick(this);
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null
                   && this.image != null)
                {
                    // NOTE(@jackson): Didn't notice any memory leakage with replacing textures.
                    // "Should" be fine.
                    PresentData();
                }
            };
        }
        #endif
    }
}
