using System;
using UnityEngine;
using UnityEngine.Networking;

using Texture2D = UnityEngine.Texture2D;

namespace ModIO
{
    public class ImageRequest
    {
        // ---------[ FIELDS ]---------
        public event Action<ImageRequest> succeeded;
        public event Action<ImageRequest> failed;

        public Texture2D imageTexture;
        public string filePath;
        public bool isDone;

        public WebRequestError error;

        private UnityWebRequestAsyncOperation m_asyncOperation;

        // --- ACCESSORS ---
        public UnityWebRequestAsyncOperation asyncOperation
        {
            get { return this.m_asyncOperation; }
            set
            {
                if(this.m_asyncOperation != value)
                {
                    if(this.m_asyncOperation != null)
                    {
                        this.m_asyncOperation.completed -= this.OnCompleted;
                    }
                    if(value != null)
                    {
                        value.completed += this.OnCompleted;
                    }

                    this.m_asyncOperation = value;
                }
            }
        }

        // ---------[ EVENTS ]---------
        private void OnCompleted(AsyncOperation operation)
        {
            Debug.Assert(operation == this.m_asyncOperation);

            UnityWebRequest webRequest = this.m_asyncOperation.webRequest;
            this.isDone = true;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                this.error = WebRequestError.GenerateFromWebRequest(webRequest);

                if(this.failed != null)
                {
                    this.failed(this);
                }
            }
            else
            {
                #if DEBUG
                if(PluginSettings.data.logAllRequests)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log("IMAGE DOWNLOAD SUCEEDED"
                              + "\nDownload completed at: " + ServerTimeStamp.ToLocalDateTime(responseTimeStamp)
                              + "\nURL: " + webRequest.url);
                }
                #endif

                this.imageTexture = (webRequest.downloadHandler as DownloadHandlerTexture).texture;

                if(succeeded != null)
                {
                    succeeded(this);
                }
            }
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("No longer necessary.")]
        public void NotifySucceeded()
        {
            if(succeeded != null)
            {
                succeeded(this);
            }
        }
        [Obsolete("No longer necessary.")]
        public void NotifyFailed()
        {
            if(failed != null)
            {
                failed(this);
            }
        }
    }
}
