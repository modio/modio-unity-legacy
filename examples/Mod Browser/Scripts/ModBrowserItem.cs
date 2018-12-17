using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    // TODO(@jackson): Match to new interface
    [RequireComponent(typeof(ModView))]
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

        [Header("Display Data")]
        public ModProfile profile = null;
        public ModStatistics statistics = null;

        // ---[ RUNTIME DATA ]---
        [Header("Runtime Data")]
        public bool isInitialized = false;
        public int index = -1;

        // --- ACCESSORS ---
        public ModView view
        { get { return this.GetComponent<ModView>(); } }

        // TODO(@jackson): Remove
        public ModBinaryDownloadDisplay             downloadDisplay
        {
            get { return this.view.downloadDisplay; }
        }

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

            if(view != null)
            {
                view.Initialize();
            }
        }

        // ---------[ UI UPDATES ]---------
        public void UpdateProfileDisplay()
        {
            if(profile == null)
            {
                view.DisplayLoading();
            }
            else
            {
                Debug.LogWarning("needs categories");
                view.DisplayMod(profile,
                                statistics,
                                null,
                                false,
                                true);
            }
        }

        public void UpdateStatisticsDisplay()
        {
            if(profile == null)
            {
                view.DisplayLoading();
            }
            else
            {
                Debug.LogWarning("needs categories");
                view.DisplayMod(profile,
                                statistics,
                                null,
                                false,
                                true);
            }
        }

        public void UpdateTagsDisplay(IEnumerable<ModTagCategory> tagCategories)
        {
            if(profile == null)
            {
                view.DisplayLoading();
            }
            else
            {
                view.DisplayMod(profile,
                                statistics,
                                tagCategories,
                                false,
                                true);
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
