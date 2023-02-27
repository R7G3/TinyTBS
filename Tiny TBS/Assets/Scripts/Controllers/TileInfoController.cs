using TMPro;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public class TileInfoController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _tileInfo;

        public void SetTileInfo(string text)
        {
            _tileInfo.text = text;
        }
    }
}
