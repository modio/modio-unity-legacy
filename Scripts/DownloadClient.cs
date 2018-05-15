using System;

using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public class TextureDownload
    {
        public event Action<Texture2D> downloadSucceeded;
        public event Action<WebRequestError> downloadFailed;

        internal void NotifyDownloadSucceeded(Texture2D texture)
        {
            if(downloadSucceeded != null)
            {
                downloadSucceeded(texture);
            }
        }
        internal void NotifyDownloadFailed(WebRequestError error)
        {
            #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS
                   && downloadFailed != APIClient.LogError)
                {
                    APIClient.LogError(error);
                }
            #endif

            if(downloadFailed != null)
            {
                downloadFailed(error);
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

        private static void OnImageDownloadCompleted(TextureDownload request,
                                                     UnityWebRequestAsyncOperation operation)
        {
            UnityWebRequest webRequest = operation.webRequest;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                WebRequestError error = WebRequestError.GenerateFromWebRequest(webRequest);
                request.NotifyDownloadFailed(error);
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
                request.NotifyDownloadSucceeded(imageTexture);
            }
        }
    }
}
