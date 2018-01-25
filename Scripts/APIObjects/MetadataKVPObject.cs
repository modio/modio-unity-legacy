using System;

namespace ModIO.API
{
    [Serializable]
    public struct MetadataKVPObject : IEquatable<MetadataKVPObject>
    {
        // - Fields -
        public string metakey; // The key of the key-value pair.
        public string metavalue; // The value of the key-value pair.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.metakey.GetHashCode() ^ this.metavalue.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is MetadataKVPObject
                    && this.Equals((MetadataKVPObject)obj));
        }

        public bool Equals(MetadataKVPObject other)
        {
            return(this.metakey.Equals(other.metakey)
                   && this.metavalue.Equals(other.metavalue));
        }
    }
}