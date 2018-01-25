using System;

namespace ModIO.API
{
    [Serializable]
    public struct LogoObject : IEquatable<LogoObject>
    {
        // - Fields -
        public string filename; // Logo filename including extension.
        public string original; // URL to the full-sized logo.
        public string thumb_320x180; // URL to the small logo thumbnail.
        public string thumb_640x360; // URL to the medium logo thumbnail.
        public string thumb_1280x720; // URL to the large logo thumbnail.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is LogoObject
                    && this.Equals((LogoObject)obj));
        }

        public bool Equals(LogoObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original)
                   && this.thumb_320x180.Equals(other.thumb_320x180)
                   && this.thumb_640x360.Equals(other.thumb_640x360)
                   && this.thumb_1280x720.Equals(other.thumb_1280x720));
        }
    }
}