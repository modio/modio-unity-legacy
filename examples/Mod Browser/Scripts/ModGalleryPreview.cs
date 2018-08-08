using System;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

public class ModGalleryPreview : MonoBehaviour
{
    public Image modLogoImage;
    public Text modNameText;
    public Text modTagsText;
    public Text modDownloadCountText;

    public ModProfile modProfile;

    public event Action<ModGalleryPreview> onClick;

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
