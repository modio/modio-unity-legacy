namespace ModIO
{
    public enum AvatarVersion
    {
        Original = 0,
        Thumb_50x50,
        Thumb_100x100
    }

    [System.Serializable]
    public class AvatarImageInfo : ImageInfo
    {
        public AvatarImageInfo()
        {
            foreach (AvatarVersion version in System.Enum.GetValues(typeof(AvatarVersion)))
            {
                this.locationMap[(int)version] = new FilePathURLPair();
            }
        }

        public FilePathURLPair GetVersionLocation(AvatarVersion version)
        {
            return this.locationMap[(int)version];
        }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyAvatarObjectValues(API.AvatarObject apiObject)
        {
            this.fileName = apiObject.filename;
            this.locationMap[(int)AvatarVersion.Original]         = new FilePathURLPair(){ url = apiObject.original };
            this.locationMap[(int)AvatarVersion.Thumb_50x50]      = new FilePathURLPair(){ url = apiObject.thumb_50x50 };
            this.locationMap[(int)AvatarVersion.Thumb_100x100]    = new FilePathURLPair(){ url = apiObject.thumb_100x100 };
        }
        public static AvatarImageInfo CreateFromAvatarObject(API.AvatarObject apiObject)
        {
            var retVal = new AvatarImageInfo();
            retVal.ApplyAvatarObjectValues(apiObject);
            return retVal;
        }
    }
}