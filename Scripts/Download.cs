#define LOG_DOWNLOADS
#define ADD_SECRET_TO_URL

using System;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    // TODO(@jackson): Create getters where necessary
    public abstract class Download
    {
        public enum Status
        {
            NotStarted,
            InProgress,
            Completed,
            Error
        }

        // --- EVENTS ---
        public event Action OnStarted;
        public event Action OnCompleted;
        public event ErrorCallback OnFailed;

        // --- FIELDS ---
        public string sourceURL = "";
        public DateTime startTime = new DateTime();
        public Status status = Status.NotStarted;
        public Func<float> GetCompletedPercentage = null;
        public Func<ulong> GetDownloadedByteCount = null;

        // --- INTERFACE ---
        public void Start()
        {
            Debug.Assert(status != Status.InProgress);
            Debug.Assert(!String.IsNullOrEmpty(sourceURL));

            UnityWebRequest webRequest = UnityWebRequest.Get(sourceURL);
            GetCompletedPercentage = () => webRequest.downloadProgress;
            GetDownloadedByteCount = () => webRequest.downloadedBytes;

            ModifyWebRequest(webRequest);

            #if ADD_SECRET_TO_URL
            webRequest.url += "?shhh=secret";
            #endif

            #if LOG_DOWNLOADS
            Debug.Log("STARTING DOWNLOAD"
                      + "\nSourceURL: " + webRequest.url);
            #endif

            // Start Download
            startTime = DateTime.Now;
            status = Status.InProgress;

            UnityWebRequestAsyncOperation downloadOperation = webRequest.SendWebRequest();
            downloadOperation.completed += Finalize;

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
                OnFinalize_Failed(webRequest.downloadHandler, error);
                
                status = Status.Error;
                if(OnFailed != null)
                {
                    OnFailed(error);
                }

                #if LOG_DOWNLOADS
                APIClient.LogError(error);
                #endif
            }
            else
            {
                OnFinalize_Succeeded(webRequest.downloadHandler);

                status = Status.Completed;
                if(OnCompleted != null)
                {
                    OnCompleted();
                }

                #if LOG_DOWNLOADS
                Debug.Log("DOWNLOAD SUCEEDED"
                          + "\nSourceURL: " + webRequest.url);
                #endif
            }
        }

        protected virtual void OnFinalize_Failed(DownloadHandler handler, APIError error) {}
        protected virtual void OnFinalize_Succeeded(DownloadHandler handler) {}
    }

    public class FileDownload : Download
    {
        public string fileURL = "";

        protected override void ModifyWebRequest(UnityWebRequest webRequest)
        {
            DownloadHandlerFile downloadHandler = new DownloadHandlerFile(fileURL);
            webRequest.downloadHandler = downloadHandler;
        }
    }

    public class TextureDownload : Download
    {
        public Texture2D texture = null;

        protected override void ModifyWebRequest(UnityWebRequest webRequest)
        {
            // true = Texture is accessible from script
            DownloadHandlerTexture downloadHandler = new DownloadHandlerTexture(true);
            webRequest.downloadHandler = downloadHandler;
        }

        protected override void OnFinalize_Succeeded(DownloadHandler handler)
        {
            DownloadHandlerTexture textureHandler = handler as DownloadHandlerTexture;

            texture = textureHandler.texture;
        }
    }
}