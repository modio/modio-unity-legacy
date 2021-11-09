namespace ModIO
{
    /// <summary>Attribute for determining the version of and default value for a given
    /// field.</summary>
    public class VersionedDataAttribute : System.Attribute
    {
        // ---------[ Fields ]---------
        /// <summary>Version the field was created.</summary>
        public int version;

        /// <summary>Default value for the field.</summary>
        public System.Object defaultValue;

        // ---------[ Initialization ]---------
        public VersionedDataAttribute(int version, System.Object defaultValue)
        {
            this.version = version;
            this.defaultValue = defaultValue;
        }

        // ---------[ Utility ]---------
        /// <summary>Creates an updated version of the data structure passed.</summary>
        public static T UpdateStructFields<T>(int dataVersion, T dataValues)
            where T : struct
        {
            // set up data
            T updatedValues = dataValues;
            System.Object boxedData = updatedValues;

            // iterate over the typed fields
            var fieldList = typeof(T).GetFields();
            foreach(var field in fieldList)
            {
                // check for the VersionedDataAttribute attribute
                var attributeList =
                    field.GetCustomAttributes(typeof(VersionedDataAttribute), false);
                if(attributeList != null && attributeList.Length == 1)
                {
                    // set the default value if attribute is newer than the dataVersion
                    VersionedDataAttribute dataAttribute = (VersionedDataAttribute)attributeList[0];
                    if(dataAttribute.version > dataVersion)
                    {
                        field.SetValue(boxedData, dataAttribute.defaultValue);
                    }
                }
            }

            //  unbox the updatedValues
            updatedValues = (T)boxedData;

            return updatedValues;
        }
    }
}
