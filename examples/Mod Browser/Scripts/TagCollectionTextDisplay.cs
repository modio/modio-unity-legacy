using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class TagCollectionTextDisplay : TagCollectionDisplayBase
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public bool includeCategory = false;
    public string tagSeparator = ", ";

    [Header("UI Components")]
    public Text text;
    public GameObject loadingDisplay;

    // --- DISPLAY DATA ---
    private int m_modId = -1;

    // ---------[ INITIALIZE ]---------
    public override void Initialize()
    {
        Debug.Assert(text != null);
    }

    // ---------[ UI FUNCTIONALITY ]--------
    public override void DisplayTags(IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories)
    {
        this.DisplayModTags(-1, tags, tagCategories);
    }

    public override void DisplayModTags(ModProfile profile, IEnumerable<ModTagCategory> tagCategories)
    {
        Debug.Assert(profile != null);
        this.DisplayModTags(profile.id, profile.tagNames, tagCategories);
    }

    public override void DisplayModTags(int modId, IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories)
    {
        m_modId = modId;

        List<string> tagNames = new List<string>(tags);
        string[] categoryNames = new string[tagNames.Count];

        if(includeCategory)
        {
            foreach(ModTagCategory category in tagCategories)
            {
                foreach(string categoryTag in tags)
                {
                    int i = tagNames.IndexOf(categoryTag);
                    while(i >= 0)
                    {
                        categoryNames[i] = category.name + ": ";

                        if(i+1 < tagNames.Count)
                        {
                            i = tagNames.IndexOf(categoryTag, i+1);
                        }
                        else
                        {
                            i = -1;
                        }
                    }
                }
            }
        }
        else
        {
            for(int i = 0; i < categoryNames.Length; ++i)
            {
                categoryNames[i] = string.Empty;
            }
        }

        StringBuilder builder = new StringBuilder();
        for(int i = 0; i < tagNames.Count; ++i)
        {
            builder.Append(categoryNames[i] + tagNames[i] + tagSeparator);
        }

        if(builder.Length > 0)
        {
            builder.Length -= tagSeparator.Length;
        }

        text.text = builder.ToString();
        text.enabled = true;

        if(loadingDisplay != null)
        {
            loadingDisplay.SetActive(false);
        }
    }

    public override void DisplayLoading(int modId = -1)
    {
        text.enabled = false;

        if(loadingDisplay != null)
        {
            loadingDisplay.SetActive(true);
        }
    }
}
