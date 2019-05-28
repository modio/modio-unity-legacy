using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Controls the options for a dropdown based on request sort options.</summary>
    [RequireComponent(typeof(Dropdown))]
    public class ExplorerSortDropdownController : MonoBehaviour
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

            /// <summary>Should the sort occur in ascending order.</summary>
            public bool isAscending;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Options for the controller to use.</summary>
        public OptionData[] options = new OptionData[]
        {
            new OptionData()
            {
                displayText = "Newest",
                fieldName = API.GetAllModsFilterFields.dateLive,
                isAscending = false,
            },
        };

        /// <summary>The Dropdown component to be controlled.</summary>
        public Dropdown dropdown
        { get { return this.gameObject.GetComponent<Dropdown>(); }}

        // ---------[ FUNCTIONALITY ]---------
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
