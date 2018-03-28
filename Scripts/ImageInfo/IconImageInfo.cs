namespace ModIO
{
    public enum IconVersion
    {
        Original = 0,
        Thumb_64x64,
        Thumb_128x128,
        Thumb_256x256,
    }

    [System.Serializable]
    public class IconImageInfo : ImageInfo
    {
        public IconImageInfo()
        {
            foreach (IconVersion version in System.Enum.GetValues(typeof(IconVersion)))
            {
                this.locationMap[(int)version] = new FilePathURLPair();
            }
        }

        public FilePathURLPair GetVersionLocation(IconVersion version)
        {
            return this.locationMap[(int)version];
        }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyAPIObjectValues(API.IconObject apiObject)
        {
            this.fileName = apiObject.filename;
            this.locationMap[(int)IconVersion.Original]         = new FilePathURLPair(){ url = apiObject.original };
            this.locationMap[(int)IconVersion.Thumb_64x64]      = new FilePathURLPair(){ url = apiObject.thumb_64x64 };
            this.locationMap[(int)IconVersion.Thumb_128x128]    = new FilePathURLPair(){ url = apiObject.thumb_128x128 };
            this.locationMap[(int)IconVersion.Thumb_256x256]    = new FilePathURLPair(){ url = apiObject.thumb_256x256 };
        }
        public static IconImageInfo CreateFromAPIObject(API.IconObject iconObject)
        {
            var retVal = new IconImageInfo();
            retVal.ApplyAPIObjectValues(iconObject);
            return retVal;
        }
    }
}