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
        public ExplorerView view = null;

        /// <summary>Options for the controller to use.</summary>
        public Option[] options = new Option[0];

        // --- ACCESSORS ---
        /// <summary>ToggleGroup this component should refer to.</summary>
        public ToggleGroup toggleGroup
        { get { return this.gameObject.GetComponent<ToggleGroup>(); }}

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            Debug.Assert(view != null);

            if(this.options != null)
            {
                foreach(Option option in this.options)
                {
                    option.toggle.onValueChanged.AddListener((b) => UpdateViewFilter());
                }
            }

            UpdateViewFilter();
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Sets the filter value on the targetted view.</summary>
        public void UpdateViewFilter()
        {
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

        // // ---------[ EVENTS ]---------
        // #if UNITY_EDITOR
        // // BUG(@jackson): There's something that needs to be done here with serialization
        // // potentially - the dropdown seems to load the option data late?
        // /// <summary>Fills the Dropdown options with the supplied data.</summary>
        // private void OnValidate()
        // {
        //     UnityEditor.EditorApplication.delayCall += () =>
        //     {
        //         // early out
        //         if(this == null
        //            || this.dropdown == null)
        //         {
        //             return;
        //         }

        //         // count options
        //         int optionCount = 0;
        //         if(this.options != null)
        //         {
        //             optionCount = this.options.Length;
        //         }

        //         var so = new UnityEditor.SerializedObject(this.dropdown);

        //         var optionsProperty = so.FindProperty("m_Options.m_Options");
        //         optionsProperty.arraySize = optionCount;

        //         for(int i = 0;
        //             i < optionCount;
        //             ++i)
        //         {
        //             optionsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Text").stringValue
        //                 = this.options[i].displayText;
        //             optionsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Image").objectReferenceValue
        //                 = null;
        //         }

        //         so.ApplyModifiedProperties();
        //     };
        // }
        // #endif
    }
}
