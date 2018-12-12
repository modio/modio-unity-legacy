using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class YouTubeThumbnailDisplay : YouTubeThumbnailDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<YouTubeThumbnailDisplayComponent> onClick;

        [Header("UI Components")]
        public Image image;
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField] private ImageDisplayData m_data;

        // --- ACCESSORS ---
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
        public override void DisplayThumbnail(int modId, string youTubeVideoId)
        {
            Debug.Assert(!String.IsNullOrEmpty(youTubeVideoId),
                         "[mod.io] youTubeVideoId needs to be set to a valid YouTube video id.");


            ImageDisplayData imageData = new ImageDisplayData()
            {
                modId = modId,
                mediaType = ImageDisplayData.MediaType.ModYouTubeThumbnail,
                youTubeId = youTubeVideoId,
                texture = null,
            };

            DisplayInternal(imageData);
        }

        // NOTE(@jackson): Called internally, this is only used when displayData.texture == null
        private void DisplayInternal(ImageDisplayData displayData)
        {
            Debug.Assert(displayData.texture == null);

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

        // ---------[ UTILITIES ]---------
        public void OpenYouTubeVideoURL()
        {
            UIUtilities.OpenYouTubeVideoURL(data.youTubeId);
        }
    }
}
