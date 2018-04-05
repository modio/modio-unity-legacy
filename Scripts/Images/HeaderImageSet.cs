namespace ModIO
{
    [System.Serializable]
    public class HeaderImageSet : ImageSet
    {
        public HeaderImageSet()
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

        public static HeaderImageSet CreateFromHeaderImageObject(API.HeaderImageObject apiObject)
        {
            var retVal = new HeaderImageSet();
            retVal.ApplyHeaderImageObjectValues(apiObject);
            return retVal;
        }
    }
}