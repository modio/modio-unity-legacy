using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    public class ModMediaCollectionContainer : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public delegate void OnLogoClicked(ModLogoDisplayComponent display);
        public delegate void OnGalleryImageClicked(ModGalleryImageDisplayComponent display);
        public delegate void OnYouTubeThumbClicked(YouTubeThumbnailDisplayComponent display);

        public event OnLogoClicked          logoClicked;
        public event OnYouTubeThumbClicked  youTubeThumbClicked;
        public event OnGalleryImageClicked  galleryImageClicked;

        [Header("Settings")]
        public GameObject logoPrefab;
        public GameObject youTubeThumbnailPrefab;
        public GameObject galleryImagePrefab;

        [Header("UI Components")]
        public RectTransform container;

        // --- RUNTIME DATA ---
        private int m_modId = -1;
        private LogoImageLocator m_logoLocator = null;
        private IEnumerable<string> m_youTubeURLs = null;
        private IEnumerable<GalleryImageLocator> m_galleryImageLocators = null;

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
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
        public void DisplayProfileMedia(ModProfile profile)
        {
            Debug.Assert(profile != null);
            Debug.Assert(profile.media != null);

            DisplayProfileMedia(profile.id, profile.logoLocator, profile.media.youTubeURLs, profile.media.galleryImageLocators);
        }

        public void DisplayProfileMedia(int modId,
                                        LogoImageLocator logoLocator,
                                        IEnumerable<string> youTubeURLs,
                                        IEnumerable<GalleryImageLocator> galleryImageLocators)
        {
            Debug.Assert(modId > 0, "[mod.io] modId needs to be set to a valid mod profile id.");

            m_modId = modId;
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

        public void DisplayLoading(int modId = -1)
        {
            m_modId = modId;

            foreach(Transform t in container)
            {
                GameObject.Destroy(t.gameObject);
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyLogoClicked(ModLogoDisplayComponent display)
        {
            if(this.logoClicked != null)
            {
                this.logoClicked(display);
            }
        }

        public void NotifyGalleryImageClicked(ModGalleryImageDisplayComponent display)
        {
            if(this.galleryImageClicked != null)
            {
                this.galleryImageClicked(display);
            }
        }

        public void NotifyYouTubeThumbnailClicked(YouTubeThumbnailDisplayComponent display)
        {
            if(this.youTubeThumbClicked != null)
            {
                this.youTubeThumbClicked(display);
            }
        }
    }
}
