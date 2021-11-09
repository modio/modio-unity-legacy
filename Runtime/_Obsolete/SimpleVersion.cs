namespace ModIO
{
    [System.Obsolete("Use ModIOVersion instead")]
    [System.Serializable]
    public struct SimpleVersion : System.IComparable<SimpleVersion>
    {
        public int major;
        public int minor;

        public SimpleVersion(int majorVersion = 0, int minorVersion = 0)
        {
            this.major = majorVersion;
            this.minor = minorVersion;
        }

        // - IComparable -
        public int CompareTo(SimpleVersion other)
        {
            if(this.major != other.major)
            {
                return this.major.CompareTo(other.major);
            }
            else
            {
                return this.minor.CompareTo(other.minor);
            }
        }

        // clang-format off
        public static bool operator > (SimpleVersion operand1, SimpleVersion operand2)
        {
            return operand1.CompareTo(operand2) == 1;
        }

        public static bool operator < (SimpleVersion operand1, SimpleVersion operand2)
        {
            return operand1.CompareTo(operand2) == -1;
        }

        public static bool operator >= (SimpleVersion operand1, SimpleVersion operand2)
        {
            return operand1.CompareTo(operand2) >= 0;
        }

        public static bool operator <= (SimpleVersion operand1, SimpleVersion operand2)
        {
            return operand1.CompareTo(operand2) <= 0;
        }
        // clang-format on
    }
}
