using System.Collections.Generic;

namespace ModIO.UI
{
    [System.Serializable]
    public struct ModDisplayData
    {
        public ModProfileDisplayData    profile;
        public UserDisplayData          submittedBy;
        public ModfileDisplayData       currentBuild;
        public ImageDisplayData[]       media;
        public ModTagDisplayData[]      tags;

        public ModStatisticsDisplayData statistics;

        public DownloadDisplayData      activeDownload;

        public bool isSubscribed;
        public bool isModEnabled;

        public ImageDisplayData GetLogo()
        {
            if(media != null)
            {
                foreach(ImageDisplayData imageData in media)
                {
                    if(imageData.mediaType == ImageDisplayData.MediaType.ModLogo)
                    {
                        return imageData;
                    }
                }
            }

            return new ImageDisplayData()
            {
                modId = profile.modId,
                mediaType = ImageDisplayData.MediaType.ModLogo,
                fileName = string.Empty,
                texture = null,
            };
        }
        public void SetLogo(ImageDisplayData value)
        {
            List<ImageDisplayData> mediaItems;
            if(media == null)
            {
                mediaItems = new List<ImageDisplayData>();
            }
            else
            {
                mediaItems = new List<ImageDisplayData>(media);
            }

            for(int i = 0; i < mediaItems.Count; ++i)
            {
                ImageDisplayData imageData = mediaItems[i];
                if(imageData.mediaType == ImageDisplayData.MediaType.ModLogo)
                {
                    mediaItems.RemoveAt(i);
                    --i;
                }
            }

            mediaItems.Insert(0, value);

            media = mediaItems.ToArray();
        }
    }
}
