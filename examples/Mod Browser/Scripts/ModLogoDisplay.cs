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
    [SerializeField] private LogoImageLocator m_logoLocator;

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
        Debug.Assert(modId > 0,
                     "[mod.io] Mod Id needs to be set to a valid mod profile id.");
        Debug.Assert(logoLocator != null,
                     "[mod.io] logoLocator needs to be set and have a fileName.");

        m_modId = modId;
        m_logoLocator = logoLocator;

        DisplayLoading();
        ModManager.GetModLogo(modId, logoLocator, logoSize,
                              (t) => OnGetThumbnail(logoLocator.fileName, t),
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

        if(fileName != m_logoLocator.fileName
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
