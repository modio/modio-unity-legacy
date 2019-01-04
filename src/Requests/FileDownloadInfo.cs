namespace ModIO
{
    [System.Serializable]
    public class FileDownloadInfo
    {
        private bool m_isDone = false;

        public UnityEngine.Networking.UnityWebRequest request;
        public string target;
        public System.Int64 fileSize;
        public bool isDone
        {
            get { return (request == null ? m_isDone : request.isDone); }
            set { m_isDone = value; }
        }
    }
}
