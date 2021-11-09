using System.Collections.Generic;

namespace ModIO.UI
{
    [System.Obsolete("No longer supported.")]
    [System.Serializable]
    public struct ModTagDisplayData
    {
        public string tagName;
        public string categoryName;

        public static ModTagDisplayData[] GenerateArray(IEnumerable<string> tagNames,
                                                        IEnumerable<ModTagCategory> categories)
        {
            UnityEngine.Debug.Assert(tagNames != null);

            // init
            List<string> unmatchedTags = new List<string>(tagNames);

            if(unmatchedTags.Count == 0)
            {
                return new ModTagDisplayData[0];
            }

            if(categories == null)
            {
                categories = new List<ModTagCategory>(0);
            }

            // match
            List<ModTagDisplayData> tags = new List<ModTagDisplayData>(unmatchedTags.Count);
            foreach(ModTagCategory category in categories)
            {
                foreach(string categoryTag in category.tags)
                {
                    if(unmatchedTags.Contains(categoryTag))
                    {
                        ModTagDisplayData newTag = new ModTagDisplayData() {
                            tagName = categoryTag,
                            categoryName = category.name,
                        };
                        tags.Add(newTag);

                        while(unmatchedTags.Remove(categoryTag)) {}

                        if(unmatchedTags.Count == 0)
                        {
                            return tags.ToArray();
                        }
                    }
                }
            }

            foreach(string tag in unmatchedTags)
            {
                ModTagDisplayData newTag = new ModTagDisplayData() {
                    tagName = tag,
                    categoryName = null,
                };
                tags.Add(newTag);
            }

            return tags.ToArray();
        }
    }

    [System.Obsolete("No longer supported.")]
    public abstract class ModTagDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModTagDisplayComponent> onClick;

        public abstract ModTagDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayModTag(string tagName, string categoryName);
        public abstract void DisplayModTag(ModTag tag, string categoryName);
        public abstract void DisplayLoading();
    }
}
