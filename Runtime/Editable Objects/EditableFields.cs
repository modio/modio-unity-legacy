namespace ModIO
{
    public class EditableField<T>
    {
        public T value = default(T);
        public bool isDirty = false;
    }
    public class EditableArrayField<T>
    {
        public T[] value = new T[0];
        public bool isDirty = false;
    }
    // ---------[ UNITY SERIALIZABLE FIELD CLASSES ]---------
    [System.Serializable]
    public class EditableIntField : EditableField<int>
    {
    }
    [System.Serializable]
    public class EditableBoolField : EditableField<bool>
    {
    }
    [System.Serializable]
    public class EditableStringField : EditableField<string>
    {
    }

    // ---------[ UNITY SERIALIZABLE ARRAY CLASSES ]---------
    [System.Serializable]
    public class EditableStringArrayField : EditableArrayField<string>
    {
    }
}
