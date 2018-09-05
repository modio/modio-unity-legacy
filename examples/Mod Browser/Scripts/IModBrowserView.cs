using System.Collections.Generic;
using ModIO;

public interface IModBrowserView
{
    UnityEngine.GameObject gameObject { get; }

    IEnumerable<ModProfile> profileCollection { get; set; }

    void InitializeLayout();
    void Refresh();
}
