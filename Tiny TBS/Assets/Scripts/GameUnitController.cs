using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class GameUnitController : MonoBehaviour
{
    private Unit _unit;
    private Transform _transform;

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
        _transform.position = FieldUtils.GetWorldPos(_unit.Coord);
    }
}
