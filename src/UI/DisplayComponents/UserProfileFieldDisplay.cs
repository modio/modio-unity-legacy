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
        [SerializeField]
        private string m_fieldName = "id";

        /// <summary>Delegate for acquiring the display string from the UserProfile.</summary>
        private Func<UserProfile, string> m_getProfileFieldValue = null;

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent UserView.</summary>
        private UserView m_view = null;


        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            this.m_getProfileFieldValue = this.GenerateGetDisplayStringDelegate();
            Component textDisplayComponent = GenericTextComponent.FindCompatibleTextComponent(this.gameObject);
            this.m_textComponent.SetTextDisplayComponent(textDisplayComponent);

            #if DEBUG
            if(this.m_getProfileFieldValue == null)
            {
                Debug.LogError("[mod.io] UserProfileFieldDisplay is unable to display the field \'"
                               + this.m_fieldName + "\' as it does not appear in the UserProfile"
                               + " object definition.",
                               this);
            }
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

        protected virtual void Start()
        {
            if(this.m_view != null)
            {
                this.DisplayProfile(this.m_view.profile);
            }
            else
            {
                this.DisplayProfile(null);
            }
        }

        // --- DELEGATE GENERATION ---
        protected virtual Func<UserProfile, string> GenerateGetDisplayStringDelegate()
        {
            foreach(var fieldInfo in typeof(UserProfile).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if(fieldInfo.Name.Equals(this.m_fieldName))
                {
                    if(fieldInfo.FieldType.IsValueType)
                    {
                        return (p) => UserProfileFieldDisplay.GetProfileFieldValueString_ValueType(p, fieldInfo);
                    }
                    else
                    {
                        return (p) => UserProfileFieldDisplay.GetProfileFieldValueString_Nullable(p, fieldInfo);
                    }
                }
            }

            return null;
        }

        // --- IUserViewElement Interface ---
        /// <summary>IUserViewElement interface.</summary>
        public void SetUserView(UserView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged -= DisplayProfile;
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged += DisplayProfile;
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
            // early out
            if(this.m_getProfileFieldValue == null) { return; }

            // display
            string displayString = this.m_getProfileFieldValue(profile);
            this.m_textComponent.text = displayString;
        }

        // ---------[ UTILITY ]---------
        protected static string GetProfileFieldValueString_ValueType(UserProfile profile, FieldInfo fieldInfo)
        {
            Debug.Assert(fieldInfo != null);

            if(profile == null)
            {
                return string.Empty;
            }
            else
            {
                return fieldInfo.GetValue(profile).ToString();
            }
        }

        protected static string GetProfileFieldValueString_Nullable(UserProfile profile, FieldInfo fieldInfo)
        {
            Debug.Assert(fieldInfo != null);

            if(profile == null)
            {
                return string.Empty;
            }
            else
            {
                var fieldValue = fieldInfo.GetValue(profile);
                if(fieldValue == null)
                {
                    return string.Empty;
                }
                else
                {
                    return fieldValue.ToString();
                }
            }
        }
    }
}
