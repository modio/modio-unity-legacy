using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    [System.Serializable]
    public struct EditableImageLocator : IImageLocator
    {
        // ---------[ INNER CLASSES ]---------
        [SerializeField] private string _fileName;
        [SerializeField] private string _source;

        // ---------[ IIMAGELOCATOR INTERFACE ]---------
        public string fileName
        {
            get { return this._fileName; }
            set { this._fileName = value;}
        }
        public string source
        {
            get { return this._source; }
            set { this._source = value;}
        }
    }
}
