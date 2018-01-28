using System;
using System.Collections.Generic;

namespace ModIO
{
    [Serializable]
    public class ModMediaInfo : IEquatable<ModMediaInfo>, IAPIObjectWrapper<API.ModMediaObject>, UnityEngine.ISerializationCallbackReceiver
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModMediaObject _data;

        public string[] youtubeURLs     { get { return _data.youtube; } }
        public string[] sketchfabURLS   { get { return _data.sketchfab; } }
        public ImageInfo[] images       { get; private set; }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.ModMediaObject apiObject)
        {
            this._data = apiObject;

            // - Load Images -
            int imageCount = (apiObject.images == null ? 0 : apiObject.images.Length);
            this.images = new ImageInfo[imageCount];
            for(int i = 0;
                i < imageCount;
                ++i)
            {
                this.images[i] = new ImageInfo();
                this.images[i].WrapAPIObject(apiObject.images[i]);
            }
        }
        public API.ModMediaObject GetAPIObject()
        {
            return this._data;
        }

        // - ISerializationCallbackReceiver -
        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            this.WrapAPIObject(this._data);
        }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ModMediaInfo);
        }

        public bool Equals(ModMediaInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }

    [Serializable]
    public class UnsubmittedModMedia
    {
        // --- FIELDS ---
        // Image file which will represent your mods logo. Must be gif, jpg or png format and cannot exceed 8MB in filesize. Dimensions must be at least 640x360 and we recommended you supply a high resolution image with a 16 / 9 ratio. mod.io will use this logo to create three thumbnails with the dimensions of 320x180, 640x360 and 1280x720.
        public string logoFilepath;
        // Zip archive of images to upload. Only valid gif, jpg and png images in the zip file will be processed. The filename must be images.zip all other zips will be ignored. Alternatively you can POST one or more images to this endpoint and they will be detected and added to the mods gallery.
        public string imagesFilepath;
        // Full Youtube link(s) you want to add - example 'https://www.youtube.com/watch?v=IGVZOLV9SPo'
        public string[] youtube;
        // Full Sketchfab link(s) you want to add - example 'https://sketchfab.com/models/71f04e390ff54e5f8d9a51b4e1caab7e'
        public string[] sketchfab;

        // --- ACCESSORS ---
        public Dictionary<string, string> GetValueFields()
        {
            Dictionary<string, string> retVal = new Dictionary<string, string>();

            retVal["youtube"] = youtube.ToString();
            retVal["sketchfab"] = sketchfab.ToString();

            return retVal;
        }
        public Dictionary<string, BinaryData> GetDataFields()
        {
            Dictionary<string, BinaryData> retVal = new Dictionary<string, BinaryData>();
            
            BinaryData binaryData;

            binaryData = GetLogoBinaryData();
            if(binaryData != null)
            {
                retVal["logo"] = GetLogoBinaryData();
            }
            binaryData = GetImagesBinaryData();
            if(binaryData != null)
            {
                retVal["images"] = GetImagesBinaryData();
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

        public BinaryData GetImagesBinaryData()
        {
            if(System.IO.File.Exists(imagesFilepath))
            {
                BinaryData newData = new BinaryData();
                newData.contents = System.IO.File.ReadAllBytes(imagesFilepath);
                newData.fileName = System.IO.Path.GetFileName(imagesFilepath);
                return newData;
            }
            return null;
        }
    }
}
