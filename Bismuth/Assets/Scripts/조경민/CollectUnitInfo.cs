using UnityEngine;

public class CollectUnitInfo : MonoBehaviour
{
    public GameObject selectedUnit;
    public void CollectInfo(GameObject unit)
    {
        selectedUnit = unit;
        UnitStat unitStat = unit.GetComponent<UnitStat>();
        DebugTool.Log($"Unit Id: {unitStat.Id}, Unit AttackPower: {unitStat.CurrentAttackPower}, Unit AttackSpeed: {unitStat.AttackSpeed} ",DebugType.UI,this);


    }
}
