namespace ModIO.UI
{
    [System.Obsolete("No longer supported.")]
    [System.Serializable]
    public struct ModDisplayData
    {
        public ModProfileDisplayData profile;
        public UserProfileDisplayData submittorProfile;
        public ImageDisplayData submittorAvatar;
        public ModfileDisplayData currentBuild;
        public ImageDisplayData logo;
        public ImageDisplayData[] youTubeThumbnails;
        public ImageDisplayData[] galleryImages;
        public ModTagDisplayData[] tags;

        public ModStatisticsDisplayData statistics;

        public DownloadDisplayData binaryDownload;

        public bool isSubscribed;
        public bool isModEnabled;
        public ModRatingValue userRating;
    }
}
