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
            get { return m_useOriginal; }
            set
            {
                if(m_useOriginal != value)
                {
                    m_useOriginal = value;
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

            if(avatarOverlay != null)
            {
                avatarOverlay.SetActive(m_data.mediaType == ImageDisplayData.MediaType.UserAvatar);
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

            // if original is missing, just use thumbnail
            bool original = m_useOriginal;
            if(original && m_data.GetImageTexture(true) == null)
            {
                original = false;
            }

            Texture2D texture = m_data.GetImageTexture(original);
            if(texture != null)
            {
                image.sprite = UIUtilities.CreateSpriteFromTexture(texture);

                if(fitter != null)
                {
                    fitter.aspectRatio = ((float)texture.width
                                          / (float)texture.height);
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
        public override void DisplayAvatar(int userId, AvatarImageLocator locator)
        {
            Debug.Assert(locator != null);
            bool original = m_useOriginal;
            UserAvatarSize size = (original ? UserAvatarSize.Original : ImageDisplayData.defaultAvatarSize);

            ImageDisplayData displayData = new ImageDisplayData()
            {
                userId = userId,
                mediaType = ImageDisplayData.MediaType.UserAvatar,
                fileName = locator.fileName,
                originalTexture = null,
                thumbnailTexture = null,
            };
            m_data = displayData;

            DisplayLoading();

            ModManager.GetUserAvatar(displayData.userId,
                                     locator,
                                     size,
                                     (t) =>
                                     {
                                        if(!Application.isPlaying) { return; }

                                        if(m_data.Equals(displayData))
                                        {
                                            m_data.SetImageTexture(original, t);
                                            PresentData();
                                        }
                                     },
                                     WebRequestError.LogAsWarning);
        }
        public override void DisplayLogo(int modId, LogoImageLocator locator)
        {
            Debug.Assert(locator != null);
            bool original = m_useOriginal;
            LogoSize size = (original ? LogoSize.Original : ImageDisplayData.defaultLogoSize);

            ImageDisplayData displayData = new ImageDisplayData()
            {
                modId = modId,
                mediaType = ImageDisplayData.MediaType.ModLogo,
                fileName = locator.fileName,
                originalTexture = null,
                thumbnailTexture = null,
            };
            m_data = displayData;

            DisplayLoading();

            ModManager.GetModLogo(displayData.modId,
                                  locator,
                                  size,
                                  (t) =>
                                  {
                                    if(!Application.isPlaying) { return; }

                                    if(m_data.Equals(displayData))
                                    {
                                        m_data.SetImageTexture(original, t);
                                        PresentData();
                                    }
                                  },
                                  WebRequestError.LogAsWarning);
        }

        public override void DisplayGalleryImage(int modId, GalleryImageLocator locator)
        {
            Debug.Assert(locator != null);
            bool original = m_useOriginal;
            ModGalleryImageSize size = (original ? ModGalleryImageSize.Original : ImageDisplayData.defaultGalleryImageSize );

            ImageDisplayData displayData = new ImageDisplayData()
            {
                modId = modId,
                mediaType = ImageDisplayData.MediaType.ModGalleryImage,
                fileName = locator.fileName,
                originalTexture = null,
                thumbnailTexture = null,
            };
            m_data = displayData;

            DisplayLoading();

            ModManager.GetModGalleryImage(displayData.modId,
                                          locator,
                                          size,
                                          (t) =>
                                          {
                                            if(!Application.isPlaying) { return; }

                                            if(m_data.Equals(displayData))
                                            {
                                                m_data.SetImageTexture(original, t);
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
                originalTexture = null,
                thumbnailTexture = null,
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
                                                    m_data.originalTexture = t;
                                                    m_data.thumbnailTexture = t;
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
