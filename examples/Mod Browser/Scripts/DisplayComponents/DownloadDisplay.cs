using System;
using UnityEngine;
using UnityEngine.UI;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;

namespace ModIO.UI
{
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
        [SerializeField] private DownloadDisplayData m_data;

        // --- RUNTIME DATA ---
        private UnityWebRequest m_request;
        private Coroutine m_updateCoroutine;

        // --- ACCESSORS ---
        public override DownloadDisplayData data
        {
            get { return m_data; }
            set
            {
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
                bytesReceivedText.text = UIUtilities.ByteCountToDisplayString(data.bytesReceived);
            }

            if(bytesTotalText != null)
            {
                bytesTotalText.text = UIUtilities.ByteCountToDisplayString(data.bytesTotal);
            }

            if(bytesPerSecondText != null)
            {
                bytesPerSecondText.text = UIUtilities.ByteCountToDisplayString(data.bytesPerSecond) + "/s";
            }

            if(timeRemainingText != null)
            {
                // TODO(@jackson): Localize?
                TimeSpan remaining = TimeSpan.FromSeconds(0f);

                timeRemainingText.text = (remaining.TotalHours + ":"
                                          + remaining.Minutes + ":"
                                          + remaining.Seconds);
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            if(Application.isPlaying && m_request != null
               && m_updateCoroutine == null)
            {
                m_updateCoroutine = this.StartCoroutine(UpdateCoroutine());
            }
        }

        public override void Initialize() {}

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayDownload(UnityWebRequest request, Int64 downloadSize)
        {
            Debug.Assert(request != null);

            if(m_updateCoroutine != null)
            {
                this.StopCoroutine(m_updateCoroutine);
            }

            m_request = request;

            m_data = new DownloadDisplayData()
            {
                bytesReceived = (Int64)request.downloadedBytes,
                bytesPerSecond = 0,
                bytesTotal = downloadSize,
            };

            if(Application.isPlaying
               && this.isActiveAndEnabled)
            {
                m_updateCoroutine = this.StartCoroutine(UpdateCoroutine());
            }
        }

        private System.Collections.IEnumerator UpdateCoroutine()
        {
            float timeStepElapsed = 0f;
            Int64 timeStepStartByteCount = (Int64)m_request.downloadedBytes;

            while(m_request != null
                  && !m_request.isDone)
            {
                if(m_data.bytesTotal <= 0
                   && m_request.downloadProgress >= 0.001f)
                {
                    m_data.bytesTotal = (Int64)(m_request.downloadedBytes * m_request.downloadProgress);
                }

                m_data.bytesReceived = (Int64)m_request.downloadedBytes;

                if(timeStepElapsed >= 1f)
                {
                    m_data.bytesPerSecond = (Int64)((m_data.bytesReceived - timeStepStartByteCount)
                                                    / timeStepElapsed);

                    timeStepElapsed = 0f;
                    timeStepStartByteCount = m_data.bytesReceived;
                }

                PresentData();

                yield return null;

                timeStepElapsed += Time.deltaTime;
            }

            m_data.bytesReceived = m_data.bytesTotal;
            m_data.bytesPerSecond = 0;
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
            {
                PresentData();
            };
        }
        #endif
    }
}
