using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    [System.Obsolete("Use TagContainer, of TagCollectionTextDisplay instead.")]
    public abstract class ModTagCollectionDisplayComponent : MonoBehaviour
    {
        public abstract IEnumerable<ModTagDisplayData> data { get; set; }

        public abstract void Initialize();

        public abstract void DisplayTags(ModProfile profile,
                                         IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayTags(IEnumerable<string> tags,
                                         IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayLoading();
    }
}
