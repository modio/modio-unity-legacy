using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModTagCollectionTextDisplay : ModTagCollectionDisplay
    {
        // ---------[ FIELDS ]---------
        [Header("Settings")]
        public bool includeCategory = false;
        public string tagSeparator = ", ";

        [Header("UI Components")]
        public Text text;
        public GameObject loadingOverlay;

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
            Debug.Assert(tags != null);

            m_modId = modId;

            IDictionary<string, string> tagCategoryMap = ModTagCollectionDisplay.GenerateTagCategoryMap(tags,
                                                                                                         tagCategories);

            StringBuilder builder = new StringBuilder();
            foreach(var tagCategory in tagCategoryMap)
            {
                if(!System.String.IsNullOrEmpty(tagCategory.Value))
                {
                    builder.Append(tagCategory.Value + ": ");
                }

                builder.Append(tagCategory.Key + tagSeparator);
            }

            if(builder.Length > 0)
            {
                builder.Length -= tagSeparator.Length;
            }

            text.text = builder.ToString();
            text.enabled = true;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
        }

        public override void DisplayLoading(int modId = -1)
        {
            text.enabled = false;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }
        }
    }
}
