using System;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the estimated time remaining for a download view.</summary>
    public class DownloadTimeRemainingDisplay : MonoBehaviour, IDownloadViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent View.</summary>
        private DownloadView m_view = null;

        /// <summary>Currently displayed Modfile object.</summary>
        private FileDownloadInfo m_download = null;

        /// <summary>Text to display if download is unstarted.</summary>
        [SerializeField]
        private string m_unstartedText = "Initializing";

        /// <summary>Text to display if download speed is close to zero.</summary>
        [SerializeField]
        private string m_notDownloadingText = "Awaiting Connection";

        /// <summary>Text to display if download speed is close to zero.</summary>
        [SerializeField]
        private string m_completedText = "Download Complete";

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            Component textDisplayComponent =
                GenericTextComponent.FindCompatibleTextComponent(this.gameObject);
            this.m_textComponent.SetTextDisplayComponent(textDisplayComponent);

#if DEBUG
            if(textDisplayComponent == null)
            {
                Debug.LogWarning("[mod.io] No compatible text components were found on this "
                                     + "GameObject to set text for."
                                     + "\nCompatible with any component that exposes a"
                                     + " publicly settable \'.text\' property.",
                                 this);
            }
#endif
        }

        protected virtual void OnEnable()
        {
            this.DisplayDownload(this.m_download);
        }

        /// <summary>IDownloadViewElement interface.</summary>
        public void SetDownloadView(DownloadView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onDownloadInfoUpdated.RemoveListener(DisplayDownload);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onDownloadInfoUpdated.AddListener(DisplayDownload);
                this.DisplayDownload(this.m_view.downloadInfo);
            }
            else
            {
                this.DisplayDownload(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the appropriate field of a given modfile.</summary>
        public void DisplayDownload(FileDownloadInfo download)
        {
            this.m_download = download;

            string displayText = string.Empty;

            if(download != null)
            {
                if(download.request == null || download.request.downloadedBytes == 0)
                {
                    displayText = this.m_unstartedText;
                }
                else if(download.isDone)
                {
                    displayText = this.m_completedText;
                }
                else if(download.bytesPerSecond <= 1)
                {
                    displayText = this.m_notDownloadingText;
                }
                else
                {
                    int secondsRemaining =
                        (int)((download.fileSize - (Int64)download.request.downloadedBytes)
                              / download.bytesPerSecond);

                    displayText = ValueFormatting.SecondsAsTime(secondsRemaining);
                }
            }

            this.m_textComponent.text = displayText;
        }
    }
}
