using System;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

public class InspectorView_VersionEntry : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public string missingChangelogDescription = "<i>No changelog added</i>";

    [Header("UI Components")]
    public Text dateAdded;
    public Text fileName;
    public Text fileSize;
    public Text version;
    public Text changelog;

    [Header("Runtime Date")]
    public Modfile modfile;


    // ---------[ FUNCTIONALITY ]---------
    public void UpdateUIComponents()
    {
        Debug.Assert(modfile != null);

        if(dateAdded != null)
        {
            dateAdded.text = ServerTimeStamp.ToLocalDateTime(modfile.dateAdded).ToString();
        }
        if(fileName != null)
        {
            fileName.text = modfile.fileName;
        }
        if(fileSize != null)
        {
            fileSize.text = ModBrowser.ByteCountToDisplayString(modfile.fileSize);
        }
        if(version != null)
        {
            version.text = modfile.version;
        }
        if(changelog != null)
        {
            string changelogString = modfile.changelog;
            if(String.IsNullOrEmpty(changelogString))
            {
                changelogString = missingChangelogDescription;
            }
            changelog.text = changelogString;
        }
    }
}
