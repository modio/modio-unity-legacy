using System;
using System.Collections.Generic;
using UnityEngine;
using ModIO;

public enum ModBrowserLayoutMode
{
    Grid,
    Table,
}

[Serializable]
public class ModBrowserLayoutSettings
{
    public GameObject itemPrefab;
    public bool isSingleColumnLayout;
    public float minColumnPadding;
    public float rowPadding;
}

public class ExplorerView_Scrolling : MonoBehaviour, IModBrowserView
{
    // ---------[ FIELDS ]---------
    // - Events -
    public event Action<ModBrowserItem> onItemClicked;

    // ---[ SCENE COMPONENTS ]---
    [Header("Settings")]
    public GameObject itemPrefab;
    public ModBrowserLayoutMode layoutMode;
    public ModBrowserLayoutSettings gridSettings;
    public ModBrowserLayoutSettings tableSettings;

    [Header("UI Components")]
    public RectTransform contentPane;

    // --- Events ---

    // ---[ RUNTIME DATA ]---
    [Header("Runtime Data")]
    public float itemHeight;
    public float rowPadding;
    public float itemWidth;
    public float columnPadding;
    public int columnCount;

    // ---------[ PRIVATES ]---------
    private IEnumerator<ModProfile> _profileEnumerator;

    // --- TEMP DATA ---
    public int TEST_rowDisplayCount;
    public int TEST_pageSize;
    public int TEST_pageIndex;


    // ---------[ IMODBROWSERVIEW ]---------
    public IEnumerable<ModProfile> profileCollection { get; set; }

    public void InitializeLayout()
    {
        ModBrowserLayoutSettings layoutSettings = null;
        switch(this.layoutMode)
        {
            case ModBrowserLayoutMode.Grid:
            {
                layoutSettings = this.gridSettings;
            }
            break;
            case ModBrowserLayoutMode.Table:
            {
                layoutSettings = this.tableSettings;
            }
            break;
        }

        // check itemPrefab transform
        RectTransform itemPrefabTransform = layoutSettings.itemPrefab.GetComponent<RectTransform>();
        if(itemPrefabTransform == null
           || itemPrefabTransform.anchorMin != new Vector2(0f, 1f)
           || itemPrefabTransform.anchorMax != new Vector2(0f, 1f))
        {
            Debug.LogError("[mod.io] Mod Browser View Item Prefab must have a "
                           + "UnityEngine.RectTransform component with an Anchor Min of [0, 1] "
                           + "and an Anchor Max of [0, 1].", layoutSettings.itemPrefab);
            return;
        }

        // check contentPane transform
        if(contentPane.GetComponent<RectTransform>().anchorMin.y != 1f
           || contentPane.GetComponent<RectTransform>().anchorMax.y != 1f)
        {
            Debug.LogError("[mod.io] Mod Browser View Content Pane must have a "
                           + "UnityEngine.RectTransform component with a top anchor",
                           this.contentPane);
            return;
        }

        // perform simple copies
        this.itemPrefab = layoutSettings.itemPrefab;
        this.itemHeight = itemPrefabTransform.rect.height;
        this.rowPadding = layoutSettings.rowPadding;

        // calculate complex vars
        if(layoutSettings.isSingleColumnLayout)
        {
            this.itemWidth = contentPane.rect.width - (2 * layoutSettings.minColumnPadding);
            this.columnCount = 1;
            this.columnPadding = layoutSettings.minColumnPadding;
        }
        else
        {
            this.itemWidth = itemPrefabTransform.rect.width;

            float minColumnWidth = itemPrefabTransform.rect.width + layoutSettings.minColumnPadding;
            this.columnCount = (int)Mathf.Floor((contentPane.rect.width - layoutSettings.rowPadding)
                                                / minColumnWidth);

            this.columnPadding = ((contentPane.rect.width - (itemPrefabTransform.rect.width * this.columnCount))
                                   / (1f + this.columnCount));
        }
    }

    /// <summary>Refreshes the view using the current settings.</summary>
    public void Refresh()
    {
        _profileEnumerator = profileCollection.GetEnumerator();

        // clear existing items
        foreach(ModBrowserItem item in this.contentPane.GetComponentsInChildren<ModBrowserItem>())
        {
            item.onClick -= NotifyItemClicked;

            #if DEBUG
            if(!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
            else
            #endif
            {
                UnityEngine.Object.Destroy(item.gameObject);
            }
        }

        // TODO(@jackson): pageSize = rows that fit +/- 0.25?
        TEST_pageIndex = 0;

        // collect the profiles in view
        List<ModProfile> modProfileCollection = new List<ModProfile>(TEST_pageSize);
        while(TEST_pageIndex < TEST_pageSize
              && _profileEnumerator.MoveNext())
        {
            modProfileCollection.Add(_profileEnumerator.Current);
            ++TEST_pageIndex;
        }

        // create new items
        for(int i = 0; i < modProfileCollection.Count; ++i)
        {
            GameObject itemGO = GameObject.Instantiate(itemPrefab,
                                                       new Vector3(),
                                                       Quaternion.identity,
                                                       contentPane);

            RectTransform itemTransform = itemGO.GetComponent<RectTransform>();
            Vector2 itemPos = CalculateItemPos(i);
            itemTransform.offsetMin = itemPos;
            itemTransform.offsetMax = new Vector2(itemPos.x + this.itemWidth,
                                                  itemPos.y + this.itemHeight);

            ModBrowserItem item = itemGO.GetComponent<ModBrowserItem>();
            item.profile = modProfileCollection[i];
            item.onClick += NotifyItemClicked;
            item.Initialize();
            item.UpdateProfileUIComponents();
            item.UpdateStatisticsUIComponents();

            ModManager.GetModStatistics(item.profile.id,
                                        (s) => { item.statistics = s; item.UpdateStatisticsUIComponents(); },
                                        null);
        }

        ResizeContentPane(modProfileCollection.Count);
    }

    /// <summary>Calculates the lower-left anchor offset of an item.</summary>
    public Vector2 CalculateItemPos(int index)
    {
        int x = index % this.columnCount;
        int y = index / this.columnCount;

        Vector2 pos = new Vector2(columnPadding * (x+1) + itemWidth * (x),
                                  -1 * (rowPadding + itemHeight) * (y+1));

        return pos;
    }

    public void ResizeContentPane(int itemCount)
    {
        float rowCount = Mathf.Ceil((float)itemCount / (float)this.columnCount);
        float newHeight = (this.rowPadding * (rowCount + 1)
                           + this.itemHeight * (rowCount));

        RectTransform contentTransform = contentPane.GetComponent<RectTransform>();
        contentTransform.sizeDelta = new Vector2(0f, newHeight);
    }

    // ---------[ EVENTS ]---------
    private void NotifyItemClicked(ModBrowserItem item)
    {
        if(onItemClicked != null) { onItemClicked(item); }
    }
}
