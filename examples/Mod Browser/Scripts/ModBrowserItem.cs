using System;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

public class ModBrowserItem : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    // ---[ UI COMPONENTS ]---
    [Header("UI Components")]
    // - Profile -
    public Text modNameText;
    public Text modCreatorText;

    // - Stats -
    public Image[] modRatingStarImages;
    public Texture2D modRatingStarFilled;
    public Texture2D modRatingStarHalfFilled;
    public Texture2D modRatingStarUnfilled;
    public Text modRatingCountText;
    public Text modDownloadCountText;

    // - Prefabs -
    public GameObject loadingPlaceholderPrefab;
    public GameObject tagBadgePrefab;

    // - Layouting -
    public LogoSize logoVersion;
    public Transform logoContainer;
    public Transform tagContainer;
    public float tagPadding;
    public float tagSpacing;

    // ---[ RUNTIME DATA ]---
    [Header("Runtime Data")]
    public ModProfile modProfile;
    public ModStatistics modStatistics;

    // - Events -
    public event Action<ModBrowserItem> onClick;

    // - Caching -
    private GameObject _loadingPlaceholderInstance;
    private Image _modLogoImage;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        _loadingPlaceholderInstance = null;

        foreach(Transform logoChild in logoContainer)
        {
            UnityEngine.Object.Destroy(logoChild.gameObject);
        }

        _loadingPlaceholderInstance = UnityEngine.Object.Instantiate(loadingPlaceholderPrefab, logoContainer) as GameObject;

        GameObject modLogo_go = new GameObject("ModLogo");
        modLogo_go.AddComponent<CanvasRenderer>();

        RectTransform logoTransfrom = modLogo_go.AddComponent<RectTransform>();
        logoTransfrom.SetParent(logoContainer);
        logoTransfrom.anchorMin = new Vector2(0f, 0f);
        logoTransfrom.anchorMax = new Vector2(1f, 1f);
        logoTransfrom.offsetMin = new Vector2(0f, 0f);
        logoTransfrom.offsetMax = new Vector2(0f, 0f);

        _modLogoImage = modLogo_go.AddComponent<Image>();
        modLogo_go.SetActive(false);
    }

    public void UpdateProfileUIComponents()
    {
        Debug.Assert(modProfile != null,
                     "[mod.io] Assign the mod profile before updating the profile UI components.");

        // profile
        modNameText.text = modProfile.name;


        if(modCreatorText != null)
        {
            modCreatorText.text = modProfile.submittedBy.username;
        }

        // logo
        _loadingPlaceholderInstance.SetActive(true);
        _modLogoImage.gameObject.SetActive(false);

        ModManager.GetModLogo(modProfile, LogoSize.Thumbnail_320x180,
                              ApplyModLogo,
                              null);

        // tags
        foreach(Transform t in tagContainer)
        {
            GameObject.Destroy(t.gameObject);
        }

        float tagContainerWidth = tagContainer.GetComponent<RectTransform>().rect.width;
        // TODO(@jackson): Handle too many tags
        // float tagContainerHeight = tagContainer.GetComponent<RectTransform>().rect.height;
        float xPos = 0f;
        float yPos = 0f;

        foreach(string tagName in modProfile.tagNames)
        {
            GameObject tag_go = GameObject.Instantiate(tagBadgePrefab, tagContainer) as GameObject;
            tag_go.name = "Tag: " + tagName;

            Text tagText = tag_go.GetComponentInChildren<Text>();
            tagText.text = tagName;

            RectTransform tagTransform = tag_go.GetComponent<RectTransform>();
            TextGenerator tagTextGen = new TextGenerator();
            TextGenerationSettings tagGenSettings = tagText.GetGenerationSettings(tagText.rectTransform.rect.size);

            float tagWidth = tagTextGen.GetPreferredWidth(tagName, tagGenSettings) + 2 * this.tagPadding;

            if(xPos + tagWidth > tagContainerWidth)
            {
                yPos -= tagTransform.rect.height + this.tagSpacing;
                xPos = 0f;
            }

            tagTransform.anchoredPosition = new Vector2(xPos, yPos);
            tagTransform.sizeDelta = new Vector2(tagWidth, tagTransform.rect.height);

            xPos += tagWidth + this.tagSpacing;
        }
    }

    public void UpdateStatisticsUIComponents()
    {
        if(modStatistics == null)
        {
            modDownloadCountText.gameObject.SetActive(false);

            if(modRatingCountText != null)
            {
                modRatingCountText.gameObject.SetActive(false);
            }

            foreach(Image ratingStarImage in modRatingStarImages)
            {
                ratingStarImage.gameObject.SetActive(false);
            }
        }
        else
        {
            modDownloadCountText.text = (ModBrowser.ConvertValueIntoShortText(modStatistics.downloadCount)
                                         + " downloads");
            modDownloadCountText.gameObject.SetActive(true);

            if(modRatingCountText != null)
            {
                modRatingCountText.text = (ModBrowser.ConvertValueIntoShortText(modStatistics.ratingsTotalCount)
                                           + " ratings");
                modRatingCountText.gameObject.SetActive(true);
            }

            // float starRating = modStatistics.ratingsWeightedAggregate * 5f;
            for(int starIndex = 0;
                starIndex < 5;
                ++starIndex)
            {
                modRatingStarImages[starIndex].gameObject.SetActive(true);

                if(modStatistics.ratingsWeightedAggregate >= (1f + starIndex))
                {
                    modRatingStarImages[starIndex].sprite = ModBrowser.CreateSpriteWithTexture(modRatingStarFilled);
                }
                else if(modStatistics.ratingsWeightedAggregate >= (0.5f + starIndex))
                {
                    modRatingStarImages[starIndex].sprite = ModBrowser.CreateSpriteWithTexture(modRatingStarHalfFilled);
                }
                else
                {
                    modRatingStarImages[starIndex].sprite = ModBrowser.CreateSpriteWithTexture(modRatingStarUnfilled);
                }
            }
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

    public void Clicked()
    {
        if(onClick != null)
        {
            onClick(this);
        }
    }
}
