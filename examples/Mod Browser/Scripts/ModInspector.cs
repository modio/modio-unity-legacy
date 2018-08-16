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
    public float mediaElementHeight;
    public LogoSize logoSize;
    public ModGalleryImageSize galleryImageSize;

    // ---[ INSPECTOR DATA ]---
    [Header("Data")]
    public ModProfile profile;
    public ModStatistics stats;

    // ---------[ INITIALIZATION ]---------
    // TODO(@jackson): Reset view (scrolling, etc)
    public void UpdateProfileUIComponents()
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

        // TODO(@jackson): tags
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

        GameObject placeholder_go = UnityEngine.Object.Instantiate(loadingPlaceholderPrefab, elementTransform) as GameObject;
        RectTransform placeholderTransform = placeholder_go.GetComponent<RectTransform>();
        placeholderTransform.anchorMin = new Vector2(0f, 0f);
        placeholderTransform.anchorMax = new Vector2(1f, 1f);
        placeholderTransform.sizeDelta = new Vector2(0f, 0f);

        return newElement;
    }

    private void ReplaceLoadingPlaceholder(GameObject mediaElement,
                                           Texture2D texture)
    {
        if(mediaElement != null)
        {
            mediaElement.GetComponent<Image>().sprite = CreateSpriteWithTexture(texture);
            GameObject.Destroy(mediaElement.transform.GetChild(0).gameObject);
        }
    }

    public static Sprite CreateSpriteWithTexture(Texture2D texture)
    {
        return Sprite.Create(texture,
                             new Rect(0.0f, 0.0f, texture.width, texture.height),
                             Vector2.zero);
    }
}
