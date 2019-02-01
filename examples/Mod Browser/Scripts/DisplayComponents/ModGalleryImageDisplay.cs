using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModGalleryImageDisplay : ModGalleryImageDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ImageDataDisplayComponent> onClick;

        [Header("Settings")]
        [SerializeField] private ModGalleryImageSize m_imageSize;
        [Tooltip("Display the image at its original resolution rather than using the thumbnail")]
        [SerializeField] private bool m_useOriginal = false;

        [Header("UI Components")]
        public Image image;
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField] private ImageDisplayData m_data;

        // --- ACCESSORS ---
        public override ModGalleryImageSize imageSize
        {
            get { return m_imageSize; }
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

        // TODO(@jackson): Add m_useOriginal
        private void PresentData()
        {
            if(m_data.texture != null)
            {
                image.sprite = UIUtilities.CreateSpriteFromTexture(m_data.texture);
            }
            else
            {
                image.sprite = null;
            }

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            if(Application.isPlaying)
            {
                Debug.Assert(image != null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayImage(int modId, GalleryImageLocator locator)
        {
            Debug.Assert(locator != null && !String.IsNullOrEmpty(locator.fileName),
                         "[mod.io] locator needs to be set and have a fileName.");

            ImageDisplayData imageData = new ImageDisplayData()
            {
                modId = modId,
                mediaType = ImageDisplayData.MediaType.ModGalleryImage,
                fileName = locator.fileName,
                texture = null,
            };

            DisplayInternal(imageData, locator);
        }

        // NOTE(@jackson): Called internally, this is only used when displayData.texture == null
        private void DisplayInternal(ImageDisplayData displayData, GalleryImageLocator locator)
        {
            Debug.Assert(displayData.texture == null);

            m_data = displayData;

            if(locator == null)
            {
                PresentData();
            }
            else
            {
                DisplayLoading();

                ModManager.GetModGalleryImage(displayData.modId,
                                              locator,
                                              imageSize,
                                              (t) =>
                                              {
                                                if(!Application.isPlaying
                                                   || image == null)
                                                {
                                                    return;
                                                }

                                                if(m_data.Equals(displayData))
                                                {
                                                    m_data.texture = t;
                                                    PresentData();
                                                }
                                              },
                                              WebRequestError.LogAsWarning);
            }
        }

        public override void DisplayLoading()
        {
            image.sprite = null;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyClicked()
        {
            if(this.onClick != null)
            {
                this.onClick(this);
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
