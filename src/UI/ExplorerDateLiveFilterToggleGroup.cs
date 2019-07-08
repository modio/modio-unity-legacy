using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Controls the options for a dropdown based on request sort options.</summary>
    [RequireComponent(typeof(ToggleGroup))]
    public class ExplorerDateLiveFilterToggleGroup : MonoBehaviour
    {
        // ---------[ NESTED DATA ]---------
        /// <summary>The data that the controller uses to create dropdown options.</summary>
        [System.Serializable]
        public class Option
        {
            /// <summary>Text to use as the dropdown option.</summary>
            public Toggle toggle = null;

            /// <summary>Number of seconds to filter for.</summary>
            public int filterPeriodSeconds = -1;

            /// <summary>Number of seconds to round the filter to.</summary>
            public int filterRoundingSeconds = 0;
        }

        // ---------[ FIELDS ]---------
        /// <summary>ExplorerView to set the sort value for.</summary>
        [HideInInspector]
        public ExplorerView view = null;

        /// <summary>Options for the controller to use.</summary>
        public Option[] options = new Option[0];

        /// <summary>Used to indicate that the toggles are currently clearing.</summary>
        private bool m_isClearing = false;

        // --- ACCESSORS ---
        /// <summary>ToggleGroup this component should refer to.</summary>
        public ToggleGroup toggleGroup
        { get { return this.gameObject.GetComponent<ToggleGroup>(); }}

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            this.view = this.gameObject.GetComponentInParent<ExplorerView>();
            if(this.view == null)
            {
                Debug.LogWarning("[mod.io] Couldn't find an ExplorerView to work with this"
                                 + " component.", this);
            }
            else
            {
                this.view.onFiltersCleared += () =>
                {
                    this.m_isClearing = true;
                    foreach(Option option in this.options)
                    {
                        option.toggle.isOn = false;
                    }
                    this.m_isClearing = false;
                };

                if(this.options != null)
                {
                    foreach(Option option in this.options)
                    {
                        option.toggle.onValueChanged.AddListener((b) => UpdateViewFilter());
                    }
                }

                UpdateViewFilter();
            }
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Sets the filter value on the targetted view.</summary>
        public void UpdateViewFilter()
        {
            if(this.m_isClearing || this.view == null) { return; }

            // get toggle
            Toggle activeToggle = null;
            foreach(Toggle toggle in this.toggleGroup.ActiveToggles())
            {
                activeToggle = toggle;
            }

            // get selected option
            Option selectedOption = null;
            if(activeToggle != null)
            {
                foreach(Option option in this.options)
                {
                    if(option.toggle == activeToggle)
                    {
                        selectedOption = option;
                    }
                }
            }


            // calc
            int fromTimeStamp = -1;
            if(selectedOption != null)
            {
                int now = ServerTimeStamp.Now;
                if(selectedOption.filterPeriodSeconds > 0)
                {
                    fromTimeStamp = now - selectedOption.filterPeriodSeconds;

                    int roundingValue = fromTimeStamp % selectedOption.filterRoundingSeconds;
                    fromTimeStamp -= roundingValue;
                }
            }

            // set
            view.SetDateLiveFilter(fromTimeStamp);
        }
    }
}
