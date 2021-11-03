namespace ModIO.API
{
    public class AddModMediaParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // Image file which will represent your mods logo. Must be gif, jpg or png format and cannot
        // exceed 8MB in filesize. Dimensions must be at least 640x360 and we recommended you supply
        // a high resolution image with a 16 / 9 ratio. mod.io will use this logo to create three
        // thumbnails with the dimensions of 320x180, 640x360 and 1280x720.
        public BinaryUpload logo
        {
            set {
                this.SetBinaryData("logo", value.fileName, value.data);
            }
        }

        // Zip archive of the gallery images to upload. Only valid gif, jpg and png images in the
        // zip file will be processed. The filename must be images.zip all other zips will be
        // ignored.
        public BinaryUpload galleryImages
        {
            set {
                this.SetBinaryData("images", "images.zip", value.data);
            }
        }

        // Full Youtube link(s) you want to add.
        // For example 'https://www.youtube.com/watch?v=IGVZOLV9SPo'
        public string[] youtube
        {
            set {
                this.SetStringArrayValue("youtube[]", value);
            }
        }

        // Full Sketchfab link(s) you want to add
        // For example 'https://sketchfab.com/models/71f04e390ff54e5f8d9a51b4e1caab7e'
        public string[] sketchfab
        {
            set {
                this.SetStringArrayValue("sketchfab[]", value);
            }
        }
    }
}
