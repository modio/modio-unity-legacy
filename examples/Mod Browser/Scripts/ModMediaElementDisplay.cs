using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModMediaElementDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public delegate void OnLogoClicked(ModMediaElementDisplay component,
                                       int modId);
    public delegate void OnYouTubeThumbClicked(ModMediaElementDisplay component,
                                               int modId, string youTubeVideoId);
    public delegate void OnGalleryImageClicked(ModMediaElementDisplay component,
                                               int modId, string imageFileName);

    public event OnLogoClicked          logoClicked;
    public event OnYouTubeThumbClicked  youTubeThumbClicked;
    public event OnGalleryImageClicked  galleryImageClicked;

    [Header("Settings")]
    public LogoSize logoSize;
    public ModGalleryImageSize galleryImageSize;

    [Header("UI Components")]
    public Image image;
    public GameObject loadingDisplay;
    public GameObject logoOverlay;
    public GameObject youTubeOverlay;
    public GameObject galleryImageOverlay;

    [Header("Display Data")]
    [SerializeField] private int m_modId = -1;
    [SerializeField] private string m_mediaId = string.Empty;
    private Action m_clickNotifier = () => {};

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(image != null);
        if(logoOverlay != null)
        {
            logoOverlay.SetActive(false);
        }
        if(youTubeOverlay != null)
        {
            youTubeOverlay.SetActive(false);
        }
        if(galleryImageOverlay != null)
        {
            galleryImageOverlay.SetActive(false);
        }
    }

    // ---------[ UI FUNCTIONALITY ]---------
    // --- LOGO ---
    public void DisplayLogo(int modId, LogoImageLocator logoLocator)
    {
        Debug.Assert(modId > 0,
                     "[mod.io] modId needs to be set to a valid mod profile id.");
        Debug.Assert(logoLocator != null,
                     "[mod.io] logoLocator needs to be set and have a fileName.");

        DisplayLoading();

        m_modId = modId;
        m_mediaId = logoLocator.fileName;
        m_clickNotifier = NotifyLogoClicked;

        ModManager.GetModLogo(modId, logoLocator, logoSize,
                              (t) => LoadTexture(t, modId, logoLocator.fileName, logoOverlay),
                              WebRequestError.LogAsWarning);
    }

    public void DisplayLogoTexture(int modId, Texture2D texture)
    {
        Debug.Assert(modId > 0, "[mod.io] modId needs to be set to a valid mod profile id.");
        Debug.Assert(texture != null);

        m_modId = modId;
        m_mediaId = "_LOGO_";
        m_clickNotifier = NotifyLogoClicked;

        if(youTubeOverlay != null)
        {
            youTubeOverlay.SetActive(false);
        }
        if(galleryImageOverlay != null)
        {
            galleryImageOverlay.SetActive(false);
        }

        LoadTexture(texture, modId, "_LOGO_", logoOverlay);
    }

    // --- YOUTUBE ---
    public void DisplayYouTubeThumb(int modId, string youTubeVideoId)
    {
        Debug.Assert(modId > 0,
                     "[mod.io] modId needs to be set to a valid mod profile id.");
        Debug.Assert(!String.IsNullOrEmpty(youTubeVideoId),
                     "[mod.io] youTubeVideoId needs to be set to a valid YouTube video id.");

        DisplayLoading();

        m_modId = modId;
        m_mediaId = youTubeVideoId;
        m_clickNotifier = NotifyYouTubeClicked;

        ModManager.GetModYouTubeThumbnail(modId, youTubeVideoId,
                                          (t) => LoadTexture(t, modId, youTubeVideoId, youTubeOverlay),
                                          WebRequestError.LogAsWarning);
    }

    public void DisplayYouTubeThumbTexture(int modId, string youTubeVideoId, Texture2D texture)
    {
        Debug.Assert(modId > 0, "[mod.io] modId needs to be set to a valid mod profile id.");
        Debug.Assert(!String.IsNullOrEmpty(youTubeVideoId),
                     "[mod.io] youTubeVideoId needs to be set to a valid YouTube video id.");
        Debug.Assert(texture != null);

        m_modId = modId;
        m_mediaId = youTubeVideoId;
        m_clickNotifier = NotifyYouTubeClicked;

        if(logoOverlay != null)
        {
            logoOverlay.SetActive(false);
        }
        if(galleryImageOverlay != null)
        {
            galleryImageOverlay.SetActive(false);
        }

        LoadTexture(texture, modId, youTubeVideoId, youTubeOverlay);
    }

    // --- GALLERY ---
    public void DisplayGalleryImage(int modId, GalleryImageLocator imageLocator)
    {
        Debug.Assert(modId > 0,
                     "[mod.io] modId needs to be set to a valid mod profile id.");
        Debug.Assert(imageLocator != null && !String.IsNullOrEmpty(imageLocator.fileName),
                     "[mod.io] imageLocator needs to be set and have a fileName.");

        DisplayLoading();

        m_modId = modId;
        m_mediaId = imageLocator.fileName;
        m_clickNotifier = NotifyImageClicked;

        ModManager.GetModGalleryImage(modId, imageLocator, galleryImageSize,
                                      (t) => LoadTexture(t, modId, imageLocator.fileName, galleryImageOverlay),
                                      WebRequestError.LogAsWarning);
    }

    public void DisplayGalleryImageTexture(int modId, string imageFileName, Texture2D texture)
    {
        Debug.Assert(modId > 0, "[mod.io] modId needs to be set to a valid mod profile id.");
        Debug.Assert(!String.IsNullOrEmpty(imageFileName));
        Debug.Assert(texture != null);

        m_modId = modId;
        m_mediaId = imageFileName;
        m_clickNotifier = NotifyImageClicked;

        if(logoOverlay != null)
        {
            logoOverlay.SetActive(false);
        }
        if(youTubeOverlay != null)
        {
            youTubeOverlay.SetActive(false);
        }

        LoadTexture(texture, modId, imageFileName, galleryImageOverlay);
    }

    // --- MISC ---
    public void DisplayLoading(int modId = -1)
    {
        m_modId = modId;
        m_mediaId = string.Empty;
        m_clickNotifier = () => {};

        image.enabled = false;

        if(loadingDisplay != null)
        {
            loadingDisplay.SetActive(true);
        }
        if(logoOverlay != null)
        {
            logoOverlay.SetActive(false);
        }
        if(youTubeOverlay != null)
        {
            youTubeOverlay.SetActive(false);
        }
        if(galleryImageOverlay != null)
        {
            galleryImageOverlay.SetActive(false);
        }
    }

    private void LoadTexture(Texture2D texture, int modId, string mediaId, GameObject overlay)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        if(image == null
           || modId != m_modId
           || mediaId != m_mediaId)
        {
            return;
        }

        if(loadingDisplay != null)
        {
            loadingDisplay.SetActive(false);
        }
        if(overlay != null)
        {
            overlay.SetActive(true);
        }

        image.sprite = ModBrowser.CreateSpriteFromTexture(texture);
        image.enabled = true;
    }

    // ---------[ EVENT HANDLING ]---------
    public void NotifyClicked()
    {
        m_clickNotifier();
    }

    private void NotifyLogoClicked()
    {
        if(logoClicked != null)
        {
            logoClicked(this, m_modId);
        }
    }

    private void NotifyYouTubeClicked()
    {
        if(youTubeThumbClicked != null)
        {
            youTubeThumbClicked(this, m_modId, m_mediaId);
        }
    }

    private void NotifyImageClicked()
    {
        if(galleryImageClicked != null)
        {
            galleryImageClicked(this, m_modId, m_mediaId);
        }
    }
}
