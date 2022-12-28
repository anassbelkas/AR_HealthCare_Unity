using Assets.Scripts.MonoBehaviours.AR;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.MonoBehaviours.UI
{
    public class ButtonControl : MonoBehaviour
    {
        [SerializeField] 
        private Sprite _onSprite;

        [SerializeField] 
        private Sprite _offSprite;

        [SerializeField]
        private bool _isOn;
        
        private Image _buttonImage;
        private bool _enable;

        private void Awake()
        {
            _buttonImage = GetComponent<Image>();
        }
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
            PlaceOnPlane.OnModelPlaced += EnableButton;
        }
        
        private void UnsubscribeFromEvents()
        {
            PlaceOnPlane.OnModelPlaced -= EnableButton;
        }

        private void EnableButton()
        {
            _enable = true;
        }

        public void Select()
        {
            if (_enable)
            {
                SwitchButtonImage();
                _isOn = !_isOn;
            }
        }

        private void SwitchButtonImage()
        {
            _buttonImage.sprite = _isOn ? _offSprite : _onSprite;
        }
    }
}