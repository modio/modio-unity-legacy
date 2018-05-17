using System;
using System.IO;

using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public static class DownloadClient
    {
        // ---------[ IMAGE DOWNLOADS ]---------
        public static ImageRequest DownloadModLogo(ModProfile profile, LogoVersion version)
        {
            ImageRequest request = new ImageRequest();
            request.isDone = false;

            string logoURL = profile.logoLocator.GetVersionURL(version);

            UnityWebRequest webRequest = UnityWebRequest.Get(logoURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, request);

            return request;
        }

        public static ImageRequest DownloadModGalleryImage(ModProfile profile,
                                                           string imageFileName,
                                                           ModGalleryImageVersion version)
        {
            ImageRequest request = new ImageRequest();

            string imageURL = profile.media.GetGalleryImageWithFileName(imageFileName).GetVersionURL(version);

            UnityWebRequest webRequest = UnityWebRequest.Get(imageURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, request);

            return request;
        }

        private static void OnImageDownloadCompleted(UnityWebRequestAsyncOperation operation,
                                                     ImageRequest request)
        {
            UnityWebRequest webRequest = operation.webRequest;
            request.isDone = true;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                request.error = WebRequestError.GenerateFromWebRequest(webRequest);
                request.NotifyFailed();
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

                request.imageTexture = (webRequest.downloadHandler as DownloadHandlerTexture).texture;
                request.NotifySucceeded();
            }
        }

        // ---------[ BINARY DOWNLOADS ]---------
        public static ModBinaryRequest DownloadModBinary(int modId, int modfileId,
                                                         string downloadFilePath)
        {
            ModBinaryRequest request = new ModBinaryRequest();

            request.isDone = false;
            request.binaryFilePath = downloadFilePath;

            // - Acquire Download URL -
            APIClient.GetModfile(modId, modfileId,
                                 (mf) => DownloadClient.OnGetModfile(mf, request),
                                 (e) => { request.error = e; request.NotifyFailed(); });

            return request;
        }

        private static void OnGetModfile(Modfile modfile, ModBinaryRequest request)
        {
            string tempFilePath = request.binaryFilePath + ".download";

            request.modfile = modfile;
            request.webRequest = UnityWebRequest.Get(modfile.downloadLocator.binaryURL);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
                request.webRequest.downloadHandler = new DownloadHandlerFile(tempFilePath);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to create download file on disk."
                      + "\nFile: " + tempFilePath + "\n");

                Utility.LogExceptionAsWarning(warningInfo, e);

                request.NotifyFailed();

                return;
            }

            var operation = request.webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnModBinaryRequestCompleted(operation,
                                                                                     request);
        }

        private static void OnModBinaryRequestCompleted(UnityWebRequestAsyncOperation operation,
                                                        ModBinaryRequest request)
        {
            UnityWebRequest webRequest = operation.webRequest;
            request.isDone = true;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                request.error = WebRequestError.GenerateFromWebRequest(webRequest);

                request.NotifyFailed();
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
                              + "\nFilePath: " + request.binaryFilePath);
                }
                #endif

                try
                {
                    if(File.Exists(request.binaryFilePath))
                    {
                        File.Delete(request.binaryFilePath);
                    }

                    File.Move(request.binaryFilePath + ".download", request.binaryFilePath);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to save mod binary."
                                          + "\nFile: " + request.binaryFilePath + "\n");

                    Utility.LogExceptionAsWarning(warningInfo, e);

                    request.NotifyFailed();
                }

                request.NotifySucceeded();
            }
        }
    }
}
