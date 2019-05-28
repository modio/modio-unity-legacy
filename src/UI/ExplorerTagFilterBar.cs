using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    [RequireComponent(typeof(ModTagContainer))]
    public class ExplorerTagFilterBar : MonoBehaviour, IGameProfileUpdateReceiver
    {
        // ---------[ FIELDS ]---------
        /// <summary>ExplorerView to set the tagFilter on.</summary>
        public ExplorerView view = null;
        /// <summary>Should this object be disabled if there are no tags.</summary>
        public bool hideIfEmpty = true;

        /// <summary>Mod Tag Categories for displaying tag data.</summary>
        private ModTagCategory[] m_tagCategories = new ModTagCategory[0];
        /// <summary>Tags to display as selected.</summary>
        private List<string> m_selectedTags = new List<string>();

        // --- ACCESSORS ---
        public ModTagContainer container
        {
            get { return this.gameObject.GetComponent<ModTagContainer>(); }
        }

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            container.Initialize();
            container.tagClicked += (display) =>
            {
                view.RemoveTagFromFilter(display.data.tagName);
            };

            // init tag selection
            this.view.onTagFilterUpdated += (t) =>
            {
                this.m_selectedTags = new List<string>(t);
                this.Refresh();
            };
            this.m_selectedTags = new List<string>(this.view.tagFilter);

            // init tag categories
            var tagCategories = ModBrowser.instance.gameProfile.tagCategories;
            if(tagCategories != null)
            {
                this.m_tagCategories = tagCategories;
            }

            // update display
            this.Refresh();
        }

        // ---------[ DISPLAY FUNCTIONALITY ]---------
        public void Refresh()
        {
            this.container.DisplayTags(this.m_selectedTags, this.m_tagCategories);
            this.gameObject.SetActive(!hideIfEmpty || this.m_selectedTags.Count > 0);
        }

        // ---------[ EVENTS ]---------
        /// <summary>React to game profile update message.</summary>
        public void OnGameProfileUpdated(GameProfile gameProfile)
        {
            Debug.Assert(gameProfile != null);

            if(this.m_tagCategories != gameProfile.tagCategories)
            {
                this.m_tagCategories = gameProfile.tagCategories;
                this.Refresh();
            }
        }
    }
}
