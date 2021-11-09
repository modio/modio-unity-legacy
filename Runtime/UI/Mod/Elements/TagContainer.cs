using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays a collection of tags as a series of text components.</summary>
    public class TagContainer : MonoBehaviour, IModViewElement, IGameProfileUpdateReceiver
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying tags.</summary>
        public RectTransform containerTemplate = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = false;

        // --- Run-Time Data ---
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Instance of the template clone.</summary>
        private GameObject m_templateClone = null;

        /// <summary>Display object template.</summary>
        private TagContainerItem m_itemTemplate = null;

        /// <summary>Container for the display objects.</summary>
        private RectTransform m_container = null;

        /// <summary>Tags to display.</summary>
        private string[] m_tags = new string[0];

        /// <summary>Display objects.</summary>
        private TagContainerItem[] m_displays = new TagContainerItem[0];

        /// <summary>Tag-category mapping.</summary>
        private Dictionary<string, string> m_tagCategoryMap = new Dictionary<string, string>();

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialize template.</summary>
        protected virtual void Awake()
        {
            this.containerTemplate.gameObject.SetActive(false);

// check template
#if DEBUG
            string message;
            if(!TagContainer.HasValidTemplate(this, out message))
            {
                Debug.LogError("[mod.io] " + message, this);
                return;
            }
#endif

            // get template vars
            Transform templateParent = this.containerTemplate.parent;
            string templateInstance_name = this.containerTemplate.gameObject.name + " (Instance)";
            int templateInstance_index = this.containerTemplate.GetSiblingIndex() + 1;
            this.m_itemTemplate =
                this.containerTemplate.GetComponentInChildren<TagContainerItem>(true);

            // check if instantiated
            bool isInstantiated =
                (templateParent.childCount > templateInstance_index
                 && templateParent.GetChild(templateInstance_index).gameObject.name
                        == templateInstance_name);
            if(isInstantiated)
            {
                this.m_templateClone = templateParent.GetChild(templateInstance_index).gameObject;
                TagContainerItem[] itemInstances =
                    this.m_templateClone.GetComponentsInChildren<TagContainerItem>(true);

                if(itemInstances == null || itemInstances.Length == 0)
                {
                    isInstantiated = false;
                    GameObject.Destroy(this.m_templateClone);
                }
                else
                {
                    this.m_container = (RectTransform)itemInstances[0].transform.parent;

                    foreach(TagContainerItem view in itemInstances)
                    {
                        GameObject.Destroy(view.gameObject);
                    }
                }
            }

            // instantiate
            if(!isInstantiated)
            {
                this.m_templateClone =
                    GameObject.Instantiate(this.containerTemplate.gameObject, templateParent);
                this.m_templateClone.transform.SetSiblingIndex(templateInstance_index);
                this.m_templateClone.name = templateInstance_name;

                TagContainerItem itemInstance =
                    this.m_templateClone.GetComponentInChildren<TagContainerItem>(true);
                this.m_container = (RectTransform)itemInstance.transform.parent;

                GameObject.Destroy(itemInstance.gameObject);

                this.m_templateClone.SetActive(true);
            }

            this.DisplayTags(this.m_tags);
        }

        /// <summary>Ensure the displays are accurate.</summary>
        protected virtual void OnEnable()
        {
            this.DisplayTags(this.m_tags);
        }

        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayProfileTags);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayProfileTags);
                this.DisplayProfileTags(this.m_view.profile);
            }
            else
            {
                this.DisplayProfileTags(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the tags for a given profile.</summary>
        public void DisplayProfileTags(ModProfile profile)
        {
            IEnumerable<string> tags = null;
            if(profile != null)
            {
                tags = profile.tagNames;
            }

            this.DisplayTags(tags);
        }

        /// <summary>Displays a set of tags.</summary>
        public void DisplayTags(IEnumerable<string> tags)
        {
            // copy tags
            if(this.m_tags != tags)
            {
                if(tags == null)
                {
                    tags = new string[0];
                }

                // copy tags
                List<string> newTagList = new List<string>();
                foreach(string tagName in tags) { newTagList.Add(tagName); }
                this.m_tags = newTagList.ToArray();
            }

            // display
            if(this.m_itemTemplate != null)
            {
                int tagCount = this.m_tags.Length;
                UIUtilities.SetInstanceCount(this.m_container, this.m_itemTemplate, "Tag", tagCount,
                                             ref this.m_displays);

                // display categories?
                if(m_itemTemplate.categoryName.displayComponent != null
                   && this.m_tagCategoryMap.Count > 0)
                {
                    for(int i = 0; i < tagCount; ++i)
                    {
                        string categoryName;
                        if(this.m_tagCategoryMap.TryGetValue(this.m_tags[i], out categoryName))
                        {
                            this.m_displays[i].categoryName.text = categoryName;
                        }
                    }
                }

                // display tag names
                for(int i = 0; i < tagCount; ++i)
                {
                    this.m_displays[i].tagName.text = this.m_tags[i];
                }

                this.m_templateClone.SetActive(tagCount > 0 || !this.hideIfEmpty);
            }
        }

        // ---------[ EVENTS ]---------
        // TODO(@jackson): Seems silly that this is a local var...
        /// <summary>Updates the tag categories.</summary>
        public void OnGameProfileUpdated(GameProfile gameProfile)
        {
            // get length
            int categoryCount = 0;
            if(gameProfile != null && gameProfile.tagCategories != null)
            {
                categoryCount = gameProfile.tagCategories.Length;
            }

            // build new map
            this.m_tagCategoryMap = new Dictionary<string, string>(categoryCount);
            for(int i = 0; i < categoryCount; ++i)
            {
                ModTagCategory category = gameProfile.tagCategories[i];
                if(category.tags != null)
                {
                    foreach(string tagName in category.tags)
                    {
                        this.m_tagCategoryMap[tagName] = category.name;
                    }
                }
            }

            // resfresh
            this.DisplayTags(this.m_tags);
        }

        // ---------[ UTILITY ]---------
        /// <summary>Checks a TagContainer's template structure.</summary>
        public static bool HasValidTemplate(TagContainer container, out string helpMessage)
        {
            helpMessage = null;
            bool isValid = true;

            if(container.containerTemplate.gameObject == container.gameObject
               || container.transform.IsChildOf(container.containerTemplate))
            {
                helpMessage = ("This Tag Container has an invalid template."
                               + "\nThe container template cannot share the same GameObject"
                               + " as this Tag Container component, and cannot be a parent of"
                               + " this object.");
                isValid = false;
            }

            TagContainerItem itemTemplate =
                container.containerTemplate.GetComponentInChildren<TagContainerItem>(true);
            if(itemTemplate == null
               || container.containerTemplate.gameObject == itemTemplate.gameObject)
            {
                helpMessage = ("This Tag Container has an invalid template."
                               + "\nThe container template needs a child with the TagContainerItem"
                               + " component attached to use as the item template.");
                isValid = false;
            }

            return isValid;
        }
    }
}
