using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModLogoDisplay : MonoBehaviour, IModProfilePresenter
{
    // ---------[ FIELDS ]---------
    public event Action<ModLogoDisplay> onClick;

    [Header("Settings")]
    public GameObject loadingPrefab;
    public LogoSize logoSize;

    [Header("UI Components")]
    public Image image;

    [Header("Display Data")]
    public int modId;
    public LogoImageLocator logoLocator;

    [Header("Runtime Data")]
    public GameObject loadingInstance;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(image != null);

        if(loadingPrefab != null)
        {
            loadingInstance = GameObject.Instantiate(loadingPrefab, image.transform);

            RectTransform instance_rt = loadingInstance.transform as RectTransform;
            instance_rt.anchorMin = new Vector2(0f, 0f);
            instance_rt.anchorMax = new Vector2(1f, 1f);
            instance_rt.offsetMin = Vector2.zero;
            instance_rt.offsetMax = Vector2.zero;

            loadingInstance.gameObject.SetActive(false);
        }
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

        this.modId = modId;
        this.logoLocator = logoLocator;

        DisplayLoading();
        ModManager.GetModLogo(modId, logoLocator, logoSize,
                              (t) => OnGetThumbnail(logoLocator.fileName, t),
                              WebRequestError.LogAsWarning);
    }

    public void DisplayLoading()
    {
        if(loadingInstance != null)
        {
            loadingInstance.gameObject.SetActive(true);
        }
        else
        {
            image.enabled = false;
        }
    }

    public void UpdateDisplay()
    {
        if(modId > 0 && logoLocator != null)
        {
            DisplayLogo(modId, logoLocator);
        }
        else
        {
            DisplayLoading();
        }
    }

    private void OnGetThumbnail(string fileName, Texture2D texture)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        if(fileName != this.logoLocator.fileName
           || this.image == null)
        {
            return;
        }

        if(loadingInstance != null)
        {
            loadingInstance.gameObject.SetActive(false);
        }

        image.sprite = ModBrowser.CreateSpriteFromTexture(texture);
        image.enabled = true;
    }

    public void NotifyClicked()
    {
        if(this.onClick != null)
        {
            this.onClick(this);
        }
    }
}
