using System;

namespace ModIO.UI
{
    [Serializable]
    public struct DownloadDisplayData
    {
        public Int64 bytesDownloaded;
        public Int64 fileSize;
    }

    public abstract class DownloadDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event Action<DownloadDisplayComponent> onClick;

        public abstract DownloadDisplayData data { get; set; }

        public abstract void Initialize();
    }
}
