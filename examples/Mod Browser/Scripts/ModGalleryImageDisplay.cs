using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModGalleryImageDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public delegate void OnClickDelegate(ModGalleryImageDisplay component,
                                         int modId, string fileName);
    public event OnClickDelegate onClick;

    [Header("Settings")]
    public ModGalleryImageSize imageSize;

    [Header("UI Components")]
    public Image image;
    public GameObject loadingPlaceholder;

    [Header("Display Data")]
    [SerializeField] private int m_modId;
    [SerializeField] private GalleryImageLocator m_imageLocator;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(image != null);
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayGalleryImage(int modId, GalleryImageLocator imageLocator)
    {
        Debug.Assert(modId > 0,
                     "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(imageLocator != null && !String.IsNullOrEmpty(imageLocator.fileName),
                     "[mod.io] imageLocator needs to be set and have a fileName.");

        m_modId = modId;
        m_imageLocator = imageLocator;

        DisplayLoading();
        ModManager.GetModGalleryImage(modId, imageLocator, imageSize,
                                      (t) => OnGetThumbnail(imageLocator.fileName, t),
                                      WebRequestError.LogAsWarning);
    }

    public void DisplayLoading()
    {
        if(loadingPlaceholder != null)
        {
            loadingPlaceholder.SetActive(true);
        }

        image.enabled = false;
    }

    private void OnGetThumbnail(string fileName, Texture2D texture)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        if(fileName != m_imageLocator.fileName
           || this.image == null)
        {
            return;
        }

        if(loadingPlaceholder != null)
        {
            loadingPlaceholder.SetActive(false);
        }

        image.sprite = ModBrowser.CreateSpriteFromTexture(texture);
        image.enabled = true;
    }

    public void NotifyClicked()
    {
        if(this.onClick != null)
        {
            this.onClick(this, m_modId, m_imageLocator.fileName);
        }
    }
}
