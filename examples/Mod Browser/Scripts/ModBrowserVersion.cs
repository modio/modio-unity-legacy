namespace ModIO.UI
{
    [System.Serializable]
    public struct ModBrowserVersion : System.IComparable<ModBrowserVersion>
    {
        public int major;
        public int minor;

        public ModBrowserVersion(int majorVersion = 0, int minorVersion = 0)
        {
            this.major = majorVersion;
            this.minor = minorVersion;
        }

        // - IComparable -
        public int CompareTo(ModBrowserVersion other)
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

        public static bool operator >  (ModBrowserVersion operand1, ModBrowserVersion operand2)
        {
           return operand1.CompareTo(operand2) == 1;
        }

        public static bool operator <  (ModBrowserVersion operand1, ModBrowserVersion operand2)
        {
           return operand1.CompareTo(operand2) == -1;
        }

        public static bool operator >=  (ModBrowserVersion operand1, ModBrowserVersion operand2)
        {
           return operand1.CompareTo(operand2) >= 0;
        }

        public static bool operator <=  (ModBrowserVersion operand1, ModBrowserVersion operand2)
        {
           return operand1.CompareTo(operand2) <= 0;
        }
    }
}
