using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Controls the options for a dropdown based on request sort options.</summary>
    [RequireComponent(typeof(Dropdown))]
    public class SubscriptionSortDropdownController : MonoBehaviour
    {
        // ---------[ NESTED DATA ]---------
        /// <summary>Attribute for facilitating inspector display.</summary>
        public class FieldSelectAttribute : PropertyAttribute {}

        /// <summary>The data that the controller uses to create dropdown options.</summary>
        [System.Serializable]
        public class OptionData
        {
            /// <summary>Text to use as the dropdown option.</summary>
            public string displayText;

            /// <summary>Value to use in the request filter.</summary>
            [FieldSelectAttribute]
            public string fieldName;
        }

        // ---------[ STATICS ]---------
        /// <summary>Options for sorting the subcriptions view</summary>
        public static readonly Dictionary<string, Comparison<ModProfile>> subscriptionSortOptions = new Dictionary<string, Comparison<ModProfile>>()
        {
            {
                "Name", (a,b) =>
                {
                    int compareResult = String.Compare(a.name, b.name);
                    if(compareResult == 0)
                    {
                        compareResult = a.id - b.id;
                    }
                    return compareResult;
                }
            },
            {
                "File Size", (a,b) =>
                {
                    int compareResult = (int)(b.currentBuild.fileSize - a.currentBuild.fileSize);
                    if(compareResult == 0)
                    {
                        compareResult = String.Compare(a.name, b.name);
                        if(compareResult == 0)
                        {
                            compareResult = a.id - b.id;
                        }
                    }
                    return compareResult;
                }
            },
            {
                "Last Updated", (a,b) =>
                {
                    int compareResult = b.dateUpdated - a.dateUpdated;
                    if(compareResult == 0)
                    {
                        compareResult = String.Compare(a.name, b.name);
                        if(compareResult == 0)
                        {
                            compareResult = a.id - b.id;
                        }
                    }
                    return compareResult;
                }
            },
            {
                "Enabled", (a,b) =>
                {
                    int compareResult = 0;
                    compareResult += (ModManager.GetEnabledModIds().Contains(a.id) ? -1 : 0);
                    compareResult += (ModManager.GetEnabledModIds().Contains(b.id) ? 1 : 0);

                    if(compareResult == 0)
                    {
                        compareResult = String.Compare(a.name, b.name);
                        if(compareResult == 0)
                        {
                            compareResult = a.id - b.id;
                        }
                    }

                    return compareResult;
                }
            },
        };

        // ---------[ FIELDS ]---------
        /// <summary>SubscriptionsView to receive the sort delegate.</summary>
        public SubscriptionsView view = null;

        /// <summary>Options for the controller to use.</summary>
        public OptionData[] options = new OptionData[0];

        // --- ACCESSORS ---
        /// <summary>The Dropdown component to be controlled.</summary>
        public Dropdown dropdown
        { get { return this.gameObject.GetComponent<Dropdown>(); }}


        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            this.dropdown.onValueChanged.AddListener((v) => UpdateViewSort());
            UpdateViewSort();
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Sets the sort delegate on the targetted view.</summary>
        public void UpdateViewSort()
        {
            Comparison<ModProfile> sortFunc = GetSelectedSortFunction();
            if(sortFunc != null)
            {
                view.sortDelegate = sortFunc;
            }
        }

        /// <summary>Returns the sort function for the currently selected dropdown option.</summary>
        public Comparison<ModProfile> GetSelectedSortFunction()
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
                        return SubscriptionSortDropdownController.subscriptionSortOptions[option.fieldName];
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
                if(this == null) { return; }

                Dropdown d = this.dropdown;
                if(d == null) { return; }

                d.ClearOptions();

                List<string> displayTextList = new List<string>();
                foreach(OptionData option in this.options)
                {
                    displayTextList.Add(option.displayText);
                }
                d.AddOptions(displayTextList);
            };
        }
        #endif
    }
}
