using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace ModIO.UI
{
    // NOTE(@jackson): The functionality of this view makes the assumption that the number of items
    // to be displayed is low enough that it does not cause memory issues. Safeguards against this
    // will be made in a future update, but is currently not a priority.
    public class SubscriptionsView : MonoBehaviour, IGameProfileUpdateReceiver, IModDownloadStartedReceiver, IModEnabledReceiver, IModDisabledReceiver, IModSubscriptionsUpdateReceiver, IModRatingAddedReceiver
    {
        // ---------[ FIELDS ]---------
        [Header("Settings")]
        public GameObject itemPrefab = null;

        [Header("UI Components")]
        public ScrollRect scrollView;
        public Text resultCount;
        [Tooltip("Object to display when there are no subscribed mods")]
        public GameObject noSubscriptionsDisplay;
        [Tooltip("Object to display when there are zero filtered results")]
        public GameObject noResultsDisplay;
        public StateToggleDisplay isActiveIndicator;

        // --- RUNTIME DATA ---
        private Dictionary<int, ModView> m_viewMap = new Dictionary<int, ModView>();
        private ModTagCategory[] m_tagCategories = new ModTagCategory[0];
        private Comparison<ModProfile> m_sortDelegate = null;
        private string m_titleFilter = string.Empty;

        // --- ACCESSORS ---
        public IEnumerable<ModView> modViews
        {
            get { return m_viewMap.Values; }
        }

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            Debug.Assert(itemPrefab != null);
            Debug.Assert(scrollView != null);
            Debug.Assert(scrollView.viewport != null && scrollView.content != null);

            RectTransform prefabTransform = itemPrefab.GetComponent<RectTransform>();
            ModView prefabView = itemPrefab.GetComponent<ModView>();

            Debug.Assert(prefabTransform != null
                         && prefabView != null,
                         "[mod.io] The SubscriptionView.itemPrefab does not have the required "
                         + "ModBrowserItem, ModView, and RectTransform components.\n"
                         + "Please ensure these are all present.");

            // init tag categories
            var tagCategories = ModBrowser.instance.gameProfile.tagCategories;
            if(tagCategories != null)
            {
                this.m_tagCategories = tagCategories;
            }

            // get page
            this.DisplayProfiles(null);
            this.Refresh();
        }

        private void OnEnable()
        {
            if(this.isActiveIndicator != null)
            {
                this.isActiveIndicator.isOn = true;
            }
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
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            ModProfileRequestManager.instance.RequestModProfiles(subscribedModIds,
            (profiles) => Refresh_OnGetModProfiles(profiles, this.m_titleFilter, this.m_sortDelegate),
            (requestError) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                           "Failed to get subscription data from mod.io servers.\n"
                                           + requestError.displayMessage);
            });
        }

        /// <summary>Handle the mods returned by the refresh request.</summary>
        protected virtual void Refresh_OnGetModProfiles(IList<ModProfile> modProfiles,
                                                        string requestedTitleFilter,
                                                        Comparison<ModProfile> requestedSortDelegate)
        {
            // check for early outs
            if(this == null
               || this.m_titleFilter != requestedTitleFilter
               || this.m_sortDelegate != requestedSortDelegate)
            {
                return;
            }

            // check for null list
            if(modProfiles == null
               || modProfiles.Count == 0)
            {
                this.DisplayProfiles(new ModProfile[0]);
            }

            // filter
            Func<ModProfile, bool> titleFilterDelegate = (p) => true;
            if(!String.IsNullOrEmpty(requestedTitleFilter))
            {
                // set initial value
                string filterString = requestedTitleFilter.ToUpper();
                titleFilterDelegate = (p) =>
                {
                    return p.name.ToUpper().Contains(filterString);
                };
            }

            List<ModProfile> filteredList = new List<ModProfile>(modProfiles.Count);
            foreach(ModProfile profile in modProfiles)
            {
                if(titleFilterDelegate(profile))
                {
                    filteredList.Add(profile);
                }
            }

            // sort
            if(requestedSortDelegate == null)
            {
                requestedSortDelegate = this.DefaultSortFunction;
            }

            filteredList.Sort(requestedSortDelegate);

            this.DisplayProfiles(filteredList);
        }

        // ---------[ UI FUNCTIONALITY ]------------
        public void DisplayProfiles(IEnumerable<ModProfile> profileCollection)
        {
            #if DEBUG
            if(!Application.isPlaying) { return; }
            #endif

            // sort lists
            List<ModProfile> orderedProfileList = new List<ModProfile>();
            List<int> removedProfiles = new List<int>(m_viewMap.Keys);

            if(profileCollection != null)
            {
                foreach(ModProfile profile in profileCollection)
                {
                    if(profile != null)
                    {
                        orderedProfileList.Add(profile);
                        removedProfiles.Remove(profile.id);
                    }
                }
            }

            // ensure there are enough game objects
            int excessProfileCount = orderedProfileList.Count - m_viewMap.Count;
            for(int i = 0; i < excessProfileCount; ++i)
            {
                // create GameObject
                GameObject viewGO = GameObject.Instantiate(itemPrefab,
                                                           scrollView.content);
                ModView view = viewGO.GetComponent<ModView>();

                // add listeners
                view.onClick +=                 (v) => ViewManager.instance.InspectMod(v.data.profile.modId);
                view.subscribeRequested +=      (v) => ModBrowser.instance.SubscribeToMod(v.data.profile.modId);
                view.unsubscribeRequested +=    (v) => ModBrowser.instance.UnsubscribeFromMod(v.data.profile.modId);
                view.enableModRequested +=      (v) => ModBrowser.instance.EnableMod(v.data.profile.modId);
                view.disableModRequested +=     (v) => ModBrowser.instance.DisableMod(v.data.profile.modId);
                view.ratePositiveRequested +=   (v) => ModBrowser.instance.AttemptRateMod(v.data.profile.modId, ModRatingValue.Positive);
                view.rateNegativeRequested +=   (v) => ModBrowser.instance.AttemptRateMod(v.data.profile.modId, ModRatingValue.Negative);

                // register in map
                int fakeModId = -i - 1;
                m_viewMap.Add(fakeModId, view);
                removedProfiles.Add(fakeModId);
            }

            // order the game objects and display new mods
            var enabledMods = ModManager.GetEnabledModIds();
            List<int> missingStatsData = new List<int>();
            for(int i = 0; i < orderedProfileList.Count; ++i)
            {
                ModProfile profile = orderedProfileList[i];
                ModView view;
                if(!m_viewMap.TryGetValue(profile.id, out view))
                {
                    // collect unused view
                    int oldModId = removedProfiles[0];
                    removedProfiles.RemoveAt(0);

                    view = m_viewMap[oldModId];

                    m_viewMap.Remove(oldModId);
                    m_viewMap.Add(profile.id, view);

                    // display mod
                    ModStatistics stats = ModStatisticsRequestManager.instance.TryGetValid(profile.id);
                    view.DisplayMod(orderedProfileList[i],
                                    stats,
                                    this.m_tagCategories,
                                    true, // assume subscribed
                                    enabledMods.Contains(profile.id),
                                    ModBrowser.instance.GetModRating(profile.id));

                    if(stats == null)
                    {
                        missingStatsData.Add(profile.id);
                    }
                }

                view.transform.SetSiblingIndex(i);
            }

            // remove unused profiles
            foreach(int removedId in removedProfiles)
            {
                GameObject.Destroy(m_viewMap[removedId].gameObject);
                m_viewMap.Remove(removedId);
            }

            // result count
            if(resultCount != null)
            {
                resultCount.text = m_viewMap.Count.ToString();
            }

            // no results
            int subCountTotal = ModManager.GetSubscribedModIds().Count;

            if(noSubscriptionsDisplay != null)
            {
                noSubscriptionsDisplay.SetActive(subCountTotal == 0);
            }

            if(noResultsDisplay != null)
            {
                noResultsDisplay.SetActive(subCountTotal > 0
                                           && m_viewMap.Count == 0);
            }

            if(missingStatsData.Count > 0)
            {
                ModStatisticsRequestManager.instance.RequestModStatistics(missingStatsData,
                (statsArray) =>
                {
                    if(this != null)
                    {
                        UpdateStatisticsDisplays(statsArray);
                    }

                    IList<int> subbedIds = ModManager.GetSubscribedModIds();
                    foreach(ModStatistics stats in statsArray)
                    {
                        if(subbedIds.Contains(stats.modId))
                        {
                            CacheClient.SaveModStatistics(stats);
                        }
                    }
                },
                // TODO(@jackson): something
                null);
            }

            // fix layouting
            if(this.isActiveAndEnabled)
            {
                LayoutRebuilder.MarkLayoutForRebuild(scrollView.GetComponent<RectTransform>());
            }
        }

        protected virtual void UpdateStatisticsDisplays(IEnumerable<ModStatistics> statsData)
        {
            if(statsData == null) { return; }

            foreach(ModStatistics stats in statsData)
            {
                ModView view = null;
                if(this.m_viewMap.TryGetValue(stats.modId, out view)
                   && view != null)
                {
                    ModDisplayData data = view.data;
                    data.statistics = ModStatisticsDisplayData.CreateFromStatistics(stats);
                    view.data = data;
                }
            }
        }

        // ---------[ FILTER CONTROL ]---------
        /// <summary>Sets the title filter and refreshes the page.</summary>
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

        /// <summary>Sets the sort delegate and refreshes the page.</summary>
        public void SetSortDelegate(Comparison<ModProfile> sortDelegate)
        {
            if(this.m_sortDelegate != sortDelegate)
            {
                this.m_sortDelegate = sortDelegate;
                this.Refresh();
            }
        }

        /// <summary>Gets the sort delegate.</summary>
        public Comparison<ModProfile> GetSortDelegate() { return this.m_sortDelegate; }

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
            this.Refresh();
        }

        public void OnModEnabled(int modId)
        {
            foreach(ModView view in this.modViews)
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
            foreach(ModView view in this.modViews)
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
            foreach(ModView view in this.modViews)
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

        // ---------[ UTILITY ]---------
        /// <summary>Provides a default sorting function for the subscription view.</summary>
        protected virtual int DefaultSortFunction(ModProfile a, ModProfile b)
        {
            return (a.id - b.id);
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
