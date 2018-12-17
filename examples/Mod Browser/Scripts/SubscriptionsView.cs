using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// TODO(@jackson): Handle no mods in list inspection
namespace ModIO.UI
{
    public class SubscriptionsView : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public event Action<ModBrowserItem> inspectRequested;
        public event Action<ModBrowserItem> subscribeRequested;
        public event Action<ModBrowserItem> unsubscribeRequested;
        public event Action<ModBrowserItem> toggleModEnabledRequested;

        [Header("Settings")]
        public GameObject itemPrefab;
        public int TEMP_pageSize = 10;

        [Header("UI Components")]
        public RectTransform contentPane;
        public InputField nameSearchField;
        public Dropdown sortByDropdown;

        [Header("Display Data")]
        public RequestPage<ModProfile> currentPage;

        [Header("Runtime Data")]
        public RectTransform currentPageContainer;


        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            Debug.Assert(itemPrefab != null);

            ModBrowserItem itemPrefabScript = itemPrefab.GetComponent<ModBrowserItem>();

            Debug.Assert(itemPrefabScript != null);
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

            InitializePageLayout(currentPageContainer);
        }

        private void InitializePageLayout(RectTransform pageTransform)
        {
            foreach(Transform t in pageTransform)
            {
                #if DEBUG
                if(!Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
                else
                #endif
                {
                    UnityEngine.Object.Destroy(t.gameObject);
                }
            }

            for(int index = 0;
                index < this.TEMP_pageSize;
                ++index)
            {
                GameObject itemGO = GameObject.Instantiate(itemPrefab,
                                                           new Vector3(),
                                                           Quaternion.identity,
                                                           pageTransform);

                // display mod profile
                ModBrowserItem item = itemGO.GetComponent<ModBrowserItem>();
                item.index = index;
                item.profile = null;
                item.inspectRequested +=            (i) => { if(inspectRequested != null) { inspectRequested(i); } };
                item.subscribeRequested +=          (i) => { if(subscribeRequested != null) { unsubscribeRequested(i); } };
                item.unsubscribeRequested +=        (i) => { if(unsubscribeRequested != null) { unsubscribeRequested(i); } };
                item.toggleModEnabledRequested +=   (i) => { if(toggleModEnabledRequested != null) { toggleModEnabledRequested(i); } };
                item.Initialize();

                itemGO.SetActive(false);
            }
        }

        // ---------[ UI FUNCTIONALITY ]------------
        public void UpdateCurrentPageDisplay()
        {
            Debug.Assert(currentPageContainer != null,
                         "[mod.io] ExplorerView.Initialize has not yet been called");
            Debug.Assert(TEMP_pageSize > 0,
                         "[mod.io] TEMP_pageSize has an invalid value. This is because either the columnCount"
                         + " or rowCount has been calculated to be less than 1.");

            // #if DEBUG
            // if(isTransitioning)
            // {
            //     Debug.LogWarning("[mod.io] Explorer View is currently transitioning between pages. It"
            //                      + " is recommended to not update page displays at this time.");
            // }
            // #endif

            UpdatePageDisplay(this.currentPage, this.currentPageContainer);
        }

        private void UpdatePageDisplay(RequestPage<ModProfile> page, RectTransform pageTransform)
        {
            #if DEBUG
            if(!Application.isPlaying) { return; }
            Debug.LogWarning("needs categories");
            #endif

            int i = 0;

            if(page != null
               && page.items != null)
            {
                for(; i < TEMP_pageSize && i < page.items.Length; ++i)
                {
                    Transform itemTransform = pageTransform.GetChild(i);
                    ModView view = itemTransform.GetComponent<ModView>();
                    ModProfile profile = page.items[i];

                    if(profile == null)
                    {
                        view.DisplayLoading();
                    }
                    else
                    {
                        view.DisplayMod(profile,
                                        null,
                                        null,
                                        true, // assume subscribed
                                        false); // TODO(@jackson): enabled?

                        ModManager.GetModStatistics(profile.id,
                                                    (s) =>
                                                    {
                                                        ModDisplayData data = view.data;
                                                        data.statistics = ModStatisticsDisplayData.CreateFromStatistics(s);
                                                        view.data = data;
                                                    },
                                                    null);
                    }

                    itemTransform.gameObject.SetActive(true);
                }
            }

            for(; i < TEMP_pageSize; ++i)
            {
                Transform itemTransform = pageTransform.GetChild(i);
                itemTransform.gameObject.SetActive(false);
            }
        }

        // public void InspectProfile(ModProfile profile)
        // {
        //     if(_unsubscribeAction != null)
        //     {
        //         unsubscribeButton.onClick.RemoveListener(_unsubscribeAction);
        //     }

        //     _unsubscribeAction = () =>
        //     {
        //         if(onUnsubscribeClicked != null)
        //         {
        //             onUnsubscribeClicked(profile);
        //         }
        //     };
        //     unsubscribeButton.onClick.AddListener(_unsubscribeAction);

        //     itemInspector_modName.text = profile.name;

        //     ModBinaryRequest request = ModManager.RequestCurrentRelease(profile);
        //     if(request.isDone)
        //     {
        //         itemInspector_buttonContainer.gameObject.SetActive(true);
        //         itemInspector_downloadContainer.gameObject.SetActive(false);
        //     }
        //     else
        //     {
        //         itemInspector_downloadContainer.gameObject.SetActive(true);
        //         itemInspector_buttonContainer.gameObject.SetActive(false);

        //         StartCoroutine(UpdateProgressBar(request));
        //     }
        // }

        // public IEnumerator UpdateProgressBar(ModBinaryRequest request)
        // {
        //     itemInspector_downloadProgressBar.sizeDelta = new Vector2(0f, 0f);
        //     itemInspector_downloadProgressText.text = "Initializing";

        //     RectTransform progressBarParent = itemInspector_downloadProgressBar.parent.GetComponent<RectTransform>();
        //     while(!request.isDone)
        //     {
        //         if(request.webRequest != null)
        //         {
        //             float percentComplete = request.webRequest.downloadProgress;

        //             float barWidth = percentComplete * progressBarParent.rect.width;
        //             itemInspector_downloadProgressBar.sizeDelta = new Vector2(barWidth, 0f);

        //             itemInspector_downloadProgressText.text = (percentComplete * 100f).ToString("0.0") + "%";
        //         }

        //         yield return null;
        //     }

        //     itemInspector_buttonContainer.gameObject.SetActive(true);
        //     itemInspector_downloadContainer.gameObject.SetActive(false);
        // }
    }
}
