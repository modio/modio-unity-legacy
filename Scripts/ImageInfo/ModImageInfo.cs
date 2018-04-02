namespace ModIO
{
    public enum ModImageVersion
    {
        Original = 0,
        Thumb_320x180,
    }

    [System.Serializable]
    public class ModImageInfo : ImageInfo
    {
        public ModImageInfo()
        {
            foreach(ModImageVersion version in System.Enum.GetValues(typeof(ModImageVersion)))
            {
                this.locationMap[(int)version] = new FilePathURLPair();
            }
        }

        public FilePathURLPair GetVersionLocation(ModImageVersion version)
        {
            return this.locationMap[(int)version];
        }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyImageObjectValues(API.ImageObject apiObject)
        {
            this.fileName = apiObject.filename;
            this.locationMap[(int)ModImageVersion.Original]         = new FilePathURLPair(){ url = apiObject.original };
            this.locationMap[(int)ModImageVersion.Thumb_320x180]    = new FilePathURLPair(){ url = apiObject.thumb_320x180 };
        }
        public static ModImageInfo CreateFromImageObject(API.ImageObject iconObject)
        {
            var retVal = new ModImageInfo();
            retVal.ApplyImageObjectValues(iconObject);
            return retVal;
        }
    }
}
