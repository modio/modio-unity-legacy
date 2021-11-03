using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [Obsolete("Use ExplorerFilterTagsSelector instead.")]
    public class ExplorerTagFilterView : MonoBehaviour, IGameProfileUpdateReceiver
    {
        // ---------[ FIELDS ]---------
        /// <summary>ExplorerView to set the tagFilter on.</summary>
        public ExplorerView view = null;

        [Header("Settings")]
        /// <summary>Prefab for the tag category display instances.</summary>
        public GameObject tagCategoryPrefab;

        [Header("UI Components")]
        /// <summary>Container for the tag category display instances.</summary>
        public RectTransform tagCategoryContainer;

        // --- RUNTIME DATA ---
        /// <summary>Categories to display.</summary>
        private ModTagCategory[] m_tagCategories = new ModTagCategory[0];
        /// <summary>Displays for the mod tag categories.</summary>
        private List<ModTagCategoryDisplay> m_categoryDisplays = new List<ModTagCategoryDisplay>();
        /// <summary>Tags to display as selected.</summary>
        private List<string> m_selectedTags = new List<string>();

        // --- ACCESSORS ---
        /// <summary>Tags to display as selected.</summary>
        public string[] selectedTags
        {
            get {
                return m_selectedTags.ToArray();
            }
            set {
                if(value == null)
                {
                    value = new string[0];
                }

                bool isSame = (this.m_selectedTags.Count == value.Length);
                for(int i = 0; isSame && i < value.Length; ++i)
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

            // init tag selection
            this.view.onTagFilterUpdated += (t) =>
            { this.selectedTags = t; };

            var viewFilter = this.view.GetTagFilter();
            if(viewFilter == null)
            {
                this.m_selectedTags = new List<string>();
            }
            else
            {
                this.m_selectedTags = new List<string>(viewFilter);
            }

            // init tag categories
            var tagCategories = ModBrowser.instance.gameProfile.tagCategories;
            if(tagCategories != null)
            {
                this.m_tagCategories = tagCategories;
            }

            // update display
            this.Refresh();
        }

        private void OnEnable()
        {
            StartCoroutine(EndOfFrameUpdateCoroutine());
        }

        private System.Collections.IEnumerator EndOfFrameUpdateCoroutine()
        {
            yield return null;
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(tagCategoryContainer);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Rebuilds the view components with the current data.</summary>
        public void Refresh()
        {
            // clear existing
            foreach(ModTagCategoryDisplay display in m_categoryDisplays)
            {
                GameObject.Destroy(display.gameObject);
            }
            m_categoryDisplays.Clear();

            // create
            foreach(ModTagCategory category in m_tagCategories)
            {
                GameObject categoryGO = CreateCategoryDisplayInstance(category, tagCategoryPrefab,
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

        /// <summary>Updates the selected element display.</summary>
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

        /// <summary>Creates the display elements for the given ModTagCategory.</summary>
        private GameObject CreateCategoryDisplayInstance(ModTagCategory category, GameObject prefab,
                                                         RectTransform container)
        {
            GameObject displayGO = GameObject.Instantiate(prefab, container);
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
                tagToggle.isOn = this.m_selectedTags.Contains(tagDisplay.data.tagName);
                tagToggle.group = toggleGroup;
            }

            return displayGO;
        }

        // ---------[ EVENTS ]---------
        /// <summary>React to game profile update message.</summary>
        public void OnGameProfileUpdated(GameProfile gameProfile)
        {
            Debug.Assert(gameProfile != null);

            if(Application.isPlaying && this != null
               && this.m_tagCategories != gameProfile.tagCategories)
            {
                var tagCategories = gameProfile.tagCategories;
                if(tagCategories == null)
                {
                    tagCategories = new ModTagCategory[0];
                }

                this.m_tagCategories = tagCategories;
                this.Refresh();
            }
        }

        /// <summary>Event handler for a tag being clicked.</summary>
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

            this.view.SetTagFilter(this.m_selectedTags);
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("No longer necessary. Initialization occurs in Start().")]
        public void Initialize() {}
    }
}
