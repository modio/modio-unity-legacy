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
    }
}
