using System;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

// TODO(@jackson): Handle guest accounts
public class ModInspector : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    // ---[ UI COMPONENTS ]---
    [Header("UI Components")]
    // - Profile -
    public Text modNameText;
    public Transform creatorAvatarContainer;
    public Text creatorUsernameText;
    // public Text creatorLastOnlineText;
    // public GameObject tagBadgePrefab;
    public Transform tagContainer;
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

    // - Controls -
    public Text subscribeButtonText;

    // - Prefabs -
    public GameObject loadingPlaceholderPrefab;
    public GameObject youTubeOverlayPrefab;

    // - Layouting -
    public float mediaElementWidth;
    public float mediaElementHeight;
    public LogoSize logoSize;
    public ModGalleryImageSize galleryImageSize;

    // ---[ INSPECTOR DATA ]---
    [Header("Data")]
    public ModProfile profile;
    public ModStatistics stats;

    // ---------[ INITIALIZATION ]---------
    public void UpdateUIComponents()
    {
        // profile
        modNameText.text = profile.name;
        creatorUsernameText.text = profile.submittedBy.username;
        // creatorLastOnlineText = profile.submittedBy.dateOnline;
        summaryText.text = profile.summary;
        // descriptionText.text = profile.description;
        versionText.text = profile.activeBuild.version;
        fileSizeText.text = (profile.activeBuild.fileSize / 1024).ToString() + "MB";
        releaseDateText.text = ServerTimeStamp.ToLocalDateTime(profile.dateLive).ToString("MMMM dd, yyyy");

        // stats
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

        // media
        foreach(Transform t in mediaGalleryContainer)
        {
            GameObject.Destroy(t.gameObject);
        }

        GameObject logo_go = CreateMediaGalleryElement(mediaElementWidth,
                                                       mediaElementHeight);
        logo_go.gameObject.name = "Mod Logo";
        logo_go.GetComponent<RectTransform>().anchoredPosition = new Vector2(10f, 0f);

        ModManager.GetModLogo(profile, logoSize,
                              (t) => ReplaceLoadingPlaceholder(logo_go, t),
                              null);

        // for(int i = 0; i < profile.media.youtubeURLs.Length; ++i)
        // {
        //     GameObject youtube_go = CreateMediaGalleryElement(mediaElementWidth,
        //                                                       mediaElementHeight);

        //     string youTubeURL = profile.media.youtubeURLs[i];
        //     string youTubeId = Utility.ExtractYouTubeIdFromURL(youTubeURL);
        //     youtube_go.gameObject.name = youTubeId;

        //     GameObject overlay_go = GameObject.Instantiate(youTubeOverlayPrefab, youtube_go.transform) as GameObject;
        //     overlay_go.GetComponent<YouTubeLinker>().youTubeVideoId = youTubeId;
        // }

        // foreach(var imageLocator in profile.media.galleryImageLocators)
        // {
        //     GameObject image_go = CreateMediaGalleryElement(mediaElementWidth,
        //                                                     mediaElementHeight);

        //     image_go.gameObject.name = imageLocator.fileName;
        // }


        // TODO(@jackson): tags
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

        Debug.Log("Element Width: " + elementTransform.rect.width);

        newElement.AddComponent<Image>();

        GameObject placeholder_go = UnityEngine.Object.Instantiate(loadingPlaceholderPrefab, elementTransform) as GameObject;
        RectTransform placeholderTransform = placeholder_go.GetComponent<RectTransform>();
        placeholderTransform.anchorMin = new Vector2(0f, 0f);
        placeholderTransform.anchorMax = new Vector2(1f, 1f);
        placeholderTransform.sizeDelta = new Vector2(0f, 0f);
        // placeholderTransform.offsetMax = new Vector2(0f, 0f);

        return newElement;
    }

    private void ReplaceLoadingPlaceholder(GameObject mediaElement,
                                           Texture2D texture)
    {
        mediaElement.GetComponent<Image>().sprite = CreateSpriteWithTexture(texture);
        GameObject.Destroy(mediaElement.transform.GetChild(0).gameObject);
    }

    public static Sprite CreateSpriteWithTexture(Texture2D texture)
    {
        return Sprite.Create(texture,
                             new Rect(0.0f, 0.0f, texture.width, texture.height),
                             Vector2.zero);
    }
}
