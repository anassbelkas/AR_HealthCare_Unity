using Assets.Scripts.MonoBehaviours.SerializableObjects;
using Assets.Scripts.MonoBehaviours.UI;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.MonoBehaviours.Controllers
{
    public class HumanController : MonoBehaviour
    {
        [SerializeField] 
        private PartsController _skeletonPartsController;

        [SerializeField] 
        private PartsController _veinsPartsController;

        [SerializeField] 
        private PartsController _stomachPartsController;

        [SerializeField] 
        private PartsController _memberPartsController;

        [SerializeField] 
        private PartsController _skinPartsController;

        [SerializeField] 
        private PartsController _musclesPartsController;

        [SerializeField] 
        private PartsController _brainPartsController;

        [SerializeField] 
        private PartsController _lungsPartsController;

        [SerializeField] 
        private PartsController _eyesPartsController;

        [SerializeField] 
        private WordsPanelController _wordsPanelController;

        private Human _human = new Human();

        private void Awake()
        {
            LoadWords();
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
            HumanButtons.OnSkeleton += Skeleton;
            HumanButtons.OnVeins += Veins;
            HumanButtons.OnStomach += Stomach;
            HumanButtons.OnMember += Member;
            HumanButtons.OnSkin += Skin;
            HumanButtons.OnMuscles += Muscles;
            HumanButtons.OnBrain += Brain;
            HumanButtons.OnLungs += Lungs;
            HumanButtons.OnEyes += Eyes;
            PartsController.OnSetWords += SetWords;
        }

        

        private void UnsubscribeFromEvents()
        {
            HumanButtons.OnSkeleton -= Skeleton;
            HumanButtons.OnVeins -= Veins;
            HumanButtons.OnStomach -= Stomach;
            HumanButtons.OnMember -= Member;
            HumanButtons.OnSkin -= Skin;
            HumanButtons.OnMuscles -= Muscles;
            HumanButtons.OnBrain -= Brain;
            HumanButtons.OnLungs -= Lungs;
            HumanButtons.OnEyes -= Eyes;
            PartsController.OnSetWords -= SetWords;
        }

        private void SetWords(string title, string description)
        {
            _wordsPanelController.gameObject.SetActive(true);
            _wordsPanelController.SetWords(title, description);
        }

        private void LoadWords()
        {
            var txtJson = (TextAsset) Resources.Load("Words", typeof(TextAsset));
            var contentJson = txtJson.text;
            _human = JsonUtility.FromJson<Human>(contentJson);
            InitializeWords();
        }

        private void InitializeWords()
        {
            for (int i = 0; i < _human.Parts.Count; i++)
            {
                if (_human.Parts[i].PartName == _skeletonPartsController.name)
                {
                    _skeletonPartsController.SetPartItem(_human.Parts[i].PartItem);
                }
                else if (_human.Parts[i].PartName == _veinsPartsController.name)
                {
                    _veinsPartsController.SetPartItem(_human.Parts[i].PartItem);
                }
                else if (_human.Parts[i].PartName == _stomachPartsController.name)
                {
                    _stomachPartsController.SetPartItem(_human.Parts[i].PartItem);
                }
                else if (_human.Parts[i].PartName == _memberPartsController.name)
                {
                    _memberPartsController.SetPartItem(_human.Parts[i].PartItem);
                }
                else if (_human.Parts[i].PartName == _skinPartsController.name)
                {
                    _skinPartsController.SetPartItem(_human.Parts[i].PartItem);
                }
                else if (_human.Parts[i].PartName == _musclesPartsController.name)
                {
                    _musclesPartsController.SetPartItem(_human.Parts[i].PartItem);
                }
                else if (_human.Parts[i].PartName == _brainPartsController.name)
                {
                    _brainPartsController.SetPartItem(_human.Parts[i].PartItem);
                }
                else if (_human.Parts[i].PartName == _lungsPartsController.name)
                {
                    _lungsPartsController.SetPartItem(_human.Parts[i].PartItem);
                }
                else if (_human.Parts[i].PartName == _eyesPartsController.name)
                {
                    _eyesPartsController.SetPartItem(_human.Parts[i].PartItem);
                }

            }
        }

        private void Skeleton()
        {
            ActivateGameObjectPart(_skeletonPartsController.gameObject);
        }

        private void Veins()
        {
            ActivateGameObjectPart(_veinsPartsController.gameObject);
        }

        private void Stomach()
        {
            ActivateGameObjectPart(_stomachPartsController.gameObject);
        }

        private void Member()
        {
            ActivateGameObjectPart(_memberPartsController.gameObject);
        }

        private void Skin()
        {
            ActivateGameObjectPart(_skinPartsController.gameObject);
        }

        private void Muscles()
        {
            ActivateGameObjectPart(_musclesPartsController.gameObject);
        }

        private void Brain()
        {
            ActivateGameObjectPart(_brainPartsController.gameObject);
        }

        private void Lungs()
        {
            ActivateGameObjectPart(_lungsPartsController.gameObject);
        }

        private void Eyes()
        {
            ActivateGameObjectPart(_eyesPartsController.gameObject);
        }

        private void ActivateGameObjectPart(GameObject currentPart)
        {
            currentPart.SetActive(!currentPart.activeInHierarchy);
        }
    }
}