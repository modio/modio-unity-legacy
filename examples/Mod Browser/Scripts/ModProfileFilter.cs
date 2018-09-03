using System.Collections.Generic;

[System.Serializable]
public class ModProfileFilter
{
    public string title;
    public IEnumerable<SimpleModTag> tags;
}
