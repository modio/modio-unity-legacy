using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the tags currently being filtered on in the ExplorerView.</summary>
    [RequireComponent(typeof(TagContainer))]
    public class ExplorerFilterTagsContainer : MonoBehaviour, IExplorerViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>ExplorerView to set the tagFilter on.</summary>
        private ExplorerView m_view = null;

        // --- Accessors ---
        /// <summary>Container that this component controls.</summary>
        public TagContainer container
        {
            get {
                return this.gameObject.GetComponent<TagContainer>();
            }
        }

        // ---------[ INITIALIZATION ]---------
        /// <summary>IExplorerViewElement interface.</summary>
        public void SetExplorerView(ExplorerView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onRequestFilterChanged.RemoveListener(DisplayInArrayFilterTags);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onRequestFilterChanged.AddListener(DisplayInArrayFilterTags);
                this.DisplayInArrayFilterTags(this.m_view.requestFilter);
            }
            else
            {
                this.DisplayInArrayFilterTags(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the tags for a given RequestFilter.</summary>
        public void DisplayInArrayFilterTags(RequestFilter filter)
        {
            List<IRequestFieldFilter> tagsFilterList = null;
            if(filter != null)
            {
                filter.fieldFilterMap.TryGetValue(ModIO.API.GetAllModsFilterFields.tags,
                                                  out tagsFilterList);
            }

            string[] tags = null;
            if(tagsFilterList != null)
            {
                MatchesArrayFilter<string> tagsFilter = null;
                for(int i = 0; i < tagsFilterList.Count && tagsFilter == null; ++i)
                {
                    IRequestFieldFilter f = tagsFilterList[i];
                    if(f.filterMethod == FieldFilterMethod.EquivalentCollection)
                    {
                        tagsFilter = f as MatchesArrayFilter<string>;
                    }
                }

                if(tagsFilter != null)
                {
                    tags = tagsFilter.filterValue;
                }
            }

            this.container.DisplayTags(tags);
        }

        /// <summary>Helper function for removing a tag from the RequestFilter.</summary>
        public void RemoveTagFromExplorerFilter(TagContainerItem tagItem)
        {
            if(this.m_view != null && tagItem != null)
            {
                this.m_view.RemoveTagFromFilter(tagItem.tagName.text);
            }
        }
    }
}
