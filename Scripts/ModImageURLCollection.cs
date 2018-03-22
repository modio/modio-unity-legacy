using System.Collections.Generic;
using UnityEngine;

namespace ModIO
{
    [System.Serializable]
    public class ModImageURLCollection : ISerializationCallbackReceiver
    {
        // TODO(@jackson): Remove?
        public string fileName = string.Empty;
        public Dictionary<ImageVersion, string> urlMap = new Dictionary<ImageVersion, string>(4);

        // - Serialized Backing For Dictionary -
        [System.Serializable]
        private class VersionURLPair
        {
            public ImageVersion version;
            public string url;
        }
        [SerializeField]
        private List<VersionURLPair> _serializableMap;

        // - ISerializationCallbackReceiver Interface -
        public void OnAfterDeserialize()
        {
            foreach(VersionURLPair pair in _serializableMap)
            {
                urlMap[pair.version] = pair.url;
            }
        }
        public void OnBeforeSerialize()
        {
            _serializableMap = new List<VersionURLPair>(urlMap.Count);
            foreach(KeyValuePair<ImageVersion, string> kvp in urlMap)
            {
                _serializableMap.Add(new VersionURLPair(){ version = kvp.Key, url = kvp.Value });
            }
        }
    }
}