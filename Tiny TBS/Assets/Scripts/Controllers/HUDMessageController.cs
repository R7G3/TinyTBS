using TMPro;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public class HUDMessageController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        public void ShowMsg(string msg)
        {
            _label.text = msg;
        }

        public void HideMsg()
        {
            _label.text = "";
        }
        
    }
}