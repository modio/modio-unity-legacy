namespace ModIO
{
    /// <summary>Attribute for determining the version of and default value for a given field.</summary>
    public class VersionedDataAttribute : System.Attribute
    {
        public int version;
        public System.Object defaultValue;

        public VersionedDataAttribute(int version, System.Object defaultValue)
        {
            this.version = version;
            this.defaultValue = defaultValue;
        }
    }
}
