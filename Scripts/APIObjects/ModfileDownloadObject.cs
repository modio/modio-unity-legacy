using System;

namespace ModIO.API
{
    [Serializable]
    public struct ModfileDownloadObject : IEquatable<ModfileDownloadObject>
    {
        // - Fields -
        public string binary_url; // URL to download the file from the mod.io CDN.
        public int date_expires;    // Unix timestamp of when the binary_url will expire.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.binary_url.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is ModfileDownloadObject
                    && this.Equals((ModfileDownloadObject)obj));
        }

        public bool Equals(ModfileDownloadObject other)
        {
            return(this.binary_url.Equals(other.binary_url)
                   && this.date_expires.Equals(other.date_expires));
        }
    }
}