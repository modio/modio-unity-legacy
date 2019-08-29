using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component used to display a field of a user profile in text.</summary>
    public class UserProfileFieldDisplay : MonoBehaviour, IUserViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>UserProfile field to display.</summary>
        [FieldValueGetter.DropdownDisplay(typeof(UserProfile), displayArrays = false, displayNested = true)]
        public FieldValueGetter fieldGetter = new FieldValueGetter("id");

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent UserView.</summary>
        private UserView m_view = null;

        /// <summary>Currently displayed UserProfile object.</summary>
        private UserProfile m_profile = null;

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
                                 + "\nCompatible with any component that exposes a"
                                 + " publicly settable \'.text\' property.",
                                 this);
            }
            #endif
        }

        protected virtual void OnEnable()
        {
            this.DisplayProfile(this.m_profile);
        }

        /// <summary>IUserViewElement interface.</summary>
        public void SetUserView(UserView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayProfile);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayProfile);
                this.DisplayProfile(this.m_view.profile);
            }
            else
            {
                this.DisplayProfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the appropriate field of a given profile.</summary>
        public void DisplayProfile(UserProfile profile)
        {
            this.m_profile = profile;

            // display
            object fieldValue = this.fieldGetter.GetValue(this.m_profile);
            string displayString = string.Empty;
            if(fieldValue != null)
            {
                displayString = fieldValue.ToString();
            }

            this.m_textComponent.text = displayString;
        }
    }
}
