using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Controls the options for a dropdown based on request sort options.</summary>
    [RequireComponent(typeof(Dropdown))]
    public class ExplorerDateLiveDropdownController : MonoBehaviour
    {
        // ---------[ NESTED DATA ]---------
        /// <summary>The data that the controller uses to create dropdown options.</summary>
        [System.Serializable]
        public class OptionData
        {
            /// <summary>Text to use as the dropdown option.</summary>
            public string displayText = "All-time";

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
        public OptionData[] options = new OptionData[1]
        {
            new OptionData(),
        };

        // --- ACCESSORS ---
        /// <summary>The Dropdown component to be controlled.</summary>
        public Dropdown dropdown
        { get { return this.gameObject.GetComponent<Dropdown>(); }}

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
                    this.dropdown.value = 0;
                };

                this.dropdown.onValueChanged.AddListener((v) => UpdateViewFilter());
                UpdateViewFilter();
            }
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Sets the filter value on the targetted view.</summary>
        public void UpdateViewFilter()
        {
            if(this.view == null) { return; }

            // get selected option
            OptionData option = GetSelectedOption();

            // calc
            int fromTimeStamp = -1;
            if(option != null)
            {
                int now = ServerTimeStamp.Now;
                if(option.filterPeriodSeconds > 0)
                {
                    fromTimeStamp = now - option.filterPeriodSeconds;

                    int roundingValue = fromTimeStamp % option.filterRoundingSeconds;
                    fromTimeStamp -= roundingValue;
                }
            }

            // set
            view.SetDateLiveFilter(fromTimeStamp);
        }

        /// <summary>Returns that sort by data for the currently selected dropdown option.</summary>
        public OptionData GetSelectedOption()
        {
            if(this.options != null
               && this.options.Length > 0
               && this.dropdown.options != null
               && this.dropdown.value < this.dropdown.options.Count)
            {
                Dropdown.OptionData selection = this.dropdown.options[this.dropdown.value];

                foreach(var option in this.options)
                {
                    if(option.displayText == selection.text)
                    {
                        return option;
                    }
                }
            }
            return null;
        }

        // ---------[ EVENTS ]---------
        #if UNITY_EDITOR
        // BUG(@jackson): There's something that needs to be done here with serialization
        // potentially - the dropdown seems to load the option data late?
        /// <summary>Fills the Dropdown options with the supplied data.</summary>
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this == null || this.dropdown == null) { return; }

                var so = new UnityEditor.SerializedObject(this.dropdown);
                var optionsProperty = so.FindProperty("m_Options.m_Options");

                optionsProperty.arraySize = this.options.Length;
                for(int i = 0;
                    i < this.options.Length;
                    ++i)
                {
                    optionsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Text").stringValue
                        = this.options[i].displayText;
                }

                so.ApplyModifiedProperties();
            };
        }
        #endif
    }
}
