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
        [Header("Settings")]
        [Range(1.0f, 2.0f)]
        public float maximumScaleFactor = 1f;

        // ---[ RUNTIME DATA ]---
        [Header("Runtime Data")]
        public int index = -1;

        // --- ACCESSORS ---
        public ModView view
        { get { return this.GetComponent<ModView>(); } }

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            Debug.Assert(view != null);
        }
    }
}
