using System;
using System.Collections.Generic;
using Assets.Scripts.MonoBehaviours.SerializableObjects;
using UnityEngine;

namespace Assets.Scripts.MonoBehaviours.Controllers
{
    public class PartsController : MonoBehaviour
    {
        public static event Action<string, string> OnSetWords;

        [SerializeField]
        private List<Part> _partList;

        private Part _currentPart;
        private Part _previousPart;
        private string _currentTitle;
        private string _currentDescription;

        private bool _isDeactivated;


        private void OnEnable()
        {
            SubscribeOnEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeOnEvents()
        {
            Part.OnPartSelect += SelectPart;
        }
        
        private void UnsubscribeFromEvents()
        {
            Part.OnPartSelect -= SelectPart;
        }

        public void SetPartItem(List<PartItem> partItem)
        {
            for (int i = 0; i < _partList.Count; i++)
            {
                _partList[i].SetWords(partItem[i].Title, partItem[i].Description);
            }
        }

        private void SelectPart(Part part, string title, string description)
        {
            _currentPart = part;
            _currentTitle = title;
            _currentDescription = description;
            OnSetWords?.Invoke(_currentTitle, _currentDescription);
            OutlineActivation(_currentPart, true);

            if (_previousPart != null)
            {
                OutlineActivation(_previousPart, false);
            }
           
            _previousPart = _currentPart;
        }

        private void OutlineActivation(Part part, bool activate)
        {
            part.GetComponent<Outline>().enabled = activate;
        }
    }
}