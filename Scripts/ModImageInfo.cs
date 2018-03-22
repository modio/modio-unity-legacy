using System.Collections.Generic;
using UnityEngine;

namespace ModIO
{
    [System.Serializable]
    public class ModImageInfo : ISerializationCallbackReceiver
    {
        // TODO(@jackson): Remove?
        public string fileName = string.Empty;
        public Dictionary<ImageVersion, FilePathURLPair> locationMap = new Dictionary<ImageVersion, FilePathURLPair>(4);

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
    }
}