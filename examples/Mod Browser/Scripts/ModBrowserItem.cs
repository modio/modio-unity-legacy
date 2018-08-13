using System;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

public class ModBrowserItem : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    // --- Core Data ---
    public event Action<ModBrowserItem> onClick;
    public ModProfile modProfile;

    // --- Display Data ---
    public GameObject loadingPlaceholderPrefab;
    public LogoSize logoVersion;

    // --- Scene Data ---
    public Text modNameText;
    public Text modTagsText;
    public Text modDownloadCountText;
    public Transform modLogoContainer;

    // --- Run-Time Data ---
    private GameObject _loadingPlaceholderInstance;
    private Image _modLogoImage;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        _loadingPlaceholderInstance = null;

        foreach(Transform logoChild in modLogoContainer)
        {
            UnityEngine.Object.Destroy(logoChild.gameObject);
        }

        _loadingPlaceholderInstance = UnityEngine.Object.Instantiate(loadingPlaceholderPrefab, modLogoContainer) as GameObject;
        _loadingPlaceholderInstance.SetActive(true);

        GameObject modLogo_go = new GameObject("ModLogo");
        modLogo_go.AddComponent<CanvasRenderer>();

        RectTransform logoTransfrom = modLogo_go.AddComponent<RectTransform>();
        logoTransfrom.SetParent(modLogoContainer);
        logoTransfrom.anchorMin = new Vector2(0f, 0f);
        logoTransfrom.anchorMax = new Vector2(1f, 1f);
        logoTransfrom.offsetMin = new Vector2(0f, 0f);
        logoTransfrom.offsetMax = new Vector2(0f, 0f);

        _modLogoImage = modLogo_go.AddComponent<Image>();
        modLogo_go.SetActive(false);
    }

    public void UpdateDisplayObjects()
    {
        if(modProfile == null)
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            // set name
            modNameText.text = modProfile.name;

            // set tags
            StringBuilder tagsString = new StringBuilder();
            foreach(string tag in modProfile.tagNames)
            {
                tagsString.Append(tag + ", ");
            }
            if(tagsString.Length > 0)
            {
                // Remove trailing ", "
                tagsString.Length -= 2;
            }
            modTagsText.text = tagsString.ToString();

            // set downloads
            modDownloadCountText.text = "▼ #TODO#";

            // set logo image
            _loadingPlaceholderInstance.SetActive(true);
            _modLogoImage.gameObject.SetActive(false);

            ModManager.GetModLogo(modProfile, LogoSize.Thumbnail_320x180,
                                  ApplyModLogo,
                                  null);
        }
    }

    public void ApplyModLogo(Texture2D logoTexture)
    {
        if(_modLogoImage.sprite != null)
        {
            if(_modLogoImage.sprite.texture != null)
            {
                UnityEngine.Object.Destroy(_modLogoImage.sprite.texture);
            }

            UnityEngine.Object.Destroy(_modLogoImage.sprite);
        }

        _modLogoImage.sprite = Sprite.Create(logoTexture,
                                             new Rect(0.0f, 0.0f, logoTexture.width, logoTexture.height),
                                             Vector2.zero);

        _modLogoImage.gameObject.SetActive(true);

        if(_loadingPlaceholderInstance != null)
        {
            UnityEngine.Object.Destroy(_loadingPlaceholderInstance);
            _loadingPlaceholderInstance = null;
        }
    }

    public void DoClick()
    {
        if(onClick != null)
        {
            onClick(this);
        }
    }
}
