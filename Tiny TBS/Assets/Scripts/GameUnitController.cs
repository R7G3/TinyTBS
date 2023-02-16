using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts;
using UnityEngine;

public class GameUnitController : MonoBehaviour
{
    private Unit _unit;
    private Transform _transform;
    [SerializeField] private UnitView _unitView;
    private Vector2Int _lastCoord;
    private Task _travelTask;

    public void Init(Unit unit)
    {
        _unit = unit;
    }

    private void Awake()
    {
        _transform = transform;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (_travelTask != null)
        {
            if (_travelTask.IsCompleted)
            {
                _travelTask = null;
            }
            else
            {
                return;
            }
        }
        
        if (_unit.Coord == _lastCoord) return;
        
        _travelTask = _unitView.Travel(Enumerable.Repeat(_unit.Coord, 1));
        _lastCoord = _unit.Coord;
    }
}
