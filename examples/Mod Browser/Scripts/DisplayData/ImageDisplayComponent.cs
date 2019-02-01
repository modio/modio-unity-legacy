using System;
using UnityEngine;

namespace ModIO.UI
{
    public abstract class ImageDisplayComponent : MonoBehaviour
    {
        public abstract event Action<ImageDisplayComponent> onClick;
        public abstract ImageDisplayData data { get; set; }
        public abstract bool useOriginal { get; set; }

        public abstract void Initialize();
        public abstract void DisplayLoading();
    }
}
