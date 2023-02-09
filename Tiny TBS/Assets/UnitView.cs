using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    private MeshRenderer _renderer;

    public Color redFractionColor;
    public Color redUnableFractionColor;
    public Color blueFractionColor;
    public Color blueUnableFractionColor;

    public GameObject unitPrefab;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void Awake()
    {
        _renderer = GetComponentInChildren<MeshRenderer>();
    }
}
