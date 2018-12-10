using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    public abstract class ModTagCollectionDisplay : MonoBehaviour
    {
        public abstract IEnumerable<ModTagDisplayData> data { get; set; }

        public abstract void Initialize();

        public abstract void DisplayTags(IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayModTags(ModProfile profile, IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayModTags(int modId, IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories);

        public abstract void DisplayLoading(int modId = -1);

        protected static IDictionary<string, string> GenerateTagCategoryMap(IEnumerable<string> tagNames,
                                                                            IEnumerable<ModTagCategory> categories)
        {
            // init
            Dictionary<string, string> map = new Dictionary<string, string>();
            List<string> unmatchedTags = new List<string>(tagNames);

            if(unmatchedTags.Count == 0)
            {
                return map;
            }

            if(categories == null)
            {
                categories = new List<ModTagCategory>(0);
            }

            // match
            foreach(ModTagCategory category in categories)
            {
                foreach(string categoryTag in category.tags)
                {
                    if(unmatchedTags.Contains(categoryTag))
                    {
                        map[categoryTag] = category.name;
                        while(unmatchedTags.Remove(categoryTag)){}

                        if(unmatchedTags.Count == 0)
                        {
                            return map;
                        }
                    }
                }
            }

            foreach(string tag in unmatchedTags)
            {
                map[tag] = null;
            }

            return map;
        }
    }
}
