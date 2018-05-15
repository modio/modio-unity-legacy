using System;

using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public class TextureDownload
    {
        public event Action<Texture2D> succeeded;
        public event Action<WebRequestError> failed;

        internal void NotifySucceeded(Texture2D texture)
        {
            if(succeeded != null)
            {
                succeeded(texture);
            }
        }
        internal void NotifyFailed(WebRequestError error)
        {
            #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS
                   && failed != APIClient.LogError)
                {
                    APIClient.LogError(error);
                }
            #endif

            if(failed != null)
            {
                failed(error);
            }
        }
    }

    public static class DownloadClient
    {
        public static TextureDownload DownloadModLogo(ModProfile profile, LogoVersion version)
        {
            string logoURL = profile.logoLocator.GetVersionURL(version);

            UnityWebRequest webRequest = UnityWebRequest.Get(logoURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            TextureDownload download = new TextureDownload();

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(download, operation);

            return download;
        }

        public static TextureDownload DownloadModGalleryImage(ModProfile profile,
                                                              string imageFileName,
                                                              ModGalleryImageVersion version)
        {
            string imageURL = profile.media.GetGalleryImageWithFileName(imageFileName).GetVersionURL(version);

            UnityWebRequest webRequest = UnityWebRequest.Get(imageURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            TextureDownload download = new TextureDownload();

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(download, operation);

            return download;
        }

        private static void OnImageDownloadCompleted(TextureDownload request,
                                                     UnityWebRequestAsyncOperation operation)
        {
            UnityWebRequest webRequest = operation.webRequest;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                WebRequestError error = WebRequestError.GenerateFromWebRequest(webRequest);
                request.NotifyFailed(error);
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
                request.NotifySucceeded(imageTexture);
            }
        }
    }
}
