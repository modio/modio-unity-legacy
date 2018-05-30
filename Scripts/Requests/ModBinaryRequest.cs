using System;

namespace ModIO
{
    public class ModBinaryRequest
    {
        public event Action<ModBinaryRequest> succeeded;
        public event Action<ModBinaryRequest> failed;

        public Modfile modfile;
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
                if(GlobalSettings.LOG_ALL_WEBREQUESTS
                   && error != null)
                {
                    WebRequestError.LogAsWarning(error);
                }
            #endif

            if(failed != null)
            {
                failed(this);
            }
        }
    }
}
