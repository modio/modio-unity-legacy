using System;

namespace ModIO
{
    public class ModBinaryRequest
    {
        public event Action<ModBinaryRequest> succeeded;
        public event Action<ModBinaryRequest> failed;

        public int modId;
        public int modfileId;
        public string binaryFilePath;
        public bool isDone;

        public WebRequestError error;

        public UnityEngine.Networking.UnityWebRequest webRequest;

        internal void NotifySucceeded()
        {
            if(succeeded != null)
            {
                succeeded(this);
            }
        }

        internal void NotifyFailed()
        {
            #if DEBUG
            #pragma warning disable 0162 // ignore unreachable code warning
                if(error != null
                   && GlobalSettings.LOG_ALL_WEBREQUESTS)
                {
                    WebRequestError.LogAsWarning(error);
                }
            #pragma warning restore 0162
            #endif

            if(failed != null)
            {
                failed(this);
            }
        }
    }
}
