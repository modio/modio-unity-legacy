using System;
using System.Collections.Generic;
using UnityEngine;
using ModIO;

public class ModMediaCollectionDisplay : MonoBehaviour, IModProfilePresenter
{
    // ---------[ FIELDS ]---------
    public delegate void OnLogoClicked(ModLogoDisplay component,
                                       int modId);
    public delegate void OnYouTubeThumbClicked(YouTubeThumbDisplay component,
                                               int modId, string youTubeVideoId);
    public delegate void OnGalleryImageClicked(ModGalleryImageDisplay component,
                                               int modId, string imageFileName);

    public event OnLogoClicked          logoClicked;
    public event OnYouTubeThumbClicked  youTubeThumbClicked;
    public event OnGalleryImageClicked  galleryImageClicked;

    [Header("Settings")]
    public GameObject logoPrefab;
    public GameObject youTubeThumbnailPrefab;
    public GameObject galleryImagePrefab;

    [Header("UI Components")]
    public RectTransform container;

    [Header("Display Data")]
    [SerializeField] private int m_modId;
    [SerializeField] private LogoImageLocator m_logoLocator;
    [SerializeField] private IEnumerable<string> m_youTubeURLs;
    [SerializeField] private IEnumerable<GalleryImageLocator> m_galleryImageLocators;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        #if DEBUG
        Debug.Assert(container != null);

        if(logoPrefab != null)
        {
            Debug.Assert(logoPrefab.GetComponent<ModLogoDisplay>() != null,
                         "[mod.io] The logoPrefab needs to have a ModLogoDisplay"
                         + " component attached in order to display correctly.");
        }

        if(galleryImagePrefab != null)
        {
            Debug.Assert(galleryImagePrefab.GetComponent<ModGalleryImageDisplay>() != null,
                         "[mod.io] The galleryImagePrefab needs to have a ModGalleryImageDisplay"
                         + " component attached in order to display correctly.");
        }

        if(youTubeThumbnailPrefab != null)
        {
            Debug.Assert(youTubeThumbnailPrefab.GetComponent<YouTubeThumbDisplay>() != null,
                         "[mod.io] The youTubeThumbnailPrefab needs to have a YouTubeThumbDisplay"
                         + " component attached in order to display correctly.");
        }
        #endif
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayProfile(ModProfile profile)
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
        Debug.Assert(modId > 0);

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
                YouTubeThumbDisplay mediaDisplay = media_go.GetComponent<YouTubeThumbDisplay>();
                mediaDisplay.Initialize();
                mediaDisplay.DisplayYouTubeThumbnail(modId, Utility.ExtractYouTubeIdFromURL(youTubeURL));
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
                mediaDisplay.DisplayGalleryImage(modId, imageLocator);
                mediaDisplay.onClick += NotifyGalleryImageClicked;
            }
        }
    }

    public void DisplayLoading()
    {
        foreach(Transform t in container)
        {
            GameObject.Destroy(t.gameObject);
        }
    }

    private void NotifyLogoClicked(ModLogoDisplay component, int modId)
    {
        if(this.logoClicked != null)
        {
            this.logoClicked(component, modId);
        }
    }

    private void NotifyYouTubeThumbnailClicked(YouTubeThumbDisplay component,
                                           int modId, string youTubeVideoId)
    {
        if(this.youTubeThumbClicked != null)
        {
            this.youTubeThumbClicked(component, modId, youTubeVideoId);
        }
    }

    private void NotifyGalleryImageClicked(ModGalleryImageDisplay component,
                                       int modId, string imageFileName)
    {
        if(this.galleryImageClicked != null)
        {
            this.galleryImageClicked(component, modId, imageFileName);
        }
    }
}
