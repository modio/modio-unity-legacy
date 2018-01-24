using System;

namespace ModIO
{
    [Serializable]
    public class MetadataKVP : IEquatable<MetadataKVP>
    {
        // - Constructors - 
        public static MetadataKVP GenerateFromAPIObject(API.MetadataKVPObject apiObject)
        {
            MetadataKVP newMetadataKVP = new MetadataKVP();
            newMetadataKVP._data = apiObject;
            return newMetadataKVP;
        }

        public static MetadataKVP[] GenerateFromAPIObjectArray(API.MetadataKVPObject[] apiObjectArray)
        {
            MetadataKVP[] objectArray = new MetadataKVP[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = MetadataKVP.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.MetadataKVPObject _data;

        public string key   { get { return _data.metakey; } }
        public string value { get { return _data.metavalue; } }
        
        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as MetadataKVP);
        }

        public bool Equals(MetadataKVP other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
