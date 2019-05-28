using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace ModIO.UI
{
    public class ExplorerTagFilterView : MonoBehaviour, IGameProfileUpdateReceiver
    {
        // ---------[ FIELDS ]---------
        [Header("Settings")]
        public GameObject tagCategoryPrefab;

        [Header("UI Components")]
        public RectTransform tagCategoryContainer;

        /// <summary>ExplorerView to set the tagFilter on.</summary>
        public ExplorerView view = null;

        // --- RUNTIME DATA ---
        private List<ModTagCategoryDisplay> m_categoryDisplays = new List<ModTagCategoryDisplay>();
        private List<string> m_selectedTags = new List<string>();
        private ModTagCategory[] m_categories = new ModTagCategory[0];

        // --- ACCESSORS ---
        public IEnumerable<ModTagCategoryDisplay> categoryDisplays
        { get { return m_categoryDisplays; } }

        public string[] selectedTags
        {
            get { return m_selectedTags.ToArray(); }
            set
            {
                if(value == null) { value = new string[0]; }

                bool isSame = (this.m_selectedTags.Count == value.Length);
                for(int i = 0;
                    isSame && i < value.Length;
                    ++i)
                {
                    isSame = (this.m_selectedTags[i] == value[i]);
                }

                if(!isSame)
                {
                    m_selectedTags = new List<string>(value);
                    this.UpdateSelectionDisplay();
                }
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            // asserts
            Debug.Assert(tagCategoryContainer != null);
            Debug.Assert(tagCategoryPrefab != null);
            Debug.Assert(tagCategoryPrefab.GetComponent<ModTagCategoryDisplay>() != null);

            ModTagContainer tagContainer = tagCategoryPrefab.GetComponent<ModTagContainer>();
            Debug.Assert(tagContainer != null,
                         "[mod.io] ModTagFilterViews require the TagCategoryPrefab to have a "
                         + "ModTagContainer component. (Any other TagCollectionDisplay type "
                         + "is incompatible.)");

            Debug.Assert(tagContainer.tagDisplayPrefab != null);

            Debug.Assert(tagContainer.tagDisplayPrefab.GetComponent<Toggle>() != null,
                         "[mod.io] ModTagFilterViews require the TagDisplayPrefab in the "
                         + "FilterView.tagCategoryPrefab to have a Toggle Component.");
        }

        private void OnEnable()
        {
            StartCoroutine(EndOfFrameUpdateCoroutine());
        }

        public System.Collections.IEnumerator EndOfFrameUpdateCoroutine()
        {
            yield return null;
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(tagCategoryContainer);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public void Refresh()
        {
            // clear existing
            foreach(ModTagCategoryDisplay display in m_categoryDisplays)
            {
                GameObject.Destroy(display.gameObject);
            }
            m_categoryDisplays.Clear();

            // create
            foreach(ModTagCategory category in m_categories)
            {
                GameObject categoryGO = CreateCategoryDisplayInstance(category,
                                                                      m_selectedTags,
                                                                      tagCategoryPrefab,
                                                                      tagCategoryContainer);

                categoryGO.GetComponent<ModTagContainer>().tagClicked += TagClickHandler;
                m_categoryDisplays.Add(categoryGO.GetComponent<ModTagCategoryDisplay>());
            }

            // layout
            if(this.isActiveAndEnabled)
            {
                StartCoroutine(EndOfFrameUpdateCoroutine());
            }
        }

        private void UpdateSelectionDisplay()
        {
            foreach(ModTagCategoryDisplay categoryDisplay in m_categoryDisplays)
            {
                ModTagContainer tagContainer = categoryDisplay.tagDisplay as ModTagContainer;
                tagContainer.tagClicked -= TagClickHandler;

                foreach(ModTagDisplay tagDisplay in tagContainer.tagDisplays)
                {
                    Toggle tagToggle = tagDisplay.GetComponent<Toggle>();
                    tagToggle.isOn = m_selectedTags.Contains(tagDisplay.data.tagName);
                }

                tagContainer.tagClicked += TagClickHandler;
            }
        }

        private static GameObject CreateCategoryDisplayInstance(ModTagCategory category,
                                                                List<string> selectedTags,
                                                                GameObject prefab,
                                                                RectTransform container)
        {
            GameObject displayGO = GameObject.Instantiate(prefab,
                                                          new Vector3(),
                                                          Quaternion.identity,
                                                          container);
            displayGO.name = category.name;

            ModTagCategoryDisplay display = displayGO.GetComponent<ModTagCategoryDisplay>();
            display.Initialize();
            display.DisplayCategory(category);

            ToggleGroup toggleGroup = null;
            if(!category.isMultiTagCategory)
            {
                toggleGroup = display.gameObject.AddComponent<ToggleGroup>();
                toggleGroup.allowSwitchOff = true;
            }

            ModTagContainer tagContainer = displayGO.GetComponent<ModTagContainer>();
            foreach(ModTagDisplay tagDisplay in tagContainer.tagDisplays)
            {
                Toggle tagToggle = tagDisplay.GetComponent<Toggle>();
                tagToggle.isOn = selectedTags.Contains(tagDisplay.data.tagName);
                tagToggle.group = toggleGroup;
                // TODO(@jackson): Need to register?
            }

            return displayGO;
        }

        // ---------[ EVENTS ]---------
        public void OnGameProfileUpdated(GameProfile gameProfile)
        {
            Debug.Assert(gameProfile != null);

            if(this.m_categories != gameProfile.tagCategories)
            {
                this.m_categories = gameProfile.tagCategories;
                this.Refresh();
            }
        }

        private void TagClickHandler(ModTagDisplayComponent display)
        {
            string tagName = display.data.tagName;
            if(this.m_selectedTags.Contains(tagName))
            {
                this.m_selectedTags.Remove(tagName);
            }
            else
            {
                this.m_selectedTags.Add(tagName);
            }

            this.view.tagFilter = this.m_selectedTags.ToArray();
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("No longer necessary. Initialization occurs in Start().")]
        public void Initialize() {}
    }
}
