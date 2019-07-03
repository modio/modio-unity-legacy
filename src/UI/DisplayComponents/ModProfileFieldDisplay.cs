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

        /// <summary>Delegate for acquiring the display string.</summary>
        private Func<ModProfile, string> m_getDisplayString = null;

        /// <summary>Action for setting the text value.</summary>
        private Action<string> m_setTextDelegate = null;

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;


        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            this.m_getDisplayString = this.GenerateGetDisplayStringDelegate();
            this.m_setTextDelegate = this.GenerateSetTextDelegate();

            #if DEBUG
            if(this.m_getDisplayString == null)
            {
                Debug.LogError("[mod.io] ModProfileFieldDisplay is unable to display the field \'"
                               + this.m_fieldName + "\' as it does not appear in the ModProfile"
                               + " object definition.",
                               this);
            }
            if(this.m_setTextDelegate == null)
            {
                Debug.LogWarning("[mod.io] No compatible text components were found on this "
                                 + "GameObject to set text for."
                                 + "\nCompatible components are UnityEngine.UI.Text and "
                                 + "UnityEngine.Text.",
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

        protected virtual Action<string> GenerateSetTextDelegate()
        {
            Text textComponent = this.gameObject.GetComponent<Text>();
            if(textComponent != null)
            {
                return (s) => textComponent.text = s;
            }

            TextMesh textMeshComponent = this.gameObject.GetComponent<TextMesh>();
            if(textMeshComponent != null)
            {
                return (s) => textMeshComponent.text = s;
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
            if(this.m_getDisplayString == null || this.m_setTextDelegate == null) { return; }

            // display
            string displayString = this.m_getDisplayString(profile);
            this.m_setTextDelegate(displayString);
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
