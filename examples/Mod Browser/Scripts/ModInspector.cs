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

        // TODO(@jackson): media

        // TODO(@jackson): tags
    }
}
