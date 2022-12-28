using System;
using System.Collections.Generic;

namespace Assets.Scripts.MonoBehaviours.SerializableObjects
{
    [Serializable]
    public class Human
    {
        public List<Parts> Parts;
    }

    [Serializable]
    public class Parts
    {
        public string PartName;
        public List<PartItem> PartItem;
    }

    [Serializable]
    public class PartItem
    {
        public string Title;
        public string Description;
    }
}
