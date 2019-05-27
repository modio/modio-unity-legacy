namespace ModIO.UI
{
    public interface IModDownloadStartedReceiver
    {
        void OnModDownloadStarted(int modId, FileDownloadInfo downloadInfo);
    }
}
