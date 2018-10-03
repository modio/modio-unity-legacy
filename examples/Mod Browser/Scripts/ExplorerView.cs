using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;


public class ExplorerView : MonoBehaviour, IModBrowserView
{
    // ---------[ FIELDS ]---------
    // ---[ EVENTS ]---
    public event Action<ModBrowserItem> onItemClicked;

    // ---[ UI ]---
    [Header("Settings")]
    public GameObject itemPrefab;
    public RectOffset minPadding;
    public float minRowSpacing;
    public float minColumnSpacing;
    public float pageTransitionTimeSeconds;

    [Header("Scene Components")]
    public RectTransform contentPane;
    public Button pageLeftButton;
    public Button pageRightButton;

    [Header("Runtime Data")]
    public RectTransform mainPage;
    public RectTransform transitionPage;
    public int columnCount;
    public int rowCount;
    public Vector2 cellSize;
    public Vector2 itemSize;
    public Vector2 cellItemOffset;
    public Vector2 gridCellOffset;
    public int currentPageIndex;
    public int targetPageIndex;
    public int queuedPageIndex;
    public int lastPageIndex;
    public bool isTransitioning;

    // ---[ CALCULATED VARS ]----
    public int pageSize { get { return this.columnCount * this.rowCount; } }

    private void Start()
    {
        isTransitioning = false;
        targetPageIndex = currentPageIndex;
        queuedPageIndex = -1;

        if(pageLeftButton != null)
        {
            pageLeftButton.onClick.AddListener(() => ChangePage(-1));
        }
        if(pageRightButton != null)
        {
            pageRightButton.onClick.AddListener(() => ChangePage(1));
        }
    }

    // ---------[ IMODBROWSERVIEW ]---------
    public IEnumerable<ModProfile> profileCollection { get; set; }

    public void InitializeLayout()
    {
        Debug.Assert(itemPrefab != null);

        // check itemPrefab componennts
        ModBrowserItem itemPrefabScript = itemPrefab.GetComponent<ModBrowserItem>();
        LayoutElement itemPrefabLayoutElement = itemPrefab.GetComponent<LayoutElement>();
        RectTransform itemPrefabTransform = itemPrefab.GetComponent<RectTransform>();

        Debug.Assert(itemPrefabScript != null
                     && itemPrefabLayoutElement != null
                     && itemPrefabTransform != null,
                     "[mod.io] The ExplorerView.itemPrefab is missing at least one of the following"
                     + " components: a ModBrowserItem, a LayoutElement, or a RectTransform.");

        Debug.Assert(itemPrefabTransform.anchorMin == new Vector2(0f, 1f)
                     && itemPrefabTransform.anchorMax == new Vector2(0f, 1f),
                     "[mod.io] The ExplorerView.itemPrefab's transfrom needs a top-left anchor."
                     + " Please ensure the both the anchor min and anchor max are at [0, 1].");

        // initialize pages
        if(mainPage == null || transitionPage == null)
        {
            foreach(Transform t in contentPane)
            {
                GameObject.Destroy(t.gameObject);
            }

            mainPage = (new GameObject("Mod Page: Main")).AddComponent<RectTransform>();
            mainPage.SetParent(contentPane);
            mainPage.anchorMin = Vector2.zero;
            mainPage.anchorMax = Vector2.zero;
            mainPage.offsetMin = Vector2.zero;
            mainPage.offsetMax = new Vector2(contentPane.rect.width, contentPane.rect.height);

            transitionPage = (new GameObject("Mod Page: Transitional")).AddComponent<RectTransform>();
            transitionPage.SetParent(contentPane);
            transitionPage.anchorMin = Vector2.zero;
            transitionPage.anchorMax = Vector2.zero;
            transitionPage.offsetMin = new Vector2(contentPane.rect.width, 0f);
            transitionPage.offsetMax = new Vector2(contentPane.rect.width * 2f,
                                                   contentPane.rect.height);
        }

        // - calculate size vars -
        // width
        float prefItemWidth = itemPrefabLayoutElement.preferredWidth;
        if(prefItemWidth < 0)
        {
            prefItemWidth = itemPrefabTransform.rect.width;
        }

        float minItemWidth = itemPrefabLayoutElement.minWidth;
        if(minItemWidth < 0)
        {
            minItemWidth = prefItemWidth;
        }

        float contentWidth = contentPane.rect.width - minPadding.horizontal + minColumnSpacing;
        this.columnCount = (int)Mathf.Floor(contentWidth / (minItemWidth + minColumnSpacing));

        cellSize.x = contentWidth / (float)this.columnCount;
        gridCellOffset.x = (contentPane.rect.width - (cellSize.x * (float)this.columnCount)) * 0.5f;
        itemSize.x = cellSize.x - minColumnSpacing;
        if(itemSize.x > prefItemWidth)
        {
            itemSize.x = prefItemWidth;
        }
        cellItemOffset.x = (cellSize.x - itemSize.x) * 0.5f;

        // height
        float prefItemHeight = itemPrefabLayoutElement.preferredHeight;
        if(prefItemHeight < 0)
        {
            prefItemHeight = itemPrefabTransform.rect.height;
        }

        float minItemHeight = itemPrefabLayoutElement.minHeight;
        if(minItemHeight < 0)
        {
            minItemHeight = prefItemHeight;
        }

        float contentHeight = contentPane.rect.height - minPadding.vertical + minRowSpacing;
        this.rowCount = (int)Mathf.Floor(contentHeight / (minItemHeight + minRowSpacing));

        cellSize.y = contentHeight / (float)this.rowCount;
        gridCellOffset.y = (contentPane.rect.height - (cellSize.y * (float)this.rowCount)) * 0.5f;
        itemSize.y = cellSize.y - minColumnSpacing;
        if(itemSize.y > prefItemHeight)
        {
            itemSize.y = prefItemHeight;
        }
        cellItemOffset.y = (cellSize.y - itemSize.y) * 0.5f;
    }

