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
    public class SubscriptionsView : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public event Action<ModView> inspectRequested;
        public event Action<ModView> subscribeRequested;
        public event Action<ModView> unsubscribeRequested;
        public event Action<ModView> enableModRequested;
        public event Action<ModView> disableModRequested;

        [Header("Settings")]
        public GameObject itemPrefab;

        [Header("UI Components")]
        public ScrollRect scrollView;
        public InputField nameSearchField;
        public Dropdown sortByDropdown;
        public Text resultCount;
        [Tooltip("Object to display when there are no subscribed mods")]
        public GameObject noResultsDisplay;

        [Header("Runtime Data")]
        private Dictionary<int, ModView> m_viewMap = new Dictionary<int, ModView>();

        // --- TEMP ---
        public IEnumerable<ModTagCategory> tagCategories { get; set; }

        // --- ACCESSORS ---
        public IEnumerable<ModView> modViews
        {
            get { return m_viewMap.Values; }
        }

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
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
                view.onClick +=                 NotifyInspectRequested;
                view.subscribeRequested +=      NotifySubscribeRequested;
                view.unsubscribeRequested +=    NotifyUnsubscribeRequested;
                view.enableModRequested +=      NotifyEnableRequested;
                view.disableModRequested +=     NotifyDisableRequested;
                view.Initialize();

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
                                    tagCategories,
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

            // null results
            if(noResultsDisplay != null)
            {
                noResultsDisplay.SetActive(m_viewMap.Count == 0);
            }

            // fix layouting
            if(this.isActiveAndEnabled)
            {
                LayoutRebuilder.MarkLayoutForRebuild(scrollView.GetComponent<RectTransform>());
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
