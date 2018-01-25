using System;

namespace ModIO.API
{
    [Serializable]
    public struct FilehashObject : IEquatable<FilehashObject>
    {
        // - Fields -
        public string md5; // MD5 hash of the file.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.md5.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is FilehashObject
                    && this.Equals((FilehashObject)obj));
        }

        public bool Equals(FilehashObject other)
        {
            return(this.md5.Equals(other.md5));
        }
    }
}