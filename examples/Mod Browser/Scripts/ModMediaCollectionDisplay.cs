using UnityEngine;

using ModIO;

public class ModMediaCollectionDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public GameObject logoPrefab;
    public GameObject galleryImagePrefab;
    public GameObject youTubeThumbnailPrefab;

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

        if(youTubeThumbnailPrefab != null)
        {
            Debug.Assert(youTubeThumbnailPrefab.GetComponent<YouTubeThumbnailDisplay>() != null,
                         "[mod.io] The youTubeThumbnailPrefab needs to have a YouTubeThumbnailDisplay"
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
        }

        if(modId > 0
           && mediaCollection != null)
        {
            if(mediaCollection.youTubeURLs != null
               && youTubeThumbnailPrefab != null)
            {
                foreach(string youTubeURL in mediaCollection.youTubeURLs)
                {
                    GameObject media_go = GameObject.Instantiate(youTubeThumbnailPrefab, container);
                    YouTubeThumbnailDisplay mediaDisplay = media_go.GetComponent<YouTubeThumbnailDisplay>();
                    mediaDisplay.modId = modId;
                    mediaDisplay.youTubeVideoId = Utility.ExtractYouTubeIdFromURL(youTubeURL);
                    mediaDisplay.Initialize();
                    mediaDisplay.UpdateDisplay();
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
                }
            }
        }
    }
}
