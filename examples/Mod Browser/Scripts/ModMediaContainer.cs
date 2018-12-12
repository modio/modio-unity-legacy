using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO.UI
{
    public class ModMediaContainer : ModMediaCollectionDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public event Action<ModLogoDisplayComponent>            logoClicked;
        public event Action<ModGalleryImageDisplayComponent>    galleryImageClicked;
        public event Action<YouTubeThumbnailDisplayComponent>   youTubeThumbnailClicked;

        [Header("Settings")]
        public GameObject logoPrefab;
        public GameObject youTubeThumbnailPrefab;
        public GameObject galleryImagePrefab;

        [Header("UI Components")]
        public RectTransform container;

        [Header("Display Data")]
        [SerializeField] private ImageDisplayData[] m_data = new ImageDisplayData[0];

        // --- RUNTIME DATA ---
        private LogoImageLocator m_logoLocator = null;
        private IEnumerable<string> m_youTubeURLs = null;
        private IEnumerable<GalleryImageLocator> m_galleryImageLocators = null;

        // --- ACCESSORS ---
        public override IEnumerable<ImageDisplayData> data
        {
            get { return m_data; }
            set
            {
                if(value == null)
                {
                    value = new ImageDisplayData[0];
                }
                m_data = value.ToArray();
            }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            Debug.Assert(container != null);

            Debug.Assert(!(logoPrefab != null && logoPrefab.GetComponent<ModLogoDisplay>() == null),
                         "[mod.io] The logoPrefab needs to have a ModLogoDisplay"
                         + " component attached in order to display correctly.");

            Debug.Assert(!(galleryImagePrefab != null && galleryImagePrefab.GetComponent<ModGalleryImageDisplay>() == null),
                         "[mod.io] The galleryImagePrefab needs to have a ModGalleryImageDisplay"
                         + " component attached in order to display correctly.");

            Debug.Assert(!(youTubeThumbnailPrefab != null && youTubeThumbnailPrefab.GetComponent<YouTubeThumbnailDisplay>() == null),
                         "[mod.io] The youTubeThumbnailPrefab needs to have a YouTubeThumbnailDisplay"
                         + " component attached in order to display correctly.");
        }

        public void OnEnable()
        {
            StartCoroutine(LateUpdateLayouting());
        }

        public System.Collections.IEnumerator LateUpdateLayouting()
        {
            yield return null;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayMedia(ModProfile profile)
        {
            Debug.Assert(profile != null);
            Debug.Assert(profile.media != null);

            DisplayMedia(profile.id, profile.logoLocator, profile.media.youTubeURLs, profile.media.galleryImageLocators);
        }

        public override void DisplayMedia(int modId,
                                          LogoImageLocator logoLocator,
                                          IEnumerable<string> youTubeURLs,
                                          IEnumerable<GalleryImageLocator> galleryImageLocators)
        {
            m_logoLocator = logoLocator;
            m_youTubeURLs = youTubeURLs;
            m_galleryImageLocators = galleryImageLocators;

            foreach(Transform t in container)
            {
                GameObject.Destroy(t.gameObject);
            }

            if(logoLocator != null
               && logoPrefab != null)
            {
                GameObject media_go = GameObject.Instantiate(logoPrefab, container);
                ModLogoDisplay mediaDisplay = media_go.GetComponent<ModLogoDisplay>();
                mediaDisplay.Initialize();
                mediaDisplay.DisplayLogo(modId, logoLocator);
                mediaDisplay.onClick += NotifyLogoClicked;
            }


            if(youTubeURLs != null
               && youTubeThumbnailPrefab != null)
            {
                foreach(string youTubeURL in youTubeURLs)
                {
                    GameObject media_go = GameObject.Instantiate(youTubeThumbnailPrefab, container);
                    YouTubeThumbnailDisplay mediaDisplay = media_go.GetComponent<YouTubeThumbnailDisplay>();
                    mediaDisplay.Initialize();
                    mediaDisplay.DisplayThumbnail(modId, Utility.ExtractYouTubeIdFromURL(youTubeURL));
                    mediaDisplay.onClick += NotifyYouTubeThumbnailClicked;
                }
            }

            if(galleryImageLocators != null
               && galleryImagePrefab != null)
            {
                foreach(GalleryImageLocator imageLocator in galleryImageLocators)
                {
                    GameObject media_go = GameObject.Instantiate(galleryImagePrefab, container);
                    ModGalleryImageDisplay mediaDisplay = media_go.GetComponent<ModGalleryImageDisplay>();
                    mediaDisplay.Initialize();
                    mediaDisplay.DisplayImage(modId, imageLocator);
                    mediaDisplay.onClick += NotifyGalleryImageClicked;
                }
            }

            if(this.isActiveAndEnabled)
            {
                StartCoroutine(LateUpdateLayouting());
            }
        }

        public override void DisplayLoading()
        {
            foreach(Transform t in container)
            {
                GameObject.Destroy(t.gameObject);
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyLogoClicked(ImageDataDisplayComponent display)
        {
            Debug.Assert(display is ModLogoDisplay);
            if(this.logoClicked != null)
            {
                this.logoClicked(display as ModLogoDisplay);
            }
        }

        public void NotifyGalleryImageClicked(ImageDataDisplayComponent display)
        {
            Debug.Assert(display is ModGalleryImageDisplay);
            if(this.galleryImageClicked != null)
            {
                this.galleryImageClicked(display as ModGalleryImageDisplay);
            }
        }

        public void NotifyYouTubeThumbnailClicked(ImageDataDisplayComponent display)
        {
            Debug.Assert(display is YouTubeThumbnailDisplay);
            if(this.youTubeThumbnailClicked != null)
            {
                this.youTubeThumbnailClicked(display as YouTubeThumbnailDisplay);
            }
        }
    }
}
