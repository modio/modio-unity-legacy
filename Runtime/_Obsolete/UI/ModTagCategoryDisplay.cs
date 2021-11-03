using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [Obsolete("No longer supported. Use TagContainer instead.")]
    [RequireComponent(typeof(ModTagCollectionDisplayComponent))]
    public class ModTagCategoryDisplay : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        [Header("Settings")]
        public bool capitalizeCategory;

        [Header("UI Components")]
        public Text nameDisplay;

        // ---------[ ACCESSORS ]---------
        public ModTagCollectionDisplayComponent tagDisplay
        {
            get {
                return this.gameObject.GetComponent<ModTagCollectionDisplayComponent>();
            }
        }

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            Debug.Assert(nameDisplay != null);
            Debug.Assert(tagDisplay != null);

            tagDisplay.Initialize();
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public void DisplayCategory(string categoryName, IEnumerable<string> tags)
        {
            Debug.Assert(categoryName != null);
            Debug.Assert(tags != null);

            ModTagCategory category = new ModTagCategory() {
                name = categoryName,
                tags = tags.ToArray(),
            };
            DisplayCategory(category);
        }
        public void DisplayCategory(ModTagCategory category)
        {
            Debug.Assert(category != null);

            nameDisplay.text = (capitalizeCategory ? category.name.ToUpper() : category.name);
            tagDisplay.DisplayTags(category.tags, new ModTagCategory[] { category });
        }
    }
}
