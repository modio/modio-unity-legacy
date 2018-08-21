using System;
using System.Collections.Generic;

using UnityEngine;

using ModIO;

public enum ViewLayoutStyle
{
    GridView,
    TableView,
}

public class ModBrowserView : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    // --- Events ---
    public event Action<ModBrowserItem> onItemClicked;

    // --- Settings ---
    public GameObject browserItemPrefab;
    public Transform contentPane;
    public ViewLayoutStyle layoutStyle;
    public float minColumnPadding;
    public float rowPadding;

    // --- Run-Time Data ---

    // --- Key Data ---

    // --- Layout Data ---
    private float _viewWidth = 0f;
    private float _itemHeight = 0f;
    private float _itemWidth = 0f;
    private float _columnPadding = 0f;
    private int _columnCount = 0;

    // --- TEMP DATA ---
    public IEnumerator<ModProfile> profileIterator;
    public int TEST_rowDisplayCount;
    public int TEST_pageSize;
    public int TEST_pageIndex;


    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // check browserItemPrefab transform
        if(browserItemPrefab.GetComponent<RectTransform>() == null
           || browserItemPrefab.GetComponent<RectTransform>().anchorMin != new Vector2(0f, 1f)
           || browserItemPrefab.GetComponent<RectTransform>().anchorMax != new Vector2(0f, 1f))
        {
            Debug.LogError("[mod.io] Mod Browser View Item Prefab must have a "
                           + "UnityEngine.RectTransform component with an Anchor Min of [0, 1] "
                           + "and an Anchor Max of [0, 1].", this.browserItemPrefab);
            return;
        }

        // check contentPane transform
        if(contentPane.GetComponent<RectTransform>() == null
           || contentPane.GetComponent<RectTransform>().anchorMin.y != 1f
           || contentPane.GetComponent<RectTransform>().anchorMax.y != 1f)
        {
            Debug.LogError("[mod.io] Mod Browser View Content Pane must have a "
                           + "UnityEngine.RectTransform component with a top anchor",
                           this.contentPane);
            return;
        }

        // set layout data
        // ModBrowserItem prefabItemScript = browserItemPrefab.GetComponent<ModBrowserItem>();
        Rect prefabItemRect = browserItemPrefab.GetComponent<RectTransform>().rect;
        Rect contentPaneRect = contentPane.GetComponent<RectTransform>().rect;

        this._viewWidth = contentPaneRect.width;
        this._itemHeight = prefabItemRect.height;

        switch(layoutStyle)
        {
            case ViewLayoutStyle.GridView:
            {
                this._itemWidth = prefabItemRect.width;

                float minColumnWidth = prefabItemRect.width + minColumnPadding;
                this._columnCount = (int)Mathf.Floor((this._viewWidth - this.rowPadding) / (float)minColumnWidth);

                this._columnPadding = ((this._viewWidth - (prefabItemRect.width * this._columnCount))
                                       / (1f + this._columnCount));
            }
            break;
            case ViewLayoutStyle.TableView:
            {
                this._itemWidth = this._viewWidth - (2 * this.minColumnPadding);
                this._columnCount = 1;
                this._columnPadding = this.minColumnPadding;
            }
            break;
        }

        // clear existing items
        foreach(ModBrowserItem item in this.contentPane.GetComponentsInChildren<ModBrowserItem>())
        {
            item.onClick -= NotifyItemClicked;

            UnityEngine.Object.Destroy(item.gameObject);
        }

        // get mod profiles to display
        // TODO(@jackson): pageSize = rows that fit +/- 0.25?
        int pageIndex = 0;
        profileIterator = CacheClient.IterateAllModProfiles().GetEnumerator();

        List<ModProfile> modProfileCollection = new List<ModProfile>(TEST_pageSize);
        while(pageIndex < TEST_pageSize
              && profileIterator.MoveNext())
        {
            modProfileCollection.Add(profileIterator.Current);
            ++pageIndex;
        }

        // create new items
        for(int i = 0; i < modProfileCollection.Count; ++i)
        {
            GameObject itemGO = GameObject.Instantiate(browserItemPrefab,
                                                       new Vector3(),
                                                       Quaternion.identity,
                                                       contentPane);

            RectTransform itemTransform = itemGO.GetComponent<RectTransform>();
            Vector2 itemPos = CalculateItemPos(i);
            itemTransform.offsetMin = itemPos;
            itemTransform.offsetMax = new Vector2(itemPos.x + this._itemWidth,
                                                  itemPos.y + this._itemHeight);

            ModBrowserItem item = itemGO.GetComponent<ModBrowserItem>();
            item.modProfile = modProfileCollection[i];
            item.onClick += NotifyItemClicked;
            item.Initialize();
            item.UpdateDisplayObjects();
        }

        UpdateContentPaneSize();
    }

    /// <summary>Calculates the lower-left anchor offset of an item.</summary>
    public Vector2 CalculateItemPos(int index)
    {
        int x = index % this._columnCount;
        int y = index / this._columnCount;

        Vector2 pos = new Vector2(_columnPadding * (x+1) + _itemWidth * (x),
                                  -1 * (rowPadding + _itemHeight) * (y+1));

        return pos;
    }

    public void UpdateContentPaneSize()
    {
        int itemCount = this.contentPane.GetComponentsInChildren<ModBrowserItem>().Length;
        float rowCount = Mathf.Ceil((float)itemCount / (float)this._columnCount);
        float newHeight = (this.rowPadding * (rowCount + 1)
                           + this._itemHeight * (rowCount));

        RectTransform contentTransform = contentPane.GetComponent<RectTransform>();
        Vector2 offsetMin = contentTransform.offsetMin;
        offsetMin.y = -1 * newHeight;
        contentTransform.offsetMin = offsetMin;
    }

    // ---------[ EVENTS ]---------
    private void NotifyItemClicked(ModBrowserItem item)
    {
        if(onItemClicked != null) { onItemClicked(item); }
    }
}
