using System;

namespace ModIO.UI
{
    [Obsolete("No longer supported.")]
    [Serializable]
    public struct DownloadDisplayData
    {
        public Int64 bytesReceived;
        public Int64 bytesPerSecond;
        public Int64 bytesTotal;
        public bool isActive;
    }

    [Obsolete("No longer supported.")]
    public abstract class DownloadDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event Action<DownloadDisplayComponent> onClick;

        public abstract DownloadDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayDownload(FileDownloadInfo downloadInfo);
    }
}
