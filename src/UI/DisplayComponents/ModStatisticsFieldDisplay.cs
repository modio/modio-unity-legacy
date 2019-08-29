using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component used to display a field of a mod statistics in text.</summary>
    public class ModStatisticsFieldDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>ModStatistics field to display.</summary>
        [FieldValueGetter.DropdownDisplay(typeof(ModStatistics), displayArrays = false, displayNested = true)]
        public FieldValueGetter fieldGetter = new FieldValueGetter("modId");

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Currently displayed ModStatistics object.</summary>
        private ModStatistics m_statistics = null;

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
            this.DisplayStatistics(this.m_statistics);
        }

        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onStatisticsChanged.RemoveListener(DisplayStatistics);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onStatisticsChanged.AddListener(DisplayStatistics);
                this.DisplayStatistics(this.m_view.statistics);
            }
            else
            {
                this.DisplayStatistics(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the appropriate field of a given statistics.</summary>
        public void DisplayStatistics(ModStatistics statistics)
        {
            this.m_statistics = statistics;

            // display
            object fieldValue = this.fieldGetter.GetValue(this.m_statistics);
            string displayString = string.Empty;
            if(fieldValue != null)
            {
                displayString = fieldValue.ToString();
            }

            this.m_textComponent.text = displayString;
        }
    }
}
