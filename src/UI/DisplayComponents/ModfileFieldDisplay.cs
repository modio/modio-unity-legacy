using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component used to display a field of a modfile in text.</summary>
    public class ModfileFieldDisplay : MonoBehaviour, IModfileViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Modfile field to display.</summary>
        [FieldValueGetter.DropdownDisplay(typeof(Modfile), displayArrays = false, displayNested = true)]
        public FieldValueGetter fieldGetter = new FieldValueGetter("id");

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent ModfileView.</summary>
        private ModfileView m_view = null;

        /// <summary>Currently displayed Modfile object.</summary>
        private Modfile m_modfile = null;

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
            this.DisplayModfile(this.m_modfile);
        }

        // --- IModfileViewElement Interface ---
        /// <summary>IModfileViewElement interface.</summary>
        public void SetModfileView(ModfileView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onModfileChanged.RemoveListener(DisplayModfile);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onModfileChanged.AddListener(DisplayModfile);
                this.DisplayModfile(this.m_view.modfile);
            }
            else
            {
                this.DisplayModfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the appropriate field of a given modfile.</summary>
        public void DisplayModfile(Modfile modfile)
        {
            this.m_modfile = modfile;

            // display
            object fieldValue = this.fieldGetter.GetValue(this.m_modfile);
            string displayString = string.Empty;
            if(fieldValue != null)
            {
                displayString = fieldValue.ToString();
            }

            this.m_textComponent.text = displayString;
        }
    }
}
