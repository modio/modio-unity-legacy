using System;

namespace ModIO
{
    [Obsolete]
    public class ModBinaryRequest
    {
        public FileDownloadInfo downloadInfo;
        public UnityEngine.Networking.UnityWebRequest webRequest { get { return downloadInfo.request; } }
        public string binaryFilePath { get { return downloadInfo.target; } }
        private bool m_isDone = false;
        public bool isDone
        {
            get { return (webRequest == null ? m_isDone : webRequest.isDone); }
            set { m_isDone = value; }
        }

        public event Action<ModBinaryRequest> succeeded;
        public event Action<ModBinaryRequest> failed;

        public int modId;
        public int modfileId;
        public WebRequestError error;

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

        public static ModBinaryRequest Create(int modId,
                                              int modfileId,
                                              FileDownloadInfo download)
        {
            ModBinaryRequest newMBR = new ModBinaryRequest()
            {
                downloadInfo = download,
                modId = modId,
                modfileId = modfileId,
            };
            return newMBR;
        }
    }
}
