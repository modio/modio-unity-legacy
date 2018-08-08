using System.Collections.Generic;

using UnityEngine;

using ModIO;

public class ModGallery : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    // --- Scene Data ---
    public GameObject modPreviewPrefab;
    public Transform contentPane;
    public ModGalleryPreview[] previewTiles;

    // --- Key Data ---
    private int pageSize;
    private int pageIndex;
    private IEnumerator<ModProfile> profileIterator;

    public int TEST_columnCount = 3;
    public int TEST_rowDisplayCount = 3;
    public float TEST_columnWidth;
    public float TEST_rowHeight;
    public float TEST_rowPadding;

    public void Start()
    {
        previewTiles = contentPane.GetComponentsInChildren<ModGalleryPreview>(true);

        foreach(ModGalleryPreview preview in previewTiles)
        {
            preview.onClick += OnPreviewClick;
        }
    }

    public void OnPreviewClick(ModGalleryPreview preview)
    {
        preview.modNameText.text = "CLICKED!";
    }

    public void Initialize()
    {
        // clear existing preview tiles
        foreach(ModGalleryPreview preview in previewTiles)
        {
            preview.onClick -= OnPreviewClick;

            Object.Destroy(preview.gameObject);
        }

        // get mod profiles to display
        pageSize = 100;
        // pageSize = TEST_columnCount * 2;
        pageIndex = 0;
        profileIterator = CacheClient.IterateAllModProfiles().GetEnumerator();

        List<ModProfile> modProfileCollection = new List<ModProfile>(pageSize);
        while(pageIndex < pageSize
              && profileIterator.MoveNext())
        {
            modProfileCollection.Add(profileIterator.Current);
            ++pageIndex;
        }

        // create new previews
        int previewX = 0;
        int previewY = 0;

        foreach(ModProfile profile in modProfileCollection)
        {
            if(previewX >= TEST_columnCount)
            {
                previewX = 0;
                ++previewY;
            }

            float xPosInitiator = -0.5f * (TEST_columnCount - 1);

            Vector2 previewPos = new Vector2((xPosInitiator + previewX) * TEST_columnWidth,
                                             -1 * (previewY * (TEST_rowPadding + TEST_rowHeight) + TEST_rowPadding));

            GameObject previewGO = Object.Instantiate(modPreviewPrefab,
                                                      new Vector3(),
                                                      Quaternion.identity,
                                                      contentPane);

            RectTransform previewTransform = previewGO.GetComponent<RectTransform>();
            previewTransform.anchoredPosition = previewPos;

            ModGalleryPreview preview = previewGO.GetComponent<ModGalleryPreview>();
            preview.modProfile = profile;
            preview.onClick += OnPreviewClick;
            preview.UpdateDisplay();

            ++previewX;
        }

        UpdateContentSize();
    }

    public void UpdateContentSize()
    {
        float newHeight = (Mathf.Ceil((float)pageIndex / (float)TEST_columnCount)
                           * (TEST_rowPadding + TEST_rowHeight)
                           + TEST_rowPadding);

        RectTransform contentTransform = contentPane.GetComponent<RectTransform>();
        contentTransform.sizeDelta = new Vector2(contentTransform.sizeDelta.x,
                                                 newHeight);
    }
}
