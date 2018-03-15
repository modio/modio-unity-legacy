using System;
using System.Collections.Generic;

namespace ModIO
{
    [Serializable]
    public class UnsubmittedGameMedia
    {
        // --- FIELDS ---
        // Image file which will represent your games logo. Must be gif, jpg or png format and cannot exceed 8MB in filesize. Dimensions must be at least 640x360 and we recommended you supply a high resolution image with a 16 / 9 ratio. mod.io will use this logo to create three thumbnails with the dimensions of 320x180, 640x360 and 1280x720.
        public string logoFilepath;
        // Image file which will represent your games icon. Must be gif, jpg or png format and cannot exceed 1MB in filesize. Dimensions must be at least 64x64 and a transparent png that works on a colorful background is recommended. mod.io will use this icon to create three thumbnails with the dimensions of 64x64, 128x128 and 256x256.
        public string iconFilepath;
        // Image file which will represent your games header. Must be gif, jpg or png format and cannot exceed 256KB in filesize. Dimensions of 400x100 and a light transparent png that works on a dark background is recommended.
        public string headerImageFilepath;

        // --- ACCESSORS ---
        public API.BinaryDataParameter[] GetDataFields()
        {
            List<API.BinaryDataParameter> retVal = new List<API.BinaryDataParameter>(3);
            
            if(System.IO.File.Exists(logoFilepath))
            {
                API.BinaryDataParameter newData = new API.BinaryDataParameter();
                newData.key = "logo";
                newData.contents = System.IO.File.ReadAllBytes(logoFilepath);
                newData.fileName = System.IO.Path.GetFileName(logoFilepath);

                retVal.Add(newData);
            }

            if(System.IO.File.Exists(iconFilepath))
            {
                API.BinaryDataParameter newData = new API.BinaryDataParameter();
                newData.key = "icon";
                newData.contents = System.IO.File.ReadAllBytes(iconFilepath);
                newData.fileName = System.IO.Path.GetFileName(iconFilepath);

                retVal.Add(newData);
            }

            if(System.IO.File.Exists(headerImageFilepath))
            {
                API.BinaryDataParameter newData = new API.BinaryDataParameter();
                newData.key = "header";
                newData.contents = System.IO.File.ReadAllBytes(headerImageFilepath);
                newData.fileName = System.IO.Path.GetFileName(headerImageFilepath);

                retVal.Add(newData);
            }

            return retVal.ToArray();
        }

        public API.AddGameMediaParameters AsAddGameMediaParameters()
        {
            var retVal = new API.AddGameMediaParameters();
            retVal.binaryData = new List<API.BinaryDataParameter>(this.GetDataFields());
            return retVal;
        }
    }
}