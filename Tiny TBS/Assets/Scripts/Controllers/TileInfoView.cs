using TMPro;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public class TileInfoView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _tileInfo;

        public void SetText(string text)
        {
            if (_tileInfo == null) // otherwise error because it always null(it's a bug)
            {
                return;
            }

            _tileInfo.text = text;
        }
    }
}
