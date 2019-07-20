using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LayoutRebuilder = UnityEngine.UI.LayoutRebuilder;

namespace ModIO.UI
{
    [Obsolete("Use ModLogoDisplay, GalleryImageContainer, and YouTubeThumbnailContainer instead.")]
    public class ModMediaContainer : ModMediaCollectionDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public event Action<ImageDisplay>  logoClicked;
        public event Action<ImageDisplay>  galleryImageClicked;
        public event Action<ImageDisplay>  youTubeThumbnailClicked;

        [Header("Settings")]
        public GameObject logoPrefab;
        public GameObject galleryImagePrefab;
        public GameObject youTubeThumbnailPrefab;

        [Header("UI Components")]
        public RectTransform container;

        [Header("Display Data")]
        private ImageDisplay m_logoDisplay = null;
        private List<ImageDisplay> m_galleryDisplays = new List<ImageDisplay>();
        private List<ImageDisplay> m_youTubeDisplays = new List<ImageDisplay>();

        private ImageDisplayData m_logoData = default(ImageDisplayData);
        private ImageDisplayData[] m_youTubeData = new ImageDisplayData[0];
        private ImageDisplayData[] m_galleryData = new ImageDisplayData[0];

        // --- ACCESSORS ---
        public ImageDisplay logoDisplay
        { get { return m_logoDisplay; } }
        public IEnumerable<ImageDisplay> youTubeDisplays
        { get { return m_youTubeDisplays; } }
        public IEnumerable<ImageDisplay> galleryDisplays
        { get { return m_galleryDisplays; } }

        public IEnumerable<ImageDisplay> allDisplays
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

        public override ImageDisplayData logoData
        {
            get
            {
                if(m_logoDisplay == null)
                {
                    return m_logoData;
                }
                else
                {
                    return m_logoDisplay.data;
                }
            }
            set
            {
                if(!logoData.Equals(value))
                {
                    m_logoData = value;
                    PresentLogoData();
                }
            }
        }

        public override IEnumerable<ImageDisplayData> youTubeData
        {
            get
            {
                if(youTubeThumbnailPrefab == null)
                {
                    foreach(var data in m_youTubeData)
                    {
                        yield return data;
                    }
                }
                else
                {
                    foreach(var display in m_youTubeDisplays)
                    {
                        yield return display.data;
                    }
                }
            }
            set
            {
                if(!youTubeData.Equals(value))
                {
                    m_youTubeData = value.ToArray();
                    PresentYouTubeData();
                }
            }
        }

        public override IEnumerable<ImageDisplayData> galleryData
        {
            get
            {
                if(galleryImagePrefab == null)
                {
                    foreach(var data in m_galleryData)
                    {
                        yield return data;
                    }
                }
                else
                {
                    foreach(var display in m_galleryDisplays)
                    {
                        yield return display.data;
                    }
                }
            }
            set
            {
                if(!galleryData.Equals(value))
                {
                    m_galleryData = value.ToArray();
                    PresentGalleryData();
                }
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
            StartCoroutine(EndOfFrameUpdateCoroutine());
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

            if(Application.isPlaying)
            {
                LateLayoutUpdate();
            }
        }

        public override void DisplayLoading()
        {
            ClearDisplays();

            if(Application.isPlaying)
            {
                LateLayoutUpdate();
            }
        }

        // ---------[ DATA PRESENTATION ]---------
        private void PresentLogoData()
        {
            if(m_logoData.descriptor == ImageDescriptor.None)
            {
                if(m_logoDisplay != null)
                {
                    #if UNITY_EDITOR
                    if(!Application.isPlaying)
                    {
                        GameObject.DestroyImmediate(m_logoDisplay.gameObject);
                    }
                    else
                    #endif
                    {
                        GameObject.Destroy(m_logoDisplay.gameObject);
                    }
                }
            }
            else if(logoPrefab != null)
            {
                if(logoDisplay == null)
                {
                    m_logoDisplay = InstantiatePrefab(logoPrefab);
                    m_logoDisplay.transform.SetSiblingIndex(0);
                    m_logoDisplay.onClick += NotifyLogoClicked;
                }

                m_logoDisplay.data = m_logoData;
            }

            if(Application.isPlaying)
            {
                LateLayoutUpdate();
            }
        }

        private void PresentYouTubeData()
        {
            #if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                foreach(ImageDisplay display in m_youTubeDisplays)
                {
                    GameObject.DestroyImmediate(display.gameObject);
                }
            }
            else
            #endif
            {
                foreach(ImageDisplay display in m_youTubeDisplays)
                {
                    GameObject.Destroy(display.gameObject);
                }
            }
            m_youTubeDisplays.Clear();

            int siblingIndex = 0;
            if(logoDisplay != null)
            {
                ++siblingIndex;
            }

            if(youTubeThumbnailPrefab != null)
            {
                foreach(var imageData in m_youTubeData)
                {
                    var display = InstantiatePrefab(youTubeThumbnailPrefab);
                    display.data = imageData;
                    display.transform.SetSiblingIndex(siblingIndex);
                    display.onClick += NotifyYouTubeThumbnailClicked;

                    this.m_youTubeDisplays.Add(display);

                    ++siblingIndex;
                }
            }

            if(Application.isPlaying)
            {
                LateLayoutUpdate();
            }
        }

        private void PresentGalleryData()
        {
            #if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                foreach(ImageDisplay display in m_galleryDisplays)
                {
                    GameObject.DestroyImmediate(display.gameObject);
                }
            }
            else
            #endif
            {
                foreach(ImageDisplay display in m_galleryDisplays)
                {
                    GameObject.Destroy(display.gameObject);
                }
            }
            m_galleryDisplays.Clear();

            if(galleryImagePrefab != null)
            {
                foreach(var imageData in m_galleryData)
                {
                    var display = InstantiatePrefab(galleryImagePrefab);
                    display.data = imageData;
                    display.onClick += NotifyGalleryImageClicked;

                    this.m_galleryDisplays.Add(display);
                }
            }

            if(Application.isPlaying)
            {
                LateLayoutUpdate();
            }
        }

        private void ClearDisplays()
        {
            #if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                foreach(ImageDisplay display in allDisplays)
                {
                    GameObject.DestroyImmediate(display.gameObject);
                }
            }
            else
            #endif
            {
                foreach(ImageDisplay display in allDisplays)
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

        private void LateLayoutUpdate()
        {
            if(this.isActiveAndEnabled)
            {
                StartCoroutine(EndOfFrameUpdateCoroutine());
            }
            else
            {
                LayoutRebuilder.MarkLayoutForRebuild(container);
            }
        }

        private System.Collections.IEnumerator EndOfFrameUpdateCoroutine()
        {
            yield return null;
            LayoutRebuilder.MarkLayoutForRebuild(container);
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyLogoClicked(ImageDisplay display)
        {
            if(this.logoClicked != null)
            {
                this.logoClicked(display);
            }
        }

        public void NotifyYouTubeThumbnailClicked(ImageDisplay display)
        {
            if(this.youTubeThumbnailClicked != null)
            {
                this.youTubeThumbnailClicked(display);
            }
        }

        public void NotifyGalleryImageClicked(ImageDisplay display)
        {
            if(this.galleryImageClicked != null)
            {
                this.galleryImageClicked(display);
            }
        }
    }
}
