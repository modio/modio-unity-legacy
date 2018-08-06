namespace ModIO
{
    public enum ModfileVirusScanStatus
    {
        NotScanned = 0,
        ScanComplete = 1,
        InProgress = 2,
        FileTooLarge = 3,
        FileNotFound = 4,
        ErrorScanning = 5,
    }
}
