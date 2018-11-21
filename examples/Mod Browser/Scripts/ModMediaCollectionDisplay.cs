using System;
using System.Collections.Generic;
using UnityEngine;
using ModIO;

public delegate void YouTubeIdReceiver(string youTubeVideoId);
public delegate void GalleryImageFileNameReceiver(string imageFileName);

public class ModMediaCollectionDisplay : MonoBehaviour, IModProfilePresenter
{
    // ---------[ FIELDS ]---------
    public event Action logoClicked;
    public event YouTubeIdReceiver youTubeThumbnailClicked;
    public event GalleryImageFileNameReceiver galleryImageClicked;

    [Header("Settings")]
    public GameObject logoPrefab;
    public GameObject youTubeThumbnailPrefab;
    public GameObject galleryImagePrefab;

    [Header("UI Components")]
    public RectTransform container;

    [Header("Display Data")]
    #pragma warning disable 0414
    [SerializeField] private int m_modId;
    [SerializeField] private LogoImageLocator m_logoLocator;
    [SerializeField] private IEnumerable<string> m_youTubeURLs;
    [SerializeField] private IEnumerable<GalleryImageLocator> m_galleryImageLocators;
    #pragma warning restore 0414

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
            Debug.Assert(youTubeThumbnailPrefab.GetComponent<YouTubeThumbnailDisplay>() != null,
                         "[mod.io] The youTubeThumbnailPrefab needs to have a YouTubeThumbnailDisplay"
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
            mediaDisplay.onClick += OnLogoClicked;
        }


        if(youTubeURLs != null
           && youTubeThumbnailPrefab != null)
        {
            foreach(string youTubeURL in youTubeURLs)
            {
                GameObject media_go = GameObject.Instantiate(youTubeThumbnailPrefab, container);
                YouTubeThumbnailDisplay mediaDisplay = media_go.GetComponent<YouTubeThumbnailDisplay>();
                mediaDisplay.Initialize();
                mediaDisplay.DisplayYouTubeThumbnail(modId, Utility.ExtractYouTubeIdFromURL(youTubeURL));
                mediaDisplay.onClick += OnYouTubeThumbnailClicked;
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
                mediaDisplay.onClick += OnGalleryImageClicked;
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

    private void OnLogoClicked(ModLogoDisplay component, int modId)
    {
        Debug.Log("CLICKED");
        if(this.logoClicked != null)
        {
            this.logoClicked();
        }
    }

    private void OnYouTubeThumbnailClicked(YouTubeThumbnailDisplay component,
                                           int modId, string youTubeVideoId)
    {
        Debug.Log("CLICKED");
        if(this.youTubeThumbnailClicked != null)
        {
            this.youTubeThumbnailClicked(youTubeVideoId);
        }
    }

    private void OnGalleryImageClicked(ModGalleryImageDisplay component,
                                       int modId, string imageFileName)
    {
        Debug.Log("CLICKED");
        if(this.galleryImageClicked != null)
        {
            this.galleryImageClicked(imageFileName);
        }
    }
}
