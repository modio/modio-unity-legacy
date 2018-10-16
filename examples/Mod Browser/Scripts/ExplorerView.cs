using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public enum PageTransitionDirection
{
    FromLeft,
    FromRight,
}

// TODO(@jackson): The padding/spacing maths might need work?
public class ExplorerView : MonoBehaviour
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
    public Text pageIndexText;
    public Text pageCountText;

    [Header("Display Data")]
    public ModProfile[] mainPageProfiles;
    public ModProfile[] transitionPageProfiles;

    [Header("Runtime Data")]
    public bool isTransitioning;
    public RectTransform mainPage;
    public RectTransform transitionPage;
    public int columnCount;
    public float columnWidth;
    public int rowCount;
    public float rowHeight;
    public Vector2 itemSize;
    public Vector2 itemOffset;

    // ---[ CALCULATED VARS ]----
    public int pageSize { get { return this.columnCount * this.rowCount; } }

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(itemPrefab != null);

        isTransitioning = false;

        // - check itemPrefab components -
        ModBrowserItem itemPrefabScript = itemPrefab.GetComponent<ModBrowserItem>();
        RectTransform itemPrefabTransform = itemPrefab.GetComponent<RectTransform>();

        Debug.Assert(itemPrefabScript != null
                     && itemPrefabTransform != null,
                     "[mod.io] The ExplorerView.itemPrefab is missing a ModBrowserItem component"
                     + " and/or a RectTransform component.");

        Debug.Assert(itemPrefabTransform.anchorMin == new Vector2(0f, 1f)
                     && itemPrefabTransform.anchorMax == new Vector2(0f, 1f),
                     "[mod.io] The ExplorerView.itemPrefab's transfrom needs a top-left anchor."
                     + " Please ensure the both the anchor min and anchor max are at [0, 1].");

        // - calculate size vars -
        // TODO(@jackson): WHOLE PIXELS!
        this.itemOffset = Vector2.zero;

        // width
        float minItemWidth = itemPrefabScript.minimumScaleFactor * itemPrefabTransform.rect.width;
        float maxItemWidth = itemPrefabScript.maximumScaleFactor * itemPrefabTransform.rect.width;
        float contentWidth = contentPane.rect.width - minPadding.horizontal + minColumnSpacing;

        this.columnCount = (int)Mathf.Floor(contentWidth / (minItemWidth + minColumnSpacing));
        this.columnWidth = (contentPane.rect.width - minPadding.horizontal) / (float)this.columnCount;
        this.itemSize.x = Mathf.Min(this.columnWidth - minColumnSpacing, maxItemWidth);
        this.itemOffset.x = 0.5f * (this.columnWidth - this.itemSize.x);

        // height
        float minItemHeight = itemPrefabScript.minimumScaleFactor * itemPrefabTransform.rect.height;
        float maxItemHeight = itemPrefabScript.maximumScaleFactor * itemPrefabTransform.rect.height;
        float contentHeight = contentPane.rect.height - minPadding.vertical + minRowSpacing;

        this.rowCount = (int)Mathf.Floor(contentHeight / (minItemHeight + minRowSpacing));
        this.rowHeight = (contentPane.rect.height - minPadding.vertical) / (float)this.rowCount;
        this.itemSize.y = Mathf.Min(this.rowHeight - minRowSpacing, maxItemHeight);
        this.itemOffset.y = 0.5f * (this.rowHeight - this.itemSize.y);

        // - initialize pages -
        foreach(Transform t in contentPane)
        {
            GameObject.Destroy(t.gameObject);
        }

        mainPage = (new GameObject("Mod Page")).AddComponent<RectTransform>();
        mainPage.SetParent(contentPane);
        mainPage.anchorMin = Vector2.zero;
        mainPage.anchorMax = Vector2.zero;
        mainPage.offsetMin = Vector2.zero;
        mainPage.offsetMax = new Vector2(contentPane.rect.width, contentPane.rect.height);
        InitializePageLayout(mainPage);

        transitionPage = (new GameObject("Mod Page")).AddComponent<RectTransform>();
        transitionPage.SetParent(contentPane);
        transitionPage.anchorMin = Vector2.zero;
        transitionPage.anchorMax = Vector2.zero;
        transitionPage.offsetMin = new Vector2(contentPane.rect.width, 0f);
        transitionPage.offsetMax = new Vector2(contentPane.rect.width * 2f,
                                               contentPane.rect.height);
        InitializePageLayout(transitionPage);

        transitionPage.gameObject.SetActive(false);

        mainPageProfiles = new ModProfile[this.pageSize];
        transitionPageProfiles = new ModProfile[this.pageSize];
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
            index < this.pageSize;
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
            itemPos.x = (this.minPadding.left + this.itemOffset.x + itemX * this.columnWidth);
            itemPos.y = (this.minPadding.top  + this.itemOffset.y + itemY * this.rowHeight) * -1;

            RectTransform itemTransform = itemGO.GetComponent<RectTransform>();
            itemTransform.anchoredPosition = itemPos;
            itemTransform.sizeDelta = itemSize;

            // display mod profile
            ModBrowserItem item = itemGO.GetComponent<ModBrowserItem>();
            item.index = index;
            item.profile = null;
            item.onClick += NotifyItemClicked;
            item.Initialize();

            itemGO.SetActive(false);
        }
    }

    // ----------[ PAGE DISPLAY ]---------
    public void UpdateMainPageUIComponents()
    {
        Debug.Assert(mainPage != null,
                     "[mod.io] ExplorerView.Initialize has not yet been called");
        Debug.Assert(pageSize > 0,
                     "[mod.io] PageSize has an invalid value. This is because either the columnCount"
                     + " or rowCount has been calculated to be less than 1.");
        Debug.Assert(mainPageProfiles.Length == pageSize,
                     "[mod.io] The quantity of profiles to display must match the the calculated page size."
                     + " Any excess array slots should be set to NULL."
                     + "\nPageSize=" + pageSize + " - ArraySize=" + mainPageProfiles.Length);

        #if DEBUG
        if(isTransitioning)
        {
            Debug.LogWarning("[mod.io] Explorer View is currently transitioning between pages. It"
                             + " is recommended to not update page displays at this time.");
        }
        #endif

        SetPageMods(this.mainPageProfiles, this.mainPage);
    }

    public void UpdateTransitionPageUIComponents()
    {
        Debug.Assert(transitionPage != null,
                     "[mod.io] ExplorerView.Initialize has not yet been called");
        Debug.Assert(pageSize > 0,
                     "[mod.io] PageSize has an invalid value. This is because either the columnCount"
                     + " or rowCount has been calculated to be less than 1.");
        Debug.Assert(transitionPageProfiles.Length == pageSize,
                     "[mod.io] The quantity of profiles to display must match the the calculated page size."
                     + " Any excess array slots should be set to NULL."
                     + "\nPageSize=" + pageSize + " - ArraySize=" + transitionPageProfiles.Length);

        #if DEBUG
        if(isTransitioning)
        {
            Debug.LogWarning("[mod.io] Explorer View is currently transitioning between pages. It"
                             + " is recommended to not update page displays at this time.");
        }
        #endif

        SetPageMods(this.transitionPageProfiles, this.transitionPage);
    }

    private void SetPageMods(ModProfile[] profiles, RectTransform pageTransform)
    {
        for(int i = 0; i < pageSize; ++i)
        {
            Transform itemTransform = pageTransform.GetChild(i);
            ModBrowserItem item = itemTransform.GetComponent<ModBrowserItem>();
            item.profile = profiles[i];
            item.statistics = null;

            if(item.profile == null)
            {
                itemTransform.gameObject.SetActive(false);
            }
            else
            {
                item.UpdateProfileUIComponents();
                item.UpdateStatisticsUIComponents();

                itemTransform.gameObject.SetActive(true);

                ModManager.GetModStatistics(item.profile.id,
                                            (s) => { item.statistics = s; item.UpdateStatisticsUIComponents(); },
                                            null);
            }
        }
    }

    // ----------[ PAGE TRANSITIONS ]---------
    public void InitiatePageTransition(PageTransitionDirection direction, Action onTransitionCompleted)
    {
        if(!isTransitioning)
        {
            float mainPaneTargetX = contentPane.rect.width * (direction == PageTransitionDirection.FromLeft ? 1f : -1f);
            float transPaneStartX = mainPaneTargetX * -1f;

            mainPage.offsetMin = Vector2.zero;
            mainPage.offsetMax = new Vector2(contentPane.rect.width, contentPane.rect.height);

            transitionPage.offsetMin = new Vector2(transPaneStartX, 0f);
            transitionPage.offsetMax = new Vector2(transPaneStartX + contentPane.rect.width,
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

        transitionPage.gameObject.SetActive(true);

        float transitionTime = 0f;

        // transition
        while(transitionTime < transitionLength)
        {
            float transPos = Mathf.Lerp(0f, mainPaneTargetX, transitionTime / transitionLength);

            mainPage.offsetMin = new Vector2(transPos,
                                             0f);
            mainPage.offsetMax = new Vector2(transPos + contentPane.rect.width,
                                             contentPane.rect.height);

            transitionPage.offsetMin = new Vector2(transPos + transitionPaneStartX,
                                                   0f);
            transitionPage.offsetMax = new Vector2(transPos + transitionPaneStartX + contentPane.rect.width,
                                                   contentPane.rect.height);

            transitionTime += Time.deltaTime;

            yield return null;
        }

        // finalize
        transitionPage.offsetMin = Vector2.zero;
        transitionPage.offsetMax = new Vector2(contentPane.rect.width, contentPane.rect.height);

        mainPage.gameObject.SetActive(false);

        RectTransform tempPage = mainPage;
        mainPage = transitionPage;
        transitionPage = tempPage;

        ModProfile[] tempProfiles = mainPageProfiles;
        mainPageProfiles = transitionPageProfiles;
        transitionPageProfiles = tempProfiles;

        isTransitioning = false;

        if(onTransitionCompleted != null)
        {
            onTransitionCompleted();
        }
    }

    // ---------[ EVENTS ]---------
    private void NotifyItemClicked(ModBrowserItem item)
    {
        if(onItemClicked != null) { onItemClicked(item); }
    }
}
