using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Displays a collection of tags as a series of text components.</summary>
    public class TagContainer : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying tags.</summary>
        public RectTransform template = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = true;

        // --- Run-Time Data ---
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Instance of the template clone.</summary>
        private GameObject m_templateClone = null;

        /// <summary>Display object template.</summary>
        private TagContainerItem m_itemTemplate = null;

        /// <summary>Container for the display objects.</summary>
        private RectTransform m_container = null;

        /// <summary>Gallery Image Locators to display.</summary>
        private string[] m_tags = new string[0];

        /// <summary>Display objects.</summary>
        private TagContainerItem[] m_displays = new TagContainerItem[0];

        /// <summary>Tag-category mapping.</summary>
        private Dictionary<string, string> m_tagCategoryMap = new Dictionary<string, string>();

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialize template.</summary>
        protected virtual void Awake()
        {
            this.template.gameObject.SetActive(false);
            this.m_itemTemplate = this.template.GetComponentInChildren<TagContainerItem>(true);

            if(this.m_itemTemplate != null
               && this.template.gameObject != this.m_itemTemplate.gameObject)
            {
                this.m_templateClone = GameObject.Instantiate(this.template.gameObject, this.template.parent);
                this.m_templateClone.SetActive(true);
                this.m_templateClone.transform.SetSiblingIndex(this.template.GetSiblingIndex() + 1);

                this.m_displays = new TagContainerItem[1];
                this.m_displays[0] = this.m_templateClone.GetComponentInChildren<TagContainerItem>(true);

                this.m_container = (RectTransform)this.m_displays[0].transform.parent;
            }
            else
            {
                Debug.LogError("[mod.io] This Tag Container has an invalid template"
                               + " hierarchy. The Template must container a child with a"
                               + " Tag Container Item component to use as the item template.",
                               this);
            }
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
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged -= DisplayProfileTags;
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged += DisplayProfileTags;
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
                foreach(string tagName in tags)
                {
                    newTagList.Add(tagName);
                }
                this.m_tags = newTagList.ToArray();
            }

            // display
            if(this.isActiveAndEnabled)
            {
                int tagCount = this.m_tags.Length;
                this.SetDisplayCount(tagCount);

                // display categories?
                if(m_itemTemplate.categoryName.displayComponent != null
                   && this.m_tagCategoryMap.Count > 0)
                {
                    for(int i = 0;
                        i < tagCount;
                        ++i)
                    {
                        string categoryName;
                        if(this.m_tagCategoryMap.TryGetValue(this.m_tags[i], out categoryName))
                        {
                            this.m_displays[i].categoryName.text = categoryName;
                        }
                    }
                }

                // display tag names
                for(int i = 0;
                    i < tagCount;
                    ++i)
                {
                    this.m_displays[i].tagName.text = this.m_tags[i];
                }

                this.m_templateClone.SetActive(tagCount > 0 || !this.hideIfEmpty);
            }
        }

        /// <summary>Creates/Destroys display objects to match the given value.</summary>
        protected virtual void SetDisplayCount(int newCount)
        {
            int difference = newCount - this.m_displays.Length;

            if(difference > 0)
            {
                TagContainerItem[] newDisplayArray = new TagContainerItem[newCount];

                for(int i = 0;
                    i < this.m_displays.Length;
                    ++i)
                {
                    newDisplayArray[i] = this.m_displays[i];
                }

                for(int i = this.m_displays.Length;
                    i < newDisplayArray.Length;
                    ++i)
                {
                    GameObject displayGO = GameObject.Instantiate(this.m_itemTemplate.gameObject);
                    displayGO.name = "Tag Container Item [" + i.ToString("00") + "]";
                    displayGO.transform.SetParent(this.m_container, false);

                    newDisplayArray[i] = displayGO.GetComponent<TagContainerItem>();
                }

                this.m_displays = newDisplayArray;
            }
            else if(difference < 0)
            {
                TagContainerItem[] newDisplayArray = new TagContainerItem[newCount];

                for(int i = 0;
                    i < newDisplayArray.Length;
                    ++i)
                {
                    newDisplayArray[i] = this.m_displays[i];
                }

                for(int i = newDisplayArray.Length;
                    i < this.m_displays.Length;
                    ++i)
                {
                    GameObject.Destroy(this.m_displays[i].gameObject);
                }

                this.m_displays = newDisplayArray;
            }
        }


        // ---------[ EVENTS ]---------
        // TODO(@jackson): Seems silly that this is a local var...
        /// <summary>Updates the tag categories.</summary>
        public void OnGameProfileUpdated(GameProfile gameProfile)
        {
            // get length
            int categoryCount = 0;
            if(gameProfile != null
               && gameProfile.tagCategories != null)
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

        // public void OnEnable()
        // {
        //     StartCoroutine(LateUpdateLayouting());
        // }

        // public System.Collections.IEnumerator LateUpdateLayouting()
        // {
        //     yield return null;
        //     UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(container);
        // }

    }
}
