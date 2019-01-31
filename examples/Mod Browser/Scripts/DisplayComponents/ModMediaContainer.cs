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
        public GameObject galleryImagePrefab;
        public GameObject youTubeThumbnailPrefab;

        [Header("UI Components")]
        public RectTransform container;

        [Header("Display Data")]
        private ImageDataDisplayComponent m_logoDisplay = null;
        private List<ImageDataDisplayComponent> m_galleryDisplays = new List<ImageDataDisplayComponent>();
        private List<ImageDataDisplayComponent> m_youTubeDisplays = new List<ImageDataDisplayComponent>();

        // --- ACCESSORS ---
        [Obsolete]
        public IEnumerable<ImageDataDisplayComponent> imageDisplays { get { return allDisplays; } }

        public ImageDataDisplayComponent logoDisplay
        { get { return m_logoDisplay; } }
        public IEnumerable<ImageDataDisplayComponent> youTubeDisplays
        { get { return m_youTubeDisplays; } }
        public IEnumerable<ImageDataDisplayComponent> galleryDisplays
        { get { return m_galleryDisplays; } }

        public IEnumerable<ImageDataDisplayComponent> allDisplays
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
                foreach(ImageDataDisplayComponent display in imageDisplays)
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
                ModLogoDisplay display = InstantiatePrefab(logoPrefab) as ModLogoDisplay;
                display.DisplayLogo(modId, logoLocator);
                display.onClick += NotifyLogoClicked;

                this.m_logoDisplay = display;
            }

            if(youTubeURLs != null
               && youTubeThumbnailPrefab != null)
            {
                foreach(string url in youTubeURLs)
                {
                    YouTubeThumbnailDisplay display = InstantiatePrefab(youTubeThumbnailPrefab) as YouTubeThumbnailDisplay;
                    display.DisplayThumbnail(modId, Utility.ExtractYouTubeIdFromURL(url));
                    display.onClick += NotifyYouTubeThumbnailClicked;

                    m_youTubeDisplays.Add(display);
                }
            }

            if(galleryImageLocators != null
               && galleryImagePrefab != null)
            {
                foreach(GalleryImageLocator locator in galleryImageLocators)
                {
                    ModGalleryImageDisplay display = InstantiatePrefab(galleryImagePrefab) as ModGalleryImageDisplay;
                    display.DisplayImage(modId, locator);
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
                ModLogoDisplay display = InstantiatePrefab(logoPrefab) as ModLogoDisplay;
                display.data = logoData;
                display.onClick += NotifyLogoClicked;

                this.m_logoDisplay = display;
            }
            if(youTubeThumbnailPrefab != null)
            {
                foreach(var imageData in youTubeThumbData)
                {
                    YouTubeThumbnailDisplay display = InstantiatePrefab(youTubeThumbnailPrefab) as YouTubeThumbnailDisplay;
                    display.data = imageData;
                    display.onClick += NotifyYouTubeThumbnailClicked;

                    this.m_youTubeDisplays.Add(display);
                }
            }
            if(galleryImagePrefab != null)
            {
                foreach(var imageData in galleryImageData)
                {
                    ModGalleryImageDisplay display = InstantiatePrefab(galleryImagePrefab) as ModGalleryImageDisplay;
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
                foreach(ImageDataDisplayComponent display in allDisplays)
                {
                    GameObject.DestroyImmediate(display.gameObject);
                }
            }
            else
            #endif
            {
                foreach(ImageDataDisplayComponent display in allDisplays)
                {
                    GameObject.Destroy(display.gameObject);
                }
            }

            this.m_logoDisplay = null;
            this.m_youTubeDisplays.Clear();
            this.m_galleryDisplays.Clear();
        }

        private ImageDataDisplayComponent InstantiatePrefab(GameObject imagePrefab)
        {
            Debug.Assert(imagePrefab != null);

            GameObject media_go = GameObject.Instantiate(imagePrefab, container);
            ImageDataDisplayComponent mediaDisplay = media_go.GetComponent<ImageDataDisplayComponent>();
            mediaDisplay.Initialize();

            return mediaDisplay;
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
