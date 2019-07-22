using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Displays a collection of tags single text component.</summary>
    public class TagCollectionTextDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>String that separates individual tags</summary>
        public string tagSeparator = ", ";

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            Component textDisplayComponent = GenericTextComponent.FindCompatibleTextComponent(this.gameObject);
            this.m_textComponent.SetTextDisplayComponent(textDisplayComponent);

            #if DEBUG
            if(textDisplayComponent == null)
            {
                Debug.LogWarning("[mod.io] No compatible text components were found on this "
                                 + "GameObject to set text for."
                                 + "\nCompatible components are UnityEngine.UI.Text, "
                                 + "UnityEngine.TextMesh, and components derived from TMPro.TMP_Text.",
                                 this);
            }
            #endif
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
            string displayString = string.Empty;

            if(tags != null)
            {
                StringBuilder builder = new StringBuilder();
                foreach(string tagName in tags)
                {
                    builder.Append(tagName + tagSeparator);
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
}
