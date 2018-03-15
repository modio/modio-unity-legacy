#define LOG_DOWNLOADS

using System;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public delegate void DownloadStartedCallback(Download download);
    public delegate void DownloadCompletedCallback(Download download);
    public delegate void DownloadFailedCallback(Download download, ErrorInfo error);

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
        public event DownloadStartedCallback OnStarted;
        public event DownloadCompletedCallback OnCompleted;
        public event DownloadFailedCallback OnFailed;

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

            UnityWebRequestAsyncOperation downloadOperation = webRequest.SendWebRequest();
            downloadOperation.completed += Finalize;

            startTime = DateTime.Now;
            status = Status.InProgress;

            #if LOG_DOWNLOADS
            Debug.Log("STARTING DOWNLOAD"
                      + "\nSourceURL: " + webRequest.url);
            #endif

            if(OnStarted != null)
            {
                OnStarted(this);
            }
        }

        public void MarkAsFailed(ErrorInfo error)
        {
            this.OnCancelled();

            status = Status.Error;
            if(OnFailed != null)
            {
                OnFailed(this, error);
            }
        }

        protected abstract void ModifyWebRequest(UnityWebRequest webRequest);

        // --- INTERNALS ---
        private void Finalize(AsyncOperation operation)
        {
            UnityWebRequest webRequest = (operation as UnityWebRequestAsyncOperation).webRequest;
            ErrorInfo error;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                error = ErrorInfo.GenerateFromWebRequest(webRequest);
                OnFinalize_Failed(webRequest.downloadHandler, error);
                
                #if LOG_DOWNLOADS
                API.Client.LogError(error);
                #endif

                status = Status.Error;
                if(OnFailed != null)
                {
                    OnFailed(this, error);
                }
            }
            else if(!IsErrorFree(webRequest.downloadHandler, out error))
            {
            	OnFinalize_Failed(webRequest.downloadHandler, error);

                #if LOG_DOWNLOADS
                Debug.Log("DOWNLOAD FAILED"
                          + "\nSourceURL: " + webRequest.url);
                #endif

                status = Status.Error;
                if(OnFailed != null)
                {
                	OnFailed(this, error);
                }
            }
            else
            {
            	OnFinalize_Succeeded(webRequest.downloadHandler);

                #if LOG_DOWNLOADS
                Debug.Log("DOWNLOAD SUCEEDED"
                          + "\nSourceURL: " + webRequest.url);
                #endif

                status = Status.Completed;
                if(OnCompleted != null)
                {
                    OnCompleted(this);
                }
            }
        }

        protected virtual bool IsErrorFree(DownloadHandler handler, out ErrorInfo error) { error = null; return true; }
        protected virtual void OnFinalize_Succeeded(DownloadHandler handler) { }
        protected virtual void OnFinalize_Failed(DownloadHandler handler, ErrorInfo error) {}
        protected virtual void OnCancelled() {}
    }

    public class FileDownload : Download
    {
        public string fileURL = "";

        private string expectedMD5 = "";

        public void EnableFilehashVerification(string md5)
        {
            expectedMD5 = md5;
        }

        protected override void ModifyWebRequest(UnityWebRequest webRequest)
        {
            DownloadHandlerFile downloadHandler = new DownloadHandlerFile(fileURL);
            webRequest.downloadHandler = downloadHandler;
        }

        protected override bool IsErrorFree(DownloadHandler handler, out ErrorInfo error)
        {
            if(expectedMD5 != "")
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    using (var stream = System.IO.File.OpenRead(fileURL))
                    {

                        var hash = md5.ComputeHash(stream);
                        string hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        bool isValidHash = (hashString == expectedMD5);

                    	Debug.Log("Checking Hashes: [" + isValidHash + "]"
                                  +"\nExpected Hash: " + expectedMD5
                                  +"\nDownload Hash: " + hashString);

                        if(!isValidHash)
                        {
                        	error = new ErrorInfo();
                            error.httpStatusCode = -1;
                            error.message = "Downloaded file failed Hash-check";
                            error.url = sourceURL;

                            return false;
                        }
                    }
                }
            }
            error = null;
            return true;
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
