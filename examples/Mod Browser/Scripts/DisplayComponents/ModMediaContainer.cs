using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO.UI
{
    public class ModMediaContainer : ModMediaCollectionDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public event Action<ImageDisplayComponent>  logoClicked;
        public event Action<ImageDisplayComponent>  galleryImageClicked;
        public event Action<ImageDisplayComponent>  youTubeThumbnailClicked;

        [Header("Settings")]
        public GameObject logoPrefab;
        public GameObject galleryImagePrefab;
        public GameObject youTubeThumbnailPrefab;

        [Header("UI Components")]
        public RectTransform container;

        [Header("Display Data")]
        private ImageDisplayComponent m_logoDisplay = null;
        private List<ImageDisplayComponent> m_galleryDisplays = new List<ImageDisplayComponent>();
        private List<ImageDisplayComponent> m_youTubeDisplays = new List<ImageDisplayComponent>();

        // --- ACCESSORS ---
        [Obsolete]
        public IEnumerable<ImageDisplayComponent> imageDisplays { get { return allDisplays; } }

        public ImageDisplayComponent logoDisplay
        { get { return m_logoDisplay; } }
        public IEnumerable<ImageDisplayComponent> youTubeDisplays
        { get { return m_youTubeDisplays; } }
        public IEnumerable<ImageDisplayComponent> galleryDisplays
        { get { return m_galleryDisplays; } }

        public IEnumerable<ImageDisplayComponent> allDisplays
        {
            get
            {
                if(this.m_logoDisplay != null)
                {
                    yield return this.m_logoDisplay;
                }
                foreach(var displayComponent in this.m_youTubeDisplays)
                {
                    yield return displayComponent;
                }
                foreach(var displayComponent in this.m_galleryDisplays)
                {
                    yield return displayComponent;
                }
            }
        }

        [Obsolete]
        public override IEnumerable<ImageDisplayData> data
        {
            get
            {
                foreach(ImageDisplayComponent display in imageDisplays)
                {
                    yield return display.data;
                }
            }
            set
            {
                if(value == null)
                {
                    value = new ImageDisplayData[0];
                }

                DisplayData(value);
            }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            Debug.Assert(container != null);

            Debug.Assert(!(logoPrefab != null && logoPrefab.GetComponent<ImageDisplay>() == null),
                         "[mod.io] The logoPrefab needs to have a ImageDisplay"
                         + " component attached in order to display correctly.");

            Debug.Assert(!(galleryImagePrefab != null && galleryImagePrefab.GetComponent<ImageDisplay>() == null),
                         "[mod.io] The galleryImagePrefab needs to have a ImageDisplay"
                         + " component attached in order to display correctly.");

            Debug.Assert(!(youTubeThumbnailPrefab != null && youTubeThumbnailPrefab.GetComponent<ImageDisplay>() == null),
                         "[mod.io] The youTubeThumbnailPrefab needs to have a ImageDisplay"
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

            DisplayMedia(profile.id,
                         profile.logoLocator,
                         profile.media.galleryImageLocators,
                         profile.media.youTubeURLs);
        }

        public override void DisplayMedia(int modId,
                                          LogoImageLocator logoLocator,
                                          IEnumerable<GalleryImageLocator> galleryImageLocators,
                                          IEnumerable<string> youTubeURLs)
        {
            ClearDisplays();

            if(logoLocator != null
               && logoPrefab != null)
            {
                var display = InstantiatePrefab(logoPrefab);
                display.DisplayLogo(modId, logoLocator);
                display.onClick += NotifyLogoClicked;

                this.m_logoDisplay = display;
            }

            if(youTubeURLs != null
               && youTubeThumbnailPrefab != null)
            {
                foreach(string url in youTubeURLs)
                {
                    var display = InstantiatePrefab(youTubeThumbnailPrefab);
                    display.DisplayYouTubeThumbnail(modId, Utility.ExtractYouTubeIdFromURL(url));
                    display.onClick += NotifyYouTubeThumbnailClicked;

                    m_youTubeDisplays.Add(display);
                }
            }

            if(galleryImageLocators != null
               && galleryImagePrefab != null)
            {
                foreach(GalleryImageLocator locator in galleryImageLocators)
                {
                    var display = InstantiatePrefab(galleryImagePrefab);
                    display.DisplayGalleryImage(modId, locator);
                    display.onClick += NotifyGalleryImageClicked;

                    m_galleryDisplays.Add(display);
                }
            }

            if(Application.isPlaying && this.isActiveAndEnabled)
            {
                StartCoroutine(LateUpdateLayouting());
            }
        }

        public override void DisplayLoading()
        {
            ClearDisplays();
        }

        // ---------[ PRIVATE METHODS ]---------
        [Obsolete]
        private void DisplayData(IEnumerable<ImageDisplayData> displayData)
        {
            return;
        }

        private void DisplayData(ImageDisplayData logoData,
                                 IEnumerable<ImageDisplayData> youTubeThumbData,
                                 IEnumerable<ImageDisplayData> galleryImageData)
        {
            ClearDisplays();

            // create
            if(logoData.mediaType != ImageDisplayData.MediaType.None
               && logoPrefab != null)
            {
                var display = InstantiatePrefab(logoPrefab);
                display.data = logoData;
                display.onClick += NotifyLogoClicked;

                this.m_logoDisplay = display;
            }
            if(youTubeThumbnailPrefab != null)
            {
                foreach(var imageData in youTubeThumbData)
                {
                    var display = InstantiatePrefab(youTubeThumbnailPrefab);
                    display.data = imageData;
                    display.onClick += NotifyYouTubeThumbnailClicked;

                    this.m_youTubeDisplays.Add(display);
                }
            }
            if(galleryImagePrefab != null)
            {
                foreach(var imageData in galleryImageData)
                {
                    var display = InstantiatePrefab(galleryImagePrefab);
                    display.data = imageData;
                    display.onClick += NotifyGalleryImageClicked;

                    this.m_galleryDisplays.Add(display);
                }
            }

            if(Application.isPlaying && this.isActiveAndEnabled)
            {
                StartCoroutine(LateUpdateLayouting());
            }
        }

        private void ClearDisplays()
        {
            #if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                foreach(ImageDisplayComponent display in allDisplays)
                {
                    GameObject.DestroyImmediate(display.gameObject);
                }
            }
            else
            #endif
            {
                foreach(ImageDisplayComponent display in allDisplays)
                {
                    GameObject.Destroy(display.gameObject);
                }
            }

            this.m_logoDisplay = null;
            this.m_youTubeDisplays.Clear();
            this.m_galleryDisplays.Clear();
        }

        private ImageDisplay InstantiatePrefab(GameObject imagePrefab)
        {
            Debug.Assert(imagePrefab != null);

            GameObject media_go = GameObject.Instantiate(imagePrefab, container);
            ImageDisplay mediaDisplay = media_go.GetComponent<ImageDisplay>();
            mediaDisplay.Initialize();

            return mediaDisplay;
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyLogoClicked(ImageDisplayComponent display)
        {
            if(this.logoClicked != null)
            {
                this.logoClicked(display);
            }
        }

        public void NotifyGalleryImageClicked(ImageDisplayComponent display)
        {
            if(this.galleryImageClicked != null)
            {
                this.galleryImageClicked(display);
            }
        }

        public void NotifyYouTubeThumbnailClicked(ImageDisplayComponent display)
        {
            if(this.youTubeThumbnailClicked != null)
            {
                this.youTubeThumbnailClicked(display);
            }
        }
    }
}
