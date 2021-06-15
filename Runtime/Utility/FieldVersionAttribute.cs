namespace ModIO
{
    /// <summary>Attribute for determining the version of and default value for a given field.</summary>
    public class FieldVersionAttribute : System.Attribute
    {
        public int version;

        public FieldVersionAttribute(int version)
        {
            this.version = version;
        }
    }
}
