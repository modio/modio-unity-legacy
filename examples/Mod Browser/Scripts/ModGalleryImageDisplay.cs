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
    [SerializeField] private string m_imageFileName;

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

        DisplayLoading();

        m_modId = modId;
        m_imageFileName = imageLocator.fileName;

        ModManager.GetModGalleryImage(modId, imageLocator, imageSize,
                                      (t) => LoadTexture(t, imageLocator.fileName),
                                      WebRequestError.LogAsWarning);
    }

    public void DisplayTexture(int modId, string imageFileName, Texture2D texture)
    {
        Debug.Assert(modId > 0, "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(texture != null);

        m_modId = modId;
        m_imageFileName = imageFileName;

        LoadTexture(texture, imageFileName);
    }

    public void DisplayLoading(int modId = -1)
    {
        m_modId = modId;

        if(loadingPlaceholder != null)
        {
            loadingPlaceholder.SetActive(true);
        }

        image.enabled = false;
    }

    private void LoadTexture(Texture2D texture, string fileName)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        if(fileName != m_imageFileName
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

    // ---------[ EVENT HANDLING ]---------
    public void NotifyClicked()
    {
        if(this.onClick != null)
        {
            this.onClick(this, m_modId, m_imageFileName);
        }
    }
}
