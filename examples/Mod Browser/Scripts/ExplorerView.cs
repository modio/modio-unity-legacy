using System;
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

    [Header("Scene Components")]
    public RectTransform contentPane;

    [Header("Runtime Data")]
    public RectTransform innerPaneMain;
    public RectTransform innerPaneTransitional;
    public int columnCount;
    public int rowCount;
    public Vector2 calculatedCellSize;
    public Vector2 calculatedItemSize;
    public Vector2 calculatedItemCellOffset;

    // ---[ DATA ]---
    public int firstModProfileOffset;

    // ---[ CALCULATED VARS ]----
    public int pageSize { get { return this.columnCount * this.rowCount; } }


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
            innerPaneMain.anchorMin = new Vector2(0f, 0f);
            innerPaneMain.anchorMax = new Vector2(1f, 1f);
            innerPaneMain.offsetMin = new Vector2(0f, 0f);
            innerPaneMain.offsetMax = new Vector2(0f, 0f);

            innerPaneTransitional = (new GameObject("Inner Pane: Transitional")).AddComponent<RectTransform>();
            innerPaneTransitional.SetParent(contentPane);
            innerPaneTransitional.anchorMin = new Vector2(0f, 0f);
            innerPaneTransitional.anchorMax = new Vector2(1f, 1f);
            innerPaneTransitional.offsetMin = new Vector2(0f, 0f);
            innerPaneTransitional.offsetMax = new Vector2(0f, 0f);
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
        calculatedItemSize.y = calculatedCellSize.y - minColumnSpacing;
        if(calculatedItemSize.y > prefItemHeight)
        {
            calculatedItemSize.y = prefItemHeight;
        }
        calculatedItemCellOffset.y = (calculatedCellSize.y - calculatedItemSize.y) * 0.5f;
    }

    /// <summary>Refreshes the view using the current settings.</summary>
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
        IEnumerator<ModProfile> profileEnumerator = CacheClient.IterateAllModProfilesFromOffset(firstModProfileOffset).GetEnumerator();
        List<ModProfile> modProfileCollection = new List<ModProfile>(this.pageSize);
        while(modProfileCollection.Count < this.pageSize
              && profileEnumerator.MoveNext())
        {
            modProfileCollection.Add(profileEnumerator.Current);
        }

        // create new items
        for(int i = 0; i < modProfileCollection.Count; ++i)
        {
            GameObject itemGO = GameObject.Instantiate(itemPrefab,
                                                       new Vector3(),
                                                       Quaternion.identity,
                                                       innerPaneMain);

            // calculate layout
            int itemX = i % this.columnCount;
            int itemY = i / this.columnCount;

            Vector2 itemPos = new Vector2();
            itemPos.x = (minPadding.left + calculatedItemCellOffset.x
                         + (itemX * calculatedCellSize.x));
            itemPos.y = (minPadding.top + calculatedItemCellOffset.y
                         + (itemY * calculatedCellSize.y)) * -1f;

            RectTransform itemTransform = itemGO.GetComponent<RectTransform>();
            itemTransform.anchoredPosition = itemPos;
            itemTransform.sizeDelta = calculatedItemSize;

            // display mod profile
            ModBrowserItem item = itemGO.GetComponent<ModBrowserItem>();
            item.modProfile = modProfileCollection[i];
            item.onClick += NotifyItemClicked;
            item.Initialize();
            item.UpdateProfileUIComponents();
            item.UpdateStatisticsUIComponents();

            ModManager.GetModStatistics(item.modProfile.id,
                                        (s) => { item.modStatistics = s; item.UpdateStatisticsUIComponents(); },
                                        null);
        }
    }

    // ---------[ EVENTS ]---------
    private void NotifyItemClicked(ModBrowserItem item)
    {
        if(onItemClicked != null) { onItemClicked(item); }
    }
}
