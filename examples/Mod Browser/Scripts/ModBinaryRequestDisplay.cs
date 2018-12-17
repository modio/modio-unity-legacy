// PROCESS:
// 1) Player clicks subscribe
// 2) ModBrowser gets event
// 3) ModBrowser add the id to the subscription ids.
// 4) ModBrowser initiates the binary download.
// ?)
// X) DownloadProgressDisplay starts Coroutine
//
// Problems:
//  - Coroutines cannot Start if component is disabled
//  - How to detect which components need to receive the message?
//  - Sending through the SubView seems superfluous? Why not directly from ModB?
//  - What about through mod pages? Through ModProfileDisplays?

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// TODO(@jackson): Needs cleanup
namespace ModIO.UI
{
    public class ModBinaryRequestDisplay : DownloadDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<DownloadDisplayComponent> onClick;

        [Header("Settings")]
        [Tooltip("If the display component has no download to show, or the download has finished, this gameObject can be set to inactive.")]
        public bool setInactiveIfNotDownloading = true;

        [Header("UI Elements")]
        public Text percentageText;
        public Text byteCountText;
        public RectTransform progressBar;

        [Header("Display Data")]
        public int modId;
        [SerializeField] private DownloadDisplayData m_data;

        // --- RUNTIME DATA ---
        private ModBinaryRequest m_request;
        private Coroutine m_updateCoroutine;

        // --- ACCESSORS ---
        public override DownloadDisplayData data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        private void PresentData()
        {

        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize() {}

        private void OnEnable()
        {
            if(m_request != null)
            {
                StartDisplayCoroutine();
            }
            else if(setInactiveIfNotDownloading)
            {
                this.gameObject.SetActive(false);
            }
        }
        private void OnDisable()
        {
            if(m_updateCoroutine != null)
            {
                this.StopCoroutine(m_updateCoroutine);
                m_updateCoroutine = null;
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        // public void DisplayModfile(int modId, int modfileId)
        // {
        //     ModBinaryRequest request = ModManager.GetDownloadInProgress(modfileId);
        //     DisplayDownload(request);

        //     if(request == null)
        //     {
        //         this.m_modId = modId;
        //         this.m_modfileId = modfileId;
        //     }
        // }

        // public void DisplayDownload(ModBinaryRequest request)
        // {
        //     if(request == null
        //        || request.isDone)
        //     {
        //         m_modId = -1;
        //         m_modfileId = -1;
        //         m_uwr = null;

        //         if(container != null)
        //         {
        //             container.gameObject.SetActive(false);
        //         }
        //     }
        //     else
        //     {
        //         m_modId = request.

        //         if(container != null)
        //         {
        //             container.gameObject.SetActive(true);
        //         }
        //         StartCoroutine(UpdateDownloadProgressElements(request));
        //     }
        // }

        public void DisplayRequest(ModBinaryRequest request)
        {
            m_request = request;

            if(request != null)
            {
                this.gameObject.SetActive(true);

                if(this.isActiveAndEnabled)
                {
                    StartDisplayCoroutine();
                }
            }
            else if(setInactiveIfNotDownloading)
            {
                this.gameObject.SetActive(false);
            }
        }

        private void StartDisplayCoroutine()
        {
            if(m_updateCoroutine != null)
            {
                StopCoroutine(m_updateCoroutine);
            }

            m_updateCoroutine = StartCoroutine(UpdateDisplayCoroutine());
        }

        private IEnumerator UpdateDisplayCoroutine()
        {
            RectTransform barTransform = null;
            if(progressBar != null)
            {
                barTransform = progressBar.GetChild(0) as RectTransform;
                barTransform.sizeDelta = new Vector2(0f, 0f);
            }
            if(byteCountText != null)
            {
                byteCountText.text = UIUtilities.ByteCountToDisplayString(0);
            }
            if(percentageText != null)
            {
                percentageText.text = "0%";
            }

            while(!m_request.isDone)
            {
                if(m_request.webRequest != null)
                {
                    float percentComplete = m_request.webRequest.downloadProgress;

                    if(progressBar != null)
                    {
                        float barWidth = percentComplete * progressBar.rect.width;
                        barTransform.sizeDelta = new Vector2(barWidth, 0f);
                    }

                    if(byteCountText != null)
                    {
                        byteCountText.text = UIUtilities.ByteCountToDisplayString((Int64)m_request.webRequest.downloadedBytes);
                    }

                    if(percentageText != null)
                    {
                        percentageText.text = (percentComplete * 100f).ToString("0.0") + "%";
                    }
                }

                yield return null;
            }

            if(progressBar != null)
            {
                barTransform.sizeDelta = new Vector2(progressBar.rect.width, 0f);
            }

            if(byteCountText != null)
            {

                try
                {
                    var info = new System.IO.FileInfo(m_request.binaryFilePath);
                    string byteCountString = UIUtilities.ByteCountToDisplayString(info.Length);
                    byteCountText.text = byteCountString;
                }
                catch(Exception e)
                {
                    Debug.LogError(Utility.GenerateExceptionDebugString(e));
                }
            }

            if(percentageText != null)
            {
                percentageText.text = "100%";
            }

            yield return new WaitForSeconds(4f);

            m_updateCoroutine = null;

            if(setInactiveIfNotDownloading)
            {
                this.gameObject.SetActive(false);
            }
        }

        // ---------[ EVENTS ]---------
        public void NotifyClicked()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            PresentData();
        }
        #endif
    }
}
