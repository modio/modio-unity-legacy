using System.Collections.Generic;

namespace ModIO
{
    public static class DownloadManager
    {
        private static List<Download> concurrentDownloads = new List<Download>();
        private static List<Download> queuedDownloads = new List<Download>();

        public static bool IsDownloadActive(string downloadURL)
        {
            foreach(Download download in concurrentDownloads)
            {
                if(download.sourceURL == downloadURL) { return true; }
            }
            foreach(Download download in queuedDownloads)
            {
                if(download.sourceURL == downloadURL)
                {
                    return download.status == Download.Status.InProgress;
                }
            }
            return false;
        }
        public static bool IsDownloadQueued(string downloadURL)
        {
            foreach(Download download in queuedDownloads)
            {
                if(download.sourceURL == downloadURL) { return true; }
            }
            return false;
        }

        public static void AddConcurrentDownload(Download download)
        {
            // Prevent doubling downloads
            if(IsDownloadActive(download.sourceURL))
            {
                return;
            }

            concurrentDownloads.Add(download);

            download.OnCompleted += (d) => concurrentDownloads.Remove(d);
            download.OnFailed += (d,e) => concurrentDownloads.Remove(d);

            download.Start();
        }

        public static void AddQueuedDownload(Download download)
        {
            // Prevent doubling downloads
            if(IsDownloadQueued(download.sourceURL) || IsDownloadActive(download.sourceURL))
            {
                return;
            }

            queuedDownloads.Add(download);

            if(queuedDownloads.Count == 1)
            {
                download.OnCompleted += OnQueuedDownloadCompleted;
                download.OnFailed += (d,e) => OnQueuedDownloadCompleted(d);

                download.Start();
            }
        }

        private static void OnQueuedDownloadCompleted(Download download)
        {
            queuedDownloads.Remove(download);

            if(queuedDownloads.Count > 0)
            {
                queuedDownloads[0].OnCompleted += OnQueuedDownloadCompleted;
                queuedDownloads[0].OnFailed += (d,e) => OnQueuedDownloadCompleted(d);

                queuedDownloads[0].Start();
            }
        }
    }
}