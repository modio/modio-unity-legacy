using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public enum PageTransitionDirection
    {
        FromLeft,
        FromRight,
    }

    public class ExplorerView : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public event Action<ModView> inspectRequested;
        public event Action<ModView> subscribeRequested;
        public event Action<ModView> unsubscribeRequested;
        public event Action<ModView> enableModRequested;
        public event Action<ModView> disableModRequested;
        public event Action onFilterTagsChanged;

        [Header("Settings")]
        public GameObject itemPrefab = null;
        public int itemSpacing = 2;
        public float pageTransitionTimeSeconds = 0.4f;

        [Header("UI Components")]
        public RectTransform contentPane;
        public InputField nameSearchField;
        public ModTagFilterView tagFilterView;
        public ModTagContainer tagFilterBar;
        public Dropdown sortByDropdown;
        public Text pageNumberText;
        public Text pageCountText;
        public Text resultCountText;
        [Tooltip("Object to display when there are no subscribed mods")]
        public GameObject noResultsDisplay;

        [Header("Display Data")]
        public RequestPage<ModProfile> currentPage = null;
        public RequestPage<ModProfile> targetPage = null;
        public List<string> filterTags = new List<string>();

        [Header("Runtime Data")]
        public bool isTransitioning = false;
        public RectTransform currentPageContainer = null;
        public RectTransform targetPageContainer = null;

        // --- RUNTIME DATA ---
        private IEnumerable<ModTagCategory> m_tagCategories = null;

        // --- ACCESSORS ---
        public IEnumerable<ModTagCategory> tagCategories
        {
            get { return m_tagCategories; }
            set
            {
                if(m_tagCategories == value)
                {
                    return;
                }

                m_tagCategories = value;
                if(value == null) { return; }

                if(tagFilterView != null)
                {
                    tagFilterView.tagCategories = value;
                }
                if(tagFilterBar != null)
                {
                    tagFilterBar.DisplayTags(filterTags, value);
                }
            }
        }

        // ---[ CALCULATED VARS ]----
        public int CurrentPageNumber
        {
            get
            {
                int pageNumber = 0;

                if(currentPage != null
                   && currentPage.size > 0
                   && currentPage.resultTotal > 0)
                {
                    pageNumber = (int)Mathf.Floor((float)currentPage.resultOffset / (float)currentPage.size) + 1;
                }

                return pageNumber;
            }
        }
        public int CurrentPageCount
        {
            get
            {
                int pageCount = 0;

                if(currentPage != null
                   && currentPage.size > 0
                   && currentPage.resultTotal > 0)
                {
                    pageCount = (int)Mathf.Ceil((float)currentPage.resultTotal / (float)currentPage.size);
                }

                return pageCount;
            }
        }

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            Debug.Assert(itemPrefab != null);

            ModBrowserItem itemPrefabScript = itemPrefab.GetComponent<ModBrowserItem>();
            RectTransform itemPrefabTransform = itemPrefab.GetComponent<RectTransform>();
            ModView viewPrefabScript = itemPrefab.GetComponent<ModView>();

            Debug.Assert(itemPrefabScript != null
                         && itemPrefabTransform != null
                         && viewPrefabScript != null,
                         "[mod.io] The ExplorerView.itemPrefab does not have the required "
                         + "ModBrowserItem, ModView, and RectTransform components.\n"
                         + "Please ensure these are all present.");

            Debug.Assert(itemPrefabTransform.anchorMin == new Vector2(0f, 1f)
                         && itemPrefabTransform.anchorMax == new Vector2(0f, 1f),
                         "[mod.io] The ExplorerView.itemPrefab's transfrom needs a top-left anchor."
                         + " Please ensure the both the anchor min and anchor max are at [0, 1].");

            // - initialize pages -
            foreach(Transform t in contentPane)
            {
                GameObject.Destroy(t.gameObject);
            }

            currentPageContainer = new GameObject("Mod Page", typeof(RectTransform)).transform as RectTransform;
            currentPageContainer.SetParent(contentPane);
            currentPageContainer.anchorMin = Vector2.zero;
            currentPageContainer.anchorMax = Vector2.zero;
            currentPageContainer.offsetMin = Vector2.zero;
            currentPageContainer.offsetMax = new Vector2(contentPane.rect.width, contentPane.rect.height);
            ApplyPageLayouter(currentPageContainer);

            targetPageContainer = new GameObject("Mod Page", typeof(RectTransform)).transform as RectTransform;
            targetPageContainer.SetParent(contentPane);
            targetPageContainer.anchorMin = Vector2.zero;
            targetPageContainer.anchorMax = Vector2.zero;
            targetPageContainer.offsetMin = new Vector2(contentPane.rect.width, 0f);
            targetPageContainer.offsetMax = new Vector2(contentPane.rect.width * 2f, contentPane.rect.height);
            ApplyPageLayouter(targetPageContainer);

            targetPageContainer.gameObject.SetActive(false);

            // - nested views -
            if(tagFilterView != null)
            {
                tagFilterView.Initialize();

                tagFilterView.tagFilterAdded += (tag) =>
                {
                    filterTags.Add(tag);

                    if(tagFilterBar != null)
                    {
                        tagFilterBar.DisplayTags(filterTags, m_tagCategories);
                    }

                    if(onFilterTagsChanged != null)
                    {
                        onFilterTagsChanged();
                    }
                };
                tagFilterView.tagFilterRemoved += (tag) =>
                {
                    filterTags.Remove(tag);

                    if(tagFilterBar != null)
                    {
                        tagFilterBar.DisplayTags(filterTags, m_tagCategories);
                    }

                    if(onFilterTagsChanged != null)
                    {
                        onFilterTagsChanged();
                    }
                };
            }

            if(tagFilterBar != null)
            {
                tagFilterBar.Initialize();

                tagFilterBar.tagClicked += (display) =>
                {
                    filterTags.Remove(display.data.tagName);

                    tagFilterBar.DisplayTags(filterTags, m_tagCategories);

                    if(tagFilterView != null)
                    {
                        tagFilterView.selectedTags = filterTags;
                    }

                    if(onFilterTagsChanged != null)
                    {
                        onFilterTagsChanged();
                    }
                };
            }
        }

        private void ApplyPageLayouter(RectTransform pageTransform)
        {
            Rect itemRect = itemPrefab.GetComponent<RectTransform>().rect;

            GridLayoutGroup layouter = pageTransform.gameObject.AddComponent<GridLayoutGroup>();
            layouter.spacing = new Vector2(itemSpacing, itemSpacing);
            layouter.padding = new RectOffset();
            layouter.cellSize = new Vector2(itemRect.width, itemRect.height);
            layouter.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layouter.startAxis = GridLayoutGroup.Axis.Horizontal;
            layouter.childAlignment = TextAnchor.MiddleLeft;
            layouter.constraint = GridLayoutGroup.Constraint.Flexible;
        }

        // ----------[ PAGE DISPLAY ]---------
        // TODO(@jackson): Remove -> calculate in InitializePageLayout
        public int CalculateItemsPerPage()
        {
            LayoutGroup layouter = currentPageContainer.GetComponent<GridLayoutGroup>();
            Rect containerDimensions = contentPane.GetComponent<RectTransform>().rect;
            int itemCount = 0;

            if(layouter is GridLayoutGroup)
            {
                GridLayoutGroup g = layouter as GridLayoutGroup;

                float gridWidth = (containerDimensions.width - g.padding.left - g.padding.right + g.spacing.x);
                float cellWidth = (g.cellSize.x + g.spacing.x);
                int columnsPerPage = (int)Mathf.Floor(gridWidth / cellWidth);
                if(columnsPerPage < 0)
                {
                    columnsPerPage = 0;
                }

                float gridHeight = (containerDimensions.height - g.padding.top - g.padding.bottom + g.spacing.y);
                float cellHeight = (g.cellSize.y + g.spacing.y);
                int rowsPerPage = (int)Mathf.Floor(gridHeight / cellHeight);
                if(rowsPerPage < 0)
                {
                    rowsPerPage = 0;
                }

                itemCount = columnsPerPage * rowsPerPage;
            }
            else
            {
                Debug.LogError("[mod.io] Support for LayoutGroup components of type "
                               + layouter.GetType().ToString() + " has not been implemented.");
            }

            return itemCount;
        }

        public void UpdateCurrentPageDisplay()
        {
            Debug.Assert(currentPageContainer != null,
                         "[mod.io] ExplorerView.Initialize has not yet been called");

            #if DEBUG
            if(isTransitioning)
            {
                Debug.LogWarning("[mod.io] Explorer View is currently transitioning between pages. It"
                                 + " is recommended to not update page displays at this time.");
            }
            #endif

            if(noResultsDisplay != null)
            {
                noResultsDisplay.SetActive(currentPage == null
                                           || currentPage.items == null
                                           || currentPage.items.Length == 0);
            }

            UpdatePageNumberDisplay();
            DisplayProfiles(this.currentPage.items, this.currentPageContainer);
        }

        public void UpdateTargetPageDisplay()
        {
            Debug.Assert(targetPageContainer != null,
                         "[mod.io] ExplorerView.Initialize has not yet been called");

            #if DEBUG
            if(isTransitioning)
            {
                Debug.LogWarning("[mod.io] Explorer View is currently transitioning between pages. It"
                                 + " is recommended to not update page displays at this time.");
            }
            #endif

            DisplayProfiles(this.targetPage.items, this.targetPageContainer);
        }

        public void UpdateSubscriptionsDisplay()
        {
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            if(currentPageContainer != null)
            {
                foreach(Transform itemTransform in currentPageContainer)
                {
                    ModView modView = itemTransform.GetComponent<ModView>();
                    ModDisplayData modData = modView.data;
                    bool isSubscribed = subscribedModIds.Contains(modData.profile.modId);

                    if(modData.isSubscribed != isSubscribed)
                    {
                        modData.isSubscribed = isSubscribed;
                        modView.data = modData;
                    }
                }
            }
            if(targetPageContainer != null)
            {
                foreach(Transform itemTransform in targetPageContainer)
                {
                    ModView modView = itemTransform.GetComponent<ModView>();
                    ModDisplayData modData = modView.data;
                    bool isSubscribed = subscribedModIds.Contains(modData.profile.modId);

                    if(modData.isSubscribed != isSubscribed)
                    {
                        modData.isSubscribed = isSubscribed;
                        modView.data = modData;
                    }
                }
            }
        }

        private void DisplayProfiles(IEnumerable<ModProfile> profileCollection, RectTransform pageTransform)
        {
            #if DEBUG
            if(!Application.isPlaying) { return; }
            #endif

            foreach(Transform t in pageTransform)
            {
                GameObject.Destroy(t.gameObject);
            }

            if(profileCollection != null)
            {
                List<ModView> viewList = new List<ModView>();
                int pageSize = CalculateItemsPerPage();
                IList<int> subscribedModIds = ModManager.GetSubscribedModIds();
                IList<int> enabledModIds = ModManager.GetEnabledModIds();

                foreach(ModProfile profile in profileCollection)
                {
                    if(viewList.Count >= pageSize)
                    {
                        // Debug.LogWarning("[mod.io] ProfileCollection contained more profiles than "
                        //                  + "can be displayed per page");
                        break;
                    }

                    GameObject itemGO = GameObject.Instantiate(itemPrefab,
                                                               new Vector3(),
                                                               Quaternion.identity,
                                                               pageTransform);

                    // initialize item
                    // TODO(@jackson): Remove
                    ModBrowserItem item = itemGO.GetComponent<ModBrowserItem>();
                    item.index = viewList.Count;

                    ModView view = itemGO.GetComponent<ModView>();
                    view.onClick +=                 NotifyInspectRequested;
                    view.subscribeRequested +=      NotifySubscribeRequested;
                    view.unsubscribeRequested +=    NotifyUnsubscribeRequested;
                    view.enableModRequested +=      NotifyEnableRequested;
                    view.disableModRequested +=     NotifyDisableRequested;
                    view.Initialize();

                    if(profile == null)
                    {
                        view.DisplayLoading();
                    }
                    else
                    {
                        view.DisplayMod(profile,
                                        null,
                                        m_tagCategories,
                                        subscribedModIds.Contains(profile.id),
                                        enabledModIds.Contains(profile.id));

                        ModManager.GetModStatistics(profile.id,
                                                    (s) =>
                                                    {
                                                        ModDisplayData data = view.data;
                                                        data.statistics = ModStatisticsDisplayData.CreateFromStatistics(s);
                                                        view.data = data;
                                                    },
                                                    null);
                    }

                    viewList.Add(view);
                }


                if(viewList.Count > 0)
                {
                    for(int i = viewList.Count; i < pageSize; ++i)
                    {
                        GameObject spacer = new GameObject("Spacing Tile [" + i.ToString("00") + "]",
                                                           typeof(RectTransform));
                        spacer.transform.SetParent(pageTransform);
                    }
                }
            }

            // fix layouting
            if(this.isActiveAndEnabled)
            {
                LayoutRebuilder.MarkLayoutForRebuild(pageTransform);
            }
        }

        private void UpdatePageNumberDisplay()
        {
            if(pageNumberText != null)
            {
                pageNumberText.text = CurrentPageNumber.ToString();
            }
            if(pageCountText != null)
            {
                pageCountText.text = CurrentPageCount.ToString();
            }
            if(resultCountText != null)
            {
                resultCountText.text = UIUtilities.ValueToDisplayString(currentPage.resultTotal);
            }
        }

        // ----------[ PAGE TRANSITIONS ]---------
        public void InitiateTargetPageTransition(PageTransitionDirection direction, Action onTransitionCompleted)
        {
            if(!isTransitioning)
            {
                float mainPaneTargetX = contentPane.rect.width * (direction == PageTransitionDirection.FromLeft ? 1f : -1f);
                float transPaneStartX = mainPaneTargetX * -1f;

                currentPageContainer.offsetMin = Vector2.zero;
                currentPageContainer.offsetMax = new Vector2(contentPane.rect.width, contentPane.rect.height);

                targetPageContainer.offsetMin = new Vector2(transPaneStartX, 0f);
                targetPageContainer.offsetMax = new Vector2(transPaneStartX + contentPane.rect.width,
                                                       contentPane.rect.height);

                StartCoroutine(TransitionPageCoroutine(mainPaneTargetX, transPaneStartX,
                                                       this.pageTransitionTimeSeconds, onTransitionCompleted));
            }
            #if DEBUG
            else
            {
                Debug.LogWarning("[mod.io] ModPages are already transitioning.");
            }
            #endif
        }

        private IEnumerator TransitionPageCoroutine(float mainPaneTargetX, float transitionPaneStartX,
                                                    float transitionLength, Action onTransitionCompleted)
        {
            isTransitioning = true;

            targetPageContainer.gameObject.SetActive(true);

            float transitionTime = 0f;

            // transition
            while(transitionTime < transitionLength)
            {
                float transPos = Mathf.Lerp(0f, mainPaneTargetX, transitionTime / transitionLength);

                currentPageContainer.offsetMin = new Vector2(transPos,
                                                 0f);
                currentPageContainer.offsetMax = new Vector2(transPos + contentPane.rect.width,
                                                 contentPane.rect.height);

                targetPageContainer.offsetMin = new Vector2(transPos + transitionPaneStartX,
                                                       0f);
                targetPageContainer.offsetMax = new Vector2(transPos + transitionPaneStartX + contentPane.rect.width,
                                                       contentPane.rect.height);

                transitionTime += Time.deltaTime;

                yield return null;
            }

            // finalize
            targetPageContainer.offsetMin = Vector2.zero;
            targetPageContainer.offsetMax = new Vector2(contentPane.rect.width, contentPane.rect.height);

            currentPageContainer.gameObject.SetActive(false);

            var tempContainer = currentPageContainer;
            currentPageContainer = targetPageContainer;
            targetPageContainer = tempContainer;

            var tempPage = currentPage;
            currentPage = targetPage;
            targetPage = tempPage;

            UpdatePageNumberDisplay();

            isTransitioning = false;

            if(onTransitionCompleted != null)
            {
                onTransitionCompleted();
            }
        }

        // ---------[ FILTER MANAGEMENT ]---------
        public void ClearFilters()
        {
            if(nameSearchField != null)
            {
                nameSearchField.text = string.Empty;
            }

            filterTags.Clear();

            if(tagFilterView != null)
            {
                tagFilterView.selectedTags = filterTags;
            }
            if(tagFilterBar != null)
            {
                tagFilterBar.DisplayTags(filterTags, m_tagCategories);
            }

            if(onFilterTagsChanged != null)
            {
                onFilterTagsChanged();
            }
        }

        // ---------[ EVENTS ]---------
        public void NotifyInspectRequested(ModView view)
        {
            if(inspectRequested != null)
            {
                inspectRequested(view);
            }
        }
        public void NotifySubscribeRequested(ModView view)
        {
            if(subscribeRequested != null)
            {
                subscribeRequested(view);
            }
        }
        public void NotifyUnsubscribeRequested(ModView view)
        {
            if(unsubscribeRequested != null)
            {
                unsubscribeRequested(view);
            }
        }
        public void NotifyEnableRequested(ModView view)
        {
            if(enableModRequested != null)
            {
                enableModRequested(view);
            }
        }
        public void NotifyDisableRequested(ModView view)
        {
            if(disableModRequested != null)
            {
                disableModRequested(view);
            }
        }
    }
}
