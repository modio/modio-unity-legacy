using System;
using System.IO;

using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public static class DownloadClient
    {
        // ---------[ IMAGE DOWNLOADS ]---------
        public static ImageDownload DownloadModLogo(ModProfile profile, LogoVersion version)
        {
            ImageDownload download = new ImageDownload();

            string logoURL = profile.logoLocator.GetVersionURL(version);

            UnityWebRequest webRequest = UnityWebRequest.Get(logoURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, download);

            return download;
        }

        public static ImageDownload DownloadModGalleryImage(ModProfile profile,
                                                              string imageFileName,
                                                              ModGalleryImageVersion version)
        {
            ImageDownload download = new ImageDownload();

            string imageURL = profile.media.GetGalleryImageWithFileName(imageFileName).GetVersionURL(version);

            UnityWebRequest webRequest = UnityWebRequest.Get(imageURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, download);

            return download;
        }

        private static void OnImageDownloadCompleted(UnityWebRequestAsyncOperation operation,
                                                     ImageDownload download)
        {
            UnityWebRequest webRequest = operation.webRequest;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                WebRequestError error = WebRequestError.GenerateFromWebRequest(webRequest);
                download.NotifyFailed(error);
            }
            else
            {
                #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log(String.Format("{0} REQUEST SUCEEDED\nResponse received at: {1} [{2}]\nURL: {3}\nResponse: {4}\n",
                                            webRequest.method.ToUpper(),
                                            ServerTimeStamp.ToLocalDateTime(responseTimeStamp),
                                            responseTimeStamp,
                                            webRequest.url,
                                            webRequest.downloadHandler.text));
                }
                #endif

                Texture2D imageTexture = (webRequest.downloadHandler as DownloadHandlerTexture).texture;
                download.NotifySucceeded(imageTexture);
            }
        }

        // ---------[ BINARY DOWNLOADS ]---------
        public static ModBinaryDownload DownloadModBinary(ModfileStub modfile)
        {
            ModBinaryDownload download = new ModBinaryDownload();

            download.isDone = false;

            // - Acquire Download URL -
            APIClient.GetModfile(modfile.modId, modfile.id,
                                 (mf) => DownloadClient.OnGetModfile(mf, download),
                                 download.NotifyFailed);

            return download;
        }

        private static void OnGetModfile(Modfile modfile, ModBinaryDownload download)
        {
            string filePath = CacheClient.GenerateModBinaryZipFilePath(modfile.modId, modfile.id);
            string tempFilePath = filePath + ".download";

            UnityWebRequest webRequest = UnityWebRequest.Get(modfile.downloadLocator.binaryURL);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                webRequest.downloadHandler = new DownloadHandlerFile(tempFilePath);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to create download file on disk."
                      + "\nFile: " + filePath + "\n");

                Utility.LogExceptionAsWarning(warningInfo, e);

                download.NotifyFailed(new WebRequestError());

                return;
            }

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnModBinaryDownloadCompleted(operation,
                                                                                 download,
                                                                                 filePath);
        }

        private static void OnModBinaryDownloadCompleted(UnityWebRequestAsyncOperation operation,
                                                         ModBinaryDownload download,
                                                         string filePath)
        {
            UnityWebRequest webRequest = operation.webRequest;
            download.isDone = true;
            download.filePath = filePath;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                WebRequestError error = WebRequestError.GenerateFromWebRequest(webRequest);
                download.NotifyFailed(error);
            }
            else
            {
                #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log("DOWNLOAD SUCEEDED"
                              + "\nDownload completed at: " + ServerTimeStamp.ToLocalDateTime(responseTimeStamp)
                              + "\nURL: " + webRequest.url
                              + "\nFilePath: " + filePath);
                }
                #endif

                try
                {
                    File.Move(filePath + ".download", filePath);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to save mod binary."
                                          + "\nFile: " + filePath + "\n");

                    Utility.LogExceptionAsWarning(warningInfo, e);

                    download.NotifyFailed(new WebRequestError());
                }

                download.NotifySucceeded();
                download.isDone = true;
            }
        }
    }
}
