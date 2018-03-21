using System;
using System.Collections.Generic;
using ModIO.API;

namespace ModIO
{
    [Serializable]
    public class ModMediaURLInfo : IEquatable<ModMediaURLInfo>, IAPIObjectWrapper<ModMediaObject>, UnityEngine.ISerializationCallbackReceiver
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private ModMediaObject _data;

        public string[] youtubeURLs     { get { return _data.youtube; } }
        public string[] sketchfabURLs   { get { return _data.sketchfab; } }
        public ImageURLInfo[] images    { get; private set; }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(ModMediaObject apiObject)
        {
            this._data = apiObject;

            // - Load Images -
            int imageCount = (apiObject.images == null ? 0 : apiObject.images.Length);
            this.images = new ImageURLInfo[imageCount];
            for(int i = 0;
                i < imageCount;
                ++i)
            {
                this.images[i] = new ImageURLInfo();
                this.images[i].WrapAPIObject(apiObject.images[i]);
            }
        }
        public ModMediaObject GetAPIObject()
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
            return this.Equals(obj as ModMediaURLInfo);
        }

        public bool Equals(ModMediaURLInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }

    [Serializable]
    public class ModMediaChanges
    {
        public int modId;

        // --- FIELDS ---
        // Images to upload. Only valid gif, jpg and png images. The filename must be images.zip all other zips will be ignored. Alternatively you can POST one or more images to this endpoint and they will be detected and added to the mods gallery.
        public string[] images = new string[0];
        // Full Youtube link(s) you want to add - example 'https://www.youtube.com/watch?v=IGVZOLV9SPo'
        public string[] youtube = new string[0];
        // Full Sketchfab link(s) you want to add - example 'https://sketchfab.com/models/71f04e390ff54e5f8d9a51b4e1caab7e'
        public string[] sketchfab = new string[0];
    }
}
