using System;

namespace ModIO
{
    public class ModBinaryRequest
    {
        public event Action succeeded;
        public event Action<WebRequestError> failed;

        public string filePath;
        public bool isDone;

        public UnityEngine.Networking.UnityWebRequest webRequest;

        internal void NotifySucceeded()
        {
            if(succeeded != null)
            {
                succeeded();
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
}
