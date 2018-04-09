namespace ModIO
{
    [System.Serializable]
    public class EditableImageLocatorField : EditableField<EditableImageLocatorField.Data>, IImageLocator
    {
        // ---------[ INNER CLASSES ]---------
        [System.Serializable]
        public struct Data
        {
            public string fileName;
            public string source;
        }

        // ---------[ IIMAGELOCATOR INTERFACE ]---------
        public string fileName
        {
            get { return this.value.fileName; }
            set { this.value.fileName = value;}
        }
        public string source
        {
            get { return this.value.source; }
            set { this.value.source = value;}
        }
    }
}
