namespace ModIO
{
    public enum ModMediaImageVersion
    {
        Original = 0,
        Thumb_320x180,
    }

    [System.Serializable]
    public class ModMediaImageSet : ImageSet
    {
        public ModMediaImageSet()
        {
            foreach(ModMediaImageVersion version in System.Enum.GetValues(typeof(ModMediaImageVersion)))
            {
                this.locationMap[(int)version] = new FilePathURLPair();
            }
        }

        public FilePathURLPair GetVersionLocation(ModMediaImageVersion version)
        {
            return this.locationMap[(int)version];
        }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyImageObjectValues(API.ImageObject apiObject)
        {
            this.fileName = apiObject.filename;
            this.locationMap[(int)ModMediaImageVersion.Original]         = new FilePathURLPair(){ url = apiObject.original };
            this.locationMap[(int)ModMediaImageVersion.Thumb_320x180]    = new FilePathURLPair(){ url = apiObject.thumb_320x180 };
        }
        public static ModMediaImageSet CreateFromImageObject(API.ImageObject iconObject)
        {
            var retVal = new ModMediaImageSet();
            retVal.ApplyImageObjectValues(iconObject);
            return retVal;
        }
    }
}
