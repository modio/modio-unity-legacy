using LogoObject = ModIO.API.LogoObject;

namespace ModIO
{
    public enum ModLogoVersion
    {
        FullSize = 0,
        Thumbnail_320x180,
        Thumbnail_640x360,
        Thumbnail_1280x720,
    }

    [System.Serializable]
    public class ModLogoImageLocator : MultiVersionImageLocator<ModLogoVersion>
    {
        // ---------[ ABSTRACTS ]---------
        protected override int FullSizeVersion() { return (int)ModLogoVersion.FullSize; }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyLogoObjectValues(LogoObject apiObject)
        {
            this._fileName = apiObject.fileName;
            this._versionPairing = new VersionSourcePair[]
            {
                new VersionSourcePair()
                {
                    versionId = (int)ModLogoVersion.FullSize,
                    url = apiObject.fullSize
                },
                new VersionSourcePair()
                {
                    versionId = (int)ModLogoVersion.Thumbnail_320x180,
                    url = apiObject.thumbnail_320x180
                },
                new VersionSourcePair()
                {
                    versionId = (int)ModLogoVersion.Thumbnail_640x360,
                    url = apiObject.thumbnail_640x360
                },
                new VersionSourcePair()
                {
                    versionId = (int)ModLogoVersion.Thumbnail_1280x720,
                    url = apiObject.thumbnail_1280x720
                },
            };
        }

        public static ModLogoImageLocator CreateFromLogoObject(LogoObject apiObject)
        {
            var retVal = new ModLogoImageLocator();
            retVal.ApplyLogoObjectValues(apiObject);
            return retVal;
        }
    }
}