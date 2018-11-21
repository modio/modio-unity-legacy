using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;


public class ModLogoDisplay : MonoBehaviour, IModProfilePresenter
{
    // ---------[ FIELDS ]---------
    public delegate void OnClickDelegate(ModLogoDisplay component, int modId);
    public event OnClickDelegate onClick;

    [Header("Settings")]
    public LogoSize logoSize;

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
    public void DisplayProfile(ModProfile profile)
    {
        Debug.Assert(profile != null);

        DisplayLogo(profile.id, profile.logoLocator);
    }

    public void DisplayLogo(int modId, LogoImageLocator logoLocator)
    {
        Debug.Assert(modId > 0, "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(logoLocator != null);

        m_modId = modId;
        m_imageFileName = logoLocator.fileName;

        DisplayLoading();
        ModManager.GetModLogo(modId, logoLocator, logoSize,
                              (t) => LoadTexture(t, logoLocator.fileName),
                              WebRequestError.LogAsWarning);
    }

    public void DisplayTexture(int modId, Texture2D logoTexture)
    {
        Debug.Assert(modId > 0, "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(logoTexture != null);

        m_modId = modId;
        m_imageFileName = string.Empty;

        LoadTexture(logoTexture, string.Empty);
    }

    public void DisplayLoading()
    {
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

    public void NotifyClicked()
    {
        if(this.onClick != null)
        {
            this.onClick(this, m_modId);
        }
    }
}
