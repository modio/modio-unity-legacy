using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Component used to display a field of a mod statistics in text.</summary>
    public class ModStatisticsFieldDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>ModStatistics field to display.</summary>
        [MemberReference.DropdownDisplay(typeof(ModStatistics), displayEnumerables = false,
                                         displayNested = true)]
        public MemberReference reference = new MemberReference("modId");

        /// <summary>Formatting to apply to the object value.</summary>
        public ValueFormatting formatting = new ValueFormatting();

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Currently displayed ModStatistics object.</summary>
        private ModStatistics m_statistics = null;

        // ---------[ INITIALIZATION ]---------
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

        protected virtual void OnEnable()
        {
            this.DisplayStatistics(this.m_statistics);
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
            object fieldValue = this.reference.GetValue(this.m_statistics);
            string displayString = ValueFormatting.FormatValue(fieldValue, this.formatting.method,
                                                               this.formatting.toStringParameter);

            this.m_textComponent.text = displayString;
        }
    }
}
