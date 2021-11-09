using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [Obsolete("No longer supported.")]
    public class ModTagDisplay : ModTagDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ModTagDisplayComponent> onClick;

        [Header("Settings")]
        public bool capitalizeName;
        public bool capitalizeCategory;

        [Header("UI Components")]
        public Text nameDisplay;
        public Text categoryDisplay;
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField]
        private ModTagDisplayData m_data = new ModTagDisplayData();

        // --- ACCESSORS ---
        public override ModTagDisplayData data
        {
            get {
                return m_data;
            }
            set {
                m_data = value;
                PresentData();
            }
        }

        // ---------[ INTIALIZATION ]---------
        public override void Initialize() {}

        // ---------[ UI FUNCTIONALITY ]---------
        private void PresentData()
        {
            if(nameDisplay != null)
            {
                nameDisplay.text = (capitalizeName ? m_data.tagName.ToUpper() : m_data.tagName);
            }

            if(categoryDisplay != null)
            {
                categoryDisplay.text =
                    (capitalizeCategory ? m_data.categoryName.ToUpper() : m_data.categoryName);
            }

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
        }

        public void DisplayTag(ModTag tag, string category)
        {
            Debug.Assert(tag != null);
            DisplayModTag(tag.name, category);
        }
        public void DisplayTag(string tag, string category)
        {
            DisplayModTag(tag, category);
        }

        public override void DisplayModTag(ModTag tag, string categoryName)
        {
            Debug.Assert(tag != null);
            DisplayModTag(tag.name, categoryName);
        }
        public override void DisplayModTag(string tagName, string categoryName)
        {
            ModTagDisplayData newData = new ModTagDisplayData() {
                tagName = tagName,
                categoryName = (categoryName == null ? string.Empty : categoryName),
            };
            m_data = newData;

            PresentData();
        }

        public override void DisplayLoading()
        {
            nameDisplay.text = string.Empty;
            if(categoryDisplay != null)
            {
                categoryDisplay.text = string.Empty;
            }

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyClicked()
        {
            if(this.onClick != null)
            {
                this.onClick(this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    PresentData();
                }
            };
        }
#endif
    }
}
