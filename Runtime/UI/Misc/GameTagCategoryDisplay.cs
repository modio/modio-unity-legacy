using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays all the Mod Tag Categories in the GameProfile.</summary>
    public class GameTagCategoryDisplay : MonoBehaviour, IGameProfileUpdateReceiver
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Template data.</summary>
        [System.Serializable]
        public struct TemplateData
        {
            public RectTransform root;
            public GenericTextComponent categoryLabel;
            public TagContainerItem tagTemplate;
        }

        /// <summary>Display Item Data.</summary>
        private class CategoryItem : MonoBehaviour
        {
            public GenericTextComponent label;
            public RectTransform tagContainer;
            public TagContainerItem[] tagInstances;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Event fired when the tags have been updated.</summary>
        public event Action<IEnumerable<TagContainerItem>> onTagsChanged = null;

        /// <summary>Template data.</summary>
        public TemplateData template = new TemplateData();

        /// <summary>Should hidden categories be displayed?</summary>
        [Tooltip("Should hidden categories be displayed?")]
        public bool displayHidden = false;

        // --- Run-Time Data ---
        /// <summary>The component version of the template.</summary>
        private CategoryItem m_itemTemplate = null;

        /// <summary>Container for the display objects.</summary>
        private RectTransform m_container = null;

        /// <summary>Display objects.</summary>
        private CategoryItem[] m_itemInstances = new CategoryItem[0];

        /// <summary>Display data.</summary>
        private ModTagCategory[] m_tagCategories = new ModTagCategory[0];

        // --- Accessors ---
        /// <summary>Tag Display Items.</summary>
        public IEnumerable<TagContainerItem> tagItems
        {
            get {
                foreach(CategoryItem c in this.m_itemInstances)
                {
                    foreach(TagContainerItem t in c.tagInstances) { yield return t; }
                }
            }
        }

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialize template.</summary>
        protected virtual void Awake()
        {
// check template
#if DEBUG
            string message;
            if(!GameTagCategoryDisplay.HasValidTemplate(this, out message))
            {
                Debug.LogError("[mod.io] " + message, this);
                return;
            }
#endif

            // Hide template
            this.template.root.gameObject.SetActive(false);

            // Check for category name duplication
            if(this.template.tagTemplate.categoryName.displayComponent
               == this.template.categoryLabel.displayComponent)
            {
                this.template.tagTemplate.categoryName.SetTextDisplayComponent(null);
            }

            // Add item component
            this.m_itemTemplate = this.template.root.gameObject.GetComponent<CategoryItem>();
            if(this.m_itemTemplate == null)
            {
                this.m_itemTemplate = this.template.root.gameObject.AddComponent<CategoryItem>();
                this.m_itemTemplate.label = this.template.categoryLabel;
                this.m_itemTemplate.tagContainer =
                    this.template.tagTemplate.transform.parent as RectTransform;
                this.m_itemTemplate.tagInstances = new TagContainerItem[1] {
                    this.template.tagTemplate,
                };
            }

            // Collect vars
            this.m_container = this.template.root.parent as RectTransform;

            // Clear any instantiated categories
            List<CategoryItem> itemInstances = new List<CategoryItem>(
                this.m_container.GetComponentsInChildren<CategoryItem>(true));
            itemInstances.Remove(this.m_itemTemplate);

            foreach(CategoryItem item in itemInstances) { GameObject.Destroy(item.gameObject); }
        }

        /// <summary>Ensure the displays are accurate.</summary>
        protected virtual void OnEnable()
        {
            this.DisplayTagCategories(this.m_tagCategories);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays a collection of tag categories.</summary>
        public void DisplayTagCategories(IEnumerable<ModTagCategory> tagCategories)
        {
            // copy categories
            if(this.m_tagCategories != tagCategories)
            {
                if(tagCategories == null)
                {
                    tagCategories = new ModTagCategory[0];
                }

                // copy categories
                List<ModTagCategory> categories = new List<ModTagCategory>();
                foreach(ModTagCategory category in tagCategories)
                {
                    if(category != null && category.tags != null && category.tags.Length > 0
                       && (displayHidden || !category.isHidden))
                    {
                        categories.Add(category);
                    }
                }
                this.m_tagCategories = categories.ToArray();
            }

            // display
            if(this.isActiveAndEnabled)
            {
                int categoryCount = this.m_tagCategories.Length;

                // create/destroy category displays
                UIUtilities.SetInstanceCount(this.m_container, this.m_itemTemplate, "Tag Category",
                                             categoryCount, ref this.m_itemInstances);

                // set category labels
                if(this.template.categoryLabel.displayComponent != null)
                {
                    for(int i = 0; i < categoryCount; ++i)
                    {
                        this.m_itemInstances[i].label.text = this.m_tagCategories[i].name;
                    }
                }

                // tag displays
                for(int cat_i = 0; cat_i < categoryCount; ++cat_i)
                {
                    ModTagCategory category = this.m_tagCategories[cat_i];
                    CategoryItem categoryItem = this.m_itemInstances[cat_i];

                    // create/destroy
                    UIUtilities.SetInstanceCount(
                        categoryItem.tagContainer, this.template.tagTemplate, "Tag",
                        category.tags.Length, ref categoryItem.tagInstances);

                    // set category name
                    if(this.template.tagTemplate.categoryName.displayComponent != null)
                    {
                        for(int tag_i = 0; tag_i < category.tags.Length; ++tag_i)
                        {
                            categoryItem.tagInstances[tag_i].categoryName.text = category.name;
                        }
                    }

                    // set tag name
                    for(int tag_i = 0; tag_i < category.tags.Length; ++tag_i)
                    {
                        categoryItem.tagInstances[tag_i].tagName.text = category.tags[tag_i];
                    }
                }

                // fire event
                if(this.onTagsChanged != null)
                {
                    this.onTagsChanged(this.tagItems);
                }
            }
        }

        // ---------[ EVENTS ]---------
        /// <summary>Updates the tag categories.</summary>
        public void OnGameProfileUpdated(GameProfile gameProfile)
        {
            // refresh
            this.DisplayTagCategories(gameProfile.tagCategories);
        }

        // ---------[ UTILITY ]---------
        /// <summary>Checks a display's template structure.</summary>
        public static bool HasValidTemplate(GameTagCategoryDisplay display, out string helpMessage)
        {
            helpMessage = null;
            bool isValid = true;

            // null check
            if(display.template.root == null)
            {
                helpMessage = ("This Game Tag Category Display has an invalid template."
                               + "\nThe template root is not assigned.");
                isValid = false;
            }
            // null check
            else if(display.template.tagTemplate == null)
            {
                helpMessage = ("This Game Tag Category Display has an invalid template."
                               + "\nThe tag template is not assigned.");
                isValid = false;
            }
            // template.root is child of Component
            else if(!display.template.root.IsChildOf(display.transform)
                    || display.template.root == display.transform)
            {
                helpMessage = ("This Game Tag Category Display has an invalid template."
                               + "\nThe template root must be a child of this object.");
                isValid = false;
            }
            // template.category is child of, or attached to template.root
            else if(display.template.categoryLabel.displayComponent != null
                    && !display.template.categoryLabel.displayComponent.transform.IsChildOf(
                        display.template.root)
                    && display.template.categoryLabel.displayComponent.transform
                           != display.template.root)
            {
                helpMessage = ("This Game Tag Category Display has an invalid template."
                               + "\nThe category label must be a child of, or attached to"
                               + " the template root.");
                isValid = false;
            }
            // template.tag is child of template.root
            else if(!display.template.tagTemplate.transform.IsChildOf(display.template.root)
                    || display.template.tagTemplate.transform == display.template.root)
            {
                helpMessage = ("This Game Tag Category Display has an invalid template."
                               + "\nThe tag template must be a child of the template root.");
                isValid = false;
            }
            // template.category is not in template.tag hierarchy
            else if(display.template.categoryLabel.displayComponent != null
                    && (display.template.categoryLabel.displayComponent.transform.IsChildOf(
                            display.template.tagTemplate.transform)
                        || display.template.categoryLabel.displayComponent.transform
                               == display.template.tagTemplate.transform))
            {
                helpMessage = ("This Game Tag Category Display has an invalid template."
                               + "\nThe category label cannot be a child of, or attached to the"
                               + " same transform as the tag template.");
                isValid = false;
            }

            return isValid;
        }
    }
}
