using System;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

public class ModGalleryPreview : MonoBehaviour
{
    public GameObject loadingPlaceholderPrefab;

    public Image modLogoImage;
    public Text modNameText;
    public Text modTagsText;
    public Text modDownloadCountText;

    public ModProfile modProfile;

    public event Action<ModGalleryPreview> onClick;

    private GameObject _loadingPlaceholderInstance;

    public void UpdateDisplay()
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
            modLogoImage.gameObject.SetActive(false);

            if(_loadingPlaceholderInstance == null)
            {
                _loadingPlaceholderInstance = UnityEngine.Object.Instantiate(loadingPlaceholderPrefab, this.transform) as GameObject;
            }
            else
            {
                _loadingPlaceholderInstance.gameObject.SetActive(true);
            }

            ModManager.GetModLogo(modProfile, LogoSize.Thumbnail_320x180,
                                  ApplyModLogo,
                                  null);
        }
    }

    public void ApplyModLogo(Texture2D logoTexture)
    {
        if(modLogoImage.sprite != null)
        {
            if(modLogoImage.sprite.texture != null)
            {
                UnityEngine.Object.Destroy(modLogoImage.sprite.texture);
            }

            UnityEngine.Object.Destroy(modLogoImage.sprite);
        }

        modLogoImage.sprite = Sprite.Create(logoTexture,
                                            new Rect(0.0f, 0.0f, logoTexture.width, logoTexture.height),
                                            Vector2.zero);

        modLogoImage.gameObject.SetActive(true);

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
