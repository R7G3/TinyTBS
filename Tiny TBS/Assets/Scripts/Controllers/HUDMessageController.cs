using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public class HUDMessageController : MonoBehaviour, IService
    {
        [SerializeField] private TextMeshProUGUI _label;

        public void ShowMsg(string msg)
        {
            gameObject.SetActive(true);
            _label.text = msg;
        }

        public void HideMsg()
        {
            gameObject.SetActive(false);
        }
        
    }
}
