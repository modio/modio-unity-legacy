using ImageObject = ModIO.API.ImageObject;

namespace ModIO
{
    public enum ModGalleryImageVersion
    {
        FullSize = 0,
        Thumbnail_320x180,
    }

    [System.Serializable]
    public class ModGalleryImageLocator : MultiVersionImageLocator<ModGalleryImageVersion>
    {
        // ---------[ ABSTRACTS ]---------
        protected override int FullSizeVersion() { return (int)ModGalleryImageVersion.FullSize; }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyImageObjectValues(ImageObject apiObject)
        {
            this._fileName = apiObject.filename;
            this._versionPairing = new VersionSourcePair[]
            {
                new VersionSourcePair()
                {
                    versionId = (int)ModGalleryImageVersion.FullSize,
                    source = apiObject.original
                },
                new VersionSourcePair()
                {
                    versionId = (int)ModGalleryImageVersion.Thumbnail_320x180,
                    source = apiObject.thumb_320x180
                },
            };
        }
        public static ModGalleryImageLocator CreateFromImageObject(ImageObject apiObject)
        {
            var retVal = new ModGalleryImageLocator();
            retVal.ApplyImageObjectValues(apiObject);
            return retVal;
        }
    }
}