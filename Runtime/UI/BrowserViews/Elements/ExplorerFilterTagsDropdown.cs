using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Displays and allows selection of the tags being filtered on in the
    /// ExplorerView.</summary>
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(GameTagCategoryDisplay))]
    public class ExplorerFilterTagsDropdown : MonoBehaviour,
                                              IExplorerViewElement,
                                              UnityEngine.EventSystems.ICancelHandler
    {
        // ---------[ FIELDS ]---------
        /// <summary>Popup view element</summary>
        public GameObject popup = null;

        // --- Run-Time Data ---
        /// <summary>ExplorerView to set the tagFilter on.</summary>
        private ExplorerView m_view = null;

        /// <summary>Selected tags.</summary>
        private string[] m_selectedTags = new string[0];

        /// <summary>Are the tag states currently being updated?</summary>
        private bool m_isUpdating = false;

        // --- Accessors ---
        /// <summary>Display that this component controls.</summary>
        public GameTagCategoryDisplay categoryDisplay
        {
            get {
                return this.gameObject.GetComponent<GameTagCategoryDisplay>();
            }
        }

        // ---------[ INITIALIZATION ]---------
        /// <summary>Set up the template.</summary>
        private void Awake()
        {
// check template
#if DEBUG
            string message;
            if(!ExplorerFilterTagsDropdown.HasValidTemplate(this, out message))
            {
                Debug.LogError("[mod.io] " + message, this);
                return;
            }
#endif

            this.categoryDisplay.onTagsChanged += (t) =>
                this.UpdateSelectedTagsDisplay(this.m_selectedTags);
        }

        /// <summary>Assert that the display is correct.</summary>
        private void OnEnable()
        {
            this.UpdateSelectedTagsDisplay(this.m_selectedTags);
        }

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
        public void DisplayInArrayFilterTags(RequestFilter requestFilter)
        {
            List<IRequestFieldFilter> filters = null;
            requestFilter.fieldFilterMap.TryGetValue(ModIO.API.GetAllModsFilterFields.tags,
                                                     out filters);

            if(filters != null && filters.Count > 0)
            {
                foreach(IRequestFieldFilter fieldFilter in filters)
                {
                    if(fieldFilter != null
                       && fieldFilter.filterMethod == FieldFilterMethod.EquivalentCollection)
                    {
                        this.UpdateSelectedTagsDisplay(
                            fieldFilter.filterValue as IEnumerable<string>);
                        return;
                    }
                }
            }
            else
            {
                this.UpdateSelectedTagsDisplay(null);
            }
        }

        /// <summary>Updates the selected status using the tag collection.</summary>
        public void UpdateSelectedTagsDisplay(IEnumerable<string> selectedTags)
        {
            // copy array
            if(this.m_selectedTags != selectedTags)
            {
                if(selectedTags == null)
                {
                    selectedTags = new string[0];
                }

                List<string> newSelection = new List<string>();
                foreach(string tag in selectedTags)
                {
                    if(!string.IsNullOrEmpty(tag))
                    {
                        newSelection.Add(tag);
                    }
                }

                this.m_selectedTags = newSelection.ToArray();
            }

            // display
            if(this.isActiveAndEnabled)
            {
                m_isUpdating = true;

                foreach(TagContainerItem tagItem in this.categoryDisplay.tagItems)
                {
                    bool isSelected = false;
                    for(int i = 0; i < this.m_selectedTags.Length && !isSelected; ++i)
                    {
                        isSelected = (this.m_selectedTags[i] == tagItem.tagName.text);
                    }

                    tagItem.GetComponentInChildren<StateToggleDisplay>(true).isOn = isSelected;
                }

                m_isUpdating = false;
            }
        }

        /// <summary>Helper function for removing a tag from the RequestFilter.</summary>
        public void AddTagToExplorerFilter(TagContainerItem tagItem)
        {
            if(this.m_view != null && !this.m_isUpdating)
            {
                this.m_view.AddTagToFilter(tagItem.tagName.text);
            }
        }

        /// <summary>Helper function for removing a tag from the RequestFilter.</summary>
        public void RemoveTagFromExplorerFilter(TagContainerItem tagItem)
        {
            if(this.m_view != null && !this.m_isUpdating)
            {
                this.m_view.RemoveTagFromFilter(tagItem.tagName.text);
            }
        }

        /// <summary>Helper function for toggling tags in the RequestFilter.</summary>
        public void ToggleTagInExplorerFilter(TagContainerItem tagItem)
        {
            if(this.m_view != null && !this.m_isUpdating)
            {
                string tagName = tagItem.tagName.text;
                StateToggleDisplay toggleComponent =
                    tagItem.GetComponentInChildren<StateToggleDisplay>(true);

                if(toggleComponent.isOn)
                {
                    this.m_view.AddTagToFilter(tagName);
                }
                else
                {
                    this.m_view.RemoveTagFromFilter(tagName);
                }
            }
        }

        // ---------[ UTILITY ]---------
        /// <summary>Checks a ModContainer's template structure.</summary>
        public static bool HasValidTemplate(ExplorerFilterTagsDropdown selector,
                                            out string helpMessage)
        {
            helpMessage = null;

            // - null checks -
            if(selector.categoryDisplay == null
               || !GameTagCategoryDisplay.HasValidTemplate(selector.categoryDisplay,
                                                           out helpMessage))
            {
                helpMessage = ("The required GameTagCategoryDisplay is missing or"
                               + " has an invalid template.");
                return false;
            }

            // - Validity checks -
            bool isValid = true;
            TagContainerItem tagItem = selector.categoryDisplay.template.tagTemplate;

            // check for StateToggleDisplay
            if(tagItem.gameObject.GetComponentInChildren<StateToggleDisplay>(true) == null)
            {
                helpMessage = ("This ExplorerFilterTagsDropdown has an invalid template."
                               + "\nThe tag template of the GameTagCategoryDisplay must"
                               + " also have a StateToggleDisplay derived component as"
                               + " a child, or on the same GameObject."
                               + "\n(EG. GameObjectToggle, StateToggle, or SlideToggle.)");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>Closes the popup element.</summary>
        public void OnCancel(UnityEngine.EventSystems.BaseEventData eventData)
        {
            if(popup != null && popup.activeInHierarchy)
            {
                popup.SetActive(false);
                this.gameObject.GetComponent<Selectable>().Select();
            }
        }
    }
}
