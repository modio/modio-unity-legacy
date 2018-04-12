namespace ModIO.API
{
    [System.Serializable]
    public struct HeaderImageObject
    {
        // Header image filename including extension.
        public string filename;
        // URL to the full-sized header image.
        public string original;
    }
}