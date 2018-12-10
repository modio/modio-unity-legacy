using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    public abstract class ModTagCollectionDisplay : MonoBehaviour
    {
        public abstract IEnumerable<ModTagDisplayData> data { get; set; }

        public abstract void Initialize();

        public abstract void DisplayTags(IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayTags(ModProfile profile, IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayTags(int modId, IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories);

        public abstract void DisplayLoading(int modId = -1);

        protected static IDictionary<string, string> GenerateTagCategoryMap(IEnumerable<string> tagNames,
                                                                            IEnumerable<ModTagCategory> categories)
        {
            ModTagDisplayData[] dataArray = ModTagDisplayData.GenerateArray(tagNames, categories);
            Dictionary<string, string> dict = new Dictionary<string, string>(dataArray.Length);

            foreach(ModTagDisplayData tag in dataArray)
            {
                dict[tag.tagName] = tag.categoryName;
            }

            return dict;
        }
    }
}
