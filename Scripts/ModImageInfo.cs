using System.Collections.Generic;
using UnityEngine;

namespace ModIO
{
    [System.Serializable]
    public class ModImageInfo : ISerializationCallbackReceiver
    {
        // TODO(@jackson): Remove?
        public string fileName = string.Empty;
        public Dictionary<ImageVersion, FilePathURLPair> locationMap = new Dictionary<ImageVersion, FilePathURLPair>();

        // - Serialized Backing For Dictionaries -
        [System.Serializable]
        private class VersionLocationPair
        {
            public ImageVersion version;
            public FilePathURLPair location;
        }
        [SerializeField]
        private List<VersionLocationPair> _locationMap;

        // - ISerializationCallbackReceiver Interface -
        public void OnAfterDeserialize()
        {
            foreach(VersionLocationPair pair in this._locationMap)
            {
                this.locationMap[pair.version] = pair.location;
            }
        }
        public void OnBeforeSerialize()
        {
            this._locationMap = new List<VersionLocationPair>(this.locationMap.Count);
            foreach(KeyValuePair<ImageVersion, FilePathURLPair> kvp in this.locationMap)
            {
                this._locationMap.Add(new VersionLocationPair(){ version = kvp.Key, location = kvp.Value });
            }
        }

        public void ApplyLogoObjectValues(API.LogoObject logoObject)
        {
            this.fileName = logoObject.filename;
            this.locationMap[ImageVersion.Original]         = new FilePathURLPair(){ url = logoObject.original };
            this.locationMap[ImageVersion.Thumb_320x180]    = new FilePathURLPair(){ url = logoObject.thumb_320x180 };
            this.locationMap[ImageVersion.Thumb_640x360]    = new FilePathURLPair(){ url = logoObject.thumb_640x360 };
            this.locationMap[ImageVersion.Thumb_1280x720]   = new FilePathURLPair(){ url = logoObject.thumb_1280x720 };
        }
    }
}