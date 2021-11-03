using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    [System.Obsolete("Use ExplorerFilterTagsContainer instead.")]
    [RequireComponent(typeof(TagContainer))]
    public class ExplorerTagFilterBar : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>ExplorerView to set the tagFilter on.</summary>
        public ExplorerView view = null;

        /// <summary>Tags to display as selected.</summary>
        private List<string> m_selectedTags = new List<string>();

        // --- ACCESSORS ---
        public TagContainer container
        {
            get {
                return this.gameObject.GetComponent<TagContainer>();
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            // init tag selection
            this.view.onTagFilterUpdated += (t) =>
            {
                this.m_selectedTags = new List<string>(t);
                this.Refresh();
            };
            this.m_selectedTags = new List<string>(this.view.GetTagFilter());
            this.Refresh();
        }

        // ---------[ DISPLAY FUNCTIONALITY ]---------
        public void Refresh()
        {
            this.container.DisplayTags(this.m_selectedTags);
        }

        // ---------[ OBSOLETE ]---------
        [System.Obsolete("Use TagContainer.hideIfEmpty instead.")]
        [HideInInspector]
        public bool hideIfEmpty;
    }
}
