namespace ModIO
{
    public enum LogoVersion
    {
        Original = 0,
        Thumb_320x180,
        Thumb_640x360,
        Thumb_1280x720
    }

    [System.Serializable]
    public class LogoImageInfo : ImageInfo
    {
        public LogoImageInfo()
        {
            foreach (LogoVersion version in System.Enum.GetValues(typeof(LogoVersion)))
            {
                this.locationMap[(int)version] = new FilePathURLPair();
            }
        }

        public FilePathURLPair GetVersionLocation(LogoVersion version)
        {
            return this.locationMap[(int)version];
        }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyAPIObjectValues(API.LogoObject apiObject)
        {
            this.fileName = apiObject.filename;
            this.locationMap[(int)ImageVersion.Original]         = new FilePathURLPair(){ url = apiObject.original };
            this.locationMap[(int)ImageVersion.Thumb_320x180]    = new FilePathURLPair(){ url = apiObject.thumb_320x180 };
            this.locationMap[(int)ImageVersion.Thumb_640x360]    = new FilePathURLPair(){ url = apiObject.thumb_640x360 };
            this.locationMap[(int)ImageVersion.Thumb_1280x720]   = new FilePathURLPair(){ url = apiObject.thumb_1280x720 };
        }

        public static LogoImageInfo CreateFromAPIObject(API.LogoObject apiObject)
        {
            var retVal = new LogoImageInfo();
            retVal.ApplyAPIObjectValues(apiObject);
            return retVal;
        }
    }

}