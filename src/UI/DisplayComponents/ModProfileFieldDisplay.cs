using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component used to display a field of a mod profile in text.</summary>
    public class ModProfileFieldDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ NESTED DATA-TYPE ]---------
        public enum ValueFormatting
        {
            None,
            ByteCount,
            TimeStampAsDate,
            AbbreviatedNumber,
            Percentage,
        }

        // ---------[ FIELDS ]---------
        /// <summary>ModProfile field to display.</summary>
        [FieldValueGetter.DropdownDisplay(typeof(ModProfile), displayArrays = false, displayNested = true)]
        public FieldValueGetter fieldGetter = new FieldValueGetter("id");

        /// <summary>Formatting to apply to the object value.</summary>
        public ValueFormatting formatting = ValueFormatting.None;

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Currently displayed ModProfile object.</summary>
        private ModProfile m_profile = null;

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

        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
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
        public void DisplayProfile(ModProfile profile)
        {
            this.m_profile = profile;

            // display
            object fieldValue = this.fieldGetter.GetValue(this.m_profile);
            string displayString = string.Empty;
            if(fieldValue != null)
            {
                switch(this.formatting)
                {
                    case ValueFormatting.ByteCount:
                    {
                        displayString = UIUtilities.ByteCountToDisplayString((Int64)fieldValue);
                    }
                    break;

                    case ValueFormatting.TimeStampAsDate:
                    {
                        displayString = ServerTimeStamp.ToLocalDateTime((int)fieldValue).ToString();
                    }
                    break;

                    case ValueFormatting.AbbreviatedNumber:
                    {
                        displayString = UIUtilities.ValueToDisplayString((int)fieldValue);
                    }
                    break;

                    case ValueFormatting.Percentage:
                    {
                        displayString = ((float)fieldValue * 100.0f).ToString("0.0") + "%";
                    }
                    break;

                    default:
                    {
                        displayString = fieldValue.ToString();
                    }
                    break;
                }
            }

            this.m_textComponent.text = displayString;
        }
    }
}
