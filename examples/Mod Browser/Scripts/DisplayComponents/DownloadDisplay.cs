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
        public override void Initialize() {}
        public override void DisplayDownload(UnityWebRequest request, Int64 downloadSize) {}

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
