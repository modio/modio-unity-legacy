using System;
using UnityEngine;
using ModIO;

public delegate void YouTubeIdReceiver(string youTubeVideoId);
public delegate void GalleryImageFileNameReceiver(string imageFileName);

public class ModMediaCollectionDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action logoClicked;
    public event YouTubeIdReceiver youTubePreviewClicked;
    public event GalleryImageFileNameReceiver galleryImageClicked;

    [Header("Settings")]
    public GameObject logoPrefab;
    public GameObject galleryImagePrefab;
    public GameObject youTubePreviewPrefab;

    [Header("UI Components")]
    public RectTransform container;

    [Header("Display Data")]
    public int modId;
    public LogoImageLocator logoLocator;
    public ModMediaCollection mediaCollection;

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

        if(youTubePreviewPrefab != null)
        {
            Debug.Assert(youTubePreviewPrefab.GetComponent<YouTubeThumbnailDisplay>() != null,
                         "[mod.io] The youTubePreviewPrefab needs to have a YouTubeThumbnailDisplay"
                         + " component attached in order to display correctly.");
        }
        #endif
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void UpdateDisplay()
    {
        foreach(Transform t in container)
        {
            GameObject.Destroy(t.gameObject);
        }

        // logo
        if(modId > 0 && logoLocator != null)
        {
            GameObject media_go = GameObject.Instantiate(logoPrefab, container);
            ModLogoDisplay mediaDisplay = media_go.GetComponent<ModLogoDisplay>();
            mediaDisplay.modId = modId;
            mediaDisplay.logoLocator = logoLocator;
            mediaDisplay.Initialize();
            mediaDisplay.UpdateDisplay();
            mediaDisplay.onClick += OnLogoClicked;
        }

        if(modId > 0
           && mediaCollection != null)
        {
            if(mediaCollection.youTubeURLs != null
               && youTubePreviewPrefab != null)
            {
                foreach(string youTubeURL in mediaCollection.youTubeURLs)
                {
                    GameObject media_go = GameObject.Instantiate(youTubePreviewPrefab, container);
                    YouTubeThumbnailDisplay mediaDisplay = media_go.GetComponent<YouTubeThumbnailDisplay>();
                    mediaDisplay.modId = modId;
                    mediaDisplay.youTubeVideoId = Utility.ExtractYouTubeIdFromURL(youTubeURL);
                    mediaDisplay.Initialize();
                    mediaDisplay.UpdateDisplay();
                    mediaDisplay.onClick += OnYouTubePreviewClicked;
                }
            }

            if(mediaCollection.galleryImageLocators != null
               && mediaCollection.galleryImageLocators.Length > 0)
            {
                foreach(GalleryImageLocator imageLocator in mediaCollection.galleryImageLocators)
                {
                    GameObject media_go = GameObject.Instantiate(galleryImagePrefab, container);
                    ModGalleryImageDisplay mediaDisplay = media_go.GetComponent<ModGalleryImageDisplay>();
                    mediaDisplay.modId = modId;
                    mediaDisplay.imageLocator = imageLocator;
                    mediaDisplay.Initialize();
                    mediaDisplay.UpdateDisplay();
                    mediaDisplay.onClick += OnGalleryImageClicked;
                }
            }
        }
    }

    private void OnLogoClicked(ModLogoDisplay display)
    {
        Debug.Log("CLICKED");
        if(this.logoClicked != null)
        {
            this.logoClicked();
        }
    }

    private void OnYouTubePreviewClicked(YouTubeThumbnailDisplay display)
    {
        Debug.Log("CLICKED");
        if(this.youTubePreviewClicked != null)
        {
            this.youTubePreviewClicked(display.youTubeVideoId);
        }
    }

    private void OnGalleryImageClicked(ModGalleryImageDisplay display)
    {
        Debug.Log("CLICKED");
        if(this.galleryImageClicked != null)
        {
            this.galleryImageClicked(display.imageLocator.fileName);
        }
    }
}
