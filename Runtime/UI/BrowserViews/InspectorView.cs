using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>ViewController for displaying a single mod using a mod id.</summary>
    [RequireComponent(typeof(ModView))]
    public class InspectorView : MonoBehaviour, IBrowserView, UnityEngine.EventSystems.ICancelHandler
    {
        // ---------[ FIELDS ]---------
        /// <summary>Id of the currently displayed mod.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>The priority to focus the selectables.</summary>
        public List<Selectable> onFocusPriority = new List<Selectable>();

        // --- ACCESSORS ---
        /// <summary>Id of the currently displayed mod.</summary>
        public int modId
        {
            get
            {
                return this.m_modId;
            }
            set
            {
                if(this.m_modId != value)
                {
                    this.m_modId = value;

                    // clear old data
                    this.modView.profile = null;
                    this.modView.statistics = null;

                    // load if not null
                    if(this.m_modId != ModProfile.NULL_ID)
                    {
                        // profile
                        ModManager.GetModProfile(this.m_modId,
                        (p) =>
                        {
                            if(this != null
                               && this.m_modId == value)
                            {
                                this.modView.profile = p;

                                if(p != null)
                                {
                                    this.modView.statistics = p.statistics;
                                }
                                else
                                {
                                    this.modView.statistics = null;
                                }
                            }
                        },
                        null);
                    }
                }
            }
        }

        /// <summary>The ModView sibling component.</summary>
        public ModView modView
        {
            get { return this.gameObject.GetComponent<ModView>(); }
        }

        // --- IBrowserView Implementation ---
        /// <summary>Canvas Group.</summary>
        public CanvasGroup canvasGroup
        { get { return this.gameObject.GetComponent<CanvasGroup>(); } }

        /// <summary>Reset selection on hide.</summary>
        bool IBrowserView.resetSelectionOnHide { get { return true; } }

        /// <summary>Is the view a root view or window view?</summary>
        bool IBrowserView.isRootView { get { return false; } }

        /// <summary>The priority to focus the selectables.</summary>
        List<Selectable> IBrowserView.onFocusPriority { get { return this.onFocusPriority; } }

        // ---------[ UI Functionality ]---------
        /// <summary>Closes this view.</summary>
        public void Close()
        {
            ViewManager.instance.CloseWindowedView(this);
        }

        /// <summary>Handles a cancel to close the view.</summary>
        public void OnCancel(UnityEngine.EventSystems.BaseEventData eventData)
        {
            this.Close();
        }
    }
}
