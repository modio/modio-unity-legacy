using System.Collections.Generic;

using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class MetadataKVP
    {
        // ---------[ FIELDS ]---------
        /// <summary>The key of the key-value pair.</summary>
        [JsonProperty("metakey")]
        public string key;

        /// <summary>The value of the key-value pair.</summary>
        [JsonProperty("metavalue")]
        public string value;

        // ---------[ HELPER FUNCTIONS ]---------
        public static Dictionary<string, string> ArrayToDictionary(MetadataKVP[] kvpArray)
        {
            var dictionary = new Dictionary<string, string>(kvpArray.Length);
            foreach(MetadataKVP kvp in kvpArray)
            {
                dictionary[kvp.key] = kvp.value;
            }
            return dictionary;
        }

        public static MetadataKVP[] DictionaryToArray(Dictionary<string, string> metaDictionary)
        {
            var array = new MetadataKVP[metaDictionary.Count];
            int index = 0;

            foreach(var kvp in metaDictionary)
            {
                MetadataKVP newKVP = new MetadataKVP()
                {
                    key = kvp.Key,
                    value = kvp.Value
                };

                array[index++] = newKVP;
            }

            return array;
        }
    }
}
