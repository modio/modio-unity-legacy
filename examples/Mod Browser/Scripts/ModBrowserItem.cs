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
            Debug.Assert(view != null);
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
