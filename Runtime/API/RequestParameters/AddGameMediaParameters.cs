namespace ModIO.API
{
    public class AddGameMediaParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // Image file which will represent your game's logo. Must be gif, jpg or png format and
        // cannot exceed 8MB in filesize. Dimensions must be at least 640x360 and we recommended you
        // supply a high resolution image with a 16 / 9 ratio. mod.io will use this logo to create
        // three thumbnails with the dimensions of 320x180, 640x360 and 1280x720.
        public BinaryUpload logo
        {
            set {
                this.SetBinaryData("logo", value.fileName, value.data);
            }
        }

        // Image file which will represent your game's icon. Must be gif, jpg or png format and
        // cannot exceed 1MB in filesize. Dimensions must be at least 64x64 and a transparent png
        // that works on a colorful background is recommended. mod.io will use this icon to create
        // three thumbnails with the dimensions of 64x64, 128x128 and 256x256.
        public BinaryUpload icon
        {
            set {
                this.SetBinaryData("icon", value.fileName, value.data);
            }
        }

        // Image file which will represent your game's header. Must be gif, jpg or png format and
        // cannot exceed 256KB in filesize. Dimensions of 400x100 and a light transparent png that
        // works on a dark background is recommended.
        public BinaryUpload headerImage
        {
            set {
                this.SetBinaryData("header", value.fileName, value.data);
            }
        }
    }
}
