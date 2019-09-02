namespace ModIO.UI
{
    /// <summary>Formats a field value for UI display/</summary>
    public static class ValueFormatter
    {
        // ---------[ NESTED DATA-TYPE ]---------
        /// <summary>Enum that defines the formatting method.</summary>
        public enum Method
        {
            None,
            ByteCount,
            TimeStampAsDate,
            AbbreviatedNumber,
            Percentage,
        }

        /// <summary>Formats a value as a display string.</summary>
        public static string FormatValue(object value, Method method)
        {
            string displayString = string.Empty;

            if(value != null)
            {
                switch(method)
                {
                    case Method.ByteCount:
                    {
                        displayString = UIUtilities.ByteCountToDisplayString((System.Int64)value);
                    }
                    break;

                    case Method.TimeStampAsDate:
                    {
                        displayString = ServerTimeStamp.ToLocalDateTime((int)value).ToString();
                    }
                    break;

                    case Method.AbbreviatedNumber:
                    {
                        displayString = UIUtilities.ValueToDisplayString((int)value);
                    }
                    break;

                    case Method.Percentage:
                    {
                        displayString = ((float)value * 100.0f).ToString("0.0") + "%";
                    }
                    break;

                    default:
                    {
                        displayString = value.ToString();
                    }
                    break;
                }
            }

            return displayString;
        }
    }
}
