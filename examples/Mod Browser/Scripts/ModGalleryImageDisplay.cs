using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(Image))]
    public class ModGalleryImageDisplay : ModGalleryImageDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ImageDataDisplayComponent> onClick;

        [Header("Settings")]
        [SerializeField] private ModGalleryImageSize m_imageSize;

        [Header("UI Components")]
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField] private ImageDisplayData m_data;

        // --- ACCESSORS ---
        public Image image
        {
            get { return this.gameObject.GetComponent<Image>(); }
        }

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
                                                if(!Application.isPlaying) { return; }
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
            if(image != null)
            {
                // NOTE(@jackson): Didn't notice any memory leakage with replacing textures.
                // "Should" be fine.
                PresentData();
            }
        }
        #endif
    }
}
