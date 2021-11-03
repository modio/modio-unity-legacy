using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [Obsolete("Use ModBinaryDownloadDisplay instead.")]
    public class DownloadDisplay : DownloadDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<DownloadDisplayComponent> onClick;

        [Header("UI Elements")]
        public Text percentageText;
        public Text bytesReceivedText;
        public Text bytesTotalText;
        public Text bytesPerSecondText;
        public Text timeRemainingText;
        public HorizontalProgressBar progressBar;

        [Header("Display Data")]
        [SerializeField]
        private DownloadDisplayData m_data;

        // --- RUNTIME DATA ---
        private FileDownloadInfo m_downloadInfo;
        private Coroutine m_updateCoroutine;

        // --- ACCESSORS ---
        public override DownloadDisplayData data
        {
            get {
                return m_data;
            }
            set {
                m_data = value;
                PresentData();
            }
        }

        private void PresentData()
        {
            float percentComplete = 0f;
            if(data.bytesTotal > 0)
            {
                percentComplete = (float)data.bytesReceived / (float)data.bytesTotal;
            }

            if(percentageText != null)
            {
                percentageText.text = (percentComplete * 100f).ToString("0.0") + "%";
            }
            if(progressBar != null)
            {
                progressBar.percentComplete = percentComplete;
            }

            if(bytesReceivedText != null)
            {
                bytesReceivedText.text = ValueFormatting.ByteCount(data.bytesReceived, "0.0");
            }

            if(bytesTotalText != null)
            {
                bytesTotalText.text = ValueFormatting.ByteCount(data.bytesTotal, "0.0");
            }

            if(bytesPerSecondText != null)
            {
                bytesPerSecondText.text =
                    ValueFormatting.ByteCount(data.bytesPerSecond, "0.0") + "/s";
            }

            if(timeRemainingText != null)
            {
                // TODO(@jackson): Localize?
                TimeSpan remaining = TimeSpan.FromSeconds(0f);

                timeRemainingText.text =
                    (remaining.TotalHours + ":" + remaining.Minutes + ":" + remaining.Seconds);
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            if(Application.isPlaying && m_downloadInfo != null && m_updateCoroutine == null)
            {
                m_updateCoroutine = this.StartCoroutine(UpdateCoroutine());
            }
        }

        public override void Initialize() {}

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayDownload(FileDownloadInfo downloadInfo)
        {
            Debug.Assert(downloadInfo != null);

            if(m_updateCoroutine != null)
            {
                this.StopCoroutine(m_updateCoroutine);
            }

            m_downloadInfo = downloadInfo;

            Int64 bytesReceived =
                (downloadInfo.request == null ? 0 : (Int64)downloadInfo.request.downloadedBytes);

            m_data = new DownloadDisplayData() {
                bytesReceived = bytesReceived,
                bytesPerSecond = 0,
                bytesTotal = downloadInfo.fileSize,
                isActive = !downloadInfo.isDone,
            };

            if(Application.isPlaying && this.isActiveAndEnabled)
            {
                m_updateCoroutine = this.StartCoroutine(UpdateCoroutine());
            }
        }

        private System.Collections.IEnumerator UpdateCoroutine()
        {
            float timeStepElapsed = 0f;
            Int64 timeStepStartByteCount =
                (m_downloadInfo.request == null ? 0
                                                : (Int64)m_downloadInfo.request.downloadedBytes);

            while(m_downloadInfo != null && !m_downloadInfo.isDone)
            {
                if(m_data.bytesTotal <= 0)
                {
                    m_data.bytesTotal = m_downloadInfo.fileSize;
                }

                if(m_downloadInfo.request != null)
                {
                    m_data.bytesReceived = (Int64)m_downloadInfo.request.downloadedBytes;
                }

                if(timeStepElapsed >= 1f)
                {
                    m_data.bytesPerSecond =
                        (Int64)((m_data.bytesReceived - timeStepStartByteCount) / timeStepElapsed);

                    timeStepElapsed = 0f;
                    timeStepStartByteCount = m_data.bytesReceived;
                }

                PresentData();

                yield return null;

                timeStepElapsed += Time.unscaledDeltaTime;
            }

            m_data.bytesReceived = m_data.bytesTotal;
            m_data.bytesPerSecond = 0;
            m_data.isActive = false;

            m_downloadInfo = null;

            PresentData();
        }

        // ---------[ EVENTS ]---------
        public void NotifyClick()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            { PresentData(); };
        }
#endif
    }
}
