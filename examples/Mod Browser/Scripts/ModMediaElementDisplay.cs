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
    public GameObject loadingPlaceholder;
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
    public void DisplayLogo(int modId, LogoImageLocator logoLocator)
    {
        Debug.Assert(modId > 0,
                     "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(logoLocator != null,
                     "[mod.io] logoLocator needs to be set and have a fileName.");

        m_modId = modId;
        m_mediaId = logoLocator.fileName;
        m_clickNotifier = NotifyLogoClicked;

        DisplayLoading();
        ModManager.GetModLogo(modId, logoLocator, logoSize,
                              (t) => LoadTexture(t, modId, logoLocator.fileName, logoOverlay),
                              WebRequestError.LogAsWarning);
    }

    public void DisplayLogoTexture(int modId, Texture2D texture)
    {
        Debug.Assert(modId > 0, "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(texture != null);

        m_modId = modId;
        m_mediaId = "_LOGO_";
        m_clickNotifier = NotifyLogoClicked;

        LoadTexture(texture, modId, "_LOGO_", logoOverlay);
    }

    public void DisplayYouTubeThumb(int modId, string youTubeVideoId)
    {
        Debug.Assert(modId > 0,
                     "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(!String.IsNullOrEmpty(youTubeVideoId),
                     "[mod.io] youTubeVideoId needs to be set to a valid YouTube video id.");

        m_modId = modId;
        m_mediaId = youTubeVideoId;
        m_clickNotifier = NotifyYouTubeClicked;

        DisplayLoading();
        ModManager.GetModYouTubeThumbnail(modId, youTubeVideoId,
                                          (t) => LoadTexture(t, modId, youTubeVideoId, youTubeOverlay),
                                          WebRequestError.LogAsWarning);
    }

    public void DisplayYouTubeThumbTexture(int modId, string youTubeVideoId, Texture2D texture)
    {
        Debug.Assert(modId > 0, "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(!String.IsNullOrEmpty(youTubeVideoId),
                     "[mod.io] youTubeVideoId needs to be set to a valid YouTube video id.");
        Debug.Assert(texture != null);

        m_modId = modId;
        m_mediaId = youTubeVideoId;
        m_clickNotifier = NotifyYouTubeClicked;

        LoadTexture(texture, modId, youTubeVideoId, youTubeOverlay);
    }

    public void DisplayGalleryImage(int modId, GalleryImageLocator imageLocator)
    {
        Debug.Assert(modId > 0,
                     "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(imageLocator != null && !String.IsNullOrEmpty(imageLocator.fileName),
                     "[mod.io] imageLocator needs to be set and have a fileName.");

        m_modId = modId;
        m_mediaId = imageLocator.fileName;
        m_clickNotifier = NotifyImageClicked;

        DisplayLoading();
        ModManager.GetModGalleryImage(modId, imageLocator, galleryImageSize,
                                      (t) => LoadTexture(t, modId, imageLocator.fileName, galleryImageOverlay),
                                      WebRequestError.LogAsWarning);
    }

    public void DisplayGalleryImageTexture(int modId, string imageFileName, Texture2D texture)
    {
        Debug.Assert(modId > 0, "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(!String.IsNullOrEmpty(imageFileName));
        Debug.Assert(texture != null);

        m_modId = modId;
        m_mediaId = imageFileName;
        m_clickNotifier = NotifyImageClicked;

        LoadTexture(texture, modId, imageFileName, galleryImageOverlay);
    }

    public void DisplayLoading()
    {
        image.enabled = false;

        if(loadingPlaceholder != null)
        {
            loadingPlaceholder.SetActive(true);
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

        if(loadingPlaceholder != null)
        {
            loadingPlaceholder.SetActive(false);
        }
        if(overlay != null)
        {
            overlay.SetActive(true);
        }

        image.sprite = ModBrowser.CreateSpriteFromTexture(texture);
        image.enabled = true;
    }

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
