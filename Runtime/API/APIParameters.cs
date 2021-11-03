using System;

using Debug = UnityEngine.Debug;

namespace ModIO.API
{
    public class BinaryUpload
    {
        public string fileName = string.Empty;
        public byte[] data = null;

        public static BinaryUpload Create(string fileName, byte[] data)
        {
            BinaryUpload retVal = new BinaryUpload();
            retVal.fileName = fileName;
            retVal.data = data;
            return retVal;
        }
    }

    public class BinaryDataParameter
    {
        public string key = "";
        public string fileName = null;
        public string mimeType = null;
        public byte[] contents = null;

        public static BinaryDataParameter Create(string key, string fileName, string mimeType,
                                                 byte[] contents)
        {
            Debug.Assert(!String.IsNullOrEmpty(key) && contents != null);

            BinaryDataParameter retVal = new BinaryDataParameter();
            retVal.key = key;
            retVal.fileName = fileName;
            retVal.mimeType = mimeType;
            retVal.contents = contents;
            return retVal;
        }
    }

    public class StringValueParameter
    {
        public string key = string.Empty;
        public string value = string.Empty;

        public static StringValueParameter Create(string k, object v)
        {
            Debug.Assert(!String.IsNullOrEmpty(k));

            StringValueParameter retVal = new StringValueParameter();
            retVal.key = k;

            if(v != null)
            {
                retVal.value = v.ToString();
            }

            return retVal;
        }
    }
}
