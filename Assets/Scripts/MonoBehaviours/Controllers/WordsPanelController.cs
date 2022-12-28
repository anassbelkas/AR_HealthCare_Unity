using TMPro;
using UnityEngine;

namespace Assets.Scripts.MonoBehaviours.Controllers
{
    public class WordsPanelController : MonoBehaviour
    {
        [SerializeField] 
        private TMP_Text _title;

        [SerializeField]
        private TMP_Text _description;

        public void SetWords(string title, string description)
        {
            _title.text = title;
            _description.text = description;
        }
    }
}