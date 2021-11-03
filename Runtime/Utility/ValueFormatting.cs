using Int64 = System.Int64;

namespace ModIO
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
            TimeStampAsDateTime,
            Percentage,
            SecondsAsTime,
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

            switch(method)
            {
                case Method.ByteCount:
                {
                    Int64 bytes = 0;

                    if(value != null)
                    {
                        bytes = (Int64)value;
                    }

                    displayString = ValueFormatting.ByteCount(bytes, toStringParameter);
                }
                break;

                case Method.AbbreviatedNumber:
                {
                    int number = 0;

                    if(value != null)
                    {
                        number = (int)value;
                    }

                    displayString = ValueFormatting.AbbreviateInteger(number, toStringParameter);
                }
                break;

                case Method.TimeStampAsDateTime:
                {
                    if(value == null)
                    {
                        displayString = "--";
                    }
                    else
                    {
                        displayString =
                            ServerTimeStamp.ToLocalDateTime((int)value).ToString(toStringParameter);
                    }
                }
                break;

                case Method.Percentage:
                {
                    if(value == null)
                    {
                        displayString = "--%";
                    }
                    else
                    {
                        displayString = ((float)value * 100.0f).ToString(toStringParameter) + "%";
                    }
                }
                break;

                case Method.SecondsAsTime:
                {
                    int seconds = 0;

                    if(value != null)
                    {
                        seconds = (int)value;
                    }

                    displayString = ValueFormatting.SecondsAsTime(seconds);
                }
                break;

                default:
                {
                    displayString = null;

                    if(value != null && !string.IsNullOrEmpty(toStringParameter))
                    {
                        if(value is float)
                        {
                            displayString = ((float)value).ToString(toStringParameter);
                        }
                        else if(value is int)
                        {
                            displayString = ((int)value).ToString(toStringParameter);
                        }
                        else if(value is Int64)
                        {
                            displayString = ((Int64)value).ToString(toStringParameter);
                        }
                    }

                    if(displayString == null)
                    {
                        if(value != null)
                        {
                            displayString = value.ToString();
                        }
                        else
                        {
                            displayString = string.Empty;
                        }
                    }
                }
                break;
            }
            return displayString;
        }

        /// <summary>Abbreviates an integer to a value with a maximum of 3 digits before the
        /// decimal.</summary>
        public static string AbbreviateInteger(int value, string toStringParameter)
        {
            if(string.IsNullOrEmpty(toStringParameter))
            {
                // Default value for ToString() in most cases is (G)eneral
                toStringParameter = "G";
            }

            if(value < 1000) // 0 - 999
            {
                return value.ToString();
            }
            else if(value < 100000) // 1.0K - 99.9K
            {
                // remove tens
                float truncatedValue = (value / 100) / 10f;
                return (truncatedValue.ToString(toStringParameter) + "K");
            }
            else if(value < 10000000) // 100K - 999K
            {
                // remove hundreds
                int truncatedValue = (value / 1000);
                return (truncatedValue.ToString() + "K");
            }
            else if(value < 1000000000) // 1.0M - 99.9M
            {
                // remove tens of thousands
                float truncatedValue = (value / 100000) / 10f;
                return (truncatedValue.ToString(toStringParameter) + "M");
            }
            else // 100M+
            {
                // remove hundreds of thousands
                int truncatedValue = (value / 1000000);
                return (truncatedValue.ToString() + "M");
            }
        }

        /// <summary>Creates a string version of a byte count by rounding to the nearest
        /// unit.</summary>
        public static string ByteCount(Int64 value, string toStringParameter)
        {
            string[] sizeSuffixes = new string[] { "B", "KB", "MB", "GB" };
            int sizeIndex = 0;
            Int64 adjustedSize = value;
            Int64 lastSize = 0;
            while(adjustedSize > 0x0400 && (sizeIndex + 1) < sizeSuffixes.Length)
            {
                lastSize = adjustedSize;
                adjustedSize /= 0x0400;
                ++sizeIndex;
            }

            if(sizeIndex > 0 && adjustedSize < 100)
            {
                decimal displayValue = (decimal)lastSize / (decimal)0x0400;
                return displayValue.ToString(toStringParameter) + sizeSuffixes[sizeIndex];
            }
            else
            {
                return adjustedSize + sizeSuffixes[sizeIndex];
            }
        }

        /// <summary>Converts seconds to a time display in the format "HH:MM:SS".</summary>
        public static string SecondsAsTime(int seconds)
        {
            int minutes = 0;
            int hours = 0;

            if(seconds > 60)
            {
                minutes = (int)System.Math.Floor(seconds / 60.0f);
                seconds %= 60;
            }
            if(minutes > 60)
            {
                hours = (int)System.Math.Floor(minutes / 60.0f);
                minutes %= 60;
            }

            return (hours.ToString("00") + ":" + minutes.ToString("00") + ":"
                    + seconds.ToString("00"));
        }
    }
}
