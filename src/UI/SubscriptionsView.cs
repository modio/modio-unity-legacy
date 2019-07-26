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
    public class SubscriptionsView : MonoBehaviour, IModSubscriptionsUpdateReceiver
    {
        // ---------[ FIELDS ]---------
        /// <summary>Container used to display mods.</summary>
        public ModContainer modContainer = null;

        [Header("UI Components")]
        public Text resultCount;
        [Tooltip("Object to display when there are no subscribed mods")]
        public GameObject noSubscriptionsDisplay;
        [Tooltip("Object to display when there are zero filtered results")]
        public GameObject noResultsDisplay;
        public StateToggleDisplay isActiveIndicator;

        // --- RUNTIME DATA ---
        private Dictionary<int, ModView> m_viewMap = new Dictionary<int, ModView>();
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
        /// <summary>Displays the profiles in the mod container.</summary>
        protected virtual void DisplayProfiles(IList<ModProfile> profiles)
        {
            Debug.Assert(this.modContainer != null);

            if(profiles == null)
            {
                profiles = new ModProfile[0];
            }

            // init vars
            int displayCount = profiles.Count;
            ModProfile[] displayProfiles = new ModProfile[displayCount];
            ModStatistics[] displayStats = new ModStatistics[displayCount];
            List<int> missingStatsData = new List<int>(displayCount);

            // build arrays
            for(int i = 0;
                i < displayCount;
                ++i)
            {
                ModProfile profile = profiles[i];
                ModStatistics stats = null;

                if(profile != null)
                {
                    stats = ModStatisticsRequestManager.instance.TryGetValid(profile.id);

                    if(stats == null)
                    {
                        missingStatsData.Add(profile.id);
                    }
                }

                displayProfiles[i] = profile;
                displayStats[i] = stats;
            }

            // display
            this.modContainer.DisplayMods(displayProfiles, displayStats);

            // fetch missing stats
            if(missingStatsData.Count > 0)
            {
                ModStatisticsRequestManager.instance.RequestModStatistics(missingStatsData,
                (statsArray) =>
                {
                    if(this != null
                       && this.modContainer != null)
                    {
                        // verify still valid
                        bool doPushStats = (displayProfiles.Length == this.modContainer.modProfiles.Length);
                        for(int i = 0;
                            doPushStats && i < displayProfiles.Length;
                            ++i)
                        {
                            // check profiles match
                            ModProfile profile = displayProfiles[i];
                            doPushStats = (profile == this.modContainer.modProfiles[i]);

                            if(doPushStats
                               && profile != null
                               && displayStats[i] == null)
                            {
                                // get missing stats
                                foreach(ModStatistics stats in statsArray)
                                {
                                    if(stats != null
                                       && stats.modId == profile.id)
                                    {
                                        displayStats[i] = stats;
                                        break;
                                    }
                                }
                            }
                        }

                        // push display data
                        if(doPushStats)
                        {
                            this.modContainer.DisplayMods(displayProfiles, displayStats);
                        }
                    }
                },
                WebRequestError.LogAsWarning);
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
        public void OnModSubscriptionsUpdated(IList<int> addedSubscriptions,
                                              IList<int> removedSubscriptions)
        {
            this.Refresh();
        }

        // ---------[ UTILITY ]---------
        /// <summary>Provides a default sorting function for the subscription view.</summary>
        protected virtual int DefaultSortFunction(ModProfile a, ModProfile b)
        {
            return (a.id - b.id);
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("Use SubscriptionView.modContainer instead.")][HideInInspector]
        public GameObject itemPrefab = null;
        [Obsolete][HideInInspector]
        public ScrollRect scrollView;

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
