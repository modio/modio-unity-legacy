using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Controls the options for a dropdown based on request date live options.</summary>
    [RequireComponent(typeof(Dropdown))]
    public class ExplorerDateLiveDropdownController : MonoBehaviour, IExplorerViewElement
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
        /// <summary>Options for the controller to use.</summary>
        public OptionData[] options = new OptionData[4] {
            new OptionData() {
                displayText = "All-time",
                filterPeriodSeconds = -1,
                filterRoundingSeconds = 0,
            },
            new OptionData() {
                displayText = "Today",
                filterPeriodSeconds = 86400,
                filterRoundingSeconds = 3600,
            },
            new OptionData() {
                displayText = "This Week",
                filterPeriodSeconds = 604800,
                filterRoundingSeconds = 43200,
            },
            new OptionData() {
                displayText = "This Month",
                filterPeriodSeconds = 2592000,
                filterRoundingSeconds = 86400,
            },
        };

        // --- Run-time Data ---
        /// <summary>ExplorerView to set the sort value for.</summary>
        private ExplorerView m_view = null;

        // --- ACCESSORS ---
        /// <summary>The Dropdown component to be controlled.</summary>
        public Dropdown dropdown
        {
            get {
                return this.gameObject.GetComponent<Dropdown>();
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            this.dropdown.onValueChanged.AddListener((v) => SetExplorerViewDateLiveFilter());
            this.SetExplorerViewDateLiveFilter();
        }

        /// <summary>IExplorerViewElement interface.</summary>
        public void SetExplorerView(ExplorerView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onRequestFilterChanged.RemoveListener(DisplayDateLiveOption);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onRequestFilterChanged.AddListener(DisplayDateLiveOption);
                this.DisplayDateLiveOption(this.m_view.requestFilter);
            }
            else
            {
                this.DisplayDateLiveOption(null);
            }
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Displays the date live selection based on the RequestFilter data.</summary>
        public void DisplayDateLiveOption(RequestFilter filter)
        {
            if(filter == null)
            {
                return;
            }

            // get filter value
            List<IRequestFieldFilter> dateLiveFilterList = null;
            if(filter != null)
            {
                filter.fieldFilterMap.TryGetValue(ModIO.API.GetAllModsFilterFields.dateLive,
                                                  out dateLiveFilterList);
            }

            int dateLiveUntil = -1;
            if(dateLiveFilterList != null)
            {
                IRequestFieldFilter<int> dateLiveFilter = null;
                for(int i = 0; i < dateLiveFilterList.Count && dateLiveFilter == null; ++i)
                {
                    IRequestFieldFilter f = dateLiveFilterList[i];
                    if(f.filterMethod == FieldFilterMethod.GreaterThan
                       || f.filterMethod == FieldFilterMethod.Minimum)
                    {
                        dateLiveFilter = f as IRequestFieldFilter<int>;
                    }
                }

                if(dateLiveFilter != null)
                {
                    dateLiveUntil = dateLiveFilter.filterValue;
                }
            }

            // set value
            int optionIndex = this.dropdown.value;

            if(dateLiveUntil < 0)
            {
                for(int i = 0; i < this.options.Length; ++i)
                {
                    if(this.options[i].filterPeriodSeconds < 0)
                    {
                        optionIndex = i;
                        break;
                    }
                }
            }
            else
            {
                int filterPeriod = dateLiveUntil - ServerTimeStamp.Now;
                int optionDifference = int.MaxValue;

                for(int i = 0; i < this.options.Length; ++i)
                {
                    OptionData option = this.options[i];
                    if(option.filterPeriodSeconds < 0)
                    {
                        continue;
                    }

                    int minOptionPeriod = option.filterPeriodSeconds - option.filterRoundingSeconds;
                    int filterDifference = filterPeriod - minOptionPeriod;

                    if(filterDifference >= 0 && filterDifference < optionDifference)
                    {
                        optionIndex = i;
                        optionDifference = filterDifference;
                    }
                }
            }

            this.dropdown.value = optionIndex;
        }

        /// <summary>Sets the filter value on the targetted view.</summary>
        public void SetExplorerViewDateLiveFilter()
        {
            if(this.m_view == null)
            {
                return;
            }

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
            MinimumFilter<int> fieldFilter = null;
            if(fromTimeStamp > 0)
            {
                fieldFilter = new MinimumFilter<int>() {
                    minimum = fromTimeStamp,
                    isInclusive = false,
                };
            }

            this.m_view.SetFieldFilters(ModIO.API.GetAllModsFilterFields.dateLive, fieldFilter);
        }

        /// <summary>Returns that sort by data for the currently selected dropdown option.</summary>
        public OptionData GetSelectedOption()
        {
            if(this.options != null && this.options.Length > 0 && this.dropdown.options != null
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
                if(this == null || this.dropdown == null)
                {
                    return;
                }

                var so = new UnityEditor.SerializedObject(this.dropdown);
                var optionsProperty = so.FindProperty("m_Options.m_Options");

                optionsProperty.arraySize = this.options.Length;
                for(int i = 0; i < this.options.Length; ++i)
                {
                    optionsProperty.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("m_Text")
                        .stringValue = this.options[i].displayText;
                }

                so.ApplyModifiedProperties();
            };
        }
#endif
    }
}