    /// <summary>Refreshes the view using the current data.</summary>
    public void Refresh()
    {
        Debug.Assert(mainPage != null && transitionPage != null,
                     "[mod.io] ExplorerView.Refresh() cannot be called until after ExplorerView.InitializeLayout() has been called.");
        Debug.Assert(pageSize > 0,
                     "[mod.io] PageSize has an invalid value. This is because either the columnCount"
                     + " or rowCount has been calculated to be less than 1.");

        Debug.Assert(itemPrefab != null);

        // clear existing items
        foreach(Transform t in mainPage)
        {
            ModBrowserItem item = t.GetComponent<ModBrowserItem>();
            if(item != null)
            {
                item.onClick -= NotifyItemClicked;
            }

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

        // collect the profiles in view
        FillModPage(CacheClient.IterateAllModProfilesFromOffset(currentPageIndex * pageSize),
                    mainPage);

        lastPageIndex = (int)Mathf.Ceil((float)CacheClient.CountModProfiles() / (float)pageSize) - 1;

        // handle page transitioning
        if(isTransitioning)
        {
            foreach(Transform t in transitionPage)
            {
                ModBrowserItem item = t.GetComponent<ModBrowserItem>();
                if(item != null)
                {
                    item.onClick -= NotifyItemClicked;
                }

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

                FillModPage(CacheClient.IterateAllModProfilesFromOffset(targetPageIndex * pageSize),
                            transitionPage);
            }
        }

        // update buttons
        if(pageLeftButton != null)
        {
            pageLeftButton.interactable = (currentPageIndex > 0);
        }
        if(pageRightButton != null)
        {
            pageRightButton.interactable = (currentPageIndex < lastPageIndex);
        }
    }

    private void FillModPage(IEnumerable<ModProfile> profiles, RectTransform pageTransform)
    {
        IEnumerator<ModProfile> pEnumerator = profiles.GetEnumerator();

        for(int index = 0;
            index < this.pageSize && pEnumerator.MoveNext();
            ++index)
        {
            GameObject itemGO = GameObject.Instantiate(itemPrefab,
                                           new Vector3(),
                                           Quaternion.identity,
                                           pageTransform);

            // calculate layout
            int itemX = index % this.columnCount;
            int itemY = index / this.columnCount;

            Vector2 itemPos = new Vector2();
            itemPos.x = (gridCellOffset.x + cellItemOffset.x
                         + (itemX * cellSize.x));
            itemPos.y = (gridCellOffset.y + cellItemOffset.y
                         + (itemY * cellSize.y)) * -1f;

            RectTransform itemTransform = itemGO.GetComponent<RectTransform>();
            itemTransform.anchoredPosition = itemPos;
            itemTransform.sizeDelta = itemSize;

            // display mod profile
            ModBrowserItem item = itemGO.GetComponent<ModBrowserItem>();
            item.profile = pEnumerator.Current;
            item.onClick += NotifyItemClicked;
            item.Initialize();
            item.UpdateProfileUIComponents();
            item.UpdateStatisticsUIComponents();

            ModManager.GetModStatistics(item.profile.id,
                                        (s) => { item.statistics = s; item.UpdateStatisticsUIComponents(); },
                                        null);
        }
    }

    public void MoveToPage(int pageIndex)
    {
        #if DEBUG
        Debug.Assert(pageIndex >= 0 && pageIndex <= lastPageIndex);
        #else
        if(pageIndex < 0) { pageIndex = 0; }
        if(pageIndex > lastPageIndex) { pageIndex = lastPageIndex; }
        #endif

        if(currentPageIndex == pageIndex) { return; }

        if(!isTransitioning)
        {
            targetPageIndex = pageIndex;
            StartCoroutine(TransitionPageCoroutine());
        }
        else
        {
            queuedPageIndex = targetPageIndex;
        }
    }

    private IEnumerator TransitionPageCoroutine()
    {
        isTransitioning = true;

        if(pageLeftButton != null)
        {
            pageLeftButton.interactable = false;
        }
        if(pageRightButton != null)
        {
            pageRightButton.interactable = false;
        }

        // load up transition panel
        FillModPage(CacheClient.IterateAllModProfilesFromOffset(targetPageIndex * pageSize),
                    transitionPage);

        // transition
        float transitionTime = Time.deltaTime;
        float mainPaneTargetX = contentPane.rect.width * (currentPageIndex < targetPageIndex ? -1f : 1f);
        float transPaneStartX = mainPaneTargetX * -1f;
        while(transitionTime < pageTransitionTimeSeconds)
        {
            float transPos = Mathf.Lerp(0f, mainPaneTargetX, transitionTime / pageTransitionTimeSeconds);

            mainPage.offsetMin = new Vector2(transPos,
                                                  0f);
            mainPage.offsetMax = new Vector2(transPos + contentPane.rect.width,
                                                  contentPane.rect.height);

            transitionPage.offsetMin = new Vector2(transPos + transPaneStartX,
                                                          0f);
            transitionPage.offsetMax = new Vector2(transPos + transPaneStartX + contentPane.rect.width,
                                                          contentPane.rect.height);

            transitionTime += Time.deltaTime;

            yield return null;
        }

        // finalize
        transitionPage.offsetMin = Vector2.zero;
        transitionPage.offsetMax = new Vector2(0f, contentPane.rect.height);

        foreach(Transform t in mainPage)
        {
            ModBrowserItem item = t.GetComponent<ModBrowserItem>();
            if(item != null)
            {
                item.onClick -= NotifyItemClicked;
            }

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

        mainPage.offsetMin = Vector2.zero;
        mainPage.offsetMax = new Vector2(0f, contentPane.rect.height);

        while(transitionPage.childCount > 0)
        {
            transitionPage.GetChild(0).SetParent(mainPage);
        }

        currentPageIndex = targetPageIndex;
        isTransitioning = false;

        if(pageLeftButton != null)
        {
            pageLeftButton.interactable = (currentPageIndex > 0);
        }
        if(pageRightButton != null)
        {
            pageRightButton.interactable = (currentPageIndex < lastPageIndex);
        }

        if(queuedPageIndex > 0)
        {
            targetPageIndex = queuedPageIndex;
            queuedPageIndex = 0;
            StartCoroutine(TransitionPageCoroutine());
        }
    }

    public void ChangePage(int direction)
    {
        MoveToPage(currentPageIndex + direction);
    }

    // ---------[ EVENTS ]---------
    private void NotifyItemClicked(ModBrowserItem item)
    {
        if(onItemClicked != null) { onItemClicked(item); }
    }
}
