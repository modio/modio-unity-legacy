using UnityEngine;

using ModIO;

public class ModMediaCollectionDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    // public GameObject galleryImagePrefab;
    public GameObject youTubeThumbnailPrefab;

    [Header("UI Components")]
    public RectTransform container;

    [Header("Display Data")]
    public int modId;
    public ModMediaCollection mediaCollection;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        #if DEBUG
        Debug.Assert(container != null);

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

        if(modId > 0
           && mediaCollection != null)
        {
            if(youTubeThumbnailPrefab != null)
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
        }
    }
}
