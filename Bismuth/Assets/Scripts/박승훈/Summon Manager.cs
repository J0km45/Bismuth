using UnityEngine;

public class SummonManager : MonoBehaviour
{
    [SerializeField] private SummonUnit summonUnit;
    [SerializeField] private bool BoardLog = true;

    private void Awake()
    {
        if (summonUnit == null)
            summonUnit = GetComponent<SummonUnit>();
    }

    private void Start()
    {
        DebugTool.DebugSelect(DebugType.Board, BoardLog);
    }

    public void SummonRandomUnit()
    {
        if (summonUnit == null)
        {
            DebugTool.Error("SummonUnit 참조가 없습니다.", DebugType.Summon, this);
            return;
        }

        bool success = summonUnit.TrySummonAndPlace();

        if (!success)
        {
            DebugTool.Warnning("소환에 실패했습니다.", DebugType.Summon, this);
        }
    }

    public void DespawnUnit(TowerUnit tower)
    {
        if (summonUnit == null)
        {
            DebugTool.Error("SummonUnit 참조가 없습니다.", DebugType.Summon, this);
            //return false;
        }

        bool success = summonUnit.TryDespawnTower(tower);

        if (!success)
        {
            DebugTool.Warnning("디스폰에 실패했습니다.", DebugType.Summon, this);
        }

        //return success;
    /// <summary>
    /// 유닛 합성 메서드
    /// 합성 결과 유닛 매개변수로 입력
    /// Combine Manager 에서 메서드 호출
    /// </summary>
    /// <param name="combineData"></param>
    public void SummonCombineUnit(CombineData combineData)
    {
        if (summonUnit == null)
            DebugTool.Log("[합성] 존재 하지 않는 유닛입니다.", DebugType.Unit, this);
        
        UnitData resultdata = new UnitData();
        
        foreach (UnitData data in summonUnit.Units[combineData.Tier - 1].Units)
        {
            if (combineData.ResultUnit == data.Id)
                resultdata = data;
            else
                DebugTool.Log("존재 하지 않는 유닛입니다.", DebugType.Unit, this);
        }
        
        bool success = summonUnit.TrySummonAndPlace(resultdata);

        if (!success)
        {
            DebugTool.Warnning("소환에 실패했습니다.", DebugType.Summon, this);
        }
    }
}