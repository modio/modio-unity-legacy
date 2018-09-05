using System;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

// TODO(@jackson): Handle guest accounts
// TODO(@jackson): Handle errors
public class ModInspector : MonoBehaviour
{
    public const float YOUTUBE_THUMB_RATIO = 4f/3f;
    public const float MODIO_THUMB_RATIO = 16f/9f;

    // ---------[ FIELDS ]---------
    // ---[ SCENE COMPONENTS ]---
    // - Prefabs -
    [Header("Prefabs")]
    public GameObject defaultAvatarPrefab;
    public GameObject mediaLoadingPrefab;
    public GameObject youTubeOverlayPrefab;
    public GameObject tagBadgePrefab;


    [Header("UI Components")]
    // - Profile -
    public Text modNameText;
    public LogoSize logoSize;
    public Image avatar;
    public UserAvatarSize avatarSize;
    public Text creatorUsernameText;
    // public Text creatorLastOnlineText;
    // public GameObject tagBadgePrefab;
    public Transform tagContainer;
    public float tagPadding;
    public float tagSpacing;
    public Text summaryText;
    // public Text descriptionText;
    public Text versionText;
    public Text fileSizeText;
    public Text releaseDateText;

    // - Stats -
    public Text popularityRankText;
    public Text downloadCountText;

    // - Media -
    public Transform mediaGalleryContainer;
    public ModGalleryImageSize galleryImageSize;
    public float mediaElementHeight;

    // - Controls -
    public Button subscribeButton;
    public Text subscribeButtonText;

    // - Layouting -

    // ---[ INSPECTOR DATA ]---
    [Header("Data")]
    public ModProfile profile;
    public ModStatistics stats;

    // ---[ RUNTIME DATA ]---
    [Header("Runtime Data")]
    public GameObject defaultAvatarInstance;

    // ---------[ INITIALIZATION ]---------
    // TODO(@jackson): Reset view (scrolling, etc)
    public void UpdateProfileUIComponents()
    {
        if(defaultAvatarInstance == null)
        {
            defaultAvatarInstance = UnityEngine.Object.Instantiate(defaultAvatarPrefab, avatar.transform) as GameObject;
            RectTransform avatarTransform = defaultAvatarInstance.GetComponent<RectTransform>();
            avatarTransform.anchorMin = new Vector2(0f, 0f);
            avatarTransform.anchorMax = new Vector2(1f, 1f);
            avatarTransform.sizeDelta = new Vector2(0f, 0f);
        }

        // profile
        modNameText.text = profile.name;
        creatorUsernameText.text = profile.submittedBy.username;
        // creatorLastOnlineText = profile.submittedBy.dateOnline;
        summaryText.text = profile.summary;
        // descriptionText.text = profile.description;
        versionText.text = profile.activeBuild.version;
        fileSizeText.text = (profile.activeBuild.fileSize / 1024).ToString() + "MB";
        releaseDateText.text = ServerTimeStamp.ToLocalDateTime(profile.dateLive).ToString("MMMM dd, yyyy");

        // user avatar
        avatar.GetComponent<Image>().enabled = false;
        defaultAvatarInstance.gameObject.SetActive(true);

        ModManager.GetUserAvatar(profile.submittedBy,
                                 avatarSize,
                                 LoadUserAvatar,
                                 null);

        // - MEDIA -
        float modioWidth = mediaElementHeight * MODIO_THUMB_RATIO;
        float youTubeWidth = mediaElementHeight * YOUTUBE_THUMB_RATIO;

        // logo
        foreach(Transform t in mediaGalleryContainer)
        {
            GameObject.Destroy(t.gameObject);
        }

        float culmativeWidth = 0f;

        GameObject logo_go = CreateMediaGalleryElement(modioWidth,
                                                       mediaElementHeight);
        logo_go.gameObject.name = "Mod Logo";
        logo_go.GetComponent<RectTransform>().anchoredPosition = new Vector2(20f, 0f);

        ModManager.GetModLogo(profile, logoSize,
                              (t) => ReplaceLoadingPlaceholder(logo_go, t),
                              null);

        culmativeWidth += 20f + modioWidth;

        // youtube
        if(profile.media.youtubeURLs != null)
        {
            for(int i = 0; i < profile.media.youtubeURLs.Length; ++i)
            {
                GameObject youtube_go = CreateMediaGalleryElement(youTubeWidth,
                                                                  mediaElementHeight);

                string youTubeURL = profile.media.youtubeURLs[i];
                string youTubeId = Utility.ExtractYouTubeIdFromURL(youTubeURL);
                youtube_go.gameObject.name = "yt_" + youTubeId;
                youtube_go.GetComponent<RectTransform>().anchoredPosition = new Vector2(20f
                                                                                        + culmativeWidth,
                                                                                        0f);

                Action<string, GameObject, Texture2D> onGetThumbnail = (id, go, t) =>
                {
                    ReplaceLoadingPlaceholder(go, t);

                    GameObject overlay_go = GameObject.Instantiate(youTubeOverlayPrefab, go.transform) as GameObject;
                    overlay_go.GetComponent<YouTubeLinker>().youTubeVideoId = id;

                    RectTransform overlayTransform = overlay_go.GetComponent<RectTransform>();
                    overlayTransform.anchorMin = new Vector2(0f, 0f);
                    overlayTransform.anchorMax = new Vector2(1f, 1f);
                    overlayTransform.sizeDelta = new Vector2(0f, 0f);
                };

                ModManager.GetModYouTubeThumbnail(profile, i,
                                                  (t) => onGetThumbnail(youTubeId, youtube_go, t),
                                                  null);

                culmativeWidth += 20f + youTubeWidth;
            }
        }

        // images
        if(profile.media.galleryImageLocators != null)
        {
            foreach(var imageLocator in profile.media.galleryImageLocators)
            {
                GameObject image_go = CreateMediaGalleryElement(modioWidth,
                                                                mediaElementHeight);

                image_go.gameObject.name = imageLocator.fileName;
                image_go.GetComponent<RectTransform>().anchoredPosition = new Vector2(20f
                                                                                      + culmativeWidth,
                                                                                      0f);

                ModManager.GetModGalleryImage(profile, imageLocator.fileName, galleryImageSize,
                                              (t) => ReplaceLoadingPlaceholder(image_go, t),
                                              null);

                culmativeWidth += 20f + modioWidth;
            }
        }

        float galleryWidth = culmativeWidth + 20f;
        mediaGalleryContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(galleryWidth, 0f);

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

        foreach(string tagName in profile.tagNames)
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
        if(stats == null)
        {
            popularityRankText.text = "Loading...";
            downloadCountText.text = "Loading...";
        }
        else
        {
            popularityRankText.text = (ModBrowser.ConvertValueIntoShortText(stats.popularityRankPosition)
                                       + " of "
                                       + ModBrowser.ConvertValueIntoShortText(stats.popularityRankModCount));

            downloadCountText.text = (ModBrowser.ConvertValueIntoShortText(stats.downloadCount));
        }
    }

