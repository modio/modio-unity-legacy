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
    public class SubscriptionsView : MonoBehaviour, IGameProfileUpdateReceiver, IModDownloadStartedReceiver, IModEnabledReceiver, IModDisabledReceiver, IModSubscriptionsUpdateReceiver
    {
        // ---------[ FIELDS ]---------
        [Header("Settings")]
        public GameObject itemPrefab = null;
        public ModProfileRequestManager requestManager = null;

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
        private Comparison<ModProfile> m_sortDelegate = (a,b) => a.id - b.id;
        private string m_titleFilter = null;

        // --- ACCESSORS ---
        public IEnumerable<ModView> modViews
        {
            get { return m_viewMap.Values; }
        }

        public Comparison<ModProfile> sortDelegate
        {
            get { return this.m_sortDelegate; }
            set
            {
                if(this.m_sortDelegate != value)
                {
                    this.m_sortDelegate = value;
                    this.Refresh();
                }
            }
        }

        public string titleFilter
        {
            get { return this.m_titleFilter; }
            set
            {
                if(this.m_titleFilter != value)
                {
                    this.m_titleFilter = value;
                    this.Refresh();
                }
            }
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

            // check for request managers
            if(this.requestManager == null)
            {
                this.requestManager = this.gameObject.AddComponent<ModProfileRequestManager>();
            }

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

            this.requestManager.GetModProfiles(subscribedModIds,
                                               (profiles) => Refresh_OnGetModProfiles(profiles,
                                                                                      this.m_titleFilter,
                                                                                      this.m_sortDelegate),
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
                                                        Comparison<ModProfile> requstedSortDelegate)
        {
            // check for early outs
            if(this == null
               || this.m_titleFilter != requestedTitleFilter
               || this.m_sortDelegate != requstedSortDelegate)
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

            filteredList.Sort(requstedSortDelegate);

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
                    orderedProfileList.Add(profile);
                    removedProfiles.Remove(profile.id);
                }
            }

            // ensure there are enough game objects
            int excessProfileCount = orderedProfileList.Count - m_viewMap.Count;
            for(int i = 0; i < excessProfileCount; ++i)
            {
                // create GameObject
                GameObject viewGO = GameObject.Instantiate(itemPrefab,
                                                           new Vector3(),
                                                           Quaternion.identity,
                                                           scrollView.content);
                ModView view = viewGO.GetComponent<ModView>();

                // add listeners
                view.onClick +=                 (v) => ViewManager.instance.InspectMod(v.data.profile.modId);
                view.subscribeRequested +=      (v) => ModBrowser.instance.SubscribeToMod(v.data.profile.modId);
                view.unsubscribeRequested +=    (v) => ModBrowser.instance.UnsubscribeFromMod(v.data.profile.modId);
                view.enableModRequested +=      (v) => ModBrowser.instance.EnableMod(v.data.profile.modId);
                view.disableModRequested +=     (v) => ModBrowser.instance.DisableMod(v.data.profile.modId);

                // register in map
                int fakeModId = -i - 1;
                m_viewMap.Add(fakeModId, view);
                removedProfiles.Add(fakeModId);
            }

            // order the game objects
            var enabledMods = ModManager.GetEnabledModIds();
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
                    view.DisplayMod(orderedProfileList[i],
                                    null,
                                    this.m_tagCategories,
                                    true, // assume subscribed
                                    enabledMods.Contains(profile.id));

                    ModManager.GetModStatistics(profile.id,
                                                (s) =>
                                                {
                                                    ModDisplayData data = view.data;
                                                    data.statistics = ModStatisticsDisplayData.CreateFromStatistics(s);
                                                    view.data = data;
                                                },
                                                null);
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

            // fix layouting
            if(this.isActiveAndEnabled)
            {
                LayoutRebuilder.MarkLayoutForRebuild(scrollView.GetComponent<RectTransform>());
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

        public void OnModSubscriptionsUpdated()
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
