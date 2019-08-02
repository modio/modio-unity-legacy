using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>A component that pairs an input field with the title filter of a ExplorerView.</summary>
    [RequireComponent(typeof(InputField))]
    public class TitleFilterInputField : MonoBehaviour, IExplorerViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Parent ExplorerView.</summary>
        private ExplorerView m_view = null;

        // ---------[ INITIALIZATION ]---------
        /// <summary>IExplorerViewElement interface.</summary>
        public void SetExplorerView(ExplorerView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onRequestFilterChanged.RemoveListener(UpdateInputField);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onRequestFilterChanged.AddListener(UpdateInputField);
                this.UpdateInputField(this.m_view.requestFilter);
            }
            else
            {
                this.UpdateInputField(string.Empty);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Attach event listeners to the input field.</summary>
        protected virtual void Start()
        {
            this.GetComponent<InputField>().onEndEdit.AddListener(SetTitleFilter);
        }

        /// <summary>Sets the value of the input field based on the value in the RequestFilter.</summary>
        public virtual void UpdateInputField(RequestFilter requestFilter)
        {
            string filterValue = string.Empty;
            IRequestFieldFilter fieldFilter;
            if(requestFilter != null
               && requestFilter.fieldFilters.TryGetValue(ModIO.API.GetAllModsFilterFields.fullTextSearch, out fieldFilter))
            {
                EqualToFilter<string> likeFilter = fieldFilter as EqualToFilter<string>;
                if(likeFilter != null)
                {
                    filterValue = likeFilter.filterValue;
                }
            }

            this.UpdateInputField(filterValue);
        }

        /// <summary>Sets the value of the input field.</summary>
        public virtual void UpdateInputField(string filterValue)
        {
            this.gameObject.GetComponent<InputField>().text = filterValue;
        }

        /// <summary>Sets the filter value in the ExplorerView.</summary>
        protected virtual void SetTitleFilter(string newValue)
        {
            if(this.m_view != null)
            {
                this.m_view.SetTitleFilter(newValue);
            }
        }
    }
}
