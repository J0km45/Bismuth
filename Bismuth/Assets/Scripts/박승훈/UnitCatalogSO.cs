using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit Catalog", menuName = "Bismuth/Unit Catalog", order = 5)]
public class UnitCatalogSO : ScriptableObject
{
    [SerializeField ] private List<UnitIdSummonedPair> _unitCatalog = new(UnitDataController.AllUnitCount);
    public List<UnitIdSummonedPair> UnitCatalog { get => _unitCatalog; set => _unitCatalog = value; }

    [Tooltip("최초 게임 실행 시 도감 초기화")]
    public bool FirstInit = true;
}

[Serializable]
public class UnitIdSummonedPair
{
    public UnitIdSummonedPair(int id, bool summoned, string name)
    {
        UnitId = id;
        Summoned = summoned;
        Name = name;
    }

    [SerializeField] private string name;
    public string Name 
    { get => name; set => name = value; }
    
    [SerializeField] private int unitId;
    public int UnitId 
    {get => unitId; set => unitId = value; }
    
    [SerializeField] private bool summoned;
    public bool Summoned 
    {get => summoned; set => summoned = value; }
}