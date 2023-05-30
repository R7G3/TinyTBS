using TMPro;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public class TileInfoView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _tileInfo;

        public void SetText(string text)
        {
            _tileInfo.text = text;
        }
    }
}
