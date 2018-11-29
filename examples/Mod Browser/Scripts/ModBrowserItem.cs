using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

// TODO(@jackson): Add "Display Loading Profile" functionality
public class ModBrowserItem : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    // ---[ EVENTS ]---
    public event Action<ModBrowserItem> inspectRequested;
    public event Action<ModBrowserItem> subscribeRequested;
    public event Action<ModBrowserItem> unsubscribeRequested;
    public event Action<ModBrowserItem> toggleModEnabledRequested;

    // ---[ UI ]---
    [Header("Settings")]
    [Range(1.0f, 2.0f)]
    public float maximumScaleFactor = 1f;
    public GameObject tagBadgePrefab;

    [Header("UI Components")]
    public ModProfileDisplay profileDisplay;
    public ModfileDisplay buildDisplay;
    public ModStatisticsDisplay statisticsDisplay;
    public Button subscribeButton;
    public Button unsubscribeButton;
    public Button enableModButton;
    public Button disableModButton;

    // TODO(@jackson): You know what to do...
    public Text downloadProgressPercentageText;
    public Text downloadProgress_byteCountText;
    public RectTransform downloadProgress_bar;
    public GameObject downloadProgressContainer;

    [Header("Display Data")]
    public ModProfile profile = null;
    public ModStatistics statistics = null;
    public bool isSubscribed = false;
    public bool isModEnabled = false;

    // ---[ RUNTIME DATA ]---
    [Header("Runtime Data")]
    public bool isInitialized = false;
    public int index = -1;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // asserts
        if(isInitialized)
        {
            #if DEBUG
            Debug.LogWarning("[mod.io] Once initialized, a ModBrowserItem cannot be re-initialized.");
            #endif

            return;
        }

        if(profileDisplay != null)
        {
            profileDisplay.Initialize();
        }
        if(buildDisplay != null)
        {
            buildDisplay.Initialize();
        }
        if(statisticsDisplay != null)
        {
            statisticsDisplay.Initialize();
        }

        // TODO(@jackson): Move to button Prefab
        if(subscribeButton != null)
        {
            subscribeButton.onClick.AddListener(SubscribeClicked);
        }

        if(unsubscribeButton != null)
        {
            unsubscribeButton.onClick.AddListener(UnsubscribeClicked);
        }

        if(enableModButton != null)
        {
            enableModButton.onClick.AddListener(ModEnabledToggled);
        }

        if(disableModButton != null)
        {
            disableModButton.onClick.AddListener(ModEnabledToggled);
        }
    }

    // ---------[ UI UPDATES ]---------
    public void UpdateProfileDisplay()
    {

        if(profile != null)
        {
            if(profileDisplay != null)
            {
                profileDisplay.DisplayProfile(profile);
            }
            if(buildDisplay != null)
            {
                buildDisplay.DisplayModfile(profile.activeBuild);
            }
            // if(downloadProgressContainer != null)
            // {
            //     downloadProgressContainer.gameObject.SetActive(false);

            // }
            // TODO(@jackson): yep
            // UpdateDownloadProgressDisplay();
        }
        else
        {
            if(profileDisplay != null)
            {
                profileDisplay.DisplayLoading();
            }
            if(buildDisplay != null)
            {
                buildDisplay.DisplayLoading();
            }
            // if(downloadProgressContainer != null)
            // {
            //     downloadProgressContainer.gameObject.SetActive(false);
            // }
        }
    }

    public void UpdateStatisticsDisplay()
    {
        if(statistics != null)
        {
            if(statisticsDisplay != null)
            {
                statisticsDisplay.DisplayStatistics(statistics);
            }
        }
        else
        {
            if(statisticsDisplay != null)
            {
                statisticsDisplay.DisplayLoading();
            }
        }
    }

    public void UpdateIsSubscribedDisplay()
    {
        if(subscribeButton != null)
        {
            if(profile == null)
            {
                subscribeButton.interactable = false;
                subscribeButton.gameObject.SetActive(true);
            }
            else
            {
                subscribeButton.interactable = true;
                subscribeButton.gameObject.SetActive(!isSubscribed);
            }
        }
        if(unsubscribeButton != null)
        {
            if(profile == null)
            {
                unsubscribeButton.gameObject.SetActive(false);
            }
            else
            {
                unsubscribeButton.gameObject.SetActive(isSubscribed);
            }
        }

        if(profile != null && isSubscribed)
        {
            // TODO(@jackson): yep
            // UpdateDownloadProgressDisplay();
        }
    }

    public void UpdateIsModEnabledDisplay()
    {
        if(enableModButton != null)
        {
            if(profile == null)
            {
                enableModButton.interactable = false;
                enableModButton.gameObject.SetActive(true);
            }
            else
            {
                enableModButton.interactable = true;
                enableModButton.gameObject.SetActive(!isModEnabled);
            }
        }
        if(unsubscribeButton != null)
        {
            if(profile == null)
            {
                disableModButton.gameObject.SetActive(false);
            }
            else
            {
                disableModButton.gameObject.SetActive(isModEnabled);
            }
        }
    }

    public void UpdateDownloadProgressDisplay()
    {
        ModBinaryRequest request = ModManager.GetDownloadInProgress(profile.activeBuild.id);
        StartCoroutine(UpdateDownloadProgressElements(request));
    }

    public IEnumerator UpdateDownloadProgressElements(ModBinaryRequest request)
    {
        if(request == null
           || request.isDone)
        {
            if(downloadProgressContainer != null)
            {
                downloadProgressContainer.gameObject.SetActive(false);
            }
        }
        else
        {
            if(downloadProgressContainer != null)
            {
                downloadProgressContainer.gameObject.SetActive(true);
            }

            RectTransform barTransform = null;
            if(downloadProgress_bar != null)
            {
                barTransform = downloadProgress_bar.GetChild(0) as RectTransform;
                barTransform.sizeDelta = new Vector2(0f, 0f);
            }
            if(downloadProgress_byteCountText != null)
            {
                downloadProgress_byteCountText.text = ModBrowser.ByteCountToDisplayString(0);
            }
            if(downloadProgressPercentageText != null)
            {
                downloadProgressPercentageText.text = "0%";
            }

            while(!request.isDone)
            {
                if(request.webRequest != null)
                {
                    float percentComplete = request.webRequest.downloadProgress;

                    if(downloadProgress_bar != null)
                    {
                        float barWidth = percentComplete * downloadProgress_bar.rect.width;
                        barTransform.sizeDelta = new Vector2(barWidth, 0f);
                    }

                    if(downloadProgress_byteCountText != null)
                    {
                        downloadProgress_byteCountText.text = ModBrowser.ByteCountToDisplayString((Int64)request.webRequest.downloadedBytes);
                    }

                    if(downloadProgressPercentageText != null)
                    {
                        downloadProgressPercentageText.text = (percentComplete * 100f).ToString("0.0") + "%";
                    }
                }

                yield return null;
            }

            if(downloadProgress_bar != null)
            {
                barTransform.sizeDelta = new Vector2(1f, 0f);
            }

            if(downloadProgress_byteCountText != null)
            {
                // TODO(@jackson): Unhack
                downloadProgress_byteCountText.text = ModBrowser.ByteCountToDisplayString(profile.activeBuild.fileSize);
            }

            if(downloadProgressPercentageText != null)
            {
                downloadProgressPercentageText.text = "100%";
            }

            yield return new WaitForSeconds(4f);

            if(downloadProgressContainer != null)
            {
                downloadProgressContainer.gameObject.SetActive(false);
            }
        }
    }

    // ---------[ EVENTS ]---------
    public void InspectClicked()
    {
        if(inspectRequested != null)
        {
            inspectRequested(this);
        }
    }
    public void SubscribeClicked()
    {
        if(subscribeRequested != null)
        {
            subscribeRequested(this);
        }
    }
    public void UnsubscribeClicked()
    {
        if(unsubscribeRequested != null)
        {
            unsubscribeRequested(this);
        }
    }
    public void ModEnabledToggled()
    {
        if(toggleModEnabledRequested != null)
        {
            toggleModEnabledRequested(this);
        }
    }
}
