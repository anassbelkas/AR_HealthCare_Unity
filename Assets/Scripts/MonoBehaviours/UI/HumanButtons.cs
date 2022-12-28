using System;
using Assets.Scripts.MonoBehaviours.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.MonoBehaviours.UI
{
    public class HumanButtons : MonoBehaviour
    {
        public static event Action OnSkeleton;
        public static event Action OnVeins;
        public static event Action OnStomach;
        public static event Action OnMember;
        public static event Action OnSkin;
        public static event Action OnMuscles;
        public static event Action OnBrain;
        public static event Action OnLungs;
        public static event Action OnEyes;

        [SerializeField] 
        private Button _skeletonBtn;

        [SerializeField] 
        private Button _veinsBtn;

        [SerializeField] 
        private Button _stomachBtn;

        [SerializeField] 
        private Button _memberBtn;

        [SerializeField] 
        private Button _skinBtn;

        [SerializeField] 
        private Button _musclesBtn;

        [SerializeField] 
        private Button _brainBtn;

        [SerializeField] 
        private Button _lungsBtn;

        [SerializeField] 
        private Button _eyesBtn;

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
            _skeletonBtn.onClick.AddListener(Skeleton);
            _veinsBtn.onClick.AddListener(Veins);
            _stomachBtn.onClick.AddListener(Stomach);
            _memberBtn.onClick.AddListener(Member);
            _skinBtn.onClick.AddListener(Skin);
            _musclesBtn.onClick.AddListener(Muscles);
            _brainBtn.onClick.AddListener(Brain);
            _lungsBtn.onClick.AddListener(Lungs);
            _eyesBtn.onClick.AddListener(Eyes);
        }

        private void UnsubscribeFromEvents()
        {
            _skeletonBtn.onClick.RemoveListener(Skeleton);
            _veinsBtn.onClick.RemoveListener(Veins);
            _stomachBtn.onClick.RemoveListener(Stomach);
            _memberBtn.onClick.RemoveListener(Member);
            _skinBtn.onClick.RemoveListener(Skin);
            _musclesBtn.onClick.RemoveListener(Muscles);
            _brainBtn.onClick.RemoveListener(Brain);
            _lungsBtn.onClick.RemoveListener(Lungs);
            _eyesBtn.onClick.RemoveListener(Eyes);
        }

        private void Skeleton()
        { 
            OnSkeleton?.Invoke();
            Select(_skeletonBtn);
        }

        private void Veins()
        {
            OnVeins?.Invoke();
            Select(_veinsBtn);
        }

        private void Stomach()
        {
            OnStomach?.Invoke();
            Select(_stomachBtn);
        }

        private void Member()
        {
            OnMember?.Invoke();
            Select(_memberBtn);
        }

        private void Skin()
        {
            OnSkin?.Invoke();
            Select(_skinBtn);
        }

        private void Muscles()
        {
            OnMuscles?.Invoke();
            Select(_musclesBtn);
        }

        private void Brain()
        {
            OnBrain?.Invoke();
            Select(_brainBtn);
        }

        private void Lungs()
        {
            OnLungs?.Invoke();
            Select(_lungsBtn);
        }

        private void Eyes()
        {
            OnEyes?.Invoke();
            Select(_eyesBtn);
        }

        private void Select(Button clickedButton)
        {
            clickedButton.GetComponent<ButtonControl>().Select();
        }
    }
}