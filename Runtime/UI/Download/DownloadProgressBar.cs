using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Controls a progress bar to match a download's progress.</summary>
    [RequireComponent(typeof(HorizontalProgressBar))]
    public class DownloadProgressBar : MonoBehaviour, IDownloadViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Parent view.</summary>
        private DownloadView m_view = null;

        // ---------[ INITIALIZATION ]---------
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
        /// <summary>Displays the download.</summary>
        public void DisplayDownload(FileDownloadInfo download)
        {
            float p = 0f;
            if(download != null)
            {
                if(download.isDone)
                {
                    p = 1f;
                }
                else if(download.request != null && download.fileSize > 0)
                {
                    p = ((float)download.request.downloadedBytes / (float)download.fileSize);
                }
            }

            this.GetComponent<HorizontalProgressBar>().percentComplete = p;
        }
    }
}
