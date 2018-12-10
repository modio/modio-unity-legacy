using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    // TODO(@jackson): Match to new interface
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
        public ModStatisticsDisplay statisticsDisplay;
        public ModTagCollectionDisplay tagsDisplay;
        public Button subscribeButton;
        public Button unsubscribeButton;
        public Button enableModButton;
        public Button disableModButton;

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
            if(statisticsDisplay != null)
            {
                statisticsDisplay.Initialize();
            }
            if(tagsDisplay != null)
            {
                tagsDisplay.Initialize();
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
            }
            else
            {
                if(profileDisplay != null)
                {
                    profileDisplay.DisplayLoading();
                }
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

        public void UpdateTagsDisplay(IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(tagCategories != null);

            if(tagsDisplay != null)
            {
                if(profile != null)
                {
                    tagsDisplay.DisplayTags(profile, tagCategories);
                }
                else
                {
                    tagsDisplay.DisplayLoading();
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
}
