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
        public int TEMP_pageSize = 10;

        [Header("UI Components")]
        public ScrollRect scrollView;
        public RectTransform contentPane;
        public InputField nameSearchField;
        public Dropdown sortByDropdown;
        [Tooltip("Object to display when there are no subscribed mods")]
        public GameObject noResultsDisplay;

        [Header("Runtime Data")]
        public RectTransform currentPageContainer;
        private int m_itemsPerScreen;
        private int m_itemsPerScreenStep;
        private List<ModView> m_modViews = new List<ModView>();

        // --- TEMP ---
        public IEnumerable<ModTagCategory> tagCategories { get; set; }

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            Debug.Assert(itemPrefab != null);
            Debug.Assert(scrollView != null);
            Debug.Assert(scrollView.viewport != null && scrollView.content != null);

            ModBrowserItem itemPrefabScript = itemPrefab.GetComponent<ModBrowserItem>();
            RectTransform itemPrefabTransform = itemPrefab.GetComponent<RectTransform>();
            ModView viewPrefabScript = itemPrefab.GetComponent<ModView>();

            Debug.Assert(itemPrefabScript != null
                         && itemPrefabTransform != null
                         && viewPrefabScript != null,
                         "[mod.io] The SubscriptionView.itemPrefab does not have the required "
                         + "ModBrowserItem, ModView, and RectTransform components.\n"
                         + "Please ensure these are all present.");
            Debug.Assert(TEMP_pageSize > 0);

            // currentPageContainer = (new GameObject("Mod Page")).AddComponent<RectTransform>();
            // currentPageContainer.SetParent(contentPane);
            // currentPageContainer.anchorMin = Vector2.zero;
            // currentPageContainer.anchorMax = Vector2.zero;
            // currentPageContainer.offsetMin = Vector2.zero;
            // currentPageContainer.offsetMax = new Vector2(contentPane.rect.width, contentPane.rect.height);
            // TODO(@jackson): FIX!
            Debug.LogWarning("@jackson FAKING PAGES HERE!");
            currentPageContainer = contentPane;

            CalculateLayoutingValues();

        }

        // NOTE(@jackson): This code seems fragile and may need to be updated to handle more situations
        private void CalculateLayoutingValues()
        {
            Debug.Assert(scrollView.content.GetComponent<LayoutGroup>() != null,
                         "[mod.io] SubscriptionsView requires a LayoutGroup component attached to"
                         + " the ScrollView.Content GameObject for layouting purposes");

            Rect itemRect = itemPrefab.GetComponent<RectTransform>().rect;
            Rect viewportRect = scrollView.viewport.GetComponent<RectTransform>().rect;
            LayoutGroup layouter = scrollView.content.GetComponent<LayoutGroup>();

            if(layouter is VerticalLayoutGroup)
            {
                VerticalLayoutGroup vlg = layouter as VerticalLayoutGroup;
                m_itemsPerScreen = (int)Mathf.Ceil((viewportRect.height + vlg.spacing) / itemRect.height);
                m_itemsPerScreenStep = 1;
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }

        // ---------[ UI FUNCTIONALITY ]------------
        public void DisplayProfiles(IEnumerable<ModProfile> profileCollection)
        {
            #if DEBUG
            if(!Application.isPlaying) { return; }
            #endif

            var enabledMods = ModManager.GetEnabledModIds();

            // clear
            foreach(ModView view in m_modViews)
            {
                GameObject.Destroy(view.gameObject);
            }
            m_modViews.Clear();

            // create
            if(profileCollection != null)
            {
                foreach(ModProfile profile in profileCollection)
                {
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

                    view.DisplayMod(profile,
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

                    m_modViews.Add(view);
                }
            }

            // null results
            if(noResultsDisplay != null)
            {
                noResultsDisplay.SetActive(m_modViews.Count == 0);
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
