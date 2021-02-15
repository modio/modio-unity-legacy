using System.Collections.Generic;

namespace ModIO.API
{
    public class RequestParameters
    {
        // ---------[ STRING VALUE FIELDS ]---------
        public List<StringValueParameter> stringValues = new List<StringValueParameter>();

        public void SetStringValue<T>(string key, T value)
        {
            foreach(StringValueParameter valueField in stringValues)
            {
                if(valueField.key == key)
                {
                    valueField.value = value.ToString();
                    return;
                }
            }

            stringValues.Add(StringValueParameter.Create(key, value));
        }

        public void SetStringArrayValue<T>(string key, T[] valueArray)
        {
            int i = 0;
            while(i < stringValues.Count)
            {
                if(stringValues[i].key.Equals(key))
                {
                    stringValues.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }

            foreach(T value in valueArray)
            {
                stringValues.Add(StringValueParameter.Create(key, value.ToString()));
            }
        }

        // ---------[ BINARY DATA FIELDS ]---------
        public List<BinaryDataParameter> binaryData = new List<BinaryDataParameter>();

        public void SetBinaryData(string key, string fileName, byte[] data)
        {
            foreach(BinaryDataParameter dataField in binaryData)
            {
                if(dataField.key == key)
                {
                    dataField.fileName = fileName;
                    dataField.contents = data;
                    return;
                }
            }

            binaryData.Add(BinaryDataParameter.Create(key, fileName, null, data));
        }
    }
}
