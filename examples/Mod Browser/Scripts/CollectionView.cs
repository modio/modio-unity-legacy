using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class CollectionView : MonoBehaviour, IModBrowserView
{
    // ---------[ FIELDS ]---------
    // ---[ SCENE COMPONENTS ]---
    [Header("Settings")]
    public GameObject itemListing_prefab;

    [Header("UI Components")]
    public RectTransform itemListingContainer;
    public Text itemInspector_modName;
    public RectTransform itemInspector_downloadContainer;
    public RectTransform itemInspector_buttonContainer;

    // // ---[ RUNTIME DATA ]---
    // [Header("Runtime Data")]
    // public ModProfile inspectedProfile;

    // ---[ PRIVATES ]---
    private float _itemHeight;

    // ---------[ IMODBROWSERVIEW ]---------
    public IEnumerable<ModProfile> profileCollection { get; set; }

    public void InitializeLayout()
    {
        _itemHeight = itemListing_prefab.GetComponent<RectTransform>().rect.height;
    }

    public void Refresh()
    {
        // clear existing items
        foreach(Transform childItem in itemListingContainer)
        {
            GameObject.Destroy(childItem.gameObject);
        }

        // generate items
        int profileCount = 0;
        foreach(ModProfile profile in profileCollection)
        {
            GameObject profileItem_go = GameObject.Instantiate(itemListing_prefab,
                                                               new Vector3(),
                                                               Quaternion.identity,
                                                               itemListingContainer);

            RectTransform profileItem_transform = profileItem_go.GetComponent<RectTransform>();
            Vector2 profileItem_pos = new Vector2(0f, -1 * (profileCount * _itemHeight + 10));
            profileItem_transform.anchoredPosition = profileItem_pos;
            // offsetMax

            profileItem_go.name = "ListItem: " + profile.name;
            profileItem_go.GetComponentInChildren<Text>().text = profile.name;
            profileItem_go.GetComponentInChildren<Button>().onClick.AddListener(() => { InspectProfile(profile); });

            ++profileCount;
        }

        // resize content pane
        itemListingContainer.sizeDelta = new Vector2(0f, 10 + profileCount * _itemHeight);
    }

    public void InspectProfile(ModProfile profile)
    {
        itemInspector_modName.text = profile.name;

        ModBinaryRequest request = ModManager.RequestCurrentRelease(profile);
        if(request.isDone)
        {
            itemInspector_buttonContainer.gameObject.SetActive(true);
            itemInspector_downloadContainer.gameObject.SetActive(false);
        }
        else
        {
            itemInspector_downloadContainer.gameObject.SetActive(true);
            itemInspector_buttonContainer.gameObject.SetActive(false);
        }
    }
}
