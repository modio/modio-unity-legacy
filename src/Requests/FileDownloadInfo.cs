namespace ModIO
{
    [System.Serializable]
    public class FileDownloadInfo
    {
        public UnityEngine.Networking.UnityWebRequest request;

        public WebRequestError error;
        public string target;
        public System.Int64 fileSize;
        public bool isDone;
        public bool wasAborted;

        /// <summary>Number of bytes being downloaded per-second.</summary>
        public System.Int64 bytesPerSecond;
    }
}
