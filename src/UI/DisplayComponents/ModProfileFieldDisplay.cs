using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component used to display a field of a mod profile in text.</summary>
    public class ModProfileFieldDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>ModProfile field to display.</summary>
        [SerializeField]
        private string m_fieldName = "id";

        /// <summary>Delegate for acquiring the display string from the ModProfile.</summary>
        private Func<ModProfile, string> m_getDisplayStringDelegate = null;

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent;

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;


        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            this.m_getDisplayStringDelegate = this.GenerateGetDisplayStringDelegate();
            UnityEngine.Object textDisplayComponent = GenericTextComponent.FindCompatibleTextComponent(this.gameObject);
            this.m_textComponent.SetTextDisplayComponent(textDisplayComponent);

            #if DEBUG
            if(this.m_getDisplayStringDelegate == null)
            {
                Debug.LogError("[mod.io] ModProfileFieldDisplay is unable to display the field \'"
                               + this.m_fieldName + "\' as it does not appear in the ModProfile"
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
        protected virtual Func<ModProfile, string> GenerateGetDisplayStringDelegate()
        {
            foreach(var fieldInfo in typeof(ModProfile).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if(fieldInfo.Name.Equals(this.m_fieldName))
                {
                    if(fieldInfo.FieldType.IsValueType)
                    {
                        return (p) => ModProfileFieldDisplay.GetStringForProfileField_ValueType(p, fieldInfo);
                    }
                    else
                    {
                        return (p) => ModProfileFieldDisplay.GetStringForProfileField_Nullable(p, fieldInfo);
                    }
                }
            }

            return null;
        }

        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
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
        /// <summary>Displays tags of a profile.</summary>
        public void DisplayProfile(ModProfile profile)
        {
            // early out
            if(this.m_getDisplayStringDelegate == null) { return; }

            // display
            string displayString = this.m_getDisplayStringDelegate(profile);
            this.m_textComponent.text = displayString;
        }

        // ---------[ UTILITY ]---------
        protected static string GetStringForProfileField_ValueType(ModProfile profile, FieldInfo fieldInfo)
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

        protected static string GetStringForProfileField_Nullable(ModProfile profile, FieldInfo fieldInfo)
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
