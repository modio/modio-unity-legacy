using System;
using System.Collections.Generic;

namespace ModIO
{
    [Serializable]
    public class UnsubmittedGameMedia
    {
        // --- FIELDS ---
        public string logoFilepath; //Image file which will represent your games logo. Must be gif, jpg or png format and cannot exceed 8MB in filesize. Dimensions must be at least 640x360 and we recommended you supply a high resolution image with a 16 / 9 ratio. mod.io will use this logo to create three thumbnails with the dimensions of 320x180, 640x360 and 1280x720.
        public string iconFilepath; //Image file which will represent your games icon. Must be gif, jpg or png format and cannot exceed 1MB in filesize. Dimensions must be at least 64x64 and a transparent png that works on a colorful background is recommended. mod.io will use this icon to create three thumbnails with the dimensions of 64x64, 128x128 and 256x256.
        public string headerImageFilepath; //Image file which will represent your games header. Must be gif, jpg or png format and cannot exceed 256KB in filesize. Dimensions of 400x100 and a light transparent png that works on a dark background is recommended.

        // --- ACCESSORS ---
        public Dictionary<string, BinaryData> GetDataFields()
        {
            Dictionary<string, BinaryData> retVal = new Dictionary<string, BinaryData>();
            
            BinaryData binaryData;

            binaryData = GetLogoBinaryData();
            if(binaryData != null)
            {
                retVal["logo"] = GetLogoBinaryData();
            }
            binaryData = GetIconBinaryData();
            if(binaryData != null)
            {
                retVal["icon"] = GetIconBinaryData();
            }
            binaryData = GetHeaderImageBinaryData();
            if(binaryData != null)
            {
                retVal["header"] = GetHeaderImageBinaryData();
            }

            return retVal;
        }

        public BinaryData GetLogoBinaryData()
        {
            if(System.IO.File.Exists(logoFilepath))
            {
                BinaryData newData = new BinaryData();
                newData.contents = System.IO.File.ReadAllBytes(logoFilepath);
                newData.fileName = System.IO.Path.GetFileName(logoFilepath);
                return newData;
            }
            return null;
        }

        public BinaryData GetIconBinaryData()
        {
            if(System.IO.File.Exists(iconFilepath))
            {
                BinaryData newData = new BinaryData();
                newData.contents = System.IO.File.ReadAllBytes(iconFilepath);
                newData.fileName = System.IO.Path.GetFileName(iconFilepath);
                return newData;
            }
            return null;
        }

        public BinaryData GetHeaderImageBinaryData()
        {
            if(System.IO.File.Exists(headerImageFilepath))
            {
                BinaryData newData = new BinaryData();
                newData.contents = System.IO.File.ReadAllBytes(headerImageFilepath);
                newData.fileName = System.IO.Path.GetFileName(headerImageFilepath);
                return newData;
            }
            return null;
        }

    }
}