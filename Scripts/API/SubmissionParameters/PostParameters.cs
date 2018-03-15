using System.Collections.Generic;

namespace ModIO.API
{
    public class PostParameters
    {
        // ---------[ STRING VALUE FIELDS ]---------
        public List<StringValueParameter> stringValues;

        protected void SetStringValue(string key, string value)
        {
            foreach(StringValueParameter valueField in stringValues)
            {
                if(valueField.key == key)
                {
                    valueField.value = value;
                    return;
                }
            }

            stringValues.Add(StringValueParameter.Create(key, value));
        }

        protected void SetStringArrayValue(string key, string[] valueArray)
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

            foreach(string value in valueArray)
            {
                stringValues.Add(StringValueParameter.Create(key, value));
            }
        }

        // ---------[ BINARY DATA FIELDS ]---------
        public List<BinaryDataParameter> binaryData;

        protected void SetBinaryData(string key, string fileName, byte[] data)
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
