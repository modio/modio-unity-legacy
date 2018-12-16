using System.Collections.Generic;

namespace ModIO.UI
{
    [System.Serializable]
    public struct ModDisplayData
    {
        public int modId;

        public ModProfileDisplayData    profile;
        public UserProfileDisplayData   submittedBy;
        public ModfileDisplayData       currentBuild;
        public ImageDisplayData[]       media;
        public ModTagDisplayData[]      tags;

        public ModStatisticsDisplayData statistics;

        public bool isSubscribed;
        public bool isEnabled;

        public ImageDisplayData GetLogo()
        {
            if(media == null)
            {
                foreach(ImageDisplayData imageData in media)
                {
                    if(imageData.mediaType == ImageDisplayData.MediaType.ModLogo)
                    {
                        return imageData;
                    }
                }
            }

            return new ImageDisplayData();
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
        }
    }

    public abstract class ModDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModDisplayComponent> onClick;

        public abstract ModDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayProfile(ModProfile profile,
                                            System.Collections.Generic.IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayLoading();
    }
}
