using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModLogoDisplay : ModLogoDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ImageDataDisplayComponent> onClick;

        [Header("Settings")]
        [SerializeField] private LogoSize m_logoSize;

        [Header("UI Components")]
        public Image image;
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField] private ImageDisplayData m_data;

        // --- ACCESSORS ---
        public override LogoSize logoSize
        {
            get { return m_logoSize; }
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
            Debug.Assert(image != null);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayLogo(int modId, LogoImageLocator locator)
        {
            Debug.Assert(locator != null);

            ImageDisplayData logoData = new ImageDisplayData()
            {
                modId = modId,
                mediaType = ImageDisplayData.MediaType.ModLogo,
                fileName = locator.fileName,
                texture = null,
            };

            DisplayInternal(logoData, locator);
        }

        // NOTE(@jackson): Called internally, this is only used when displayData.texture == null
        private void DisplayInternal(ImageDisplayData displayData, LogoImageLocator locator)
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

                ModManager.GetModLogo(displayData.modId,
                                      locator,
                                      logoSize,
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
