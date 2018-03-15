using System.Collections.Generic;
using ModIO;

namespace ModIO.API
{
    public class PostParameters
    {
        // ---------[ STRING VALUE FIELDS ]---------
        public List<StringValueField> valueFields;

        protected void SetStringParameter(string key, string value)
        {
            foreach(StringValueField valueField in valueFields)
            {
                if(valueField.key == key)
                {
                    valueField.value = value;
                    return;
                }
            }

            valueFields.Add(StringValueField.Create(key, value));
        }

        protected void SetStringArrayParameter(string key, string[] valueArray)
        {
            int i = 0;
            while(i < valueFields.Count)
            {
                if(valueFields[i].key.Equals(key))
                {
                    valueFields.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }

            foreach(string value in valueArray)
            {
                valueFields.Add(StringValueField.Create(key, value));
            }
        }

        // ---------[ BINARY DATA FIELDS ]---------
        public List<BinaryDataField> dataFields;

        protected void SetBinaryDataParameter(string key, string fileName, byte[] data)
        {
            foreach(BinaryDataField dataField in dataFields)
            {
                if(dataField.key == key)
                {
                    dataField.fileName = fileName;
                    dataField.contents = data;
                    return;
                }
            }

            dataFields.Add(BinaryDataField.Create(key, fileName, null, data));
        }
    }
}
