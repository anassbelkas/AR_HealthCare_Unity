using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.MonoBehaviours.Controllers
{
    public class Part : MonoBehaviour
    {
        public static event Action<Part, string, string> OnPartSelect;
    
        public string _title;
        public string _description;

        private void OnMouseDown()
        {
            OnPartSelect?.Invoke(this, _title, _description);
        }

        public void SetWords(string title, string description)
        {
            _title = title;
            _description = description;
        }
    }

    
}
