using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>ViewController for displaying a single mod using a mod id.</summary>
    [RequireComponent(typeof(ModView))]
    public class InspectorView : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Id of the currently displayed mod.</summary>
        private int m_modId = ModProfile.NULL_ID;

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
                        ModProfileRequestManager.instance.RequestModProfile(this.m_modId,
                        (p) =>
                        {
                            if(this != null
                               && this.m_modId == value)
                            {
                                this.modView.profile = p;
                            }
                        },
                        WebRequestError.LogAsWarning);

                        // statistics
                        ModStatisticsRequestManager.instance.RequestModStatistics(this.m_modId,
                        (s) =>
                        {
                            if(this != null
                               && this.m_modId == value)
                            {
                                this.modView.statistics = s;
                            }
                        },
                        WebRequestError.LogAsWarning);
                    }
                }
            }
        }

        /// <summary>The ModView sibling component.</summary>
        public ModView modView
        {
            get { return this.gameObject.GetComponent<ModView>(); }
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("Use InspectorView.highlightedImage instead.")][HideInInspector]
        public ImageDisplay selectedMediaPreview;
        [Obsolete("No longer supported. Try an ObjectActiverSetter component instead.")]
        public GameObject loadingDisplay;

        [Obsolete("Use a ModReleaseHistoryView instead.")]
        public GameObject versionHistoryItemPrefab = null;
        [Obsolete("Use ModfileView.emptyChangelogText instead.")]
        public string missingVersionChangelogText = "<i>None recorded.</i>";

        [Obsolete("Use a ModReleaseHistoryView instead.")]
        public RectTransform versionHistoryContainer;

        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        public ModProfile profile;
        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        private ModProfile m_profile;
        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        private ModStatistics m_statistics;
        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        private bool m_isModSubscribed;
        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        private bool m_isModEnabled;

        [Obsolete("No longer necessary. Initialization occurs in Start().")]
        public void Initialize() {}

        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModProfile> subscribeRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifySubscribeRequested()
        {
            if(subscribeRequested != null)
            {
                subscribeRequested(this.m_profile);
            }
        }
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModProfile> unsubscribeRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyUnsubscribeRequested()
        {
            if(unsubscribeRequested != null)
            {
                unsubscribeRequested(this.m_profile);
            }
        }
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModProfile> enableRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyEnableRequested()
        {
            if(enableRequested != null)
            {
                enableRequested(this.m_profile);
            }
        }
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModProfile> disableRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyDisableRequested()
        {
            if(disableRequested != null)
            {
                disableRequested(this.m_profile);
            }
        }

        [Obsolete("Use OnModSubscriptionsUpdated() instead")]
        public void DisplayModSubscribed(bool isSubscribed)
        {
            if(this.isActiveAndEnabled)
            {
                ModDisplayData data = modView.data;
                if(data.isSubscribed != isSubscribed)
                {
                    data.isSubscribed = isSubscribed;
                    modView.data = data;
                }
            }
        }

        [Obsolete("No longer necessary.")]
        public void DisplayModEnabled(bool isEnabled) {}

        [Obsolete("Set the modId value and/or use Refresh() instead.")]
        public void DisplayMod(ModProfile profile, ModStatistics statistics,
                               IEnumerable<ModTagCategory> tagCategories,
                               bool isModSubscribed, bool isModEnabled)
        {
            Debug.Assert(profile != null);
            this.modId = profile.id;
        }

        [Obsolete("No longer necessary.")]
        public void SetLoadingDisplay(bool visible) {}

        [Obsolete("No longer necessary.")]
        public void Refresh() {}
    }
}
