namespace ModIO
{
    public enum ModBinaryStatus
    {
        Missing,
        PartiallyDownloaded,
        Error_FileSizeMismatch,
        Error_HashCheckFailed,
        Error_UnableToReadFile,
        CompleteAndVerified,
    }
}
