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

    public class ExplorerView : MonoBehaviour, IGameProfileUpdateReceiver, IModDownloadStartedReceiver, IModEnabledReceiver, IModDisabledReceiver, IModSubscriptionsUpdateReceiver, IModRatingAddedReceiver
    {
        // ---------[ FIELDS ]---------
        public event Action<string[]> onTagFilterUpdated;

        [Header("Settings")]
        public GameObject itemPrefab = null;
        public float pageTransitionTimeSeconds = 0.4f;
        public RectTransform pageTemplate = null;
        public string defaultSortString = "-" + API.GetAllModsFilterFields.dateLive;

        [Header("UI Components")]
        public RectTransform contentPane;
        public Button prevPageButton;
        public Button nextPageButton;
        public Text pageNumberText;
        public Text pageCountText;
        public Text resultCountText;
        [Tooltip("Object to display when there are no subscribed mods")]
        public GameObject noResultsDisplay;
        public StateToggleDisplay isActiveIndicator;

        [Header("Display Data")]
        public GridLayoutGroup gridLayout = null;
        public RequestPage<ModProfile> currentPage = null;
        public RequestPage<ModProfile> targetPage = null;

        [Header("Request Data")]
        /// <summary>String to use for filtering the mod request.</summary>
        [SerializeField]
        private string m_titleFilter = string.Empty;
        /// <summary>String to use for sorting the mod request.</summary>
        [SerializeField]
        private string m_sortString = string.Empty;
        /// <summary>Tags to filter by.</summary>
        [SerializeField]
        private List<string> m_tagFilter = new List<string>();

        [Header("Runtime Data")]
        public bool isTransitioning = false;
        public RectTransform currentPageContainer = null;
        public RectTransform targetPageContainer = null;

        // --- RUNTIME DATA ---
        private List<ModView> m_modViews = new List<ModView>();
        private IEnumerable<ModTagCategory> m_tagCategories = null;

        // --- ACCESSORS ---
        public int itemsPerPage
        {
            get
            {
                if(this.gridLayout == null) { return 0; }

                return UIUtilities.CountVisibleGridCells(this.gridLayout);
            }
        }
        public IEnumerable<ModView> modViews
        {
            get
            {
                return this.m_modViews;
            }
        }
        private ModProfileRequestManager profileManager { get { return ModProfileRequestManager.instance; } }

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
        private void OnEnable()
        {
            // NOTE(@jackson): This appears to be unnecessary?
            // UpdateCurrentPageDisplay();

            if(this.isActiveIndicator != null)
            {
                this.isActiveIndicator.isOn = true;
            }

            if(this.currentPageContainer != null)
            {
                int viewsOnCurrentPageCount = this.currentPageContainer.childCount;
                if(this.itemsPerPage != viewsOnCurrentPageCount)
                {
                    Refresh();
                }
            }
        }

        private void Start()
        {
            // asserts
            Debug.Assert(itemPrefab != null);

            RectTransform prefabTransform = itemPrefab.GetComponent<RectTransform>();
            ModView prefabView = itemPrefab.GetComponent<ModView>();

            Debug.Assert(prefabTransform != null
                         && prefabView != null,
                         "[mod.io] The ExplorerView.itemPrefab does not have the required "
                         + "ModBrowserItem, ModView, and RectTransform components.\n"
                         + "Please ensure these are all present.");

            if(pageTemplate == null)
            {
                Debug.LogWarning("[mod.io] Page Template variable needs to be set in order for the"
                                 + " Explorer View to function", this.gameObject);
                this.enabled = false;
                return;
            }

            this.gridLayout = pageTemplate.GetComponent<GridLayoutGroup>();
            if(this.gridLayout == null)
            {
                Debug.LogWarning("[mod.io] Page Template needs a grid layout component in order for the"
                                 + " Explorer View to function", this.gameObject);
                this.enabled = false;
                return;
            }

            // - create pages -
            pageTemplate.gameObject.SetActive(false);

            GameObject pageGO;

            pageGO = (GameObject)GameObject.Instantiate(pageTemplate.gameObject, pageTemplate.parent);
            pageGO.name = "Mod Page A";
            currentPageContainer = pageGO.GetComponent<RectTransform>();
            currentPageContainer.gameObject.SetActive(true);

            pageGO = (GameObject)GameObject.Instantiate(pageTemplate.gameObject, pageTemplate.parent);
            pageGO.name = "Mod Page B";
            targetPageContainer = pageGO.GetComponent<RectTransform>();
            targetPageContainer.gameObject.SetActive(false);

            this.UpdateCurrentPageDisplay();
            this.UpdatePageButtonInteractibility();

            // - perform initial fetch -
            this.Refresh();
        }

        private void OnDisable()
        {
            if(this.isActiveIndicator != null)
            {
                this.isActiveIndicator.isOn = false;
            }
        }

        public void Refresh()
        {
            int pageIndex = 0;

            RequestFilter filter = this.GenerateRequestFilter();
            int pageSize = this.itemsPerPage;
            int pageOffset = pageIndex * pageSize;
            bool wasDisplayUpdated = false;

            RequestPage<ModProfile> filteredPage = new RequestPage<ModProfile>()
            {
                size = pageSize,
                items = new ModProfile[pageSize],
                resultOffset = pageOffset,
                resultTotal = 0,
            };
            this.currentPage = filteredPage;

            ModProfileRequestManager.instance.FetchModProfilePage(filter, pageOffset, pageSize,
            (page) =>
            {
                if(this != null
                   && this.currentPage == filteredPage)
                {
                    this.currentPage = page;
                    this.UpdateCurrentPageDisplay();
                    this.UpdatePageButtonInteractibility();

                    wasDisplayUpdated = true;
                }
            },
            WebRequestError.LogAsWarning);

            if(!wasDisplayUpdated)
            {
                this.UpdateCurrentPageDisplay();
            }
        }

        public RequestFilter GenerateRequestFilter()
        {
            RequestFilter filter = new RequestFilter();

            // sort
            if(string.IsNullOrEmpty(this.m_sortString))
            {
                filter.sortFieldName = this.defaultSortString;
            }
            else
            {
                filter.sortFieldName = this.m_sortString;
            }

            // title
            if(String.IsNullOrEmpty(this.m_titleFilter))
            {
                filter.fieldFilters.Remove(ModIO.API.GetAllModsFilterFields.name);
            }
            else
            {
                filter.fieldFilters[ModIO.API.GetAllModsFilterFields.name]
                    = new StringLikeFilter() { likeValue = "*"+this.m_titleFilter+"*" };
            }

            // tags
            string[] filterTagNames = this.m_tagFilter.ToArray();

            if(filterTagNames.Length == 0)
            {
                filter.fieldFilters.Remove(ModIO.API.GetAllModsFilterFields.tags);
            }
            else
            {
                filter.fieldFilters[ModIO.API.GetAllModsFilterFields.tags]
                    = new MatchesArrayFilter<string>() { filterArray = filterTagNames };
            }

            return filter;
        }

        public void UpdatePageButtonInteractibility()
        {
            if(this.prevPageButton != null)
            {
                this.prevPageButton.interactable = (!this.isTransitioning
                                                    && this.CurrentPageNumber > 1);
            }
            if(this.nextPageButton != null)
            {
                this.nextPageButton.interactable = (!this.isTransitioning
                                                    && this.CurrentPageNumber < this.CurrentPageCount);
            }
        }

        public void ChangePage(int pageDifferential)
        {
            if(this.isTransitioning)
            {
                return;
            }

            int pageSize = this.itemsPerPage;
            int targetPageIndex = this.CurrentPageNumber - 1 + pageDifferential;
            int targetPageProfileOffset = targetPageIndex * pageSize;

            Debug.Assert(targetPageIndex >= 0);
            Debug.Assert(targetPageIndex < this.CurrentPageCount);

            int pageItemCount = (int)Mathf.Min(pageSize,
                                               this.currentPage.resultTotal - targetPageProfileOffset);

            RequestPage<ModProfile> targetPage = new RequestPage<ModProfile>()
            {
                size = pageSize,
                items = new ModProfile[pageItemCount],
                resultOffset = targetPageProfileOffset,
                resultTotal = this.currentPage.resultTotal,
            };
            this.targetPage = targetPage;
            this.UpdateTargetPageDisplay();

            ModProfileRequestManager.instance.FetchModProfilePage(this.GenerateRequestFilter(), targetPageProfileOffset, pageSize,
            (page) =>
            {
                if(this.targetPage == targetPage)
                {
                    this.targetPage = page;
                    this.UpdateTargetPageDisplay();
                }
                if(this.currentPage == targetPage)
                {
                    this.currentPage = page;
                    this.UpdateCurrentPageDisplay();
                    this.UpdatePageButtonInteractibility();
                }
            },
            null);

            PageTransitionDirection transitionDirection = (pageDifferential < 0
                                                           ? PageTransitionDirection.FromLeft
                                                           : PageTransitionDirection.FromRight);

            this.InitiateTargetPageTransition(transitionDirection, () =>
            {
                this.UpdatePageButtonInteractibility();
            });
            this.UpdatePageButtonInteractibility();
        }

        // ---------[ FILTER CONTROL ]---------
        /// <summary>Sets the title filter and refreshes the results.</summary>
        public void SetTitleFilter(string titleFilter)
        {
            if(titleFilter == null) { titleFilter = string.Empty; }

            if(this.m_titleFilter.ToUpper() != titleFilter.ToUpper())
            {
                this.m_titleFilter = titleFilter;
                Refresh();
            }
        }

        /// <summary>Gets the title filter string.</summary>
        public string GetTitleFilter() { return this.m_titleFilter; }

        /// <summary>Sets the sort method for the view and refreshes.</summary>
        public void SetSortString(string sortString)
        {
            if(sortString == null) { sortString = string.Empty; }

            if(this.m_sortString.ToUpper() != sortString.ToUpper())
            {
                this.m_sortString = sortString;
                Refresh();
            }
        }

        /// <summary>Gets the sort string.</summary>
        public string GetSortString() { return this.m_sortString; }

        /// <summary>Sets the tag filter and refreshes the results.</summary>
        public void SetTagFilter(IList<string> tagFilter)
        {
            if(tagFilter == null) { tagFilter = new string[0]; }

            bool isSame = (this.m_tagFilter.Count == tagFilter.Count);
            for(int i = 0;
                isSame && i < tagFilter.Count;
                ++i)
            {
                isSame = (this.m_tagFilter[i] == tagFilter[i]);
            }

            if(!isSame)
            {
                this.m_tagFilter = new List<string>(tagFilter);
                this.Refresh();

                if(this.onTagFilterUpdated != null)
                {
                    this.onTagFilterUpdated(this.m_tagFilter.ToArray());
                }
            }
        }

        /// <summary>Gets the tag filter.</summary>
        public string[] GetTagFilter()
        {
            return this.m_tagFilter.ToArray();
        }

        /// <summary>Adds a tag to the tag filter and refreshes the results.</summary>
        public void AddTagToFilter(string tagName)
        {
            if(this.m_tagFilter.Contains(tagName)) { return; }

            this.m_tagFilter.Add(tagName);
            this.Refresh();

            if(this.onTagFilterUpdated != null)
            {
                this.onTagFilterUpdated(this.m_tagFilter.ToArray());
            }
        }

        /// <summary>Removes a tag from the tag filter and refreshes the results.</summary>
        public void RemoveTagFromFilter(string tagName)
        {
            if(!this.m_tagFilter.Contains(tagName)) { return; }

            this.m_tagFilter.Remove(tagName);
            this.Refresh();

            if(this.onTagFilterUpdated != null)
            {
                this.onTagFilterUpdated(this.m_tagFilter.ToArray());
            }
        }

        // ---------[ PAGE DISPLAY ]---------
        public void UpdateCurrentPageDisplay()
        {
            if(currentPageContainer == null) { return; }

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

            IEnumerable<ModProfile> profiles = null;
            if(this.currentPage != null)
            {
                profiles = this.currentPage.items;
            }

            UpdatePageNumberDisplay();
            DisplayProfiles(profiles, this.currentPageContainer);
        }

        public void UpdateTargetPageDisplay()
        {
            if(targetPageContainer == null) { return; }

            #if DEBUG
            if(isTransitioning)
            {
                Debug.LogWarning("[mod.io] Explorer View is currently transitioning between pages. It"
                                 + " is recommended to not update page displays at this time.");
            }
            #endif

            DisplayProfiles(this.targetPage.items, this.targetPageContainer);
        }

        private void DisplayProfiles(IEnumerable<ModProfile> profileCollection, RectTransform pageTransform)
        {
            #if DEBUG
            if(!Application.isPlaying) { return; }
            #endif

            foreach(Transform t in pageTransform)
            {
                ModView view = t.GetComponent<ModView>();
                if(view != null)
                {
                    m_modViews.Remove(view);
                }
                GameObject.Destroy(t.gameObject);
            }

            List<int> missingStatsData = new List<int>();
            List<ModView> pageModViews = new List<ModView>();
            if(profileCollection != null)
            {
                IList<int> subscribedModIds = ModManager.GetSubscribedModIds();
                IList<int> enabledModIds = ModManager.GetEnabledModIds();

                foreach(ModProfile profile in profileCollection)
                {
                    if(pageModViews.Count >= itemsPerPage)
                    {
                        Debug.LogWarning("[mod.io] ProfileCollection contained more profiles than "
                                         + "can be displayed per page", this.gameObject);
                        break;
                    }

                    GameObject itemGO = GameObject.Instantiate(itemPrefab,
                                                               pageTransform);
                    itemGO.name = "Mod Tile [" + pageModViews.Count.ToString() + "]";

                    // initialize item
                    ModView view = itemGO.GetComponent<ModView>();

                    if(profile == null)
                    {
                        view.DisplayLoading();
                    }
                    else
                    {
                        // add listeners
                        view.onClick +=                 (v) => ViewManager.instance.InspectMod(v.data.profile.modId);
                        view.subscribeRequested +=      (v) => ModBrowser.instance.SubscribeToMod(v.data.profile.modId);
                        view.unsubscribeRequested +=    (v) => ModBrowser.instance.UnsubscribeFromMod(v.data.profile.modId);
                        view.enableModRequested +=      (v) => ModBrowser.instance.EnableMod(v.data.profile.modId);
                        view.disableModRequested +=     (v) => ModBrowser.instance.DisableMod(v.data.profile.modId);
                        view.ratePositiveRequested +=   (v) => ModBrowser.instance.AttemptRateMod(v.data.profile.modId, ModRatingValue.Positive);
                        view.rateNegativeRequested +=   (v) => ModBrowser.instance.AttemptRateMod(v.data.profile.modId, ModRatingValue.Negative);

                        // display
                        ModStatistics stats = ModStatisticsRequestManager.instance.TryGetValid(profile.id);
                        bool isModSubscribed = subscribedModIds.Contains(profile.id);
                        bool isModEnabled = enabledModIds.Contains(profile.id);
                        ModRatingValue ratingValue = ModBrowser.instance.GetModRating(profile.id);

                        view.DisplayMod(profile,
                                        stats,
                                        m_tagCategories,
                                        isModSubscribed,
                                        isModEnabled,
                                        ratingValue);

                        if(stats == null)
                        {
                            missingStatsData.Add(profile.id);
                        }
                    }

                    pageModViews.Add(view);
                }

                if(pageModViews.Count > 0)
                {
                    for(int i = pageModViews.Count; i < itemsPerPage; ++i)
                    {
                        GameObject spacer = new GameObject("Spacing Tile [" + i.ToString("00") + "]",
                                                           typeof(RectTransform));
                        spacer.transform.SetParent(pageTransform);
                    }
                }
            }
            m_modViews.AddRange(pageModViews);

            if(missingStatsData.Count > 0)
            {
                ModStatisticsRequestManager.instance.RequestModStatistics(missingStatsData,
                (statsArray) =>
                {
                    if(this != null)
                    {
                        UpdateStatisticsDisplays(statsArray);
                    }
                },
                WebRequestError.LogAsWarning);
            }

            // fix layouting
            if(this.isActiveAndEnabled)
            {
                LayoutRebuilder.MarkLayoutForRebuild(pageTransform);
            }
        }

        protected virtual void UpdateStatisticsDisplays(IEnumerable<ModStatistics> statsData)
        {
            if(statsData == null) { return; }

            List<ModStatistics> statsList = new List<ModStatistics>(statsData);
            foreach(ModView view in this.m_modViews)
            {
                if(view != null)
                {
                    ModDisplayData data = view.data;
                    ModStatistics stats = null;

                    for(int i = 0;
                        i < statsList.Count && stats == null;
                        ++i)
                    {
                        stats = statsList[i];
                        if(stats.modId == data.profile.modId)
                        {
                            statsList.RemoveAt(i);
                        }
                        else
                        {
                            stats = null;
                        }
                    }

                    if(stats != null)
                    {
                        data.statistics = ModStatisticsDisplayData.CreateFromStatistics(stats);
                        view.data = data;
                    }
                }
            }
        }

        private void UpdatePageNumberDisplay()
        {
            if(currentPage == null) { return; }

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

                currentPageContainer.anchoredPosition = Vector2.zero;
                targetPageContainer.anchoredPosition = new Vector2(transPaneStartX, 0f);

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

                currentPageContainer.anchoredPosition = new Vector2(transPos, 0f);
                targetPageContainer.anchoredPosition = new Vector2(transPos + transitionPaneStartX, 0f);

                transitionTime += Time.unscaledDeltaTime;

                yield return null;
            }

            // flip
            var tempContainer = currentPageContainer;
            currentPageContainer = targetPageContainer;
            targetPageContainer = tempContainer;

            var tempPage = currentPage;
            currentPage = targetPage;
            targetPage = tempPage;

            // finalize
            currentPageContainer.anchoredPosition = Vector2.zero;
            targetPageContainer.gameObject.SetActive(false);

            UpdatePageNumberDisplay();

            isTransitioning = false;

            if(onTransitionCompleted != null)
            {
                onTransitionCompleted();
            }
        }

        [Obsolete("Replaced by ClearAllFilters() - but still used in ModBrowser (prefab), ClearFilterButton")]
        public void ClearFilters()
        {
            ClearAllFilters();
        }
        
        // ---------[ FILTER MANAGEMENT ]---------
        public void ClearAllFilters()
        {
            // Check if already cleared
            if(string.IsNullOrEmpty(this.m_titleFilter)
               && (string.IsNullOrEmpty(this.m_sortString) || this.m_sortString == this.defaultSortString)
               && this.m_tagFilter.Count == 0)
            {
                return;
            }

            this.m_titleFilter = string.Empty;
            this.m_sortString = string.Empty;
            this.m_tagFilter.Clear();

            this.Refresh();

            if(this.onTagFilterUpdated != null)
            {
                this.onTagFilterUpdated(new string[0]);
            }
        }

        // ---------[ EVENTS ]---------
        public void OnGameProfileUpdated(GameProfile gameProfile)
        {
            if(this.m_tagCategories != gameProfile.tagCategories)
            {
                this.m_tagCategories = gameProfile.tagCategories;

                if(this.isActiveAndEnabled)
                {
                    this.Refresh();
                }
            }
        }

        public void OnModSubscriptionsUpdated(IList<int> addedSubscriptions,
                                              IList<int> removedSubscriptions)
        {
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            foreach(ModView view in m_modViews)
            {
                ModDisplayData modData = view.data;
                bool isSubscribed = subscribedModIds.Contains(modData.profile.modId);

                if(modData.isSubscribed != isSubscribed)
                {
                    modData.isSubscribed = isSubscribed;
                    view.data = modData;
                }
            }
        }

        public void OnModEnabled(int modId)
        {
            foreach(ModView view in this.m_modViews)
            {
                if(view.data.profile.modId == modId)
                {
                    ModDisplayData data = view.data;
                    data.isModEnabled = true;
                    view.data = data;
                }
            }
        }

        public void OnModDisabled(int modId)
        {
            foreach(ModView view in this.m_modViews)
            {
                if(view.data.profile.modId == modId)
                {
                    ModDisplayData data = view.data;
                    data.isModEnabled = false;
                    view.data = data;
                }
            }
        }

        public void OnModDownloadStarted(int modId, FileDownloadInfo downloadInfo)
        {
            foreach(ModView view in this.m_modViews)
            {
                if(view.data.profile.modId == modId)
                {
                    view.DisplayDownload(downloadInfo);
                }
            }
        }

        public void OnModRatingAdded(int modId, ModRatingValue rating)
        {
            foreach(ModView view in this.modViews)
            {
                if(view.data.profile.modId == modId)
                {
                    ModDisplayData data = view.data;
                    data.userRating = rating;
                    view.data = data;
                }
            }
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("No longer necessary. Initialization occurs in Start().")]
        public void Initialize() {}

        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModView> inspectRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyInspectRequested(ModView view)
        {
            if(inspectRequested != null)
            {
                inspectRequested(view);
            }
        }

        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModView> subscribeRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifySubscribeRequested(ModView view)
        {
            if(subscribeRequested != null)
            {
                subscribeRequested(view);
            }
        }

        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModView> unsubscribeRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyUnsubscribeRequested(ModView view)
        {
            if(unsubscribeRequested != null)
            {
                unsubscribeRequested(view);
            }
        }

        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModView> enableModRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyEnableRequested(ModView view)
        {
            if(enableModRequested != null)
            {
                enableModRequested(view);
            }
        }

        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModView> disableModRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyDisableRequested(ModView view)
        {
            if(disableModRequested != null)
            {
                disableModRequested(view);
            }
        }
    }
}
