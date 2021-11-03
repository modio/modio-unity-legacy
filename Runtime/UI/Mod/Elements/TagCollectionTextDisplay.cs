using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays a collection of tags single text component.</summary>
    public class TagCollectionTextDisplay : MonoBehaviour,
                                            IModViewElement,
                                            IGameProfileUpdateReceiver
    {
        // ---------[ FIELDS ]---------
        /// <summary>Should the category be included in the tag displays?</summary>
        public bool includeCategory = false;

        /// <summary>String that separates individual tags</summary>
        public string tagSeparator = ", ";

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Tags to display.</summary>
        private string[] m_tags = new string[0];

        /// <summary>Tag-category mapping.</summary>
        private Dictionary<string, string> m_tagCategoryMap = new Dictionary<string, string>();

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialize text component.</summary>
        protected virtual void Awake()
        {
            Component textDisplayComponent =
                GenericTextComponent.FindCompatibleTextComponent(this.gameObject);
            this.m_textComponent.SetTextDisplayComponent(textDisplayComponent);

#if DEBUG
            if(textDisplayComponent == null)
            {
                Debug.LogWarning("[mod.io] No compatible text components were found on this "
                                     + "GameObject to set text for."
                                     + "\nCompatible with any component that exposes a"
                                     + " publicly settable \'.text\' property.",
                                 this);
            }
#endif
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
            if(tags == null)
            {
                tags = new string[0];
            }

            // copy tags
            List<string> newTagList = new List<string>();
            foreach(string tagName in tags) { newTagList.Add(tagName); }
            this.m_tags = newTagList.ToArray();

            // display
            if(this.isActiveAndEnabled)
            {
                string displayString = string.Empty;

                if(this.m_tags.Length > 0)
                {
                    StringBuilder builder = new StringBuilder();
                    List<string> tagDisplayStrings = new List<string>(this.m_tags);

                    // append categories?
                    if(this.includeCategory && this.m_tagCategoryMap.Count > 0)
                    {
                        for(int i = 0; i < tagDisplayStrings.Count; ++i)
                        {
                            string tagName = tagDisplayStrings[i];
                            string categoryName;
                            if(this.m_tagCategoryMap.TryGetValue(tagName, out categoryName))
                            {
                                tagDisplayStrings[i] = categoryName + ":" + tagName;
                            }
                        }
                    }

                    // build string
                    foreach(string tagString in tagDisplayStrings)
                    {
                        builder.Append(tagString + tagSeparator);
                    }
                    if(builder.Length > 0)
                    {
                        builder.Length -= tagSeparator.Length;
                    }
                    displayString = builder.ToString();
                }

                this.m_textComponent.text = displayString;
            }
        }

        // ---------[ EVENTS ]---------
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
    }
}
