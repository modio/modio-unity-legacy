namespace ModIO.UI
{
    /// <summary>Formatting a field value for UI display.</summary>
    [System.Serializable]
    public struct ValueFormatting
    {
        // ---------[ NESTED DATA-TYPE ]---------
        /// <summary>Enum that defines the formatting method.</summary>
        public enum Method
        {
            None,
            ByteCount,
            AbbreviatedNumber,
            DateTime,
            Percentage,
        }

        // ---------[ FIELDS ]---------
        /// <summary>Method to use when formatting.</summary>
        public Method method;

        /// <summary>Value to pass to any "ToString" function calls.</summary>
        public string toStringParameter;

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Formats a value as a display string.</summary>
        public static string FormatValue(object value, Method method, string toStringParameter)
        {
            string displayString = string.Empty;
            if(string.IsNullOrEmpty(toStringParameter))
            {
                // Default value for ToString() in most cases is (G)eneral
                toStringParameter = "G";
            }

            if(value != null)
            {
                switch(method)
                {
                    case Method.ByteCount:
                    {
                        displayString = UIUtilities.ByteCountToDisplayString((System.Int64)value);
                    }
                    break;

                    case Method.AbbreviatedNumber:
                    {
                        displayString = UIUtilities.ValueToDisplayString((int)value);
                    }
                    break;

                    case Method.DateTime:
                    {
                        displayString = ServerTimeStamp.ToLocalDateTime((int)value).ToString(toStringParameter);
                    }
                    break;
                    case Method.Percentage:
                    {
                        displayString = ((float)value * 100.0f).ToString(toStringParameter) + "%";
                    }
                    break;

                    default:
                    {
                        displayString = null;

                        if(!string.IsNullOrEmpty(toStringParameter))
                        {
                            if(value is float)
                            {
                                displayString = ((float)value).ToString(toStringParameter);
                            }
                            else if(value is int)
                            {
                                displayString = ((int)value).ToString(toStringParameter);
                            }
                            else if(value is System.Int64)
                            {
                                displayString = ((System.Int64)value).ToString(toStringParameter);
                            }
                        }

                        if(displayString == null)
                        {
                            displayString = value.ToString();
                        }
                    }
                    break;
                }
            }

            return displayString;
        }
    }
}
