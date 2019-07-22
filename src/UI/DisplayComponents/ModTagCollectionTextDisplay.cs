using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Displays the tags of a mod in a single text component.</summary>
    public class ModTagCollectionTextDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        [Header("Settings")]
        public bool includeCategory = false;
        public string tagSeparator = ", ";

        [Header("UI Components")]
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField] private ModTagDisplayData[] m_data = new ModTagDisplayData[0];

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        // --- ACCESSORS ---
        public IEnumerable<ModTagDisplayData> data
        {
            get { return m_data; }
            set
            {
                if(value == null)
                {
                    m_data = new ModTagDisplayData[0];
                }
                else
                {
                    m_data = value.ToArray();
                }

                PresentData(m_data);
            }
        }

        private void PresentData(ModTagDisplayData[] displayData)
        {
            Debug.Assert(displayData != null);

            StringBuilder builder = new StringBuilder();
            foreach(ModTagDisplayData tag in displayData)
            {
                if(includeCategory
                   && !System.String.IsNullOrEmpty(tag.categoryName))
                {
                    builder.Append(tag.categoryName + ": ");
                }

                builder.Append(tag.tagName + tagSeparator);
            }

            if(builder.Length > 0)
            {
                builder.Length -= tagSeparator.Length;
            }

            this.m_textComponent.text = builder.ToString();

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
        }

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
                                 + "\nCompatible components are UnityEngine.UI.Text, "
                                 + "UnityEngine.TextMesh, and components derived from TMPro.TMP_Text.",
                                 this);
            }
            #endif
        }
        // ---------[ INITIALIZATION ]---------
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged -= DisplayProfileTags;
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged += DisplayProfileTags;
                this.DisplayProfileTags(this.m_view.profile);
            }
            else
            {
                this.DisplayProfileTags(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the tags for a given profile.</summary>
        public void DisplayProfileTags(ModProfile profile)
        {
            IEnumerable<string> tags = null;
            if(profile != null)
            {
                tags = profile.tagNames;
            }

            this.DisplayTags(tags, null);
        }

        public void DisplayTags(ModProfile profile, IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(profile != null);
            this.DisplayTags(profile.tagNames, tagCategories);
        }

        public void DisplayTags(IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories)
        {
            if(tags == null)
            {
                tags = new string[0];
            }

            m_data = ModTagDisplayData.GenerateArray(tags, tagCategories);
            PresentData(m_data);
        }

        public void DisplayLoading()
        {
            this.m_textComponent.text = string.Empty;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }
        }

        // ---------[ EVENT HANDLING ]---------
        #if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    PresentData(m_data);
                }
            };
        }
        #endif

        // ---------[ OBSOLETE ]---------
        [System.Obsolete("No longer available.")]
        public event System.Action<ModTagCollectionDisplayComponent> onClick;

        [System.Obsolete("No longer necessary.")]
        public void Initialize() {}

        [System.Obsolete("No longer available.")]
        public void NotifyClicked() {}
    }
}
