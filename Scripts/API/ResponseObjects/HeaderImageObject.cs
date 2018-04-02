namespace ModIO.API
{
    [System.Serializable]
    public struct HeaderImageObject
    {
        // Header image filename including extension.
        public readonly string filename;
        // URL to the full-sized header image.
        public readonly string original;
    }
}