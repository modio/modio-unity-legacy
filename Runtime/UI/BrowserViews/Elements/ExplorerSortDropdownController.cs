using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Controls the options for a dropdown based on request sort options.</summary>
    [RequireComponent(typeof(Dropdown))]
    public class ExplorerSortDropdownController : MonoBehaviour, IExplorerViewElement
    {
        // ---------[ NESTED DATA ]---------
        /// <summary>Attribute for facilitating inspector display.</summary>
        public class FieldSelectAttribute : PropertyAttribute
        {
        }

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
        public OptionData[] options = new OptionData[] {
            new OptionData() {
                displayText = "Newest",
                fieldName = API.GetAllModsFilterFields.dateLive,
                isAscending = false,
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
            this.dropdown.onValueChanged.AddListener((v) => SetExplorerViewSortMethod());
            this.SetExplorerViewSortMethod();
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
                this.m_view.onRequestFilterChanged.RemoveListener(DisplaySortOption);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onRequestFilterChanged.AddListener(DisplaySortOption);
                this.DisplaySortOption(this.m_view.requestFilter);
            }
            else
            {
                this.DisplaySortOption(null);
            }
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Displays the sort option for a request filter.</summary>
        public void DisplaySortOption(RequestFilter filter)
        {
            if(filter != null)
            {
                this.DisplaySortOption(filter.sortFieldName, filter.isSortAscending);
            }
        }

        /// <summary>Displays the sort option.</summary>
        public void DisplaySortOption(string fieldName, bool isAscending)
        {
            // early out
            OptionData selectedOption = this.GetSelectedOption();
            if(selectedOption != null && selectedOption.fieldName == fieldName
               && selectedOption.isAscending == isAscending)
            {
                return;
            }

            // get the matching option index
            int optionIndex = -1;
            for(int i = 0; i < this.options.Length && optionIndex < 0; ++i)
            {
                OptionData data = this.options[i];
                if(data.fieldName == fieldName && data.isAscending == isAscending)
                {
                    optionIndex = i;
                }
            }

            if(optionIndex < 0)
            {
                optionIndex = 0;
            }

            // set
            this.dropdown.value = optionIndex;
        }

        /// <summary>Sets the sort value on the targetted view.</summary>
        public void SetExplorerViewSortMethod()
        {
            if(this.m_view == null)
            {
                return;
            }

            OptionData option = GetSelectedOption();
            if(option != null)
            {
                this.m_view.SetSortMethod(option.isAscending, option.fieldName);
            }
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
                if(this == null || Application.isPlaying)
                {
                    return;
                }

                // sync dropdown
                Dropdown d = this.dropdown;
                if(d == null)
                {
                    return;
                }

                d.ClearOptions();

                List<string> displayTextList = new List<string>();
                foreach(OptionData option in this.options)
                {
                    displayTextList.Add(option.displayText);
                }
                d.AddOptions(displayTextList);

                // set explorer view default sort
                ExplorerView view = this.GetComponentInParent<ExplorerView>();
                if(view != null)
                {
                    bool defaultAscending = false;
                    string defaultField = ModIO.API.GetAllModsFilterFields.dateLive;

                    if(this.options != null && this.options.Length > 0)
                    {
                        defaultAscending = this.options[0].isAscending;
                        defaultField = this.options[0].fieldName;
                    }

                    var so = new UnityEditor.SerializedObject(view);
                    so.FindProperty("defaultSortMethod.ascending").boolValue = defaultAscending;
                    so.FindProperty("defaultSortMethod.fieldName").stringValue = defaultField;
                    so.ApplyModifiedProperties();
                }
            };
        }
#endif
    }
}
