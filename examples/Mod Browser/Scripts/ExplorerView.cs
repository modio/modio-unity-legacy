using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;


public class ExplorerView : MonoBehaviour, IModBrowserView
{
    // ---------[ FIELDS ]---------
    // - Events -
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
    public RectTransform innerPaneMain;
    public RectTransform innerPaneTransitional;
    public int columnCount;
    public int rowCount;
    public Vector2 calculatedCellSize;
    public Vector2 calculatedItemSize;
    public Vector2 calculatedItemCellOffset;
    public Vector2 calculatedGridOffset;
    public int currentPage;
    public int targetPage;
    public int queuedTargetPage;
    public int lastPage;
    public bool isPageTransitioning;

    // ---[ CALCULATED VARS ]----
    public int pageSize { get { return this.columnCount * this.rowCount; } }

    private void Start()
    {
        pageLeftButton.onClick.AddListener(() => ChangePage(-1));
        pageRightButton.onClick.AddListener(() => ChangePage(1));
        isPageTransitioning = false;
        targetPage = currentPage;
        queuedTargetPage = -1;
    }

    // ---------[ IMODBROWSERVIEW ]---------
    public IEnumerable<ModProfile> profileCollection { get; set; }

    public void InitializeLayout()
    {
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

        // initialize inner panes
        if(innerPaneMain == null || innerPaneTransitional == null)
        {
            foreach(Transform t in contentPane)
            {
                GameObject.Destroy(t.gameObject);
            }

            innerPaneMain = (new GameObject("Inner Pane: Main")).AddComponent<RectTransform>();
            innerPaneMain.SetParent(contentPane);
            innerPaneMain.anchorMin = Vector2.zero;
            innerPaneMain.anchorMax = Vector2.zero;
            innerPaneMain.offsetMin = Vector2.zero;
            innerPaneMain.offsetMax = new Vector2(contentPane.rect.width, contentPane.rect.height);

            innerPaneTransitional = (new GameObject("Inner Pane: Transitional")).AddComponent<RectTransform>();
            innerPaneTransitional.SetParent(contentPane);
            innerPaneTransitional.anchorMin = Vector2.zero;
            innerPaneTransitional.anchorMax = Vector2.zero;
            innerPaneTransitional.offsetMin = new Vector2(contentPane.rect.width, 0f);
            innerPaneTransitional.offsetMax = new Vector2(contentPane.rect.width * 2f,
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

        calculatedCellSize.x = contentWidth / (float)this.columnCount;
        calculatedGridOffset.x = (contentPane.rect.width - (calculatedCellSize.x * (float)this.columnCount)) * 0.5f;
        calculatedItemSize.x = calculatedCellSize.x - minColumnSpacing;
        if(calculatedItemSize.x > prefItemWidth)
        {
            calculatedItemSize.x = prefItemWidth;
        }
        calculatedItemCellOffset.x = (calculatedCellSize.x - calculatedItemSize.x) * 0.5f;

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

        calculatedCellSize.y = contentHeight / (float)this.rowCount;
        calculatedGridOffset.y = (contentPane.rect.height - (calculatedCellSize.y * (float)this.rowCount)) * 0.5f;
        calculatedItemSize.y = calculatedCellSize.y - minColumnSpacing;
        if(calculatedItemSize.y > prefItemHeight)
        {
            calculatedItemSize.y = prefItemHeight;
        }
        calculatedItemCellOffset.y = (calculatedCellSize.y - calculatedItemSize.y) * 0.5f;
    }

    /// <summary>Refreshes the view using the current data.</summary>
    public void Refresh()
    {
        // clear existing items
        foreach(Transform t in innerPaneMain)
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
        IEnumerator<ModProfile> mainProfiles = CacheClient.IterateAllModProfilesFromOffset(currentPage * pageSize).GetEnumerator();
        List<ModProfile> mainPaneMods = new List<ModProfile>(this.pageSize);

        while(mainPaneMods.Count < this.pageSize
              && mainProfiles.MoveNext())
        {
            mainPaneMods.Add(mainProfiles.Current);
        }
        lastPage = (int)Mathf.Ceil((float)CacheClient.CountModProfiles() / (float)pageSize) - 1;

        // create new items
        for(int i = 0; i < mainPaneMods.Count; ++i)
        {
            CreateItem(mainPaneMods[i], innerPaneMain, i);
        }

        // handle page transitioning
        if(isPageTransitioning)
        {
            foreach(Transform t in innerPaneTransitional)
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

            IEnumerator<ModProfile> transProfiles = CacheClient.IterateAllModProfilesFromOffset(targetPage * pageSize).GetEnumerator();
            List<ModProfile> transitionPaneMods = new List<ModProfile>(this.pageSize);

            while(transitionPaneMods.Count < this.pageSize
                  && transProfiles.MoveNext())
            {
                transitionPaneMods.Add(transProfiles.Current);
            }

            for(int i = 0; i < transitionPaneMods.Count; ++i)
            {
                CreateItem(transitionPaneMods[i], innerPaneTransitional, i);
            }

        }

        // update buttons
        pageLeftButton.interactable = (currentPage > 0);
        pageRightButton.interactable = (currentPage < lastPage);
    }

    private GameObject CreateItem(ModProfile profile, RectTransform pane, int index)
    {
        GameObject itemGO = GameObject.Instantiate(itemPrefab,
                                                   new Vector3(),
                                                   Quaternion.identity,
                                                   pane);

        // calculate layout
        int itemX = index % this.columnCount;
        int itemY = index / this.columnCount;

        Vector2 itemPos = new Vector2();
        itemPos.x = (calculatedGridOffset.x + calculatedItemCellOffset.x
                     + (itemX * calculatedCellSize.x));
        itemPos.y = (calculatedGridOffset.y + calculatedItemCellOffset.y
                     + (itemY * calculatedCellSize.y)) * -1f;

        RectTransform itemTransform = itemGO.GetComponent<RectTransform>();
        itemTransform.anchoredPosition = itemPos;
        itemTransform.sizeDelta = calculatedItemSize;

        // display mod profile
        ModBrowserItem item = itemGO.GetComponent<ModBrowserItem>();
        item.profile = profile;
        item.onClick += NotifyItemClicked;
        item.Initialize();
        item.UpdateProfileUIComponents();
        item.UpdateStatisticsUIComponents();

        ModManager.GetModStatistics(item.profile.id,
                                    (s) => { item.statistics = s; item.UpdateStatisticsUIComponents(); },
                                    null);

        return itemGO;
    }

    public void MoveToPage(int pageIndex)
    {
        Debug.Assert(pageIndex >= 0 && pageIndex <= lastPage);
        if(currentPage == pageIndex) { return; }

        if(!isPageTransitioning)
        {
            targetPage = pageIndex;
            StartCoroutine(TransitionPageCoroutine());
        }
        else
        {
            queuedTargetPage = targetPage;
        }
    }

    private IEnumerator TransitionPageCoroutine()
    {
        isPageTransitioning = true;

        pageLeftButton.interactable = false;
        pageRightButton.interactable = false;

        // load up transition panel
        IEnumerator<ModProfile> profileEnumerator = CacheClient.IterateAllModProfilesFromOffset(targetPage * pageSize).GetEnumerator();
        List<ModProfile> transitionPaneMods = new List<ModProfile>(pageSize);

        while(transitionPaneMods.Count < this.pageSize
              && profileEnumerator.MoveNext())
        {
            transitionPaneMods.Add(profileEnumerator.Current);
        }

        for(int i = 0; i < transitionPaneMods.Count; ++i)
        {
            CreateItem(transitionPaneMods[i], innerPaneTransitional, i);
        }

        // transition
        float transitionTime = Time.deltaTime;
        float mainPaneTargetX = contentPane.rect.width * (currentPage < targetPage ? -1f : 1f);
        float transPaneStartX = mainPaneTargetX * -1f;
        while(transitionTime < pageTransitionTimeSeconds)
        {
            float transPos = Mathf.Lerp(0f, mainPaneTargetX, transitionTime / pageTransitionTimeSeconds);

            innerPaneMain.offsetMin = new Vector2(transPos,
                                                  0f);
            innerPaneMain.offsetMax = new Vector2(transPos + contentPane.rect.width,
                                                  contentPane.rect.height);

            innerPaneTransitional.offsetMin = new Vector2(transPos + transPaneStartX,
                                                          0f);
            innerPaneTransitional.offsetMax = new Vector2(transPos + transPaneStartX + contentPane.rect.width,
                                                          contentPane.rect.height);

            transitionTime += Time.deltaTime;

            yield return null;
        }

        // finalize
        innerPaneTransitional.offsetMin = Vector2.zero;
        innerPaneTransitional.offsetMax = new Vector2(0f, contentPane.rect.height);

        foreach(Transform t in innerPaneMain)
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

        innerPaneMain.offsetMin = Vector2.zero;
        innerPaneMain.offsetMax = new Vector2(0f, contentPane.rect.height);

        while(innerPaneTransitional.childCount > 0)
        {
            innerPaneTransitional.GetChild(0).SetParent(innerPaneMain);
        }

        currentPage = targetPage;
        pageLeftButton.interactable = (currentPage > 0);
        pageRightButton.interactable = (currentPage < lastPage);
        isPageTransitioning = false;

        if(queuedTargetPage > 0)
        {
            targetPage = queuedTargetPage;
            queuedTargetPage = 0;
            StartCoroutine(TransitionPageCoroutine());
        }
    }

    public void ChangePage(int direction)
    {
        MoveToPage(currentPage + direction);
    }

    // ---------[ EVENTS ]---------
    private void NotifyItemClicked(ModBrowserItem item)
    {
        if(onItemClicked != null) { onItemClicked(item); }
    }
}
