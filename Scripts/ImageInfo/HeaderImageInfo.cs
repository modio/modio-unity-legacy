namespace ModIO
{
    [System.Serializable]
    public class HeaderImageInfo : ImageInfo
    {
        public HeaderImageInfo()
        {
            this.locationMap[0] = new FilePathURLPair();
        }

        public FilePathURLPair GetLocation()
        {
            return this.locationMap[0];
        }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyHeaderImageObjectValues(API.HeaderImageObject apiObject)
        {
            this.fileName = apiObject.filename;
            this.locationMap[0] = new FilePathURLPair(){ url = apiObject.original };
        }

        public static HeaderImageInfo CreateFromHeaderImageObject(API.HeaderImageObject apiObject)
        {
            var retVal = new HeaderImageInfo();
            retVal.ApplyHeaderImageObjectValues(apiObject);
            return retVal;
        }
    }
}