    public void SetSubscribedState(bool isModInCollection)
    {
        if(isModInCollection)
        {
            subscribeButton.onClick.AddListener(() => OnViewInCollectionClicked());
            subscribeButton.GetComponentInChildren<Text>().text = "View In Collection";
        }
        else
        {
            subscribeButton.onClick.AddListener(() => OnSubscribeClicked());
            subscribeButton.GetComponentInChildren<Text>().text = "Add To Collection";
        }
    }

    public void LoadUserAvatar(Texture2D texture)
    {
        if(texture != null)
        {
            defaultAvatarInstance.gameObject.SetActive(false);

            var avatarImage = avatar.GetComponent<Image>();
            avatarImage.sprite = ModBrowser.CreateSpriteWithTexture(texture);
            avatarImage.enabled = true;
        }
    }

    private GameObject CreateMediaGalleryElement(float width, float height)
    {
        GameObject newElement = new GameObject("Media Gallery Item");
        newElement.AddComponent<CanvasRenderer>();

        RectTransform elementTransform = newElement.AddComponent<RectTransform>();
        elementTransform.SetParent(mediaGalleryContainer);
        elementTransform.anchorMin = new Vector2(0f, 0.5f);
        elementTransform.anchorMax = new Vector2(0f, 0.5f);
        elementTransform.pivot = new Vector2(0f, 0.5f);
        elementTransform.sizeDelta = new Vector2(width, height);

        newElement.AddComponent<Image>();

        GameObject placeholder_go = UnityEngine.Object.Instantiate(mediaLoadingPrefab, elementTransform) as GameObject;
        RectTransform placeholderTransform = placeholder_go.GetComponent<RectTransform>();
        placeholderTransform.anchorMin = new Vector2(0f, 0f);
        placeholderTransform.anchorMax = new Vector2(1f, 1f);
        placeholderTransform.sizeDelta = new Vector2(0f, 0f);

        return newElement;
    }

    private void ReplaceLoadingPlaceholder(GameObject imageGameObject,
                                           Texture2D texture)
    {
        if(imageGameObject != null)
        {
            imageGameObject.GetComponent<Image>().sprite = ModBrowser.CreateSpriteWithTexture(texture);
            imageGameObject.GetComponent<Image>().enabled = true;
            GameObject.Destroy(imageGameObject.transform.GetChild(0).gameObject);
        }
    }

    private void OnSubscribeClicked()
    {


        SetSubscribedState(true);
    }

    private void OnViewInCollectionClicked()
    {
        SetSubscribedState(false);
    }
}
