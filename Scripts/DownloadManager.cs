// using System.Collections.Generic;

// namespace ModIO
// {
//     public static class DownloadManager
//     {
//         private static Dictionary<string, Download> urlDownloadMap = new Dictionary<string, Download>();
//         private static List<string> queuedDownloadURLs = new List<string>();

//         public static bool IsDownloadActive(string downloadURL)
//         {
//             Download download;
//             if(urlDownloadMap.TryGetValue(downloadURL, out download))
//             {
//                 return download.status == Download.Status.InProgress;
//             }
//             return false;
//         }

//         public static bool IsDownloadQueued(string downloadURL)
//         {
//             return queuedDownloadURLs.Contains(downloadURL);
//         }

//         public static void StartDownload(Download download)
//         {
//             // Prevent doubling downloads
//             if(IsDownloadActive(download.sourceURL)) { return; }

//             int queueIndex = queuedDownloadURLs.IndexOf(download.sourceURL);
//             if(queueIndex >= 0)
//             {
//                 queuedDownloadURLs.RemoveAt(queueIndex);
//             }

//             download.Start();
//         }

//         public static void QueueDownload(Download download)
//         {
//             // Prevent doubling downloads
//             if(urlDownloadMap.ContainsKey(download.sourceURL)) { return; }

//             urlDownloadMap.Add(download.sourceURL, download);
//             queuedDownloadURLs.Add(download.sourceURL);

//             if(queuedDownloadURLs.Count == 1)
//             {
//                 StartNextQueuedDownload();
//             }
//         }

//         private static void StartNextQueuedDownload()
//         {
//             if(queuedDownloadURLs.Count > 0)
//             {
//                 Download download = urlDownloadMap[queuedDownloadURLs[0]];

//                 download.OnCompleted += (d) => StartNextQueuedDownload();
//                 download.OnFailed += (d,e) => StartNextQueuedDownload();

//                 StartDownload(download);
//             }
//         }
//     }
// }
