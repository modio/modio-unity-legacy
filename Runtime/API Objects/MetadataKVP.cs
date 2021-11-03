using System.Collections.Generic;

using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Represents a key-value pairing that can be added to a mod as metadata.</summary>
    [System.Serializable]
    public class MetadataKVP
    {
        // ---------[ Fields ]---------
        /// <summary>The key of the key-value pair.</summary>
        [JsonProperty("metakey")]
        public string key;

        /// <summary>The value of the key-value pair.</summary>
        [JsonProperty("metavalue")]
        public string value;

        // ---------[ Utility ]---------
        /// <summary>Converts an array of MetadataKVP to a Dictionary.</summary>
        public static Dictionary<string, string> ArrayToDictionary(MetadataKVP[] kvpArray)
        {
            Debug.Assert(kvpArray != null);

            var dictionary = new Dictionary<string, string>(kvpArray.Length);
            foreach(MetadataKVP kvp in kvpArray)
            {
                if(string.IsNullOrEmpty(kvp.key))
                {
                    continue;
                }

                dictionary.Add(kvp.key, kvp.value);
            }
            return dictionary;
        }

        /// <summary>Converts a dictionary to a MetadataKVP array.</summary>
        public static MetadataKVP[] DictionaryToArray(Dictionary<string, string> dictionary)
        {
            Debug.Assert(dictionary != null);

            var array = new MetadataKVP[dictionary.Count];
            int index = 0;

            foreach(var kvp in dictionary)
            {
                MetadataKVP newKVP = new MetadataKVP() { key = kvp.Key, value = kvp.Value };

                array[index++] = newKVP;
            }

            return array;
        }

        /// <summary>Converts an array of MetadataKVP to a Dictionary.</summary>
        public static Dictionary<string, List<string>> ArrayToDictionary_DuplicateKeys(
            MetadataKVP[] kvpArray)
        {
            Debug.Assert(kvpArray != null);

            List<string> stringList = null;
            var dictionary = new Dictionary<string, List<string>>(kvpArray.Length);

            foreach(MetadataKVP kvp in kvpArray)
            {
                if(string.IsNullOrEmpty(kvp.key))
                {
                    continue;
                }

                if(!dictionary.TryGetValue(kvp.key, out stringList))
                {
                    stringList = new List<string>();
                    dictionary[kvp.key] = stringList;
                }

                stringList.Add(kvp.value);
            }

            return dictionary;
        }

        /// <summary>Converts a dictionary to a MetadataKVP array.</summary>
        public static IList<MetadataKVP> DictionaryToArray(
            Dictionary<string, List<string>> dictionary)
        {
            Debug.Assert(dictionary != null);

            var list = new List<MetadataKVP>();

            foreach(var kvp in dictionary)
            {
                if(kvp.Value == null)
                {
                    MetadataKVP newKVP = new MetadataKVP() {
                        key = kvp.Key,
                        value = null,
                    };

                    list.Add(newKVP);
                }
                else
                {
                    foreach(var stringValue in kvp.Value)
                    {
                        MetadataKVP newKVP = new MetadataKVP() {
                            key = kvp.Key,
                            value = stringValue,
                        };

                        list.Add(newKVP);
                    }
                }
            }

            return list;
        }
    }
}
