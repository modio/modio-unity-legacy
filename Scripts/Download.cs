#define LOG_DOWNLOADS
#define ADD_SECRET_TO_URL

using System;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    [Serializable]
    public abstract class Download
    {
        // --- EVENTS ---
        public event Action OnStarted;
        public event Action OnCompleted;
        public event ErrorCallback OnFailed;

        // --- FIELDS ---
        public string sourceURI = "";
        public DateTime startTime = new DateTime();
        public Func<float> getDownloadedPercentage = null;
        public Func<ulong> getDownloadedByteCount = null;

        // --- INTERFACE ---
        public void Start()
        {
            // Debug.Assert(IsNotDownloadingAlready);
            Debug.Assert(!String.IsNullOrEmpty(sourceURI));

            #if ADD_SECRET_TO_URL
            sourceURI += "?shhh=secret";
            #endif

            UnityWebRequest webRequest = UnityWebRequest.Get(sourceURI);
            getDownloadedPercentage = () => webRequest.downloadProgress;
            getDownloadedByteCount = () => webRequest.downloadedBytes;

            ModifyWebRequest(webRequest);

            #if LOG_DOWNLOADS
            Debug.Log("STARTING DOWNLOAD"
                      + "\nSourceURI: " + sourceURI);
            #endif

            // Start Download
            // client.StartCoroutine(DownloadData(webRequest, download));
            UnityWebRequestAsyncOperation downloadOperation = webRequest.SendWebRequest();
            downloadOperation.completed += Finalize;

            startTime = DateTime.Now;
            if(OnStarted != null)
            {
                OnStarted();
            }
        }

        protected abstract void ModifyWebRequest(UnityWebRequest webRequest);

        // --- INTERNALS ---
        private void Finalize(AsyncOperation operation)
        {
            UnityWebRequest webRequest = (operation as UnityWebRequestAsyncOperation).webRequest;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                APIError error = APIError.GenerateFromWebRequest(webRequest);
                
                #if LOG_DOWNLOADS
                APIClient.LogError(error);
                #endif
                
                if(OnFailed != null)
                {
                    OnFailed(error);
                }
            }
            else
            {
                #if LOG_DOWNLOADS
                Debug.Log("DOWNLOAD SUCEEDED"
                          + "\nSourceURI: " + webRequest.url);
                #endif

                if(OnCompleted != null)
                {
                    OnCompleted();
                }
            }


        }
    }

    [Serializable]
    public class FileDownload : Download
    {
        public string fileURI = "";

        protected override void ModifyWebRequest(UnityWebRequest webRequest)
        {
            DownloadHandlerFile downloadHandler = new DownloadHandlerFile(fileURI);
            webRequest.downloadHandler = downloadHandler;
        }
    }

    [Serializable]
    public class TextureDownload : Download
    {
        public Texture2D texture = null;

        protected override void ModifyWebRequest(UnityWebRequest webRequest)
        {
            DownloadHandlerTexture downloadHandler = new DownloadHandlerTexture(false);
            webRequest.downloadHandler = downloadHandler;
        }
    }
}