using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [System.Obsolete("Use TagCollectionTextDisplay instead.")]
    [RequireComponent(typeof(Text))]
    public class ModTagCollectionTextDisplay : ModTagCollectionDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public event System.Action<ModTagCollectionDisplayComponent> onClick;

        [Header("Settings")]
        public bool includeCategory = false;
        public string tagSeparator = ", ";

        [Header("UI Components")]
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField]
        private ModTagDisplayData[] m_data = new ModTagDisplayData[0];

        // --- ACCESSORS ---
        public Text text
        {
            get {
                return this.gameObject.GetComponent<Text>();
            }
        }

        public override IEnumerable<ModTagDisplayData> data
        {
            get {
                return m_data;
            }
            set {
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
                if(includeCategory && !System.String.IsNullOrEmpty(tag.categoryName))
                {
                    builder.Append(tag.categoryName + ": ");
                }

                builder.Append(tag.tagName + tagSeparator);
            }

            if(builder.Length > 0)
            {
                builder.Length -= tagSeparator.Length;
            }

            text.text = builder.ToString();
            text.enabled = true;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
        }

        // ---------[ INITIALIZE ]---------
        public override void Initialize()
        {
            if(Application.isPlaying)
            {
                Debug.Assert(text != null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]--------
        public override void DisplayTags(ModProfile profile,
                                         IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(profile != null);
            this.DisplayTags(profile.tagNames, tagCategories);
        }

        public override void DisplayTags(IEnumerable<string> tags,
                                         IEnumerable<ModTagCategory> tagCategories)
        {
            if(tags == null)
            {
                tags = new string[0];
            }

            m_data = ModTagDisplayData.GenerateArray(tags, tagCategories);
            PresentData(m_data);
        }

        public override void DisplayLoading()
        {
            text.text = string.Empty;

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
                    PresentData(m_data);
                }
            };
        }
#endif
    }
}
