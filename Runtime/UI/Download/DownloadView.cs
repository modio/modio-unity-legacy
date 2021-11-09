using System;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Represents the status of a mod binary download visually.</summary>
    [DisallowMultipleComponent]
    public class DownloadView : MonoBehaviour, IModViewElement
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Event for notifying listeners of a change to the download info.</summary>
        [Serializable]
        public class DownloadInfoUpdatedEvent : UnityEngine.Events.UnityEvent<FileDownloadInfo>
        {
        }

        // ---------[ CONSTANTS ]---------
        /// <summary>Interval between download speed updates.</summary>
        public const float DOWNLOAD_SPEED_UPDATE_INTERVAL = 0.5f;
        /// <summary>Delay before hiding the game object (if enabled).</summary>
        public const float HIDE_DELAY_SECONDS = 1.5f;

        // ---------[ FIELDS ]---------
        /// <summary>Download Info.</summary>
        private FileDownloadInfo m_downloadInfo = null;

        /// <summary>Determines whether the component should hide if not downloading.</summary>
        public bool hideIfInactive = true;

        /// <summary>Event for notifying listeners of a change to the download info.</summary>
        public DownloadInfoUpdatedEvent onDownloadInfoUpdated = null;

        // --- Run-time Data ---
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>ModId of the mod currently being monitored.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>The currently running update coroutine.</summary>
        private Coroutine m_updateCoroutine = null;

        // --- Accessors ---
        /// <summary>Download Info.</summary>
        public FileDownloadInfo downloadInfo
        {
            get {
                return this.m_downloadInfo;
            }
        }

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            DownloadClient.modfileDownloadStarted += OnDownloadStarted;
        }

        protected virtual void Start()
        {
#if UNITY_EDITOR
            DownloadView[] nested = this.gameObject.GetComponentsInChildren<DownloadView>(true);
            if(nested.Length > 1)
            {
                Debug.LogError(
                    "[mod.io] Nesting DownloadViews is currently not supported due to the"
                        + " way IDownloadViewElement component parenting works."
                        + "\nThe nested DownloadViews must be removed to allow DownloadView functionality."
                        + "\nthis=" + this.gameObject.name
                        + "\nnested=" + nested[1].gameObject.name,
                    this);
                return;
            }
#endif

            // assign download view elements to this
            var downloadViewElements =
                this.gameObject.GetComponentsInChildren<IDownloadViewElement>(true);
            foreach(IDownloadViewElement viewElement in downloadViewElements)
            {
                viewElement.SetDownloadView(this);
            }
        }

        protected virtual void OnDestroy()
        {
            DownloadClient.modfileDownloadStarted -= OnDownloadStarted;
        }

        protected virtual void OnEnable()
        {
            // check for auto-hide and download start
            if(this.m_downloadInfo != null)
            {
                if(this.m_updateCoroutine == null)
                {
                    this.m_updateCoroutine = this.StartCoroutine(this.UpdateCoroutine());
                }
            }
            else if(this.hideIfInactive)
            {
                this.gameObject.SetActive(false);
            }
        }

        protected virtual void OnDisable()
        {
            if(this.m_updateCoroutine != null)
            {
                this.StopCoroutine(this.m_updateCoroutine);
                this.m_updateCoroutine = null;
            }
        }

        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayProfile);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayProfile);
                this.DisplayProfile(this.m_view.profile);
            }
            else
            {
                this.DisplayProfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays download for a given mod.</summary>
        public void DisplayProfile(ModProfile profile)
        {
            int newId = ModProfile.NULL_ID;
            if(profile != null)
            {
                newId = profile.id;
            }

            // check for change
            if(this.m_modId != newId)
            {
                // reset everything
                if(this.m_updateCoroutine != null)
                {
                    this.StopCoroutine(this.m_updateCoroutine);
                    this.m_updateCoroutine = null;
                }

                this.m_modId = newId;
                this.m_downloadInfo = null;

                // check if currently downloading
                bool isDownloading = false;

                if(newId != ModProfile.NULL_ID)
                {
                    foreach(var kvp in DownloadClient.modfileDownloadMap)
                    {
                        if(kvp.Key.modId == this.m_modId)
                        {
                            isDownloading = true;
                            OnDownloadStarted(kvp.Key, kvp.Value);
                        }
                    }
                }

                // set active/inactive as appropriate
                this.gameObject.SetActive(isDownloading || !this.hideIfInactive);
            }
        }

        /// <summary>Updates the UI representation every frame.</summary>
        protected virtual System.Collections.IEnumerator UpdateCoroutine()
        {
            Debug.Assert(this.m_downloadInfo != null);

            // set initial speed marker
            float lastSpeedUpdate = Time.unscaledTime;

            // loop while downloading
            while(this != null && this.m_downloadInfo != null && this.onDownloadInfoUpdated != null
                  && !this.m_downloadInfo.isDone)
            {
                float now = Time.unscaledTime;
                if(now - lastSpeedUpdate >= DownloadView.DOWNLOAD_SPEED_UPDATE_INTERVAL)
                {
                    lastSpeedUpdate = now;
                }

                this.onDownloadInfoUpdated.Invoke(this.m_downloadInfo);

                yield return null;
            }

            this.onDownloadInfoUpdated.Invoke(this.m_downloadInfo);

            if(this.hideIfInactive)
            {
                yield return new WaitForSecondsRealtime(HIDE_DELAY_SECONDS);
                this.gameObject.SetActive(false);
            }
        }

        // ---------[ EVENTS ]---------
        /// <summary>Initializes the component display.</summary>
        protected virtual void OnDownloadStarted(ModfileIdPair idPair,
                                                 FileDownloadInfo downloadInfo)
        {
            if(this.m_modId == idPair.modId)
            {
                this.m_downloadInfo = downloadInfo;

                if(!this.isActiveAndEnabled && this.hideIfInactive)
                {
                    this.gameObject.SetActive(true);
                }

                if(this.isActiveAndEnabled && this.m_updateCoroutine == null)
                {
                    this.m_updateCoroutine = this.StartCoroutine(this.UpdateCoroutine());
                }
            }
        }
    }
}
