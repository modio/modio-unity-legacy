using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using ModIO;

// TODO(@jackson): Handle no mods in list inspection
// TODO(@jackson): Set inspect on load
public class CollectionView : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    // ---[ EVENTS ]---
    public event Action<ModProfile> onUnsubscribeClicked;

    // ---[ SCENE COMPONENTS ]---
    [Header("Settings")]
    public GameObject itemListing_prefab;

    [Header("UI Components")]
    public RectTransform itemListingContainer;
    public Text itemInspector_modName;
    public RectTransform itemInspector_downloadContainer;
    public Text itemInspector_downloadProgressText;
    public RectTransform itemInspector_downloadProgressBar;
    public RectTransform itemInspector_buttonContainer;
    public Button unsubscribeButton;
    public Button enableButton;

    // // ---[ RUNTIME DATA ]---
    // [Header("Runtime Data")]
    // public ModProfile inspectedProfile;

    // ---[ PRIVATES ]---
    private float _itemHeight;
    private UnityAction _unsubscribeAction;

    // ---------[ IMODBROWSERVIEW ]---------
    public IEnumerable<ModProfile> profileCollection { get; set; }

    public void Initialize()
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
        if(_unsubscribeAction != null)
        {
            unsubscribeButton.onClick.RemoveListener(_unsubscribeAction);
        }

        _unsubscribeAction = () =>
        {
            if(onUnsubscribeClicked != null)
            {
                onUnsubscribeClicked(profile);
            }
        };
        unsubscribeButton.onClick.AddListener(_unsubscribeAction);

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

            StartCoroutine(UpdateProgressBar(request));
        }
    }

    public IEnumerator UpdateProgressBar(ModBinaryRequest request)
    {
        itemInspector_downloadProgressBar.sizeDelta = new Vector2(0f, 0f);
        itemInspector_downloadProgressText.text = "Initializing";

        RectTransform progressBarParent = itemInspector_downloadProgressBar.parent.GetComponent<RectTransform>();
        while(!request.isDone)
        {
            if(request.webRequest != null)
            {
                float percentComplete = request.webRequest.downloadProgress;

                float barWidth = percentComplete * progressBarParent.rect.width;
                itemInspector_downloadProgressBar.sizeDelta = new Vector2(barWidth, 0f);

                itemInspector_downloadProgressText.text = (percentComplete * 100f).ToString("0.0") + "%";
            }

            yield return null;
        }

        itemInspector_buttonContainer.gameObject.SetActive(true);
        itemInspector_downloadContainer.gameObject.SetActive(false);
    }
}
