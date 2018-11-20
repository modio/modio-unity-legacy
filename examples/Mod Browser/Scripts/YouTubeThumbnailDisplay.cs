using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class YouTubeThumbnailDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action<YouTubeThumbnailDisplay> onClick;

    [Header("Settings")]
    public GameObject loadingPrefab;

    [Header("UI Components")]
    public Image thumbnailImage;

    [Header("Display Data")]
    public int modId;
    public string youTubeVideoId;

    [Header("Runtime Data")]
    public GameObject loadingInstance;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(thumbnailImage != null);

        if(loadingPrefab != null)
        {
            loadingInstance = GameObject.Instantiate(loadingPrefab, thumbnailImage.transform);

            RectTransform instance_rt = loadingInstance.transform as RectTransform;
            instance_rt.anchorMin = new Vector2(0f, 0f);
            instance_rt.anchorMax = new Vector2(1f, 1f);
            instance_rt.offsetMin = Vector2.zero;
            instance_rt.offsetMax = Vector2.zero;

            loadingInstance.gameObject.SetActive(false);
        }
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void UpdateDisplay()
    {
        Debug.Assert(modId > 0,
                     "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(!String.IsNullOrEmpty(youTubeVideoId),
                     "[mod.io] youTubeVideoId needs to be set to a valid YouTube video id.");

        if(loadingInstance != null)
        {
            loadingInstance.gameObject.SetActive(true);
        }

        string vidId = youTubeVideoId;
        ModManager.GetModYouTubeThumbnail(modId, vidId,
                                          (t) => OnGetThumbnail(vidId, t),
                                          WebRequestError.LogAsWarning);
    }

    private void OnGetThumbnail(string id, Texture2D texture)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        if(id != this.youTubeVideoId
           || this.thumbnailImage == null)
        {
            return;
        }

        if(loadingInstance != null)
        {
            loadingInstance.gameObject.SetActive(false);
        }

        thumbnailImage.sprite = ModBrowser.CreateSpriteFromTexture(texture);
    }

    // public void OpenYouTubeVideoURL()
    // {
    //     Application.OpenURL(@"https://youtu.be/" + youTubeVideoId);
    // }

    public void NotifyClicked()
    {
        if(this.onClick != null)
        {
            this.onClick(this);
        }
    }
}
